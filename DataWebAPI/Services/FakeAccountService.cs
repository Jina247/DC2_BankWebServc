using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    // TEMPORARY: in-memory, for Postman testing only. Replaced by AccountService (ADO.NET) in Step 3.
    public class FakeAccountService : IAccountService
    {
        private static readonly Dictionary<string, decimal> _balances = new()
        {
            ["1001"] = 250.50m,
            ["1002"] = 980.00m
        };
        private static long _nextId = 26;

        // 25 fake past transactions so paging/sorting has something to chew on
        private static readonly List<TransactionResult> _history = Enumerable.Range(1, 25).Select(i => new TransactionResult
        {
            TransactionId = i,
            AccountNumber = i % 2 == 0 ? "1001" : "1002",
            Type = i % 3 == 0 ? "Withdrawal" : "Deposit",
            Amount = i * 10,
            BalanceAfter = 0,
            Timestamp = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        private static readonly object _lock = new();

        public Task<TransactionResult> DepositAsync(DepositRequest r)
        {
            lock (_lock)
            {
                Check(r.AccountNumber);
                _balances[r.AccountNumber] += r.Amount;
                return Task.FromResult(Make(r.AccountNumber, "Deposit", r.Amount));
            }
        }

        public Task<TransactionResult> WithdrawAsync(WithdrawRequest r)
        {
            lock (_lock)
            {
                Check(r.AccountNumber);
                Funds(r.AccountNumber, r.Amount);
                _balances[r.AccountNumber] -= r.Amount;
                return Task.FromResult(Make(r.AccountNumber, "Withdrawal", -r.Amount));
            }
        }

        public Task<TransactionResult> TransferAsync(TransferRequest r)
        {
            lock (_lock)
            {
                if (r.FromAccountNumber == r.ToAccountNumber)
                    throw new BusinessRuleException("SAME_ACCOUNT", "Cannot transfer to the same account.");
                Check(r.FromAccountNumber);
                Check(r.ToAccountNumber);
                Funds(r.FromAccountNumber, r.Amount);   // all checks BEFORE changing anything
                _balances[r.FromAccountNumber] -= r.Amount;
                _balances[r.ToAccountNumber] += r.Amount;
                return Task.FromResult(Make(r.FromAccountNumber, "TransferOut", -r.Amount));
            }
        }

        private static void Check(string acc)
        {
            if (!_balances.ContainsKey(acc))
                throw new NotFoundException($"Account {acc} not found.");
        }

        private static void Funds(string acc, decimal amount)
        {
            if (_balances[acc] < amount)
                throw new BusinessRuleException("INSUFFICIENT_FUNDS",
                    $"Balance is {_balances[acc]:F2}; requested {amount:F2}.");
        }

        private static TransactionResult Make(string acc, string type, decimal amount)
        {
            var t = new TransactionResult
            {
                TransactionId = _nextId++,
                AccountNumber = acc,
                Type = type,
                Amount = amount,
                BalanceAfter = _balances[acc],
                Timestamp = DateTime.UtcNow
            };
            _history.Add(t);
            return t;
        }

        private static readonly Dictionary<string, AccountDto> _accounts = new()
        {
            ["1001"] = new AccountDto { AccountNumber = "1001", UserId = 1, AccountType = "Savings", IsActive = true },
            ["1002"] = new AccountDto { AccountNumber = "1002", UserId = 2, AccountType = "Checking", IsActive = true }
        };
        private static int _nextAcc = 1003;

        public Task<AccountDto> CreateAccountAsync(CreateAccountRequest r)
        {
            lock (_lock)
            {
                var num = (_nextAcc++).ToString();
                _balances[num] = 0m;
                _accounts[num] = new AccountDto { AccountNumber = num, UserId = r.UserId, AccountType = r.AccountType, IsActive = true };
                return Task.FromResult(WithBalance(_accounts[num]));
            }
        }

        public Task<AccountDto> GetAccountAsync(string n)
        {
            lock (_lock) { Check(n); return Task.FromResult(WithBalance(_accounts[n])); }
        }

        public Task UpdateAccountAsync(string n, UpdateAccountRequest r)
        {
            lock (_lock) { Check(n); _accounts[n].AccountType = r.AccountType; return Task.CompletedTask; }
        }

        public Task DeactivateAccountAsync(string n)
        {
            lock (_lock) { Check(n); _accounts[n].IsActive = false; return Task.CompletedTask; }
        }

        private static AccountDto WithBalance(AccountDto a) => new()
        {
            AccountNumber = a.AccountNumber,
            UserId = a.UserId,
            AccountType = a.AccountType,
            IsActive = a.IsActive,
            Balance = _balances[a.AccountNumber]
        };

        public Task<PagedResult<TransactionResult>> SearchTransactionsAsync(TransactionQuery q)
        {
            lock (_lock)
            {
                IEnumerable<TransactionResult> data = _history;

                if (!string.IsNullOrWhiteSpace(q.AccountNumber)) data = data.Where(t => t.AccountNumber == q.AccountNumber);
                if (q.From.HasValue) data = data.Where(t => t.Timestamp >= q.From.Value);
                if (q.To.HasValue) data = data.Where(t => t.Timestamp <= q.To.Value);
                if (!string.IsNullOrWhiteSpace(q.Search))
                    data = data.Where(t => t.Type.Contains(q.Search, StringComparison.OrdinalIgnoreCase)
                                        || t.AccountNumber.Contains(q.Search));

                bool desc = q.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase);
                data = (q.SortBy.ToLower(), desc) switch
                {
                    ("amount", true) => data.OrderByDescending(t => t.Amount),
                    ("amount", false) => data.OrderBy(t => t.Amount),
                    ("timestamp", false) => data.OrderBy(t => t.Timestamp),
                    _ => data.OrderByDescending(t => t.Timestamp)
                };

                int total = data.Count();
                var items = data.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToList();

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

        public Task<List<AccountDto>> GetAccountsByUserAsync(int userId)
        {
            lock (_lock)
                return Task.FromResult(_accounts.Values.Where(a => a.UserId == userId).Select(WithBalance).ToList());
        }
    }
}