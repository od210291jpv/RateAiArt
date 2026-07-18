using System.ComponentModel.DataAnnotations;

using System.Text.Json.Serialization;

namespace RateAiArt.DTO.Ai
{
    public class PromptRequest
    {
        [Required]
        [JsonPropertyName("base64Image")]
        public string? Base64Image { get; set; }

        [Required]
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; } = string.Empty;
    }
}
