using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using VexCash.SmsService.Services;
using VexCash.SmsService.Services.Options;
using Xunit;

namespace VexCash.SmsService.Tests
{
    public class MessageBirdClientTests
    {
        [Fact]
        public async Task SendSmsAsync_WhenApiKeyIsPlaceholder_ReturnsSimulationResult()
        {
            var options = Options.Create(new MessageBirdOptions
            {
                ApiKey = "YOUR_MESSAGEBIRD_API_KEY",
                Originator = "VexCash",
                SimulationMode = false
            });

            var client = new MessageBirdClient(
                new HttpClient(),
                options,
                NullLogger<MessageBirdClient>.Instance
            );

            var result = await client.SendSmsAsync("+491512345678", "Test text");

            result.Success.Should().BeTrue();
            result.Status.Should().Be("Simulated");
            result.StatusReason.Should().Contain("simulated");
            result.MessageId.Should().StartWith("sim_");
        }

        [Fact]
        public async Task SendSmsAsync_WhenSimulationModeIsTrue_ReturnsSimulationResult()
        {
            var options = Options.Create(new MessageBirdOptions
            {
                ApiKey = "any_key",
                Originator = "VexCash",
                SimulationMode = true
            });

            var client = new MessageBirdClient(
                new HttpClient(),
                options,
                NullLogger<MessageBirdClient>.Instance
            );

            var result = await client.SendSmsAsync("+491512345678", "Test text");

            result.Success.Should().BeTrue();
            result.Status.Should().Be("Simulated");
        }

        [Fact]
        public async Task SendSmsAsync_WithActiveApiKey_SendsExpectedHttpRequestWithNormalizedMsisdn()
        {
            var handlerMock = new Mock<HttpMessageHandler>();
            HttpRequestMessage? capturedRequest = null;
            string? capturedContent = null;

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
                {
                    capturedRequest = req;
                    capturedContent = req.Content != null ? await req.Content.ReadAsStringAsync() : null;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Created,
                        Content = new StringContent("{\"id\":\"mb_real_999\",\"recipients\":{\"totalCount\":1,\"totalSentCount\":1,\"items\":[{\"recipient\":491512345678,\"status\":\"sent\",\"statusReason\":\"successfully delivered\"}]}}")
                    };
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var options = Options.Create(new MessageBirdOptions
            {
                ApiKey = "real_live_or_test_key_12345",
                Originator = "VexCash",
                SimulationMode = false
            });

            var client = new MessageBirdClient(httpClient, options, NullLogger<MessageBirdClient>.Instance);

            var result = await client.SendSmsAsync("+491512345678", "Hello Test");

            result.Success.Should().BeTrue();
            result.MessageId.Should().Be("mb_real_999");
            result.Status.Should().Be("sent");
            result.StatusReason.Should().Be("successfully delivered");

            capturedRequest.Should().NotBeNull();
            capturedRequest!.Headers.Authorization?.Scheme.Should().Be("AccessKey");
            capturedRequest.Headers.Authorization?.Parameter.Should().Be("real_live_or_test_key_12345");

            // Verify payload has recipients array with normalized MSISDN integer as documented in send_sms.md
            capturedContent.Should().Contain("\"recipients\":[491512345678]");
            capturedContent.Should().Contain("Hello Test");
        }

        [Fact]
        public async Task SendSmsAsync_ParsesDocumentationPayloadFromSendSmsMdCorrectly()
        {
            // Response JSON taken directly from https://developers.messagebird.com/api/sms-messaging/#the-message-object (with all operator, country, price fields)
            var documentationJson = @"{
                ""id"":""e8077d803532c0b5937c639b60216938"",
                ""href"":""https://rest.messagebird.com/messages/e8077d803532c0b5937c639b60216938"",
                ""direction"":""mt"",
                ""type"":""sms"",
                ""originator"":""MessageBird"",
                ""body"":""The message to be sent"",
                ""reference"":""the-client-reference"",
                ""validity"":null,
                ""gateway"":240,
                ""typeDetails"":{},
                ""datacoding"":""plain"",
                ""mclass"":1,
                ""scheduledDatetime"":null,
                ""createdDatetime"":""2016-04-29T09:42:26+00:00"",
                ""recipients"":{
                    ""totalCount"":1,
                    ""totalSentCount"":1,
                    ""totalDeliveredCount"":1,
                    ""totalDeliveryFailedCount"":0,
                    ""items"":[
                        {
                            ""recipient"":31612345678,
                            ""status"":""sent"",
                            ""statusReason"":""successfully delivered"",
                            ""statusErrorCode"":null,
                            ""statusDatetime"":""2016-04-29T09:42:26+00:00"",
                            ""recipientCountry"":""Netherlands"",
                            ""recipientCountryPrefix"":31,
                            ""recipientOperator"":""KPN"",
                            ""mccmnc"":""20408"",
                            ""mcc"":""204"",
                            ""mnc"":""08"",
                            ""messageLength"":22,
                            ""messagePartCount"":1,
                            ""price"":{
                                ""amount"":0.075,
                                ""currency"":""EUR""
                            }
                        }
                    ]
                }
            }";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Created,
                    Content = new StringContent(documentationJson)
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var options = Options.Create(new MessageBirdOptions
            {
                ApiKey = "real_key",
                Originator = "VexCash",
                SimulationMode = false
            });

            var client = new MessageBirdClient(httpClient, options, NullLogger<MessageBirdClient>.Instance);

            var result = await client.SendSmsAsync("+31612345678", "Test text");

            result.Success.Should().BeTrue();
            result.MessageId.Should().Be("e8077d803532c0b5937c639b60216938");
            result.Status.Should().Be("sent");
            result.StatusReason.Should().Be("successfully delivered");
            result.StatusDatetime.Should().Be(DateTime.Parse("2016-04-29T09:42:26+00:00"));
        }

        [Theory]
        [InlineData("+491512345678", 491512345678L)]
        [InlineData("00491512345678", 491512345678L)]
        [InlineData("+31 6 12345678", 31612345678L)]
        [InlineData("31612345678", 31612345678L)]
        public void NormalizeToMsisdn_FormatsToStandardInteger(string input, long expected)
        {
            var result = MessageBirdClient.NormalizeToMsisdn(input);
            result.Should().Be(expected);
        }
    }
}
