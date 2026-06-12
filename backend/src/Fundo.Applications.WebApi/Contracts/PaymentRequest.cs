using System.ComponentModel.DataAnnotations;

namespace Fundo.Applications.WebApi.Contracts
{
    public class PaymentRequest
    {
        [Range(0.01, 1_000_000_000, ErrorMessage = "Payment amount must be greater than zero.")]
        public decimal Amount { get; init; }
    }
}
