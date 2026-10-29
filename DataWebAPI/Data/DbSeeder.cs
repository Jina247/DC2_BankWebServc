using DataWebAPI.Models;
using DataWebAPI.Services;
using Microsoft.Data.Sqlite;

namespace DataWebAPI.Data
{
    public static class DbSeeder
    {
        public const string AdminUsername = "admin";
        public const string AdminPassword = "Admin@123";
        public const string CustomerUsername = "customer";
        public const string CustomerPassword = "Customer@123";
        public const string OtherCustomersPassword = "Password@123";

        private const string TimeFmt = "yyyy-MM-dd HH:mm:ss";

        private const int RandomSeed = 3008;

        private const int MinTransactions = 500;

        private static readonly string[] FirstNames ={
            "Olivia", "Jack", "Amelia", "Noah", "Isla", "Oliver", "Mia", "William", "Ava", "Leo",
            "Charlotte", "Lucas", "Grace", "Henry", "Zoe", "Thomas", "Ruby", "James", "Chloe"
        };

        private static readonly string[] LastNames ={
            "Smith", "Nguyen", "Brown", "Wilson", "Taylor", "Rahman", "Martin", "Anderson", "Thompson", "White",
            "Walker", "Hossain", "Harris", "Lee", "Kelly", "Patel", "King", "Wright", "Chen"
        };

        private static readonly string[] Streets ={
            "Hay St", "Murray St", "Kent St", "Manning Rd", "Albany Hwy", "Canning Hwy", "Stirling Hwy", "Beaufort St"
        };

        private static readonly string[] Suburbs ={
            "Perth WA 6000", "Bentley WA 6102", "Victoria Park WA 6100", "Como WA 6152", "Subiaco WA 6008", "Fremantle WA 6160"
        };

        private static readonly string[] DepositNotes ={ "Salary", "Cash deposit", "Refund", "Gift", "Interest", "Tax return" };
        private static readonly string[] WithdrawNotes ={ "ATM withdrawal", "Groceries", "Rent", "Electricity bill", "Fuel", "Phone bill" };
        private static readonly string[] TransferNotes ={ "Savings", "Dinner split", "Rent share", "Loan repayment", "Birthday gift", "Holiday fund" };

        public static void SeedIfEmpty(string connStr, ILogger logger){
            using var conn = new SqliteConnection(connStr);
            conn.Open();
            SeedIfEmpty(conn, logger);
        }

        public static void SeedIfEmpty(SqliteConnection conn, ILogger logger){
            using (var check = conn.CreateCommand()){
                check.CommandText = "SELECT COUNT(*) FROM Users";
                if (Convert.ToInt64(check.ExecuteScalar()) > 0)
                    return;
            }

            var rng = new Random(RandomSeed);
            var now = DateTime.UtcNow;
            var start = now.Date.AddDays(-180);  

            using var tx = conn.BeginTransaction();

            var adminId = InsertUser(conn, tx, new User{
                Username = AdminUsername,
                Name = "System Administrator",
                Email = "admin@bank.local",
                Address = "1 Bank Plaza, Perth WA 6000",
                Phone = "08 9000 0000",
                Role = UserRoles.Admin
            }, AdminPassword);

            var customerIds = new List<int>{

                InsertUser(conn, tx, new User{
                    Username = CustomerUsername, Name = "Sam Customer", Email = "customer@bank.local",
                    Address = "12 Kent St, Bentley WA 6102", Phone = "0400 000 001", Role = UserRoles.Customer
                }, CustomerPassword)
            };

            for (int i = 0; i < 19; i++){
                var first = FirstNames[i];
                var last = LastNames[i];
                var username = $"{first}.{last}".ToLowerInvariant();
                customerIds.Add(InsertUser(conn, tx, new User{
                    Username = username,
                    Name = $"{first} {last}",
                    Email = $"{username}@example.com",
                    Address = $"{rng.Next(1, 300)} {Streets[rng.Next(Streets.Length)]}, {Suburbs[rng.Next(Suburbs.Length)]}",
                    Phone = $"04{rng.Next(10, 99)} {rng.Next(100, 999)} {rng.Next(100, 999)}",
                    Role = UserRoles.Customer
                }, OtherCustomersPassword));
            }

            var balances = new Dictionary<string, decimal>();
            int nextNumber = 1001;
            for (int i = 0; i < customerIds.Count; i++){
                var types = i < 15 ? new[] { "Everyday", "Savings" } : new[] { "Savings" };
                foreach (var type in types){
                    var number = (nextNumber++).ToString();
                    using var cmd = Cmd(conn, tx,
                        "INSERT INTO Accounts (AccountNumber, UserId, AccountType, Balance, IsActive, CreatedAt) VALUES (@a, @u, @t, 0, 1, @c)",
                        ("@a", number), ("@u", customerIds[i]), ("@t", type), ("@c", start.ToString(TimeFmt)));
                    cmd.ExecuteNonQuery();
                    balances[number] = 0m;
                }
            }
            var accountNumbers = balances.Keys.ToList();

            var rows = new List<Transaction>();

            var openings = accountNumbers
                .Select(a => (Account: a, Time: start.AddMinutes(rng.Next(0, 10 * 24 * 60))))
                .OrderBy(o => o.Time)
                .ToList();
            foreach (var o in openings){
                var amount = Money(rng, 500, 5000);
                balances[o.Account] += amount;
                rows.Add(Tx(o.Account, TransactionTypes.Deposit, amount, balances[o.Account], "Opening deposit", null, o.Time));
            }

            var activityStart = start.AddDays(10);
            var minutesAvailable = (int)(now.AddHours(-1) - activityStart).TotalMinutes;
            var times = Enumerable.Range(0, 400)
                .Select(_ => activityStart.AddMinutes(rng.Next(0, minutesAvailable)))
                .OrderBy(t => t)
                .ToList();

            foreach (var time in times){
                var acc = accountNumbers[rng.Next(accountNumbers.Count)];
                var roll = rng.NextDouble();


                if (roll < 0.30 || balances[acc] < 20){
                    var amount = Money(rng, 20, 1500);
                    balances[acc] += amount;
                    rows.Add(Tx(acc, TransactionTypes.Deposit, amount, balances[acc], Pick(rng, DepositNotes), null, time));
                }
                else if (roll < 0.65){
                    var amount = Money(rng, 10, Math.Min(balances[acc], 800));
                    balances[acc] -= amount;
                    rows.Add(Tx(acc, TransactionTypes.Withdrawal, -amount, balances[acc], Pick(rng, WithdrawNotes), null, time));
                }
                else{
                    string to;
                    do { to = accountNumbers[rng.Next(accountNumbers.Count)]; } while (to == acc);

                    var amount = Money(rng, 10, Math.Min(balances[acc], 1000));
                    var note = Pick(rng, TransferNotes);

                    balances[acc] -= amount;
                    rows.Add(Tx(acc, TransactionTypes.TransferOut, -amount, balances[acc], note, to, time));
                    balances[to] += amount;
                    rows.Add(Tx(to, TransactionTypes.TransferIn, amount, balances[to], note, acc, time));
                }
            }

            var topUpTime = now.AddMinutes(-59);
            while (rows.Count < MinTransactions){
                var acc = accountNumbers[rng.Next(accountNumbers.Count)];
                var amount = Money(rng, 20, 200);
                balances[acc] += amount;
                topUpTime = topUpTime.AddSeconds(1);
                rows.Add(Tx(acc, TransactionTypes.Deposit, amount, balances[acc], "Cash deposit", null, topUpTime));
            }

            foreach (var r in rows){
                using var cmd = Cmd(conn, tx,
                    @"INSERT INTO Transactions (AccountNumber, Type, Amount, BalanceAfter, Description, RelatedAccountNumber, Timestamp)
                      VALUES (@a, @t, @amt, @bal, @d, @rel, @ts)",
                    ("@a", r.AccountNumber), ("@t", r.Type), ("@amt", r.Amount), ("@bal", r.BalanceAfter),
                    ("@d", r.Description), ("@rel", r.RelatedAccountNumber), ("@ts", r.Timestamp.ToString(TimeFmt)));
                cmd.ExecuteNonQuery();
            }

            foreach (var (acc, balance) in balances){
                using var cmd = Cmd(conn, tx, "UPDATE Accounts SET Balance = @b WHERE AccountNumber = @a",
                    ("@b", balance), ("@a", acc));
                cmd.ExecuteNonQuery();
            }

            using (var cmd = Cmd(conn, tx,
                "INSERT INTO AuditLogs (AdminUserId, Action, TargetType, TargetId, Details, Timestamp) VALUES (@u, @a, @tt, @ti, @d, @ts)",
                ("@u", adminId), ("@a", "SEED_DATABASE"), ("@tt", "System"), ("@ti", null),
                ("@d", $"Seeded {customerIds.Count + 1} users, {accountNumbers.Count} accounts, {rows.Count} transactions."),
                ("@ts", now.ToString(TimeFmt)))){
                cmd.ExecuteNonQuery();
            }

            VerifyBalances(conn, tx);

            tx.Commit();
            logger.LogInformation("Seeded {Users} users, {Accounts} accounts, {Tx} transactions",
                customerIds.Count + 1, accountNumbers.Count, rows.Count);
        }

        public static void VerifyBalances(SqliteConnection conn, SqliteTransaction? tx){
            using var cmd = Cmd(conn, tx, @"
                SELECT a.AccountNumber
                FROM Accounts a
                WHERE ROUND(a.Balance, 2) <> ROUND(COALESCE((SELECT SUM(t.Amount) FROM Transactions t WHERE t.AccountNumber = a.AccountNumber), 0), 2)
                   OR ROUND(a.Balance, 2) <> ROUND(COALESCE((SELECT t.BalanceAfter FROM Transactions t WHERE t.AccountNumber = a.AccountNumber
                                                             ORDER BY t.Timestamp DESC, t.TransactionId DESC LIMIT 1), 0), 2)");
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
                throw new InvalidOperationException($"Seed check failed: account {rd.GetString(0)} balance does not match its history.");
        }

        private static int InsertUser(SqliteConnection conn, SqliteTransaction tx, User u, string password){
            u.PasswordHash = PasswordHasher.Hash(password);
            using var cmd = Cmd(conn, tx,
                @"INSERT INTO Users (Username, Name, Email, Address, Phone, PicturePath, PasswordHash, Role, IsActive)
                  VALUES (@u, @n, @e, @a, @p, @pic, @h, @r, @act);
                  SELECT last_insert_rowid();",
                ("@u", u.Username), ("@n", u.Name), ("@e", u.Email), ("@a", u.Address), ("@p", u.Phone),
                ("@pic", u.PicturePath), ("@h", u.PasswordHash), ("@r", u.Role), ("@act", u.IsActive));
            u.UserId = Convert.ToInt32(cmd.ExecuteScalar());
            return u.UserId;
        }

        private static Transaction Tx(string account, string type, decimal amount, decimal balanceAfter,
            string description, string? related, DateTime time) => new(){
                AccountNumber = account,
                Type = type,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Description = description,
                RelatedAccountNumber = related,
                Timestamp = time
            };

        private static decimal Money(Random rng, decimal min, decimal max){
            if (max < min) max = min;
            return Math.Round(min + (decimal)rng.NextDouble() * (max - min), 2);
        }

        private static string Pick(Random rng, string[] options) => options[rng.Next(options.Length)];

        private static SqliteCommand Cmd(SqliteConnection conn, SqliteTransaction? tx, string sql,
            params (string Name, object? Value)[] ps){
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = sql;
            foreach (var (name, value) in ps)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);
            return cmd;
        }
    }
}
