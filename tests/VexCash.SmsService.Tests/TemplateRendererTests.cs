using FluentAssertions;
using VexCash.SmsService.Models;
using VexCash.SmsService.Services;
using Xunit;

namespace VexCash.SmsService.Tests
{
    public class TemplateRendererTests
    {
        private readonly TemplateRenderer _renderer = new TemplateRenderer();

        [Fact]
        public void Render_ReplacesAllPlaceholdersCorrectly()
        {
            // Arrange
            var customer = new Customer
            {
                Id = 1,
                Name = "Max Mustermann",
                PhoneNumber = "+491512345678",
                CreditNumber = "CR-2026-9999",
                Amount = 1450.75m,
                DueDate = new DateTime(2026, 11, 15)
            };

            var template = "{Creditnumber}\n\nSehr geehrte/r {Name}\nBitte zahlen sie bis zum {dueDate}\nDie Summe {amount} zurück";

            // Act
            var result = _renderer.Render(template, customer);

            // Assert
            result.Should().Contain("CR-2026-9999");
            result.Should().Contain("Sehr geehrte/r Max Mustermann");
            result.Should().Contain("15.11.2026");
            result.Should().Contain("1.450,75 EUR");
        }

        [Fact]
        public void Render_IsCaseInsensitiveForPlaceholders()
        {
            // Arrange
            var customer = new Customer
            {
                Name = "Erika",
                CreditNumber = "CR-123",
                Amount = 50.0m,
                DueDate = new DateTime(2026, 5, 1)
            };

            var template = "Kredit: {creditnumber}, Name: {NAME}, Datum: {DUEDATE}, Betrag: {Amount}";

            // Act
            var result = _renderer.Render(template, customer);

            // Assert
            result.Should().Be("Kredit: CR-123, Name: Erika, Datum: 01.05.2026, Betrag: 50,00 EUR");
        }

        [Fact]
        public void Render_HandlesNullOrEmptyTemplate()
        {
            var customer = new Customer { Name = "Test" };
            _renderer.Render(string.Empty, customer).Should().BeEmpty();
        }
    }
}
