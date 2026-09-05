using System.Text.RegularExpressions;
using CopilotInteractionApp.Models;

namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// Flattened, grid-friendly projection of an <see cref="AiInteraction"/>.
    /// </summary>
    public sealed class InteractionRow
    {
        /// <summary>Creation time converted to local time for display.</summary>
        public DateTime? Timestamp { get; init; }

        public string User { get; init; } = string.Empty;
        public string UserPrincipalName { get; init; } = string.Empty;

        /// <summary>"Prompt" or "Response" (from the Graph userPrompt/aiResponse values).</summary>
        public string Type { get; init; } = string.Empty;

        /// <summary>Display name of the user or bot that produced the message.</summary>
        public string Author { get; init; } = string.Empty;

        /// <summary>Friendly product name resolved from <see cref="AppClass"/>.</summary>
        public string App { get; init; } = string.Empty;

        /// <summary>Raw Graph appClass, e.g. IPM.SkypeTeams.Message.Copilot.BizChat.</summary>
        public string AppClass { get; init; } = string.Empty;

        public string Conversation { get; init; } = string.Empty;

        /// <summary>Message body with markup stripped, ready for the grid.</summary>
        public string Text { get; init; } = string.Empty;

        public string SessionId { get; init; } = string.Empty;
        public string RequestId { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
        public int Attachments { get; init; }
        public int Links { get; init; }

        /// <summary>The original interaction, used by the details pane to show untouched data.</summary>
        public AiInteraction Source { get; init; } = new();

        public static InteractionRow FromUserInteraction(UserInteraction item) =>
            FromInteraction(item.Interaction, item.User);

        public static InteractionRow FromInteraction(AiInteraction interaction, GraphUser? user = null)
        {
            var author = interaction.From?.User?.DisplayName
                         ?? interaction.From?.Application?.DisplayName
                         ?? interaction.From?.User?.Id
                         ?? interaction.From?.Application?.Id
                         ?? string.Empty;

            return new InteractionRow
            {
                Timestamp = interaction.CreatedDateTime?.ToLocalTime().DateTime,
                User = user?.DisplayLabel ?? string.Empty,
                UserPrincipalName = user?.UserPrincipalName ?? user?.Id ?? string.Empty,
                Type = FriendlyType(interaction.InteractionType),
                Author = author,
                App = AppClassCatalog.FriendlyName(interaction.AppClass),
                AppClass = interaction.AppClass ?? string.Empty,
                Conversation = interaction.ConversationType ?? string.Empty,
                Text = ToPlainText(interaction.Body?.Content),
                SessionId = interaction.SessionId ?? string.Empty,
                RequestId = interaction.RequestId ?? string.Empty,
                Id = interaction.Id ?? string.Empty,
                Attachments = interaction.Attachments?.Count ?? 0,
                Links = interaction.Links?.Count ?? 0,
                Source = interaction
            };
        }

        private static string FriendlyType(string? interactionType) => interactionType switch
        {
            "userPrompt" => "Prompt",
            "aiResponse" => "Response",
            null or "" => "Unknown",
            _ => interactionType
        };

        /// <summary>
        /// Strips HTML/attachment markup so message bodies render readably in the grid.
        /// </summary>
        public static string ToPlainText(string? content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            var text = Regex.Replace(content, "<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }
    }
}
