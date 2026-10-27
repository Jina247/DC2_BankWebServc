// DataWebAPI/Models/Exceptions.cs
namespace DataWebAPI.Models
{
    // -> 404: resource doesn't exist
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    // -> 409: conflict (e.g. duplicate account number)
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    // -> 422: valid request that breaks a business rule
    public class BusinessRuleException : Exception
    {
        public string Code { get; }   // e.g. "INSUFFICIENT_FUNDS"

        public BusinessRuleException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}