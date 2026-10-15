using Microsoft.EntityFrameworkCore;
using VexCash.SmsService.Data;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Repositories
{
    public interface ITemplateRepository
    {
        Task<IEnumerable<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<MessageTemplate?> GetByTypeAsync(TemplateType type, CancellationToken cancellationToken = default);
        Task<MessageTemplate> AddOrUpdateAsync(TemplateType type, string title, string content, CancellationToken cancellationToken = default);
    }

    public class TemplateRepository : ITemplateRepository
    {
        private readonly AppDbContext _context;

        public TemplateRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<MessageTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Templates
                .OrderBy(t => t.Type)
                .ToListAsync(cancellationToken);
        }

        public async Task<MessageTemplate?> GetByTypeAsync(TemplateType type, CancellationToken cancellationToken = default)
        {
            return await _context.Templates
                .FirstOrDefaultAsync(t => t.Type == type, cancellationToken);
        }

        public async Task<MessageTemplate> AddOrUpdateAsync(TemplateType type, string title, string content, CancellationToken cancellationToken = default)
        {
            var template = await _context.Templates.FirstOrDefaultAsync(t => t.Type == type, cancellationToken);

            if (template == null)
            {
                template = new MessageTemplate
                {
                    Type = type,
                    Title = title.Trim(),
                    Content = content.Trim(),
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Templates.Add(template);
            }
            else
            {
                template.Title = title.Trim();
                template.Content = content.Trim();
                template.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return template;
        }
    }
}
