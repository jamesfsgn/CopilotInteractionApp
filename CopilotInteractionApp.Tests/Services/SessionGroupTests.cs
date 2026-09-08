using System;
using System.Collections.Generic;
using System.Linq;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class SessionGroupTests
{
    private static InteractionRow Row(string user, string sessionId, string type, DateTime timestamp, string text = "") =>
        new()
        {
            UserPrincipalName = user,
            SessionId = sessionId,
            Type = type,
            Timestamp = timestamp,
            Text = text,
            User = user,
            App = "Copilot in Word",
            AppClass = "IPM.SkypeTeams.Message.Copilot.Word"
        };

    [Fact]
    public void Build_GroupsByUserCaseInsensitive_AndSessionIdCaseSensitive()
    {
        var rows = new List<InteractionRow>
        {
            Row("ada@contoso.com", "session-A", "Prompt", new DateTime(2026, 1, 1, 9, 0, 0)),
            Row("ADA@CONTOSO.COM", "session-A", "Response", new DateTime(2026, 1, 1, 9, 1, 0)),
            Row("ada@contoso.com", "session-a", "Prompt", new DateTime(2026, 1, 1, 10, 0, 0))
        };

        var sessions = SessionGroup.Build(rows);

        Assert.Equal(2, sessions.Count);
        Assert.Contains(sessions, s => s.SessionId == "session-A" && s.MessageCount == 2);
        Assert.Contains(sessions, s => s.SessionId == "session-a" && s.MessageCount == 1);
    }

    [Fact]
    public void Build_OrdersConversationsNewestFirst()
    {
        var rows = new List<InteractionRow>
        {
            Row("ada@contoso.com", "older", "Prompt", new DateTime(2026, 1, 1, 9, 0, 0)),
            Row("ada@contoso.com", "newer", "Prompt", new DateTime(2026, 1, 2, 9, 0, 0))
        };

        var sessions = SessionGroup.Build(rows);

        Assert.Equal("newer", sessions[0].SessionId);
        Assert.Equal("older", sessions[1].SessionId);
    }

    [Fact]
    public void Build_FirstPrompt_PrefersFirstPromptTypeRow_OverEarliestRow()
    {
        var rows = new List<InteractionRow>
        {
            Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 0, 0), "orphan response"),
            Row("ada@contoso.com", "s1", "Prompt", new DateTime(2026, 1, 1, 9, 1, 0), "first real prompt")
        };

        var session = SessionGroup.Build(rows).Single();

        Assert.Equal("first real prompt", session.FirstPrompt);
    }

    [Fact]
    public void Build_FirstPrompt_FallsBackToEarliestRow_WhenNoPromptExists()
    {
        var rows = new List<InteractionRow>
        {
            Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 0, 0), "only a response")
        };

        var session = SessionGroup.Build(rows).Single();

        Assert.Equal("only a response", session.FirstPrompt);
    }

    [Fact]
    public void BuildTurns_ConsecutiveResponses_AttachToSamePrecedingPrompt()
    {
        var session = new SessionGroup
        {
            Messages = new List<InteractionRow>
            {
                Row("ada@contoso.com", "s1", "Prompt", new DateTime(2026, 1, 1, 9, 0, 0), "prompt"),
                Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 1, 0), "part 1"),
                Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 1, 5), "part 2")
            }
        };

        var turns = session.BuildTurns();

        Assert.Single(turns);
        Assert.Equal(2, turns[0].Responses.Count);
    }

    [Fact]
    public void BuildTurns_ResponseWithNoPrecedingPrompt_BecomesPromptlessTurn()
    {
        var session = new SessionGroup
        {
            Messages = new List<InteractionRow>
            {
                Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 0, 0), "orphan")
            }
        };

        var turns = session.BuildTurns();

        Assert.Single(turns);
        Assert.Null(turns[0].Prompt);
        Assert.Single(turns[0].Responses);
    }

    [Fact]
    public void BuildTurns_EachPromptStartsANewTurn()
    {
        var session = new SessionGroup
        {
            Messages = new List<InteractionRow>
            {
                Row("ada@contoso.com", "s1", "Prompt", new DateTime(2026, 1, 1, 9, 0, 0), "prompt 1"),
                Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 1, 0), "reply 1"),
                Row("ada@contoso.com", "s1", "Prompt", new DateTime(2026, 1, 1, 9, 2, 0), "prompt 2"),
                Row("ada@contoso.com", "s1", "Response", new DateTime(2026, 1, 1, 9, 3, 0), "reply 2")
            }
        };

        var turns = session.BuildTurns();

        Assert.Equal(2, turns.Count);
        Assert.Equal(1, turns[0].TurnNumber);
        Assert.Equal("prompt 1", turns[0].PromptText);
        Assert.Equal(2, turns[1].TurnNumber);
        Assert.Equal("prompt 2", turns[1].PromptText);
    }
}
