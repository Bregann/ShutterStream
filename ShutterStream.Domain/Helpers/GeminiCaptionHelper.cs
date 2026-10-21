using Serilog;
using ShutterStream.Domain.DTOs.Captions;
using ShutterStream.Domain.DTOs.Gemini;
using ShutterStream.Domain.Enums;
using ShutterStream.Domain.Interfaces.Helpers;
using System.Text;
using System.Text.Json;

namespace ShutterStream.Domain.Helpers
{
    /// <summary>
    /// Captions photos with Google Gemini. The API key and model come from the environmental settings,
    /// and AI captions are switched off while the key is empty.
    /// </summary>
    public class GeminiCaptionHelper(IEnvironmentalSettingHelper environmentalSettingHelper, HttpClient httpClient) : IAiCaptionHelper
    {
        private static readonly string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";

        private const string SystemPrompt = """
            You write captions for photos in a personal photography portfolio, in the style of a Flickr photostream.

            For each photo, write:
            - title: a short, evocative title of 2 to 8 words. No full stop at the end, no quotation marks, and never start with "A photo of", "Image of" or similar.
            - description: one to three sentences describing what the photo shows - the subject, setting, light and mood. Write it as the photographer would, plainly and without hype.

            Use British English. The album title often names the place or trip, so use it to identify locations when it fits what you can see.
            Only name a specific place, landmark, species or object when the image or the album title makes it clear - otherwise describe it generally rather than guessing.
            Never guess the identity of people in the photo.
            """;

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private string ApiKey => environmentalSettingHelper.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.GeminiApiKey);
        private string Model => environmentalSettingHelper.GetEnvironmentalSettingValue(EnvironmentalSettingEnum.GeminiModel);

        public bool IsEnabled => !string.IsNullOrWhiteSpace(ApiKey);

        public async Task<PhotoCaption?> GenerateCaption(byte[] jpegBytes, PhotoCaptionContext context, CancellationToken cancellationToken = default)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException("AI captions are not configured. Set the GeminiApiKey environmental setting");
            }

            var hints = $"Album: {context.AlbumTitle}";

            if (context.DateTaken != null)
            {
                hints += $"\nDate taken: {context.DateTaken.Value:d MMMM yyyy}";
            }

            var requestBody = new GeminiGenerateContentRequest
            {
                SystemInstruction = new GeminiContent
                {
                    Parts = [new GeminiPart { Text = SystemPrompt }]
                },
                Contents =
                [
                    new GeminiContent
                    {
                        Role = "user",
                        Parts =
                        [
                            new GeminiPart { InlineData = new GeminiInlineData { MimeType = "image/jpeg", Data = Convert.ToBase64String(jpegBytes) } },
                            new GeminiPart { Text = hints }
                        ]
                    }
                ],
                // Forces the reply to be JSON matching this shape, so it never needs scraping out of prose
                GenerationConfig = new GeminiGenerationConfig
                {
                    ResponseMimeType = "application/json",
                    ResponseSchema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            title = new { type = "STRING" },
                            description = new { type = "STRING" }
                        },
                        required = new[] { "title", "description" }
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{GeminiApiBaseUrl}/models/{Model}:generateContent")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("x-goog-api-key", ApiKey);

            var response = await httpClient.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Log.Error($"Gemini caption request failed: {response.StatusCode} - {content}");
                throw new HttpRequestException($"Gemini caption request failed: {response.StatusCode}");
            }

            var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(content, _jsonOptions);

            // Gemini blocks some images outright, or stops a reply early for safety reasons
            if (geminiResponse?.PromptFeedback?.BlockReason != null)
            {
                Log.Warning("Gemini declined to caption a photo in album {Album}: {Reason}", context.AlbumTitle, geminiResponse.PromptFeedback.BlockReason);
                return null;
            }

            var candidate = geminiResponse?.Candidates?.FirstOrDefault();

            if (candidate?.FinishReason is "SAFETY" or "IMAGE_SAFETY" or "PROHIBITED_CONTENT" or "BLOCKLIST")
            {
                Log.Warning("Gemini stopped captioning a photo in album {Album}: {Reason}", context.AlbumTitle, candidate.FinishReason);
                return null;
            }

            var json = string.Concat(candidate?.Content?.Parts.Select(p => p.Text) ?? []);
            var caption = json == "" ? null : JsonSerializer.Deserialize<PhotoCaption>(json, _jsonOptions);

            if (caption == null || string.IsNullOrWhiteSpace(caption.Title))
            {
                throw new InvalidDataException($"Gemini returned an empty caption (finish reason {candidate?.FinishReason})");
            }

            return new PhotoCaption
            {
                Title = caption.Title.Trim(),
                Description = caption.Description.Trim()
            };
        }
    }
}
