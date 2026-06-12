using FluentAssertions;
using Fundo.Applications.WebApi.Contracts;
using Fundo.Applications.WebApi.Data;
using Fundo.Applications.WebApi.Domain;
using Fundo.Applications.WebApi.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Fundo.Services.Tests.Unit
{
    /// <summary>
    /// Unit tests for the loan business rules. Each test gets its own
    /// in-memory SQLite database, so tests are fully isolated.
    /// </summary>
    public class LoanServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly LoanManagementDbContext _dbContext;
        private readonly LoanService _service;

        public LoanServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<LoanManagementDbContext>()
                .UseSqlite(_connection)
                .Options;

            _dbContext = new LoanManagementDbContext(options);
            _dbContext.Database.EnsureCreated();

            _service = new LoanService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            _connection.Dispose();
        }

        private async Task<Loan> AddLoanAsync(decimal amount, decimal currentBalance, LoanStatus status)
        {
            var loan = new Loan
            {
                Amount = amount,
                CurrentBalance = currentBalance,
                ApplicantName = "Test Applicant",
                Status = status
            };

            _dbContext.Loans.Add(loan);
            await _dbContext.SaveChangesAsync();

            return loan;
        }

        [Fact]
        public async Task CreateAsync_ShouldStartLoanWithFullBalanceAndActiveStatus()
        {
            var request = new CreateLoanRequest { Amount = 2000.00m, ApplicantName = "  Maria Silva  " };

            var result = await _service.CreateAsync(request);

            result.Amount.Should().Be(2000.00m);
            result.CurrentBalance.Should().Be(2000.00m);
            result.ApplicantName.Should().Be("Maria Silva");
            result.Status.Should().Be(LoanStatus.Active);
            result.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetByIdAsync_WithExistingLoan_ShouldReturnIt()
        {
            var loan = await AddLoanAsync(1000m, 600m, LoanStatus.Active);

            var result = await _service.GetByIdAsync(loan.Id);

            result.Should().NotBeNull();
            result!.CurrentBalance.Should().Be(600m);
            result.ApplicantName.Should().Be("Test Applicant");
        }

        [Fact]
        public async Task GetByIdAsync_WithUnknownLoan_ShouldReturnNull()
        {
            var result = await _service.GetByIdAsync(9999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnLoansOrderedById()
        {
            var created = await AddLoanAsync(1000m, 1000m, LoanStatus.Active);

            var result = await _service.GetAllAsync();

            result.Should().NotBeEmpty();
            result.Should().BeInAscendingOrder(loan => loan.Id);
            result.Should().Contain(loan => loan.Id == created.Id);
        }

        [Fact]
        public async Task MakePaymentAsync_WithPartialPayment_ShouldDeductBalanceAndStayActive()
        {
            var loan = await AddLoanAsync(1000m, 1000m, LoanStatus.Active);

            var result = await _service.MakePaymentAsync(loan.Id, 400m);

            result.Outcome.Should().Be(PaymentOutcome.Success);
            result.Loan!.CurrentBalance.Should().Be(600m);
            result.Loan.Status.Should().Be(LoanStatus.Active);
        }

        [Fact]
        public async Task MakePaymentAsync_WithFullPayment_ShouldMarkLoanAsPaid()
        {
            var loan = await AddLoanAsync(1000m, 1000m, LoanStatus.Active);

            var result = await _service.MakePaymentAsync(loan.Id, 1000m);

            result.Outcome.Should().Be(PaymentOutcome.Success);
            result.Loan!.CurrentBalance.Should().Be(0m);
            result.Loan.Status.Should().Be(LoanStatus.Paid);
        }

        [Fact]
        public async Task MakePaymentAsync_ExceedingBalance_ShouldBeRejected()
        {
            var loan = await AddLoanAsync(1000m, 300m, LoanStatus.Active);

            var result = await _service.MakePaymentAsync(loan.Id, 500m);

            result.Outcome.Should().Be(PaymentOutcome.InvalidPayment);
            result.ErrorMessage.Should().Contain("exceeds the current balance");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-50)]
        public async Task MakePaymentAsync_WithNonPositiveAmount_ShouldBeRejected(decimal amount)
        {
            var loan = await AddLoanAsync(1000m, 1000m, LoanStatus.Active);

            var result = await _service.MakePaymentAsync(loan.Id, amount);

            result.Outcome.Should().Be(PaymentOutcome.InvalidPayment);
        }

        [Fact]
        public async Task MakePaymentAsync_OnPaidLoan_ShouldBeRejected()
        {
            var loan = await AddLoanAsync(1000m, 0m, LoanStatus.Paid);

            var result = await _service.MakePaymentAsync(loan.Id, 100m);

            result.Outcome.Should().Be(PaymentOutcome.InvalidPayment);
            result.ErrorMessage.Should().Contain("already fully paid");
        }

        [Fact]
        public async Task MakePaymentAsync_OnUnknownLoan_ShouldReturnLoanNotFound()
        {
            var result = await _service.MakePaymentAsync(9999, 100m);

            result.Outcome.Should().Be(PaymentOutcome.LoanNotFound);
        }

        [Fact]
        public async Task MakePaymentAsync_ShouldPersistChanges()
        {
            var loan = await AddLoanAsync(1000m, 1000m, LoanStatus.Active);

            await _service.MakePaymentAsync(loan.Id, 250m);

            var persisted = await _dbContext.Loans.AsNoTracking().SingleAsync(l => l.Id == loan.Id);
            persisted.CurrentBalance.Should().Be(750m);
        }
    }
}
