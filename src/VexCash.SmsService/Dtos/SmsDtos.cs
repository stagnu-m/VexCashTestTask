using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Dtos
{
    public class SendSmsRequestDto
    {
        [Required]
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [Required]
        [JsonPropertyName("templateType")]
        public TemplateType TemplateType { get; set; } = TemplateType.Erinnerung;

        /// <summary>
        /// Optional: provide custom message text instead of using stored template.
        /// Placeholders {Creditnumber}, {Name}, {dueDate}, {amount} will still be replaced.
        /// </summary>
        [JsonPropertyName("customTemplate")]
        public string? CustomTemplate { get; set; }
    }

    public class BulkSendSmsRequestDto
    {
        [Required]
        [JsonPropertyName("templateType")]
        public TemplateType TemplateType { get; set; } = TemplateType.Mahnung;

        /// <summary>
        /// Optional: List of customer IDs to send to.
        /// If null or empty, will target all overdue customers (dueDate &lt; today).
        /// </summary>
        [JsonPropertyName("customerIds")]
        public List<int>? CustomerIds { get; set; }

        /// <summary>
        /// If true, only customers whose dueDate has already passed will be messaged.
        /// Default is false when explicit customerIds are passed, true when customerIds is omitted.
        /// </summary>
        [JsonPropertyName("onlyOverdue")]
        public bool? OnlyOverdue { get; set; }
    }

    public class SmsPreviewRequestDto
    {
        [Required]
        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        [Required]
        [JsonPropertyName("templateType")]
        public TemplateType TemplateType { get; set; } = TemplateType.Erinnerung;

        [JsonPropertyName("customTemplate")]
        public string? CustomTemplate { get; set; }
    }

    public class SmsPreviewResponseDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string CreditNumber { get; set; } = string.Empty;
        public TemplateType TemplateType { get; set; }
        public string RenderedMessage { get; set; } = string.Empty;
        public int CharacterCount => RenderedMessage.Length;
    }

    public class SmsSendResultDto
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string RecipientPhoneNumber { get; set; } = string.Empty;
        public TemplateType TemplateType { get; set; }
        public string RenderedMessage { get; set; } = string.Empty;
        public string? MessageBirdId { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class BulkSendSmsResultDto
    {
        public int TotalProcessed { get; set; }
        public int SucceededCount { get; set; }
        public int FailedCount { get; set; }
        public List<SmsSendResultDto> Results { get; set; } = new List<SmsSendResultDto>();
    }
}
