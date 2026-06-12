using FluentAssertions;
using Fundo.Applications.WebApi.Contracts;
using Fundo.Applications.WebApi.Domain;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Fundo.Services.Tests.Integration
{
    public class LoanManagementControllerTests : IClassFixture<TestWebApplicationFactory>
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private readonly HttpClient _client;

        public LoanManagementControllerTests(TestWebApplicationFactory factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
        }

        private async Task<LoanResponse> CreateLoanAsync(decimal amount, string applicantName = "Integration Test")
        {
            var response = await _client.PostAsJsonAsync("/loans", new { amount, applicantName });
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions))!;
        }

        [Fact]
        public async Task GetAll_ShouldReturnSeededLoans()
        {
            var response = await _client.GetAsync("/loans");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var loans = await response.Content.ReadFromJsonAsync<List<LoanResponse>>(JsonOptions);
            loans.Should().NotBeNull();
            loans!.Should().Contain(loan => loan.ApplicantName == "Maria Silva");
        }

        [Fact]
        public async Task GetById_WithExistingLoan_ShouldReturnLoan()
        {
            var response = await _client.GetAsync("/loans/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var loan = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
            loan!.Id.Should().Be(1);
            loan.ApplicantName.Should().Be("John Doe");
        }

        [Fact]
        public async Task GetById_WithUnknownLoan_ShouldReturnNotFound()
        {
            var response = await _client.GetAsync("/loans/99999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Create_WithValidRequest_ShouldReturnCreatedLoan()
        {
            var response = await _client.PostAsJsonAsync("/loans", new { amount = 3000.00m, applicantName = "Ana Torres" });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            var loan = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
            loan!.Amount.Should().Be(3000.00m);
            loan.CurrentBalance.Should().Be(3000.00m);
            loan.Status.Should().Be(LoanStatus.Active);

            var fetched = await _client.GetAsync(response.Headers.Location);
            fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(-100, "Ana Torres")]
        [InlineData(0, "Ana Torres")]
        [InlineData(1000, "")]
        public async Task Create_WithInvalidRequest_ShouldReturnBadRequest(decimal amount, string applicantName)
        {
            var response = await _client.PostAsJsonAsync("/loans", new { amount, applicantName });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Payment_WithPartialAmount_ShouldDeductBalance()
        {
            var loan = await CreateLoanAsync(1000m);

            var response = await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 400m });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updated = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
            updated!.CurrentBalance.Should().Be(600m);
            updated.Status.Should().Be(LoanStatus.Active);
        }

        [Fact]
        public async Task Payment_CoveringFullBalance_ShouldMarkLoanAsPaid()
        {
            var loan = await CreateLoanAsync(1000m);

            var response = await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 1000m });

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var updated = await response.Content.ReadFromJsonAsync<LoanResponse>(JsonOptions);
            updated!.CurrentBalance.Should().Be(0m);
            updated.Status.Should().Be(LoanStatus.Paid);
        }

        [Fact]
        public async Task Payment_ExceedingBalance_ShouldReturnBadRequest()
        {
            var loan = await CreateLoanAsync(1000m);

            var response = await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 5000m });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Payment_OnPaidLoan_ShouldReturnBadRequest()
        {
            var loan = await CreateLoanAsync(500m);
            await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 500m });

            var response = await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 100m });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Payment_WithNonPositiveAmount_ShouldReturnBadRequest()
        {
            var loan = await CreateLoanAsync(1000m);

            var response = await _client.PostAsJsonAsync($"/loans/{loan.Id}/payment", new { amount = 0m });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Payment_OnUnknownLoan_ShouldReturnNotFound()
        {
            var response = await _client.PostAsJsonAsync("/loans/99999/payment", new { amount = 100m });

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
