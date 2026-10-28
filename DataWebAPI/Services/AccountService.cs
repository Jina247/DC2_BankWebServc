using DataWebAPI.Models;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace DataWebAPI.Services
{
    public class AccountService : IAccountService
    {
        // connection string comes from appsettings.json
        private readonly string _connStr;

        // timestamp format saved in the db (same as sqlite datetime('now'), so it sorts & compares properly)
        private const string TimeFmt = "yyyy-MM-dd HH:mm:ss";

        public AccountService(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection") ?? "Data Source=bank.db";
        }

        // helper method to open a connection to the database
        private SqliteConnection Open()
        {
            var conn = new SqliteConnection(_connStr);
            conn.Open();
            return conn;
        }

        // helper method to build a sql command with parameters (parameters = no sql injection)
        private static SqliteCommand Cmd(SqliteConnection conn, SqliteTransaction? tx, string sql, params (string Name, object? Value)[] ps)
        {
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            foreach (var (name, value) in ps)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return cmd;
        }

        // method to deposit money into an account
        public Task<TransactionResult> DepositAsync(DepositRequest r)
        {
            // transaction replaces the mutexlock. if anything throws before Commit, everything is rolled back
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            // check for account using account number (must exist & be active)
            var balance = GetBalance(conn, tx, r.AccountNumber, true);

            // add deposit amount to the balance, then save it
            balance += r.Amount;
            SetBalance(conn, tx, r.AccountNumber, balance);

            // save the transaction into the history
            // 1001 Deposit 200.00
            var result = Make(conn, tx, r.AccountNumber, "Deposit", r.Amount, balance, r.Description, null);

            tx.Commit();
            return Task.FromResult(result);
        }

        // method to withdraw money from an account
        public Task<TransactionResult> WithdrawAsync(WithdrawRequest r)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            // check for account using account number
            var balance = GetBalance(conn, tx, r.AccountNumber, true);

            // check for sufficient funds, it will throw an exception if there are insufficient funds
            Funds(balance, r.Amount);

            // if sufficient funds, minus withdrawal amount from balance
            balance -= r.Amount;
            SetBalance(conn, tx, r.AccountNumber, balance);

            // 1001 Withdrawal -50.00
            var result = Make(conn, tx, r.AccountNumber, "Withdrawal", -r.Amount, balance, r.Description, null);

            tx.Commit();
            return Task.FromResult(result);
        }

        // method to transfer money from one account to another
        public Task<TransactionResult> TransferAsync(TransferRequest r)
        {
            // throw exception if the receiver account is same as the sender account
            if (r.FromAccountNumber == r.ToAccountNumber)
                throw new BusinessRuleException("SAME_ACCOUNT", "Cannot transfer to the same account.");

            // one transaction for both sides: debit + credit both save, or both roll back
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            // check both accounts using account number
            var fromBal = GetBalance(conn, tx, r.FromAccountNumber, true);
            var toBal = GetBalance(conn, tx, r.ToAccountNumber, true);

            // check for sufficient funds in the sender account
            Funds(fromBal, r.Amount);

            // deduct the transfer amount from the sender account, then add it into the receiver account
            fromBal -= r.Amount;
            toBal += r.Amount;
            SetBalance(conn, tx, r.FromAccountNumber, fromBal);
            SetBalance(conn, tx, r.ToAccountNumber, toBal);

            // save the transaction for both accounts
            // 1001 TransferOut -100.00 & 1002 TransferIn +100.00
            var debit = Make(conn, tx, r.FromAccountNumber, "TransferOut", -r.Amount, fromBal, r.Description, r.ToAccountNumber);
            Make(conn, tx, r.ToAccountNumber, "TransferIn", r.Amount, toBal, r.Description, r.FromAccountNumber);

            tx.Commit();

            // return the transaction result for the sender account
            return Task.FromResult(debit);
        }

        // helper method to get the balance of an account (throws if the account does not exist)
        private static decimal GetBalance(SqliteConnection conn, SqliteTransaction tx, string acc, bool requireActive)
        {
            using var cmd = Cmd(conn, tx, "SELECT Balance, IsActive FROM Accounts WHERE AccountNumber = @a", ("@a", acc));
            using var rd = cmd.ExecuteReader();

            // throw exception if the account does not exist
            if (!rd.Read())
                throw new NotFoundException($"Account {acc} not found.");

            // throw exception if the account is deactivated
            if (requireActive && rd.GetInt32(1) == 0)
                throw new BusinessRuleException("ACCOUNT_INACTIVE", $"Account {acc} is deactivated.");

            return rd.GetDecimal(0);
        }

        // helper method to save the new balance of an account
        private static void SetBalance(SqliteConnection conn, SqliteTransaction tx, string acc, decimal balance)
        {
            using var cmd = Cmd(conn, tx, "UPDATE Accounts SET Balance = @b WHERE AccountNumber = @a",
                ("@b", decimal.Round(balance, 2)), ("@a", acc));
            cmd.ExecuteNonQuery();
        }

        // helper method to check if the account has sufficient funds for withdrawal or transfer
        private static void Funds(decimal balance, decimal amount)
        {
            // throw exception if the account does not have sufficient funds
            if (balance < amount)
                throw new BusinessRuleException("INSUFFICIENT_FUNDS",
                    $"Balance is {balance:F2}; requested {amount:F2}.");
        }

        // helper method to create a transaction result and save it into the transaction history
        private static TransactionResult Make(SqliteConnection conn, SqliteTransaction tx, string acc, string type,
            decimal amount, decimal balanceAfter, string? description, string? related)
        {
            var now = DateTime.UtcNow;

            // insert the transaction, then get its new ID
            using var cmd = Cmd(conn, tx,
                @"INSERT INTO Transactions (AccountNumber, Type, Amount, BalanceAfter, Description, RelatedAccountNumber, Timestamp)
                  VALUES (@a, @t, @amt, @bal, @d, @rel, @ts);
                  SELECT last_insert_rowid();",
                ("@a", acc), ("@t", type), ("@amt", amount), ("@bal", balanceAfter),
                ("@d", description), ("@rel", related), ("@ts", now.ToString(TimeFmt)));
            var id = Convert.ToInt64(cmd.ExecuteScalar());

            return new TransactionResult
            {
                TransactionId = id,
                AccountNumber = acc,
                Type = type,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Timestamp = now
            };
        }

        // function to create an account
        public Task<AccountDto> CreateAccountAsync(CreateAccountRequest r)
        {
            using var conn = Open();
            using var tx = conn.BeginTransaction();

            // generate account number = highest existing number + 1 (starts from 1001)
            long next;
            using (var cmd = Cmd(conn, tx, "SELECT COALESCE(MAX(CAST(AccountNumber AS INTEGER)), 1000) + 1 FROM Accounts"))
                next = Convert.ToInt64(cmd.ExecuteScalar());
            var num = next.ToString();

            try
            {
                // add account details to accounts table, balance starts as 0
                using var ins = Cmd(conn, tx,
                    "INSERT INTO Accounts (AccountNumber, UserId, AccountType, Balance, IsActive) VALUES (@a, @u, @t, 0, 1)",
                    ("@a", num), ("@u", r.UserId), ("@t", r.AccountType));
                ins.ExecuteNonQuery();
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 19)   // 19 = constraint failed (user ID does not exist)
            {
                throw new NotFoundException($"User {r.UserId} not found.");
            }

            tx.Commit();

            // return the account details with balance
            return Task.FromResult(new AccountDto
            {
                AccountNumber = num,
                UserId = r.UserId,
                AccountType = r.AccountType,
                Balance = 0m,
                IsActive = true
            });
        }

        // function to fetch account details
        public Task<AccountDto> GetAccountAsync(string n)
        {
            using var conn = Open();
            using var cmd = Cmd(conn, null,
                "SELECT AccountNumber, UserId, AccountType, Balance, IsActive FROM Accounts WHERE AccountNumber = @a", ("@a", n));
            using var rd = cmd.ExecuteReader();

            if (!rd.Read())
                throw new NotFoundException($"Account {n} not found.");

            return Task.FromResult(ReadAccount(rd));
        }

        // helper function to turn a database row into account details
        private static AccountDto ReadAccount(SqliteDataReader rd) => new()
        {
            AccountNumber = rd.GetString(0),
            UserId = rd.GetInt32(1),
            AccountType = rd.GetString(2),
            Balance = rd.GetDecimal(3),
            IsActive = rd.GetInt32(4) != 0
        };

        // function to update account details
        public Task UpdateAccountAsync(string n, UpdateAccountRequest r)
        {
            using var conn = Open();
            using var cmd = Cmd(conn, null, "UPDATE Accounts SET AccountType = @t WHERE AccountNumber = @a",
                ("@t", r.AccountType), ("@a", n));

            // 0 rows changed = account does not exist
            if (cmd.ExecuteNonQuery() == 0)
                throw new NotFoundException($"Account {n} not found.");

            return Task.CompletedTask;
        }

        // function to deactivate an account
        public Task DeactivateAccountAsync(string n)
        {
            using var conn = Open();
            using var cmd = Cmd(conn, null, "UPDATE Accounts SET IsActive = 0 WHERE AccountNumber = @a", ("@a", n));

            if (cmd.ExecuteNonQuery() == 0)
                throw new NotFoundException($"Account {n} not found.");

            return Task.CompletedTask;
        }

        // method to search specific transactions from the transaction table
        public Task<PagedResult<TransactionResult>> SearchTransactionsAsync(TransactionQuery q)
        {
            using var conn = Open();

            // build the WHERE part, only with the filters that were given
            var where = new List<string>();
            var ps = new List<(string Name, object? Value)>();

            if (!string.IsNullOrWhiteSpace(q.AccountNumber))
            {
                where.Add("AccountNumber = @acc");
                ps.Add(("@acc", q.AccountNumber));
            }
            if (q.From.HasValue)
            {
                where.Add("Timestamp >= @from");
                ps.Add(("@from", q.From.Value.ToString(TimeFmt)));
            }
            if (q.To.HasValue)
            {
                // if only a date was given (midnight), include that whole day
                var to = q.To.Value;
                if (to.TimeOfDay == TimeSpan.Zero)
                {
                    where.Add("Timestamp < @to");
                    to = to.AddDays(1);
                }
                else
                {
                    where.Add("Timestamp <= @to");
                }
                ps.Add(("@to", to.ToString(TimeFmt)));
            }
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                where.Add("(Type LIKE @s OR AccountNumber LIKE @s)");
                ps.Add(("@s", "%" + q.Search + "%"));
            }
            string whereSql = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "";

            // sort the data. only fixed column names are allowed (never put user input straight into sql)
            string dir = q.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
            string orderSql = q.SortBy.ToLower() switch
            {
                "amount" => $"Amount {dir}",
                "timestamp" => $"Timestamp {dir}",
                _ => "Timestamp DESC"
            };

            // count all the matching data
            int total;
            using (var cnt = Cmd(conn, null, $"SELECT COUNT(*) FROM Transactions {whereSql}", ps.ToArray()))
                total = Convert.ToInt32(cnt.ExecuteScalar());

            // fetch only the page asked for (LIMIT/OFFSET replaces Skip/Take)
            ps.Add(("@size", q.PageSize));
            ps.Add(("@off", (q.Page - 1) * q.PageSize));

            var items = new List<TransactionResult>();
            using (var cmd = Cmd(conn, null,
                $@"SELECT TransactionId, AccountNumber, Type, Amount, BalanceAfter, Timestamp
                   FROM Transactions {whereSql}
                   ORDER BY {orderSql}, TransactionId DESC
                   LIMIT @size OFFSET @off", ps.ToArray()))
            using (var rd = cmd.ExecuteReader())
            {
                while (rd.Read())
                {
                    items.Add(new TransactionResult
                    {
                        TransactionId = rd.GetInt64(0),
                        AccountNumber = rd.GetString(1),
                        Type = rd.GetString(2),
                        Amount = rd.GetDecimal(3),
                        BalanceAfter = rd.GetDecimal(4),
                        Timestamp = DateTime.SpecifyKind(DateTime.Parse(rd.GetString(5), CultureInfo.InvariantCulture), DateTimeKind.Utc)
                    });
                }
            }

            // returns the filtered & sorted list for user to view
            return Task.FromResult(new PagedResult<TransactionResult>
            {
                Items = items,
                Page = q.Page,
                PageSize = q.PageSize,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)q.PageSize)
            });
        }

        // method to fetch all the accounts for a specific user ID
        public Task<List<AccountDto>> GetAccountsByUserAsync(int userId)
        {
            using var conn = Open();
            using var cmd = Cmd(conn, null,
                "SELECT AccountNumber, UserId, AccountType, Balance, IsActive FROM Accounts WHERE UserId = @u", ("@u", userId));
            using var rd = cmd.ExecuteReader();

            var list = new List<AccountDto>();
            while (rd.Read())
                list.Add(ReadAccount(rd));

            return Task.FromResult(list);
        }
    }
}