// DataWebAPI/Models/Exceptions.cs
namespace DataWebAPI.Models
{
    // throws 404 error: resource doesn't exist
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
    }

    // throws 409 error: conflict (e.g. duplicate account number)
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) { }
    }

    // throws 422 error: valid request that breaks a business rule
    public class BusinessRuleException : Exception
    {
        public string Code { get; }   // e.g. "INSUFFICIENT_FUNDS"

        // set the exception up after getting the error business type
        public BusinessRuleException(string code, string message) : base(message)
        {
            Code = code;
        }
    }
}