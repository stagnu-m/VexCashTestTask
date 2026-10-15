using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using VexCash.SmsService.Models;

namespace VexCash.SmsService.Dtos
{
    public class MessageTemplateDto
    {
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public TemplateType Type { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }

    public class UpdateTemplateDto
    {
        [Required]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}
