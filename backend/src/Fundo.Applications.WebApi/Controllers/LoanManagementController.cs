using Fundo.Applications.WebApi.Contracts;
using Fundo.Applications.WebApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fundo.Applications.WebApi.Controllers
{
    [ApiController]
    [Route("loans")]
    public class LoanManagementController : ControllerBase
    {
        private readonly ILoanService _loanService;

        public LoanManagementController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<LoanResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<LoanResponse>>> GetAll()
        {
            return Ok(await _loanService.GetAllAsync());
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LoanResponse>> GetById(int id)
        {
            var loan = await _loanService.GetByIdAsync(id);

            return loan is null ? NotFound() : Ok(loan);
        }

        [HttpPost]
        [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<LoanResponse>> Create([FromBody] CreateLoanRequest request)
        {
            var loan = await _loanService.CreateAsync(request);

            return CreatedAtAction(nameof(GetById), new { id = loan.Id }, loan);
        }

        [HttpPost("{id:int}/payment")]
        [ProducesResponseType(typeof(LoanResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<LoanResponse>> MakePayment(int id, [FromBody] PaymentRequest request)
        {
            var result = await _loanService.MakePaymentAsync(id, request.Amount);

            return result.Outcome switch
            {
                PaymentOutcome.Success => Ok(result.Loan),
                PaymentOutcome.LoanNotFound => NotFound(),
                _ => Problem(statusCode: StatusCodes.Status400BadRequest, detail: result.ErrorMessage)
            };
        }
    }
}
