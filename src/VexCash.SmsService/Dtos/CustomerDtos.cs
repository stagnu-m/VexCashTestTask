using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VexCash.SmsService.Dtos
{
    public class CustomerDto
    {
        public int Id { get; set; }

        [JsonPropertyName("phonenr")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("creditnumber")]
        public string CreditNumber { get; set; } = string.Empty;

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }

        public bool IsOverdue => DueDate < DateTime.UtcNow.Date;
    }

    public class CreateCustomerDto
    {
        [Required]
        [JsonPropertyName("phonenr")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("creditnumber")]
        public string CreditNumber { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 1000000)]
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [Required]
        [JsonPropertyName("dueDate")]
        public DateTime DueDate { get; set; }
    }

    public class UpdateCustomerDto : CreateCustomerDto
    {
    }
}
