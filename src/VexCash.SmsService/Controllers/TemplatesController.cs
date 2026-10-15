using Microsoft.AspNetCore.Mvc;
using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;

namespace VexCash.SmsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TemplatesController : ControllerBase
    {
        private readonly ITemplateRepository _templateRepository;

        public TemplatesController(ITemplateRepository templateRepository)
        {
            _templateRepository = templateRepository;
        }

        /// <summary>
        /// Retrieves all message templates (e.g. Erinnerung, Mahnung, Kündigung).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<MessageTemplateDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<MessageTemplateDto>>> GetAll()
        {
            var templates = await _templateRepository.GetAllAsync(HttpContext.RequestAborted);

            var dtos = templates.Select(t => new MessageTemplateDto
            {
                Id = t.Id,
                Type = t.Type,
                Title = t.Title,
                Content = t.Content,
                UpdatedAt = t.UpdatedAt
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a template by its category/type (Erinnerung, Mahnung, Kuendigung).
        /// </summary>
        [HttpGet("{type}")]
        [ProducesResponseType(typeof(MessageTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageTemplateDto>> GetByType(TemplateType type)
        {
            var template = await _templateRepository.GetByTypeAsync(type, HttpContext.RequestAborted);
            if (template == null)
            {
                return NotFound(new { message = $"Template for type '{type}' was not found." });
            }

            return Ok(new MessageTemplateDto
            {
                Id = template.Id,
                Type = template.Type,
                Title = template.Title,
                Content = template.Content,
                UpdatedAt = template.UpdatedAt
            });
        }

        /// <summary>
        /// Updates the text and title of a template for a given category.
        /// </summary>
        [HttpPut("{type}")]
        [ProducesResponseType(typeof(MessageTemplateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MessageTemplateDto>> UpdateByType(TemplateType type, [FromBody] UpdateTemplateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var template = await _templateRepository.AddOrUpdateAsync(type, dto.Title, dto.Content, HttpContext.RequestAborted);

            return Ok(new MessageTemplateDto
            {
                Id = template.Id,
                Type = template.Type,
                Title = template.Title,
                Content = template.Content,
                UpdatedAt = template.UpdatedAt
            });
        }
    }
}
