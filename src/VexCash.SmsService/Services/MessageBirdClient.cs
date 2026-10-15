using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using VexCash.SmsService.Services.Options;

namespace VexCash.SmsService.Services
{
    public class MessageBirdClient : IMessageBirdClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly HttpClient _httpClient;
        private readonly MessageBirdOptions _options;
        private readonly ILogger<MessageBirdClient> _logger;

        public MessageBirdClient(
            HttpClient httpClient,
            IOptions<MessageBirdOptions> options,
            ILogger<MessageBirdClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<MessageBirdSendResult> SendSmsAsync(
            string recipientPhoneNumber,
            string messageBody,
            string? originator = null,
            CancellationToken cancellationToken = default)
        {
            var from = string.IsNullOrWhiteSpace(originator) ? _options.Originator : originator;

            if (IsSimulationMode())
            {
                var simulatedId = $"sim_{Guid.NewGuid():N}";
                _logger.LogInformation(
                    "[SIMULATION MODE] MessageBird SMS to {Recipient} from '{Originator}'. Generated Id: {Id}. Content:\n{Body}",
                    recipientPhoneNumber, from, simulatedId, messageBody);

                return new MessageBirdSendResult(
                    success: true,
                    messageId: simulatedId,
                    status: "Simulated",
                    errorMessage: null,
                    statusReason: "successfully delivered (simulated)",
                    statusErrorCode: null,
                    statusDatetime: DateTime.UtcNow
                );
            }

            try
            {
                var requestUrl = "https://rest.messagebird.com/messages";
                using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("AccessKey", _options.ApiKey);

                // Per MessageBird API documentation (send_sms.md):
                // 'recipient' is defined as the MSISDN integer of the recipient (e.g. 31612345678).
                // The 'recipients' array in the request payload contains only the necessary recipient number(s).
                var recipientMsisdn = NormalizeToMsisdn(recipientPhoneNumber);

                var payload = new
                {
                    originator = from,
                    recipients = new object[] { recipientMsisdn },
                    body = messageBody
                };

                var json = JsonSerializer.Serialize(payload, SerializerOptions);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation(
                    "Sending SMS via MessageBird to {Recipient} (Originator: {Originator})...",
                    recipientMsisdn, from);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var successObj = JsonSerializer.Deserialize<MessageBirdSuccessResponse>(responseContent);
                    var id = successObj?.Id ?? Guid.NewGuid().ToString("N");
                    var firstItem = successObj?.Recipients?.Items?.FirstOrDefault();

                    var status = firstItem?.Status ?? "sent";
                    var statusReason = firstItem?.StatusReason;
                    var statusErrorCode = firstItem?.StatusErrorCode;
                    var statusDatetime = firstItem?.StatusDatetime;

                    _logger.LogInformation(
                        "MessageBird SMS successfully sent. Id: {Id}, Status: {Status}, Reason: {Reason}",
                        id, status, statusReason ?? "N/A");

                    return new MessageBirdSendResult(
                        success: true,
                        messageId: id,
                        status: status,
                        errorMessage: null,
                        statusReason: statusReason,
                        statusErrorCode: statusErrorCode,
                        statusDatetime: statusDatetime);
                }
                else
                {
                    var errorMsg = ExtractErrorMessage(responseContent, (int)response.StatusCode);
                    _logger.LogWarning("MessageBird API returned status {StatusCode}: {Error}", response.StatusCode, errorMsg);
                    return new MessageBirdSendResult(false, null, "Failed", errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception while sending SMS via MessageBird to {Recipient}", recipientPhoneNumber);
                return new MessageBirdSendResult(false, null, "Failed", ex.Message);
            }
        }

        /// <summary>
        /// Normalizes phone number into standard MSISDN integer format per MessageBird documentation
        /// (removes '+', spaces, dashes, and leading '00').
        /// Returns long integer if valid, or fallback clean string.
        /// </summary>
        public static object NormalizeToMsisdn(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return string.Empty;
            }

            var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");
            if (digitsOnly.StartsWith("00"))
            {
                digitsOnly = digitsOnly.Substring(2);
            }

            if (long.TryParse(digitsOnly, out var msisdnInt))
            {
                return msisdnInt;
            }

            return digitsOnly;
        }

        private bool IsSimulationMode()
        {
            if (_options.SimulationMode)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                return true;
            }

            var key = _options.ApiKey.Trim();
            if (key.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("YOUR_API_KEY", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("placeholder", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static string ExtractErrorMessage(string responseContent, int statusCode)
        {
            try
            {
                var errObj = JsonSerializer.Deserialize<MessageBirdErrorResponse>(responseContent);
                if (errObj?.Errors != null && errObj.Errors.Count > 0)
                {
                    var descriptions = errObj.Errors.Select(e => $"[{e.Code}] {e.Description}");
                    return string.Join("; ", descriptions);
                }
            }
            catch
            {
                // Fallback if not valid JSON
            }

            return string.IsNullOrWhiteSpace(responseContent)
                ? $"HTTP request failed with status code {statusCode}"
                : responseContent;
        }

        #region Response Models (per send_sms.md documentation)

        /// <summary>
        /// Message object returned by MessageBird API.
        /// Includes only necessary fields from the recipients container.
        /// </summary>
        private class MessageBirdSuccessResponse
        {
            [JsonPropertyName("id")]
            public string? Id { get; set; }

            [JsonPropertyName("recipients")]
            public MessageBirdRecipientsContainer? Recipients { get; set; }
        }

        /// <summary>
        /// The recipients container from Message Object Example.
        /// Contains only the essential counts and the items array.
        /// </summary>
        private class MessageBirdRecipientsContainer
        {
            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }

            [JsonPropertyName("totalSentCount")]
            public int TotalSentCount { get; set; }

            [JsonPropertyName("items")]
            public List<MessageBirdRecipientItem>? Items { get; set; }
        }

        /// <summary>
        /// Item from "The items array" in send_sms.md.
        /// Retains only the necessary attributes (recipient, status, statusReason, statusErrorCode, statusDatetime)
        /// and intentionally omits non-essential metadata (country, operator, mcc/mnc, pricing, etc.).
        /// </summary>
        private class MessageBirdRecipientItem
        {
            /// <summary>
            /// The MSISDN integer of the recipient (e.g. 31612345678).
            /// </summary>
            [JsonPropertyName("recipient")]
            public long? Recipient { get; set; }

            /// <summary>
            /// Status: scheduled, sent, buffered, delivered, expired, delivery_failed.
            /// </summary>
            [JsonPropertyName("status")]
            public string? Status { get; set; }

            /// <summary>
            /// Details: successfully delivered, pending DLR, etc.
            /// </summary>
            [JsonPropertyName("statusReason")]
            public string? StatusReason { get; set; }

            /// <summary>
            /// Extra error code describing failure in detail (null if none).
            /// </summary>
            [JsonPropertyName("statusErrorCode")]
            public int? StatusErrorCode { get; set; }

            /// <summary>
            /// Date and time of last status in RFC3339 format.
            /// </summary>
            [JsonPropertyName("statusDatetime")]
            public DateTime? StatusDatetime { get; set; }
        }

        private class MessageBirdErrorResponse
        {
            [JsonPropertyName("errors")]
            public List<MessageBirdErrorItem>? Errors { get; set; }
        }

        private class MessageBirdErrorItem
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("parameter")]
            public string? Parameter { get; set; }
        }

        #endregion
    }
}
