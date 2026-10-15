using Microsoft.AspNetCore.Mvc;
using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;
using VexCash.SmsService.Services;

namespace VexCash.SmsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SmsController : ControllerBase
    {
        private readonly ISmsDispatchService _smsDispatchService;
        private readonly ISmsLogRepository _smsLogRepository;

        public SmsController(ISmsDispatchService smsDispatchService, ISmsLogRepository smsLogRepository)
        {
            _smsDispatchService = smsDispatchService;
            _smsLogRepository = smsLogRepository;
        }

        /// <summary>
        /// Previews what the rendered SMS text will look like for a given customer and template,
        /// without sending any SMS.
        /// </summary>
        [HttpPost("preview")]
        [ProducesResponseType(typeof(SmsPreviewResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SmsPreviewResponseDto>> Preview([FromBody] SmsPreviewRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var preview = await _smsDispatchService.PreviewSmsAsync(request, HttpContext.RequestAborted);
                return Ok(preview);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sends an SMS notification to a specific customer using the selected template (Erinnerung, Mahnung, Kuendigung)
        /// via the MessageBird API.
        /// </summary>
        [HttpPost("send")]
        [ProducesResponseType(typeof(SmsSendResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SmsSendResultDto>> Send([FromBody] SendSmsRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _smsDispatchService.SendSmsAsync(request, HttpContext.RequestAborted);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Sends bulk SMS notifications. If customerIds is not specified, sends to all customers whose dueDate is past.
        /// </summary>
        [HttpPost("send-bulk")]
        [ProducesResponseType(typeof(BulkSendSmsResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BulkSendSmsResultDto>> SendBulk([FromBody] BulkSendSmsRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _smsDispatchService.BulkSendSmsAsync(request, HttpContext.RequestAborted);
            return Ok(result);
        }

        /// <summary>
        /// Retrieves the audit history of all SMS dispatch attempts.
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<SmsLog>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<SmsLog>>> GetHistory(
            [FromQuery] int? customerId = null,
            [FromQuery] TemplateType? templateType = null)
        {
            var logs = await _smsLogRepository.GetHistoryAsync(customerId, templateType, 100, HttpContext.RequestAborted);
            return Ok(logs);
        }
    }
}
