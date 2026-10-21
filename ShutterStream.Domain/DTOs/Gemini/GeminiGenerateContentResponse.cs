using System.Text.Json.Serialization;

namespace ShutterStream.Domain.DTOs.Gemini
{
    public class GeminiGenerateContentResponse
    {
        [JsonPropertyName("candidates")]
        public GeminiCandidate[]? Candidates { get; set; }

        [JsonPropertyName("promptFeedback")]
        public GeminiPromptFeedback? PromptFeedback { get; set; }
    }

    public class GeminiCandidate
    {
        [JsonPropertyName("content")]
        public GeminiContent? Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string? FinishReason { get; set; }
    }

    public class GeminiPromptFeedback
    {
        /// <summary>
        /// Set when the whole request was blocked, e.g. SAFETY or IMAGE_SAFETY
        /// </summary>
        [JsonPropertyName("blockReason")]
        public string? BlockReason { get; set; }
    }
}
