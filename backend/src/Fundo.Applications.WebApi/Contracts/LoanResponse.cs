using Fundo.Applications.WebApi.Domain;

namespace Fundo.Applications.WebApi.Contracts
{
    public record LoanResponse(
        int Id,
        decimal Amount,
        decimal CurrentBalance,
        string ApplicantName,
        LoanStatus Status);
}
