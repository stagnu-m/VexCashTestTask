using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using VexCash.SmsService.Data;
using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;
using VexCash.SmsService.Services;
using Xunit;

namespace VexCash.SmsService.Tests
{
    public class SmsDispatchServiceTests
    {
        private readonly AppDbContext _dbContext;
        private readonly TemplateRenderer _renderer = new TemplateRenderer();
        private readonly Mock<IMessageBirdClient> _messageBirdMock = new Mock<IMessageBirdClient>();
        private readonly SmsDispatchService _service;

        public SmsDispatchServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            DbInitializer.Seed(_dbContext);

            var customerRepo = new CustomerRepository(_dbContext);
            var templateRepo = new TemplateRepository(_dbContext);
            var smsLogRepo = new SmsLogRepository(_dbContext);

            _service = new SmsDispatchService(
                customerRepo,
                templateRepo,
                smsLogRepo,
                _renderer,
                _messageBirdMock.Object,
                NullLogger<SmsDispatchService>.Instance
            );
        }

        [Fact]
        public async Task PreviewSmsAsync_ExistingCustomer_ReturnsRenderedMessage()
        {
            // Max Mustermann is seeded with Id = 1, CR-2026-1001, 250 EUR
            var request = new SmsPreviewRequestDto
            {
                CustomerId = 1,
                TemplateType = TemplateType.Erinnerung
            };

            var preview = await _service.PreviewSmsAsync(request);

            preview.Should().NotBeNull();
            preview.CustomerId.Should().Be(1);
            preview.CustomerName.Should().Be("Max Mustermann");
            preview.RenderedMessage.Should().Contain("CR-2026-1001");
            preview.RenderedMessage.Should().Contain("Max Mustermann");
            preview.RenderedMessage.Should().Contain("250,00 EUR");
        }

        [Fact]
        public async Task PreviewSmsAsync_NonExistingCustomer_ThrowsKeyNotFoundException()
        {
            var request = new SmsPreviewRequestDto
            {
                CustomerId = 99999,
                TemplateType = TemplateType.Erinnerung
            };

            Func<Task> act = async () => await _service.PreviewSmsAsync(request);
            await act.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task SendSmsAsync_ValidRequest_CallsMessageBirdAndCreatesLog()
        {
            _messageBirdMock
                .Setup(m => m.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MessageBirdSendResult(true, "mb_test_123", "sent", null));

            var request = new SendSmsRequestDto
            {
                CustomerId = 1,
                TemplateType = TemplateType.Erinnerung
            };

            var result = await _service.SendSmsAsync(request);

            result.Success.Should().BeTrue();
            result.MessageBirdId.Should().Be("mb_test_123");
            result.Status.Should().Be("sent");

            // Verify stored in DB
            var log = await _dbContext.SmsLogs.FirstOrDefaultAsync(l => l.MessageBirdId == "mb_test_123");
            log.Should().NotBeNull();
            log!.RecipientPhoneNumber.Should().Be("+491512345678");
            log.Status.Should().Be("sent");
        }

        [Fact]
        public async Task BulkSendSmsAsync_WithOnlyOverdue_ProcessesOnlyOverdueCustomers()
        {
            _messageBirdMock
                .Setup(m => m.SendSmsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MessageBirdSendResult(true, "mb_bulk_1", "sent", null));

            var request = new BulkSendSmsRequestDto
            {
                TemplateType = TemplateType.Mahnung,
                OnlyOverdue = true
            };

            var bulkResult = await _service.BulkSendSmsAsync(request);

            // In seeded data, 2 customers (Erika Musterfrau -3d, Johannes Schmidt -14d) are overdue
            bulkResult.TotalProcessed.Should().Be(2);
            bulkResult.SucceededCount.Should().Be(2);
            bulkResult.FailedCount.Should().Be(0);
        }
    }
}
