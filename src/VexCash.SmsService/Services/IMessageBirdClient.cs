using System;
using System.Threading;
using System.Threading.Tasks;

namespace VexCash.SmsService.Services
{
    public class MessageBirdSendResult
    {
        public bool Success { get; set; }
        public string? MessageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public int? StatusErrorCode { get; set; }
        public DateTime? StatusDatetime { get; set; }
        public string? ErrorMessage { get; set; }

        public MessageBirdSendResult()
        {
        }

        public MessageBirdSendResult(
            bool success,
            string? messageId,
            string status,
            string? errorMessage = null,
            string? statusReason = null,
            int? statusErrorCode = null,
            DateTime? statusDatetime = null)
        {
            Success = success;
            MessageId = messageId;
            Status = status;
            ErrorMessage = errorMessage;
            StatusReason = statusReason;
            StatusErrorCode = statusErrorCode;
            StatusDatetime = statusDatetime;
        }
    }

    public interface IMessageBirdClient
    {
        Task<MessageBirdSendResult> SendSmsAsync(
            string recipientPhoneNumber,
            string messageBody,
            string? originator = null,
            CancellationToken cancellationToken = default);
    }
}
