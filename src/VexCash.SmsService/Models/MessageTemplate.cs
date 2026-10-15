using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VexCash.SmsService.Models
{
    /// <summary>
    /// SMS message template with placeholders for customer-specific data.
    /// </summary>
    public class MessageTemplate : EntityModel
    {
        /// <summary>
        /// Category of the template (Erinnerung, Mahnung, Kuendigung).
        /// </summary>
        [Required]
        [JsonPropertyName("type")]
        public TemplateType Type { get; set; }

        /// <summary>
        /// Short human-readable title or label for the template.
        /// </summary>
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Template text containing placeholders such as {Creditnumber}, {Name}, {dueDate}, {amount}.
        /// </summary>
        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of last modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
