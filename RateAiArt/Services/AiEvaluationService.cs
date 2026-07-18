using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

using RateAiArt.DTO.Ai;

using System.Text.Json;

namespace RateAiArt.Services
{
    public class AiEvaluationService : IAiEvaluationService
    {
        private readonly IChatCompletionService _chatCompletion;
        private readonly Kernel _kernel; // Додайте, якщо вам потрібен сам об'єкт Kernel для плагінів

        // ASP.NET Core автоматично "підкладе" сюди налаштовані сервіси з Program.cs
        public AiEvaluationService(IChatCompletionService chatCompletion, Kernel kernel)
        {
            _chatCompletion = chatCompletion;
            _kernel = kernel;
        }

        public async Task<EvaluationResponse> EvaluateArtAsync(byte[] imageBytes, string mimeType)
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("""
                You are an expert AI Art Critic and Technical Evaluator.
                Your task is to analyze the provided AI-generated image and evaluate it across 6 criteria on a scale from 1 to 10 (where 1 is completely failed, and 10 is absolute perfection).

                The criteria are:
                1. Creativity: Originality of the concept.
                2. Complexity: Intricacy of details and difficulty of the prompt.
                3. RenderQuality: Absence of AI artifacts (e.g., bad hands, blurry textures, anomalies).
                4. LightingAndColors: Harmony of the color palette and realism of lighting.
                5. Composition: Framing, balance, and focus of the image.
                6. StylisticConsistency: How well the image adheres to a unified visual style.

                You MUST respond ONLY with a valid JSON object. Do not include any greetings, explanations, or markdown formatting (like ```json). Just the raw JSON.

                The JSON structure MUST exactly match this template:
                {
                  "Creativity": 0,
                  "Complexity": 0,
                  "RenderQuality": 0,
                  "LightingAndColors": 0,
                  "Composition": 0,
                  "StylisticConsistency": 0,
                  "ImprovementTips": [
                    "First specific actionable tip to improve the image or prompt",
                    "Second specific actionable tip",
                    "Third specific actionable tip"
                  ]
                }
                """);

            var messageItems = new ChatMessageContentItemCollection { new TextContent("Evaluate this image and return the JSON.") };
            messageItems.Add(new ImageContent(imageBytes, mimeType));

            chatHistory.AddUserMessage(messageItems);

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                Temperature = 0.0,
                TopP = 0.1
            };

            ChatMessageContent response = await _chatCompletion.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel);

            string rawText = response.Content ?? string.Empty;

            var cleanJson = rawText
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<EvaluationResponse>(cleanJson, options) ?? throw new InvalidOperationException("Failed to deserialize AI response.");
        }
    }
}
