using System.Text.Json.Serialization;

namespace VexCash.SmsService.Models
{
    /// <summary>
    /// Supported message template types for customer communications.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TemplateType
    {
        Erinnerung = 1,
        Mahnung = 2,
        Kuendigung = 3
    }
}
