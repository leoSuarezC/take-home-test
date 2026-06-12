using Fundo.Applications.WebApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace Fundo.Applications.WebApi.Data
{
    public class LoanManagementDbContext : DbContext
    {
        public LoanManagementDbContext(DbContextOptions<LoanManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<Loan> Loans => Set<Loan>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Loan>(loan =>
            {
                loan.Property(l => l.Amount).HasPrecision(18, 2);
                loan.Property(l => l.CurrentBalance).HasPrecision(18, 2);
                loan.Property(l => l.ApplicantName).IsRequired().HasMaxLength(100);
                loan.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

                loan.HasData(
                    new Loan { Id = 1, Amount = 25000.00m, CurrentBalance = 18750.00m, ApplicantName = "John Doe", Status = LoanStatus.Active },
                    new Loan { Id = 2, Amount = 15000.00m, CurrentBalance = 0.00m, ApplicantName = "Jane Smith", Status = LoanStatus.Paid },
                    new Loan { Id = 3, Amount = 50000.00m, CurrentBalance = 32500.00m, ApplicantName = "Robert Johnson", Status = LoanStatus.Active },
                    new Loan { Id = 4, Amount = 1500.00m, CurrentBalance = 500.00m, ApplicantName = "Maria Silva", Status = LoanStatus.Active },
                    new Loan { Id = 5, Amount = 8000.00m, CurrentBalance = 0.00m, ApplicantName = "Carlos Mendes", Status = LoanStatus.Paid });
            });
        }
    }
}
