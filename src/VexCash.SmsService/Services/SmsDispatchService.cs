using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;

namespace VexCash.SmsService.Services
{
    public interface ISmsDispatchService
    {
        Task<SmsPreviewResponseDto> PreviewSmsAsync(SmsPreviewRequestDto request, CancellationToken cancellationToken = default);

        Task<SmsSendResultDto> SendSmsAsync(SendSmsRequestDto request, CancellationToken cancellationToken = default);

        Task<BulkSendSmsResultDto> BulkSendSmsAsync(BulkSendSmsRequestDto request, CancellationToken cancellationToken = default);
    }

    public class SmsDispatchService : ISmsDispatchService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly ISmsLogRepository _smsLogRepository;
        private readonly ITemplateRenderer _templateRenderer;
        private readonly IMessageBirdClient _messageBirdClient;
        private readonly ILogger<SmsDispatchService> _logger;

        public SmsDispatchService(
            ICustomerRepository customerRepository,
            ITemplateRepository templateRepository,
            ISmsLogRepository smsLogRepository,
            ITemplateRenderer templateRenderer,
            IMessageBirdClient messageBirdClient,
            ILogger<SmsDispatchService> logger)
        {
            _customerRepository = customerRepository;
            _templateRepository = templateRepository;
            _smsLogRepository = smsLogRepository;
            _templateRenderer = templateRenderer;
            _messageBirdClient = messageBirdClient;
            _logger = logger;
        }

        public async Task<SmsPreviewResponseDto> PreviewSmsAsync(SmsPreviewRequestDto request, CancellationToken cancellationToken = default)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {request.CustomerId} was not found.");
            }

            var templateText = await ResolveTemplateTextAsync(request.TemplateType, request.CustomTemplate, cancellationToken);
            var rendered = _templateRenderer.Render(templateText, customer);

            return new SmsPreviewResponseDto
            {
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                PhoneNumber = customer.PhoneNumber,
                CreditNumber = customer.CreditNumber,
                TemplateType = request.TemplateType,
                RenderedMessage = rendered
            };
        }

        public async Task<SmsSendResultDto> SendSmsAsync(SendSmsRequestDto request, CancellationToken cancellationToken = default)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer == null)
            {
                throw new KeyNotFoundException($"Customer with ID {request.CustomerId} was not found.");
            }

            var templateText = await ResolveTemplateTextAsync(request.TemplateType, request.CustomTemplate, cancellationToken);
            var rendered = _templateRenderer.Render(templateText, customer);

            var sendResult = await _messageBirdClient.SendSmsAsync(
                recipientPhoneNumber: customer.PhoneNumber,
                messageBody: rendered,
                cancellationToken: cancellationToken);

            var logEntry = new SmsLog
            {
                CustomerId = customer.Id,
                RecipientPhoneNumber = customer.PhoneNumber,
                RecipientName = customer.Name,
                CreditNumber = customer.CreditNumber,
                TemplateType = request.TemplateType,
                RenderedMessage = rendered,
                SentAt = DateTime.UtcNow,
                Status = sendResult.Success ? sendResult.Status : "Failed",
                StatusReason = sendResult.StatusReason,
                MessageBirdId = sendResult.MessageId,
                ErrorMessage = sendResult.ErrorMessage
            };

            await _smsLogRepository.AddAsync(logEntry, cancellationToken);

            return new SmsSendResultDto
            {
                Success = sendResult.Success,
                Status = logEntry.Status,
                StatusReason = sendResult.StatusReason,
                CustomerId = customer.Id,
                CustomerName = customer.Name,
                RecipientPhoneNumber = customer.PhoneNumber,
                TemplateType = request.TemplateType,
                RenderedMessage = rendered,
                MessageBirdId = sendResult.MessageId,
                ErrorMessage = sendResult.ErrorMessage,
                Timestamp = logEntry.SentAt
            };
        }

        public async Task<BulkSendSmsResultDto> BulkSendSmsAsync(BulkSendSmsRequestDto request, CancellationToken cancellationToken = default)
        {
            var targetCustomers = (await _customerRepository.GetFilteredAsync(
                request.CustomerIds,
                request.OnlyOverdue,
                cancellationToken)).ToList();

            var bulkResult = new BulkSendSmsResultDto
            {
                TotalProcessed = targetCustomers.Count
            };

            foreach (var customer in targetCustomers)
            {
                var singleResult = await SendSmsAsync(new SendSmsRequestDto
                {
                    CustomerId = customer.Id,
                    TemplateType = request.TemplateType
                }, cancellationToken);

                bulkResult.Results.Add(singleResult);
                if (singleResult.Success)
                {
                    bulkResult.SucceededCount++;
                }
                else
                {
                    bulkResult.FailedCount++;
                }
            }

            return bulkResult;
        }

        private async Task<string> ResolveTemplateTextAsync(TemplateType type, string? customTemplate, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(customTemplate))
            {
                return customTemplate;
            }

            var templateEntity = await _templateRepository.GetByTypeAsync(type, cancellationToken);
            if (templateEntity != null && !string.IsNullOrWhiteSpace(templateEntity.Content))
            {
                return templateEntity.Content;
            }

            return type switch
            {
                TemplateType.Erinnerung => "{Creditnumber}\n\nSehr geehrte/r {Name}\nBitte zahlen sie bis zum {dueDate}\nDie Summe {amount} zurück",
                TemplateType.Mahnung => "Mahnung: {Creditnumber}\n\nSehr geehrte/r {Name},\nIhre Rate über {amount} war am {dueDate} fällig. Bitte überweisen Sie umgehend.",
                TemplateType.Kuendigung => "Kündigung: {Creditnumber}\n\nSehr geehrte/r {Name},\nda die Summe von {amount} zum {dueDate} nicht beglichen wurde, kündigen wir den Kredit.",
                _ => "{Creditnumber}: Hallo {Name}, Betrag {amount} ist fällig am {dueDate}."
            };
        }
    }
}
