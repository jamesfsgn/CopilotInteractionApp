namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// One exchange in a conversation: a prompt and the response(s) it produced.
    /// </summary>
    public sealed class ConversationTurn
    {
        /// <summary>1-based position of this exchange within its conversation.</summary>
        public int TurnNumber { get; init; }

        /// <summary>Null when Copilot replied without a preceding prompt in the retrieved data.</summary>
        public InteractionRow? Prompt { get; init; }

        /// <summary>Copilot sometimes emits an answer as several messages; all parts are kept here.</summary>
        public List<InteractionRow> Responses { get; } = new();

        public string PromptText => Prompt?.Text ?? string.Empty;

        /// <summary>All response parts joined into a single readable answer.</summary>
        public string ResponseText =>
            string.Join(" ", Responses.Select(r => r.Text).Where(t => !string.IsNullOrWhiteSpace(t)));

        public DateTime? PromptTime => Prompt?.Timestamp;

        /// <summary>Timestamp of the last response part, used to measure reply latency.</summary>
        public DateTime? ResponseTime => Responses.Count > 0 ? Responses[^1].Timestamp : null;

        public string RequestId =>
            Prompt?.RequestId is { Length: > 0 } id ? id : Responses.FirstOrDefault()?.RequestId ?? string.Empty;

        public int PromptAttachments => Prompt?.Attachments ?? 0;

        public int ResponseAttachments => Responses.Sum(r => r.Attachments);

        public int ResponseLinks => Responses.Sum(r => r.Links);

        public string App => Prompt?.App ?? Responses.FirstOrDefault()?.App ?? string.Empty;

        public string AppClass => Prompt?.AppClass ?? Responses.FirstOrDefault()?.AppClass ?? string.Empty;

        public string Conversation =>
            Prompt?.Conversation ?? Responses.FirstOrDefault()?.Conversation ?? string.Empty;
    }
}
