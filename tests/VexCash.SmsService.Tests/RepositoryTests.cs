using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VexCash.SmsService.Data;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;
using Xunit;

namespace VexCash.SmsService.Tests
{
    public class RepositoryTests
    {
        private readonly AppDbContext _context;
        private readonly CustomerRepository _customerRepo;
        private readonly TemplateRepository _templateRepo;
        private readonly SmsLogRepository _smsLogRepo;

        public RepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            DbInitializer.Seed(_context);

            _customerRepo = new CustomerRepository(_context);
            _templateRepo = new TemplateRepository(_context);
            _smsLogRepo = new SmsLogRepository(_context);
        }

        [Fact]
        public async Task CustomerRepository_GetAllAsync_ReturnsAllCustomers()
        {
            var customers = await _customerRepo.GetAllAsync();
            customers.Should().HaveCountGreaterOrEqualTo(4);
        }

        [Fact]
        public async Task CustomerRepository_GetByIdAsync_ReturnsCorrectCustomer()
        {
            var customer = await _customerRepo.GetByIdAsync(1);
            customer.Should().NotBeNull();
            customer!.Name.Should().Be("Max Mustermann");
        }

        [Fact]
        public async Task CustomerRepository_AddAndRemove_WorksProperly()
        {
            var newCust = new Customer
            {
                Name = "Unit Test Customer",
                PhoneNumber = "+491799999999",
                CreditNumber = "CR-TEST-1",
                Amount = 100m,
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            var added = await _customerRepo.AddAsync(newCust);
            added.Id.Should().BeGreaterThan(0);

            var retrieved = await _customerRepo.GetByIdAsync(added.Id);
            retrieved.Should().NotBeNull();

            await _customerRepo.DeleteAsync(retrieved!);
            var afterDelete = await _customerRepo.GetByIdAsync(added.Id);
            afterDelete.Should().BeNull();
        }

        [Fact]
        public async Task TemplateRepository_GetByTypeAndAddOrUpdate_WorksProperly()
        {
            var template = await _templateRepo.GetByTypeAsync(TemplateType.Erinnerung);
            template.Should().NotBeNull();
            template!.Type.Should().Be(TemplateType.Erinnerung);

            var updated = await _templateRepo.AddOrUpdateAsync(TemplateType.Erinnerung, "Neuer Titel", "Neuer Inhalt");
            updated.Title.Should().Be("Neuer Titel");
            updated.Content.Should().Be("Neuer Inhalt");

            var recheck = await _templateRepo.GetByTypeAsync(TemplateType.Erinnerung);
            recheck!.Title.Should().Be("Neuer Titel");
        }

        [Fact]
        public async Task SmsLogRepository_AddAndGetHistory_WorksProperly()
        {
            var log = new SmsLog
            {
                CustomerId = 1,
                RecipientPhoneNumber = "+491512345678",
                RecipientName = "Max Mustermann",
                CreditNumber = "CR-2026-1001",
                TemplateType = TemplateType.Erinnerung,
                RenderedMessage = "Test message content",
                Status = "Simulated",
                MessageBirdId = "sim_log_1",
                SentAt = DateTime.UtcNow
            };

            await _smsLogRepo.AddAsync(log);

            var history = await _smsLogRepo.GetHistoryAsync(customerId: 1);
            history.Should().NotBeEmpty();
            history.First().MessageBirdId.Should().Be("sim_log_1");
        }
    }
}
