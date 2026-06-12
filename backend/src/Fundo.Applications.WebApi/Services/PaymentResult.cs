using Fundo.Applications.WebApi.Contracts;

namespace Fundo.Applications.WebApi.Services
{
    public enum PaymentOutcome
    {
        Success,
        LoanNotFound,
        InvalidPayment
    }

    public record PaymentResult(PaymentOutcome Outcome, LoanResponse? Loan = null, string? ErrorMessage = null)
    {
        public static PaymentResult Success(LoanResponse loan) => new(PaymentOutcome.Success, loan);

        public static PaymentResult LoanNotFound() => new(PaymentOutcome.LoanNotFound);

        public static PaymentResult InvalidPayment(string message) => new(PaymentOutcome.InvalidPayment, ErrorMessage: message);
    }
}
