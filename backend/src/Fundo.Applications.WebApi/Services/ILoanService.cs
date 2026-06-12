using Fundo.Applications.WebApi.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fundo.Applications.WebApi.Services
{
    public interface ILoanService
    {
        Task<IReadOnlyList<LoanResponse>> GetAllAsync();

        Task<LoanResponse?> GetByIdAsync(int id);

        Task<LoanResponse> CreateAsync(CreateLoanRequest request);

        Task<PaymentResult> MakePaymentAsync(int id, decimal amount);
    }
}
