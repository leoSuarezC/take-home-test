using Fundo.Applications.WebApi.Contracts;
using Fundo.Applications.WebApi.Data;
using Fundo.Applications.WebApi.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fundo.Applications.WebApi.Services
{
    public class LoanService : ILoanService
    {
        private readonly LoanManagementDbContext _dbContext;
        private readonly ILogger<LoanService> _logger;

        public LoanService(LoanManagementDbContext dbContext, ILogger<LoanService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<IReadOnlyList<LoanResponse>> GetAllAsync()
        {
            return await _dbContext.Loans
                .AsNoTracking()
                .OrderBy(loan => loan.Id)
                .Select(loan => ToResponse(loan))
                .ToListAsync();
        }

        public async Task<LoanResponse?> GetByIdAsync(int id)
        {
            var loan = await _dbContext.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);

            return loan is null ? null : ToResponse(loan);
        }

        public async Task<LoanResponse> CreateAsync(CreateLoanRequest request)
        {
            var loan = new Loan
            {
                Amount = request.Amount,
                CurrentBalance = request.Amount,
                ApplicantName = request.ApplicantName.Trim(),
                Status = LoanStatus.Active
            };

            _dbContext.Loans.Add(loan);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Loan {LoanId} created for {ApplicantName} with amount {Amount}",
                loan.Id, loan.ApplicantName, loan.Amount);

            return ToResponse(loan);
        }

        public async Task<PaymentResult> MakePaymentAsync(int id, decimal amount)
        {
            var loan = await _dbContext.Loans.FirstOrDefaultAsync(l => l.Id == id);

            if (loan is null)
            {
                _logger.LogWarning("Payment rejected: loan {LoanId} not found", id);
                return PaymentResult.LoanNotFound();
            }

            if (loan.Status == LoanStatus.Paid)
            {
                _logger.LogWarning("Payment rejected for loan {LoanId}: loan is already fully paid", id);
                return PaymentResult.InvalidPayment("Loan is already fully paid.");
            }

            if (amount <= 0)
            {
                _logger.LogWarning(
                    "Payment rejected for loan {LoanId}: non-positive amount {PaymentAmount}", id, amount);
                return PaymentResult.InvalidPayment("Payment amount must be greater than zero.");
            }

            if (amount > loan.CurrentBalance)
            {
                _logger.LogWarning(
                    "Payment rejected for loan {LoanId}: amount {PaymentAmount} exceeds balance {CurrentBalance}",
                    id, amount, loan.CurrentBalance);
                return PaymentResult.InvalidPayment(
                    $"Payment amount ({amount}) exceeds the current balance ({loan.CurrentBalance}).");
            }

            loan.CurrentBalance -= amount;

            if (loan.CurrentBalance == 0)
            {
                loan.Status = LoanStatus.Paid;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Payment of {PaymentAmount} applied to loan {LoanId}; new balance {CurrentBalance}, status {Status}",
                amount, loan.Id, loan.CurrentBalance, loan.Status);

            return PaymentResult.Success(ToResponse(loan));
        }

        private static LoanResponse ToResponse(Loan loan) =>
            new(loan.Id, loan.Amount, loan.CurrentBalance, loan.ApplicantName, loan.Status);
    }
}
