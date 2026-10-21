using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.Helpers;
using System.Net;
using System.Text.Json;
using MockFactory = ShutterStream.Tests.Infrastructure.MockFactory;

namespace ShutterStream.Tests.Helpers
{
    [TestFixture]
    public class GeminiCaptionHelperTests
    {
        private static readonly PhotoCaptionContext _context = new()
        {
            AlbumTitle = "2025-01 Iceland",
            DateTaken = new DateTime(2025, 1, 14, 0, 0, 0, DateTimeKind.Utc)
        };

        private static string SuccessResponse(string captionJson) => JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new { role = "model", parts = new[] { new { text = captionJson } } },
                    finishReason = "STOP"
                }
            }
        });

        [Test]
        public async Task GenerateCaption_ShouldParseStructuredResponse()
        {
            // Arrange
            var httpClient = MockFactory.CreateMockHttpClient(SuccessResponse("{\"title\":\" Harbour at Dusk \",\"description\":\"Fishing boats moored in the old harbour.\"}"), HttpStatusCode.OK);
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper().Object, httpClient);

            // Act
            var caption = await helper.GenerateCaption([1, 2, 3], _context);

            // Assert
            Assert.That(caption, Is.Not.Null);
            Assert.That(caption!.Title, Is.EqualTo("Harbour at Dusk"));
            Assert.That(caption.Description, Is.EqualTo("Fishing boats moored in the old harbour."));
        }

        [Test]
        public async Task GenerateCaption_ShouldSendImageContextAndKeyToTheConfiguredModel()
        {
            // Arrange
            var requests = new List<(HttpRequestMessage Request, string Body)>();
            var httpClient = MockFactory.CreateMockHttpClient(SuccessResponse("{\"title\":\"T\",\"description\":\"D\"}"), HttpStatusCode.OK, requests);
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper().Object, httpClient);

            // Act
            await helper.GenerateCaption([1, 2, 3], _context);

            // Assert
            var (request, body) = requests.Single();
            Assert.That(request.RequestUri!.ToString(), Is.EqualTo("https://generativelanguage.googleapis.com/v1beta/models/gemini-test-model:generateContent"));
            Assert.That(request.Headers.GetValues("x-goog-api-key").Single(), Is.EqualTo("test-gemini-key"));

            using var json = JsonDocument.Parse(body);
            var parts = json.RootElement.GetProperty("contents")[0].GetProperty("parts");
            Assert.That(parts[0].GetProperty("inlineData").GetProperty("mimeType").GetString(), Is.EqualTo("image/jpeg"));
            Assert.That(parts[0].GetProperty("inlineData").GetProperty("data").GetString(), Is.EqualTo(Convert.ToBase64String([1, 2, 3])));
            Assert.That(parts[1].GetProperty("text").GetString(), Is.EqualTo("Album: 2025-01 Iceland\nDate taken: 14 January 2025"));
            Assert.That(json.RootElement.GetProperty("generationConfig").GetProperty("responseMimeType").GetString(), Is.EqualTo("application/json"));
        }

        [Test]
        public async Task GenerateCaption_WhenPromptBlocked_ShouldReturnNull()
        {
            // Arrange
            var httpClient = MockFactory.CreateMockHttpClient("{\"promptFeedback\":{\"blockReason\":\"IMAGE_SAFETY\"}}", HttpStatusCode.OK);
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper().Object, httpClient);

            // Act
            var caption = await helper.GenerateCaption([1, 2, 3], _context);

            // Assert
            Assert.That(caption, Is.Null);
        }

        [Test]
        public async Task GenerateCaption_WhenStoppedForSafety_ShouldReturnNull()
        {
            // Arrange
            var httpClient = MockFactory.CreateMockHttpClient("{\"candidates\":[{\"finishReason\":\"SAFETY\"}]}", HttpStatusCode.OK);
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper().Object, httpClient);

            // Act
            var caption = await helper.GenerateCaption([1, 2, 3], _context);

            // Assert
            Assert.That(caption, Is.Null);
        }

        [Test]
        public async Task GenerateCaption_WhenApiErrors_ShouldThrow()
        {
            // Arrange
            var httpClient = MockFactory.CreateMockHttpClient("{\"error\":{\"code\":429}}", HttpStatusCode.TooManyRequests);
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper().Object, httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(async () => await helper.GenerateCaption([1, 2, 3], _context));
        }

        [Test]
        public async Task IsEnabled_WhenApiKeyEmpty_ShouldBeFalse()
        {
            // Arrange
            var helper = new GeminiCaptionHelper(MockFactory.CreateEnvironmentalSettingHelper("").Object, new HttpClient());

            // Act & Assert
            Assert.That(helper.IsEnabled, Is.False);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await helper.GenerateCaption([1, 2, 3], _context));
        }
    }
}
