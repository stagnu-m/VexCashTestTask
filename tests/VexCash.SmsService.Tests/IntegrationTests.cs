using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using VexCash.SmsService.Dtos;
using VexCash.SmsService.Models;
using Xunit;

namespace VexCash.SmsService.Tests
{
    public class IntegrationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _client;

        public IntegrationTests(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetCustomers_ReturnsSeededCustomers()
        {
            var response = await _client.GetAsync("/api/customers");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            var customers = JsonSerializer.Deserialize<List<CustomerDto>>(json, JsonOptions);

            customers.Should().NotBeNull();
            customers!.Should().HaveCountGreaterOrEqualTo(4);
            customers!.Any(c => c.Name == "Max Mustermann").Should().BeTrue();
        }

        [Fact]
        public async Task PostCustomer_CreatesNewCustomerSuccessfully()
        {
            var newCustomer = new CreateCustomerDto
            {
                Name = "Sabine Zimmermann",
                PhoneNumber = "+4917612345678",
                CreditNumber = "CR-2026-7777",
                Amount = 890.50m,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var content = new StringContent(JsonSerializer.Serialize(newCustomer), Encoding.UTF8, "application/json");
            var postResponse = await _client.PostAsync("/api/customers", content);
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var json = await postResponse.Content.ReadAsStringAsync();
            var created = JsonSerializer.Deserialize<CustomerDto>(json, JsonOptions);

            created.Should().NotBeNull();
            created!.Name.Should().Be("Sabine Zimmermann");
            created.CreditNumber.Should().Be("CR-2026-7777");
            created.Amount.Should().Be(890.50m);
        }

        [Fact]
        public async Task GetTemplates_ReturnsSeededTemplates()
        {
            var response = await _client.GetAsync("/api/templates");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            var templates = JsonSerializer.Deserialize<List<MessageTemplateDto>>(json, JsonOptions);

            templates.Should().NotBeNull();
            templates!.Should().HaveCount(3);
            templates!.Select(t => t.Type).Should().Contain(new[]
            {
                TemplateType.Erinnerung,
                TemplateType.Mahnung,
                TemplateType.Kuendigung
            });
        }

        [Fact]
        public async Task PutTemplate_UpdatesTemplateContent()
        {
            var updateDto = new UpdateTemplateDto
            {
                Title = "Aktualisierte Mahnung",
                Content = "Neue Mahnung {Creditnumber} für {Name}: bitte {amount} bis {dueDate} begleichen!"
            };

            var content = new StringContent(JsonSerializer.Serialize(updateDto), Encoding.UTF8, "application/json");
            var response = await _client.PutAsync("/api/templates/Mahnung", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            var updated = JsonSerializer.Deserialize<MessageTemplateDto>(json, JsonOptions);

            updated.Should().NotBeNull();
            updated!.Title.Should().Be("Aktualisierte Mahnung");
            updated.Content.Should().Contain("Neue Mahnung");
        }

        [Fact]
        public async Task PostSmsPreview_ReturnsRenderedMessage()
        {
            var previewRequest = new SmsPreviewRequestDto
            {
                CustomerId = 1,
                TemplateType = TemplateType.Erinnerung
            };

            var content = new StringContent(JsonSerializer.Serialize(previewRequest), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/sms/preview", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            var preview = JsonSerializer.Deserialize<SmsPreviewResponseDto>(json, JsonOptions);

            preview.Should().NotBeNull();
            preview!.CustomerId.Should().Be(1);
            preview.RenderedMessage.Should().Contain("Max Mustermann");
            preview.RenderedMessage.Should().Contain("CR-2026-1001");
        }

        [Fact]
        public async Task PostSmsSend_SendsAndLogsSms()
        {
            var sendRequest = new SendSmsRequestDto
            {
                CustomerId = 1,
                TemplateType = TemplateType.Erinnerung
            };

            var content = new StringContent(JsonSerializer.Serialize(sendRequest), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/sms/send", content);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<SmsSendResultDto>(json, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.RecipientPhoneNumber.Should().Be("+491512345678");

            // Check history endpoint
            var historyResponse = await _client.GetAsync("/api/sms/history?customerId=1");
            historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var historyJson = await historyResponse.Content.ReadAsStringAsync();
            var logs = JsonSerializer.Deserialize<List<SmsLog>>(historyJson, JsonOptions);

            logs.Should().NotBeNull();
            logs!.Should().NotBeEmpty();
        }
    }
}
