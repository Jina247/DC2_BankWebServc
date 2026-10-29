using DataWebAPI.Models;

namespace DataWebAPI.Services
{
    public interface IAuditService
    {
        Task<AuditEntryDto> CreateAsync(CreateAuditRequest req);

        Task<AuditEntryDto> GetAsync(long auditId);

        Task<PagedResult<AuditEntryDto>> SearchAsync(AuditQuery q);
    }
}
