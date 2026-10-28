using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    public class FakeAccountService : IAccountService
    {
        private static readonly Dictionary<string, decimal> _balances = new()
        {
            ["1001"] = 250.50m,
            ["1002"] = 980.00m
        };
        private static long _nextId = 26;

        // --- temporary generate a list of 25 users and their transactions for testing purposes ---
        private static readonly List<TransactionResult> _history = Enumerable.Range(1, 25).Select(i => new TransactionResult
        {
            TransactionId = i,
            AccountNumber = i % 2 == 0 ? "1001" : "1002",
            Type = i % 3 == 0 ? "Withdrawal" : "Deposit",
            Amount = i * 10,
            BalanceAfter = 0,
            Timestamp = DateTime.UtcNow.AddDays(-i)
        }).ToList();
        // -----------------------------------------------------------------------------------------

        // declare mutex lock for transaction actions
        private static readonly object _lock = new();

        // method to deposit money into an account
        public Task<TransactionResult> DepositAsync(DepositRequest r)
        {
            // mutexlock
            lock (_lock)
            {
                // check for account using account number
                Check(r.AccountNumber);

                // look for the balance for that account, then add deposit amount to it
                _balances[r.AccountNumber] += r.Amount;

                // return the transaction result
                // 1001 Deposit 200.00
                return Task.FromResult(Make(r.AccountNumber, "Deposit", r.Amount));
            }
        }

        // method to withdraw money from an account
        public Task<TransactionResult> WithdrawAsync(WithdrawRequest r)
        {
            // mutexlock
            lock (_lock)
            {
                // check for account using account number
                Check(r.AccountNumber);

                // check for sufficient funds in the account
                // it will throw an exception if there are insufficient funds
                Funds(r.AccountNumber, r.Amount);

                // if return sufficient funds, will minus withdrawal amount from balance
                _balances[r.AccountNumber] -= r.Amount;

                // return the transaction result
                // 1001 Withdrawal -50.00
                return Task.FromResult(Make(r.AccountNumber, "Withdrawal", -r.Amount));
            }
        }

        // method to transfer money from one account to another
        public Task<TransactionResult> TransferAsync(TransferRequest r)
        {
            // mutexlock
            lock (_lock)
            {
                // throw exception if the receiver account is same as the sender account
                if (r.FromAccountNumber == r.ToAccountNumber)
                    throw new BusinessRuleException("SAME_ACCOUNT", "Cannot transfer to the same account.");

                // check both accounts using account number
                Check(r.FromAccountNumber);
                Check(r.ToAccountNumber);

                // check for sufficient funds in the sender account
                Funds(r.FromAccountNumber, r.Amount);

                // deduct the transfer amount from the sender account
                _balances[r.FromAccountNumber] -= r.Amount;

                // then add that amount into the receiver account
                _balances[r.ToAccountNumber] += r.Amount;

                // return the transaction result for the sender account
                // 1001 TransferOut -100.00
                return Task.FromResult(Make(r.FromAccountNumber, "TransferOut", -r.Amount));
            }
        }

        // helper method to check if the account exists in the account or not
        private static void Check(string acc)
        {
            // throw exception if the account does not exist
            if (!_balances.ContainsKey(acc))
                throw new NotFoundException($"Account {acc} not found.");
        }

        // helper method to check if the account has sufficient funds for withdrawal or transfer
        private static void Funds(string acc, decimal amount)
        {
            // throw exception if the account does not have sufficient funds
            if (_balances[acc] < amount)
                throw new BusinessRuleException("INSUFFICIENT_FUNDS",
                    $"Balance is {_balances[acc]:F2}; requested {amount:F2}.");
        }

        // helper method to create a transaction result and add it to the transaction history
        private static TransactionResult Make(string acc, string type, decimal amount)
        {
            // fetch all the transaction details such as ID, name, type, amount, balance after transaction & timestamp
            var t = new TransactionResult
            {
                TransactionId = _nextId++,
                AccountNumber = acc,
                Type = type,
                Amount = amount,
                BalanceAfter = _balances[acc],
                Timestamp = DateTime.UtcNow
            };

            // add that specific transaction to the transaction history list
            _history.Add(t);

            // return when the transaction is completed
            return t;
        }

        // temporary declare object variable to store account details ------------------------------
        // such as account number, user ID, account type & its status ------------------------------
        private static readonly Dictionary<string, AccountDto> _accounts = new()
        {
            ["1001"] = new AccountDto { AccountNumber = "1001", UserId = 1, AccountType = "Savings", IsActive = true },
            ["1002"] = new AccountDto { AccountNumber = "1002", UserId = 2, AccountType = "Checking", IsActive = true }
        };
        private static int _nextAcc = 1003;
        // -----------------------------------------------------------------------------------------


        // function to create an account
        public Task<AccountDto> CreateAccountAsync(CreateAccountRequest r)
        {
            // mutexlock
            lock (_lock)
            {
                // generate account number
                var num = (_nextAcc++).ToString();

                // set balance as 0
                _balances[num] = 0m;

                // add account details to account list
                _accounts[num] = new AccountDto { AccountNumber = num, UserId = r.UserId, AccountType = r.AccountType, IsActive = true };

                // return the account details with balance
                return Task.FromResult(WithBalance(_accounts[num]));
            }
        }

        // function to fetch account details
        public Task<AccountDto> GetAccountAsync(string n)
        {
            lock (_lock) { Check(n); return Task.FromResult(WithBalance(_accounts[n])); }
        }

        // helper function to fetch account details
        private static AccountDto WithBalance(AccountDto a) => new()
        {
            AccountNumber = a.AccountNumber,
            UserId = a.UserId,
            AccountType = a.AccountType,
            IsActive = a.IsActive,
            Balance = _balances[a.AccountNumber]
        };

        // function to update account details
        public Task UpdateAccountAsync(string n, UpdateAccountRequest r)
        {
            lock (_lock) { Check(n); _accounts[n].AccountType = r.AccountType; return Task.CompletedTask; }
        }

        // function to deactivate an account
        public Task DeactivateAccountAsync(string n)
        {
            lock (_lock) { Check(n); _accounts[n].IsActive = false; return Task.CompletedTask; }
        }

        // method to search specific transactions from its list
        public Task<PagedResult<TransactionResult>> SearchTransactionsAsync(TransactionQuery q)
        {
            // mutexlock
            lock (_lock)
            {
                // extract all the data from the transaction history list
                IEnumerable<TransactionResult> data = _history;

                // filter the data based on the query parameters
                if (!string.IsNullOrWhiteSpace(q.AccountNumber)) data = data.Where(t => t.AccountNumber == q.AccountNumber);
                if (q.From.HasValue) data = data.Where(t => t.Timestamp >= q.From.Value);
                if (q.To.HasValue) data = data.Where(t => t.Timestamp <= q.To.Value);
                if (!string.IsNullOrWhiteSpace(q.Search))
                    data = data.Where(t => t.Type.Contains(q.Search, StringComparison.OrdinalIgnoreCase)
                                        || t.AccountNumber.Contains(q.Search));

                // sort the data based on the description given
                bool desc = q.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);

                // udpate the data to the filtered & sorted list 
                data = (q.SortBy.ToLower(), desc) switch
                {
                    ("amount", true) => data.OrderByDescending(t => t.Amount),
                    ("amount", false) => data.OrderBy(t => t.Amount),
                    ("timestamp", false) => data.OrderBy(t => t.Timestamp),
                    _ => data.OrderByDescending(t => t.Timestamp)
                };

                // count all the data & put it into viewable list
                int total = data.Count();
                var items = data.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

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
        }

        // method to fetch all the accounts for a specific user ID
        public Task<List<AccountDto>> GetAccountsByUserAsync(int userId)
        {
            lock (_lock)
                return Task.FromResult(_accounts.Values.Where(a => a.UserId == userId).Select(WithBalance).ToList());
        }
    }
}