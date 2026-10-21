using System.Text.Json.Serialization;

namespace ShutterStream.Domain.DTOs.Gemini
{
    public class GeminiGenerateContentRequest
    {
        [JsonPropertyName("systemInstruction")]
        public required GeminiContent SystemInstruction { get; set; }

        [JsonPropertyName("contents")]
        public required GeminiContent[] Contents { get; set; }

        [JsonPropertyName("generationConfig")]
        public required GeminiGenerationConfig GenerationConfig { get; set; }
    }

    public class GeminiContent
    {
        [JsonPropertyName("role")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Role { get; set; }

        [JsonPropertyName("parts")]
        public required GeminiPart[] Parts { get; set; }
    }

    public class GeminiPart
    {
        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("inlineData")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public GeminiInlineData? InlineData { get; set; }
    }

    public class GeminiInlineData
    {
        [JsonPropertyName("mimeType")]
        public required string MimeType { get; set; }

        /// <summary>
        /// Base64 encoded file contents
        /// </summary>
        [JsonPropertyName("data")]
        public required string Data { get; set; }
    }

    public class GeminiGenerationConfig
    {
        [JsonPropertyName("responseMimeType")]
        public required string ResponseMimeType { get; set; }

        [JsonPropertyName("responseSchema")]
        public required object ResponseSchema { get; set; }
    }
}
