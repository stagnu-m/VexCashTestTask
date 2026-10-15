namespace VexCash.SmsService.Models
{
    /// <summary>
    /// Audit record of an outbound SMS notification attempt.
    /// </summary>
    public class SmsLog: EntityModel
    {
        public int? CustomerId { get; set; }

        public string RecipientPhoneNumber { get; set; } = string.Empty;

        public string RecipientName { get; set; } = string.Empty;

        public string CreditNumber { get; set; } = string.Empty;

        public TemplateType TemplateType { get; set; }

        public string RenderedMessage { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Delivery status: "Sent", "Simulated", "Failed".
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Details about status per MessageBird docs (e.g. successfully delivered, pending DLR).
        /// </summary>
        public string? StatusReason { get; set; }

        /// <summary>
        /// Message identifier assigned by MessageBird (if available).
        /// </summary>
        public string? MessageBirdId { get; set; }

        /// <summary>
        /// Error message if the API request failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
