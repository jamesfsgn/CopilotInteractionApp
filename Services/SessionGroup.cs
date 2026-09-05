namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// A Copilot conversation: all prompts and responses that share a session ID for one user.
    /// </summary>
    public sealed class SessionGroup
    {
        /// <summary>Graph session ID. Empty for interactions that carry no session.</summary>
        public string SessionId { get; init; } = string.Empty;

        public string User { get; init; } = string.Empty;
        public string UserPrincipalName { get; init; } = string.Empty;

        /// <summary>Timestamp of the first message, in local time.</summary>
        public DateTime? Start { get; init; }

        /// <summary>Timestamp of the last message, in local time.</summary>
        public DateTime? End { get; init; }

        public int Prompts { get; init; }
        public int Responses { get; init; }
        public int MessageCount => Messages.Count;

        /// <summary>Text of the first prompt, used as the conversation's label in the grid.</summary>
        public string FirstPrompt { get; init; } = string.Empty;

        /// <summary>Friendly product name of the app this conversation happened in.</summary>
        public string App { get; init; } = string.Empty;

        /// <summary>Raw Graph appClass, preserved alongside <see cref="App"/>.</summary>
        public string AppClass { get; init; } = string.Empty;

        /// <summary>Messages in chronological order.</summary>
        public List<InteractionRow> Messages { get; init; } = new();

        public string DisplaySessionId =>
            string.IsNullOrEmpty(SessionId) ? "(no session)" : SessionId;

        /// <summary>
        /// Pairs each prompt with the response(s) that follow it so a conversation reads as turns.
        /// A response that arrives without a preceding prompt becomes a prompt-less turn.
        /// </summary>
        public List<ConversationTurn> BuildTurns()
        {
            var turns = new List<ConversationTurn>();
            ConversationTurn? current = null;

            foreach (var message in Messages)
            {
                var isPrompt = string.Equals(message.Type, "Prompt", StringComparison.OrdinalIgnoreCase);

                if (isPrompt)
                {
                    current = new ConversationTurn { TurnNumber = turns.Count + 1, Prompt = message };
                    turns.Add(current);
                    continue;
                }

                if (current is null)
                {
                    current = new ConversationTurn { TurnNumber = turns.Count + 1 };
                    turns.Add(current);
                }

                current.Responses.Add(message);
            }

            return turns;
        }

        /// <summary>
        /// Groups interactions into conversations keyed by user + session ID, newest conversation first.
        /// </summary>
        public static List<SessionGroup> Build(IEnumerable<InteractionRow> rows)
        {
            return rows
                .GroupBy(r => (r.UserPrincipalName, r.SessionId), TupleComparer.Instance)
                .Select(group =>
                {
                    var ordered = group
                        .OrderBy(r => r.Timestamp ?? DateTime.MinValue)
                        .ThenBy(r => r.Type, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var first = ordered[0];
                    var firstPrompt = ordered.FirstOrDefault(r =>
                        string.Equals(r.Type, "Prompt", StringComparison.OrdinalIgnoreCase));

                    return new SessionGroup
                    {
                        SessionId = group.Key.SessionId,
                        User = first.User,
                        UserPrincipalName = first.UserPrincipalName,
                        Start = ordered.First().Timestamp,
                        End = ordered.Last().Timestamp,
                        Prompts = ordered.Count(r => string.Equals(r.Type, "Prompt", StringComparison.OrdinalIgnoreCase)),
                        Responses = ordered.Count(r => string.Equals(r.Type, "Response", StringComparison.OrdinalIgnoreCase)),
                        FirstPrompt = (firstPrompt ?? first).Text,
                        App = first.App,
                        AppClass = first.AppClass,
                        Messages = ordered
                    };
                })
                .OrderByDescending(s => s.Start ?? DateTime.MinValue)
                .ToList();
        }

        private sealed class TupleComparer : IEqualityComparer<(string User, string SessionId)>
        {
            public static readonly TupleComparer Instance = new();

            public bool Equals((string User, string SessionId) x, (string User, string SessionId) y) =>
                string.Equals(x.User, y.User, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.SessionId, y.SessionId, StringComparison.Ordinal);

            public int GetHashCode((string User, string SessionId) obj) =>
                HashCode.Combine(
                    obj.User?.ToLowerInvariant() ?? string.Empty,
                    obj.SessionId ?? string.Empty);
        }
    }
}
