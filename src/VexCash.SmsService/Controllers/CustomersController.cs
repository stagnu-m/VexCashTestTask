using Microsoft.AspNetCore.Mvc;
using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using VexCash.SmsService.Repositories;

namespace VexCash.SmsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomersController(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        /// <summary>
        /// Retrieves all customers from the repository.
        /// </summary>
        /// <param name="overdueOnly">Optional filter to retrieve only overdue customers.</param>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll([FromQuery] bool? overdueOnly = null)
        {
            var customers = await _customerRepository.GetAllAsync(overdueOnly, HttpContext.RequestAborted);

            var dtos = customers.Select(c => new CustomerDto
            {
                Id = c.Id,
                PhoneNumber = c.PhoneNumber,
                Name = c.Name,
                CreditNumber = c.CreditNumber,
                Amount = c.Amount,
                DueDate = c.DueDate
            });

            return Ok(dtos);
        }

        /// <summary>
        /// Retrieves a single customer by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            return Ok(new CustomerDto
            {
                Id = customer.Id,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                CreditNumber = customer.CreditNumber,
                Amount = customer.Amount,
                DueDate = customer.DueDate
            });
        }

        /// <summary>
        /// Creates a new customer record.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = new Customer
            {
                PhoneNumber = dto.PhoneNumber.Trim(),
                Name = dto.Name.Trim(),
                CreditNumber = dto.CreditNumber.Trim(),
                Amount = dto.Amount,
                DueDate = dto.DueDate,
                CreatedAt = DateTime.UtcNow
            };

            await _customerRepository.AddAsync(customer, HttpContext.RequestAborted);

            var resultDto = new CustomerDto
            {
                Id = customer.Id,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                CreditNumber = customer.CreditNumber,
                Amount = customer.Amount,
                DueDate = customer.DueDate
            };

            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, resultDto);
        }

        /// <summary>
        /// Updates an existing customer record.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var customer = await _customerRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            customer.PhoneNumber = dto.PhoneNumber.Trim();
            customer.Name = dto.Name.Trim();
            customer.CreditNumber = dto.CreditNumber.Trim();
            customer.Amount = dto.Amount;
            customer.DueDate = dto.DueDate;

            await _customerRepository.UpdateAsync(customer, HttpContext.RequestAborted);

            return Ok(new CustomerDto
            {
                Id = customer.Id,
                PhoneNumber = customer.PhoneNumber,
                Name = customer.Name,
                CreditNumber = customer.CreditNumber,
                Amount = customer.Amount,
                DueDate = customer.DueDate
            });
        }

        /// <summary>
        /// Deletes a customer by ID.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _customerRepository.GetByIdAsync(id, HttpContext.RequestAborted);
            if (customer == null)
            {
                return NotFound(new { message = $"Customer with ID {id} was not found." });
            }

            await _customerRepository.DeleteAsync(customer, HttpContext.RequestAborted);

            return NoContent();
        }
    }
}
