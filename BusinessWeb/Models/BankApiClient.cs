using System.Net;
using System.Text.Json;
using BankMvc.Models;
using RestSharp;

namespace BankMvc.Services
{
    // the ONLY place that knows Part A's URLs
    public static class Routes
    {
        // all accounts of one user, the user ID is sent in the query (Part A: GetAccountsByUserAsync)
        public const string Accounts = "api/accounts";

        // all transactions with paging, sorting and filters (Part A: SearchTransactionsAsync)
        public const string Transactions = "api/transactions";

        // transfer money between 2 accounts (Part A: TransferAsync)
        public const string Transfer = "api/transactions/transfer";
    }

    // object to hold the result of every call to the service (success or error)
    // so controllers never have to deal with exceptions
    public class ApiResult<T>
    {
        // true if the service replied with success
        public bool Ok { get; set; }

        // the data from the service (only when Ok)
        public T? Data { get; set; }

        // friendly message to show the user (only when not Ok)
        public string Message { get; set; } = "";

        // messages for each field from the service's "details" (validation errors)
        public List<string> Details { get; set; } = new();
    }

    public class BankApiClient
    {
        // ignore the upper/lower case of the json names when reading the reply
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        // the client used to send all the requests to the service
        private readonly RestClient _client;

        public BankApiClient(IConfiguration config)
        {
            // base URL comes from appsettings.json, NOT hardcoded
            var baseUrl = config["BankService:BaseUrl"]
                ?? throw new InvalidOperationException("BankService:BaseUrl is missing in appsettings.json");

            // 10 second timeout so the page doesn't hang forever if the service is stuck
            _client = new RestClient(new RestClientOptions(baseUrl) { Timeout = TimeSpan.FromSeconds(10) });
        }

        // get all accounts of a user (and also used to check who owns what)
        public Task<ApiResult<List<AccountDto>>> GetAccountsAsync(int userId) =>
            Send<List<AccountDto>>(new RestRequest(Routes.Accounts).AddQueryParameter("userId", userId.ToString()));

        // get transactions with the filters, paging and sorting
        public Task<ApiResult<PagedResult<TransactionDto>>> GetTransactionsAsync(TransactionQuery q)
        {
            var req = new RestRequest(Routes.Transactions);

            // only send the filters that were filled in
            if (!string.IsNullOrWhiteSpace(q.AccountNumber)) req.AddQueryParameter("accountNumber", q.AccountNumber);
            if (!string.IsNullOrWhiteSpace(q.Search)) req.AddQueryParameter("search", q.Search);
            if (q.From.HasValue) req.AddQueryParameter("from", q.From.Value.ToString("yyyy-MM-dd"));
            if (q.To.HasValue) req.AddQueryParameter("to", q.To.Value.ToString("yyyy-MM-dd"));

            // sorting and paging are always sent
            req.AddQueryParameter("sortBy", q.SortBy);
            req.AddQueryParameter("sortDir", q.SortDir);
            req.AddQueryParameter("page", q.Page.ToString());
            req.AddQueryParameter("pageSize", q.PageSize.ToString());

            return Send<PagedResult<TransactionDto>>(req);
        }

        // transfer money, service gives back the sender's transaction
        public Task<ApiResult<TransactionDto>> TransferAsync(TransferViewModel m)
        {
            var req = new RestRequest(Routes.Transfer, Method.Post)
                .AddJsonBody(new { m.FromAccountNumber, m.ToAccountNumber, m.Amount, m.Description });

            return Send<TransactionDto>(req);
        }

        // helper method to send the request, it always gives back an ApiResult (it never throws)
        private async Task<ApiResult<T>> Send<T>(RestRequest req)
        {
            RestResponse resp;
            try
            {
                resp = await _client.ExecuteAsync(req);
            }
            catch
            {
                // the request failed before we got any reply from the service
                return Fail<T>("The bank service is unavailable. Please try again later.");
            }

            // status 0 = no answer at all (service is down), 503 = service unavailable
            if (resp.StatusCode == 0 || resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                return Fail<T>("The bank service is unavailable. Please try again later.");

            // if successful, read the json into the type we asked for
            if (resp.IsSuccessful)
            {
                try
                {
                    var data = string.IsNullOrWhiteSpace(resp.Content)
                        ? default
                        : JsonSerializer.Deserialize<T>(resp.Content, Json);
                    return new ApiResult<T> { Ok = true, Data = data };
                }
                catch
                {
                    return Fail<T>("The bank service sent a response we couldn't read.");
                }
            }

            // if error, part A sends { error, message, details } so we take the message from it
            var result = Fail<T>("Something went wrong. Please try again.");
            try
            {
                using var doc = JsonDocument.Parse(resp.Content ?? "{}");

                // the main message, ex: "Balance is 45.20; requested 100.00."
                if (doc.RootElement.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                    result.Message = msg.GetString()!;

                // details = messages for each field (only for validation errors, otherwise null)
                if (doc.RootElement.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in details.EnumerateObject())
                    {
                        if (field.Value.ValueKind == JsonValueKind.Array)
                            foreach (var item in field.Value.EnumerateArray())
                                result.Details.Add(item.ToString());
                        else
                            result.Details.Add(field.Value.ToString());
                    }
                }
            }
            catch
            {
                // the reply is not json, so keep the default message (don't show raw errors)
            }
            return result;
        }

        // helper method to create a failed result with a message
        private static ApiResult<T> Fail<T>(string message) => new() { Ok = false, Message = message };
    }
}
