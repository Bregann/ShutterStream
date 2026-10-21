using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Moq;
using Moq.Protected;
using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Helpers;
using System.Net;

namespace ShutterStream.Tests.Infrastructure
{
    /// <summary>
    /// Factory class to create common mocks used across tests
    /// </summary>
    public static class MockFactory
    {
        /// <summary>
        /// Creates a mock IBackgroundJobClient. Enqueue is an extension method that calls Create under the hood
        /// </summary>
        public static Mock<IBackgroundJobClient> CreateBackgroundJobClient()
        {
            var mock = new Mock<IBackgroundJobClient>();

            mock.Setup(x => x.Create(It.IsAny<Job>(), It.IsAny<IState>()))
                .Returns("test-job-id");

            return mock;
        }

        /// <summary>
        /// Creates a mock IAiCaptionHelper that captions every photo as "Caption {n}"
        /// </summary>
        public static Mock<IAiCaptionHelper> CreateAiCaptionHelper(bool isEnabled = true)
        {
            var mock = new Mock<IAiCaptionHelper>();
            var count = 0;

            mock.Setup(x => x.IsEnabled).Returns(isEnabled);

            mock.Setup(x => x.GenerateCaption(It.IsAny<byte[]>(), It.IsAny<PhotoCaptionContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() =>
                {
                    var n = Interlocked.Increment(ref count);
                    return new PhotoCaption { Title = $"Caption {n}", Description = $"Description {n}" };
                });

            return mock;
        }

        /// <summary>
        /// Creates a mock IEnvironmentalSettingHelper with a Gemini key and model set
        /// </summary>
        public static Mock<IEnvironmentalSettingHelper> CreateEnvironmentalSettingHelper(string geminiApiKey = "test-gemini-key")
        {
            var mock = new Mock<IEnvironmentalSettingHelper>();

            mock.Setup(x => x.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.GeminiApiKey)).Returns(geminiApiKey);
            mock.Setup(x => x.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.GeminiModel)).Returns("gemini-test-model");

            return mock;
        }

        /// <summary>
        /// Creates a mock HttpClient that returns the given response, and captures the request it was sent
        /// </summary>
        public static HttpClient CreateMockHttpClient(string responseContent, HttpStatusCode statusCode, List<(HttpRequestMessage Request, string Body)>? capturedRequests = null)
        {
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(async (HttpRequestMessage request, CancellationToken _) =>
                {
                    capturedRequests?.Add((request, request.Content == null ? "" : await request.Content.ReadAsStringAsync()));

                    return new HttpResponseMessage
                    {
                        StatusCode = statusCode,
                        Content = new StringContent(responseContent)
                    };
                });

            return new HttpClient(mockHttpMessageHandler.Object);
        }
    }
}
