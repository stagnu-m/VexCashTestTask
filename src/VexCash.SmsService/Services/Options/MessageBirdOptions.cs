namespace VexCash.SmsService.Services.Options
{
    public class MessageBirdOptions
    {
        public const string SectionName = "MessageBird";

        /// <summary>
        /// MessageBird Access Key (Live or Test key from MessageBird dashboard).
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Sender ID (alphanumeric string max 11 chars or an E.164 phone number).
        /// </summary>
        public string Originator { get; set; } = "VexCash";

        /// <summary>
        /// When true, API calls will simulate SMS sending without contacting MessageBird servers.
        /// Automatically enabled if ApiKey is blank or has dummy default value.
        /// </summary>
        public bool SimulationMode { get; set; } = false;
    }
}
