using System.ComponentModel.DataAnnotations;

namespace Fundo.Applications.WebApi.Contracts
{
    public class CreateLoanRequest
    {
        [Range(0.01, 1_000_000_000, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; init; }

        [Required(AllowEmptyStrings = false, ErrorMessage = "Applicant name is required.")]
        [MaxLength(100)]
        public string ApplicantName { get; init; } = string.Empty;
    }
}
