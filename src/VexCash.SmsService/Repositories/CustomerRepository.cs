using Microsoft.EntityFrameworkCore;
using VexCash.SmsService.Data;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Repositories
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<Customer>> GetAllAsync(bool? overdueOnly = null, CancellationToken cancellationToken = default);
        Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default);
        Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
        Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default);
        Task<IEnumerable<Customer>> GetFilteredAsync(IEnumerable<int>? customerIds, bool? onlyOverdue, CancellationToken cancellationToken = default);
    }

    public class CustomerRepository : ICustomerRepository
    {
        private readonly AppDbContext _context;

        public CustomerRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Customer>> GetAllAsync(bool? overdueOnly = null, CancellationToken cancellationToken = default)
        {
            var query = _context.Customers.AsQueryable();

            if (overdueOnly == true)
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(c => c.DueDate < today);
            }

            return await query.OrderBy(c => c.Id).ToListAsync(cancellationToken);
        }

        public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
        }

        public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync(cancellationToken);
            return customer;
        }

        public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IEnumerable<Customer>> GetFilteredAsync(IEnumerable<int>? customerIds, bool? onlyOverdue, CancellationToken cancellationToken = default)
        {
            var query = _context.Customers.AsQueryable();

            if (customerIds != null && customerIds.Any())
            {
                query = query.Where(c => customerIds.Contains(c.Id));

                if (onlyOverdue == true)
                {
                    var today = DateTime.UtcNow.Date;
                    query = query.Where(c => c.DueDate < today);
                }
            }
            else
            {
                var today = DateTime.UtcNow.Date;
                query = query.Where(c => c.DueDate < today);
            }

            return await query.ToListAsync(cancellationToken);
        }
    }
}
