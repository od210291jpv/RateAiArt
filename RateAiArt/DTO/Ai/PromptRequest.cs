using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace RateAiArt.DTO.Ai
{
    public class PromptRequest
    {
        [Required]
        [JsonPropertyName("base64Image")]
        public string Base64Image { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("showcaseResultAccepted")]
        public bool ShowcaseResultAccepted { get; set; }

        [JsonPropertyName("nickname")]
        public string? Nickname { get; set; } = string.Empty;
    }
}
