using Microsoft.EntityFrameworkCore;
using VexCash.SmsService.Data;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Repositories
{
    public interface ISmsLogRepository
    {
        Task<SmsLog> AddAsync(SmsLog log, CancellationToken cancellationToken = default);
        Task<IEnumerable<SmsLog>> GetHistoryAsync(int? customerId = null, TemplateType? templateType = null, int limit = 100, CancellationToken cancellationToken = default);
    }

    public class SmsLogRepository : ISmsLogRepository
    {
        private readonly AppDbContext _context;

        public SmsLogRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SmsLog> AddAsync(SmsLog log, CancellationToken cancellationToken = default)
        {
            _context.SmsLogs.Add(log);
            await _context.SaveChangesAsync(cancellationToken);
            return log;
        }

        public async Task<IEnumerable<SmsLog>> GetHistoryAsync(int? customerId = null, TemplateType? templateType = null, int limit = 100, CancellationToken cancellationToken = default)
        {
            var query = _context.SmsLogs.AsQueryable();

            if (customerId.HasValue)
            {
                query = query.Where(l => l.CustomerId == customerId.Value);
            }

            if (templateType.HasValue)
            {
                query = query.Where(l => l.TemplateType == templateType.Value);
            }

            return await query
                .OrderByDescending(l => l.SentAt)
                .Take(limit)
                .ToListAsync(cancellationToken);
        }
    }
}
