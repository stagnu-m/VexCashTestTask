using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VexCash.SmsService.Models
{
    /// <summary>
    /// Customer entity storing credit agreement and contact details.
    /// </summary>
    public class Customer : EntityModel
    {
        /// <summary>
        /// Recipient phone number in international E.164 format (e.g. +491701234567).
        /// </summary>
        [Required]
        [JsonPropertyName("phonenr")]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>
        /// Customer full name (e.g. Max Mustermann).
        /// </summary>
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Unique credit / loan contract identifier.
        /// </summary>
        [Required]
        [JsonPropertyName("creditnumber")]
        public string CreditNumber { get; set; } = string.Empty;

        /// <summary>
        /// Outstanding loan or installment amount.
        /// </summary>
        [Required]
        [Range(0.01, 1000000)]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Due date for repayment.
        /// </summary>
        [Required]
        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Date when the customer record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
