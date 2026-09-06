using System;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class ConversationTurnTests
{
    [Fact]
    public void ResponseText_JoinsNonBlankResponsePartsWithSpace()
    {
        var turn = new ConversationTurn();
        turn.Responses.Add(new InteractionRow { Text = "Part one." });
        turn.Responses.Add(new InteractionRow { Text = "" });
        turn.Responses.Add(new InteractionRow { Text = "Part two." });

        Assert.Equal("Part one. Part two.", turn.ResponseText);
    }

    [Fact]
    public void ResponseTime_UsesLastResponsePart()
    {
        var first = new DateTime(2026, 1, 1, 9, 0, 0);
        var last = new DateTime(2026, 1, 1, 9, 5, 0);
        var turn = new ConversationTurn();
        turn.Responses.Add(new InteractionRow { Timestamp = first });
        turn.Responses.Add(new InteractionRow { Timestamp = last });

        Assert.Equal(last, turn.ResponseTime);
    }

    [Fact]
    public void ResponseTime_NoResponses_ReturnsNull()
    {
        var turn = new ConversationTurn();

        Assert.Null(turn.ResponseTime);
    }

    [Fact]
    public void RequestId_PrefersPromptRequestId_FallsBackToFirstResponse()
    {
        var withPrompt = new ConversationTurn { Prompt = new InteractionRow { RequestId = "prompt-req" } };
        withPrompt.Responses.Add(new InteractionRow { RequestId = "response-req" });
        Assert.Equal("prompt-req", withPrompt.RequestId);

        var noPrompt = new ConversationTurn();
        noPrompt.Responses.Add(new InteractionRow { RequestId = "response-req" });
        Assert.Equal("response-req", noPrompt.RequestId);

        var neither = new ConversationTurn();
        Assert.Equal(string.Empty, neither.RequestId);
    }

    [Fact]
    public void ResponseAttachmentsAndLinks_SumAcrossAllResponseParts()
    {
        var turn = new ConversationTurn();
        turn.Responses.Add(new InteractionRow { Attachments = 1, Links = 2 });
        turn.Responses.Add(new InteractionRow { Attachments = 3, Links = 0 });

        Assert.Equal(4, turn.ResponseAttachments);
        Assert.Equal(2, turn.ResponseLinks);
    }

    [Fact]
    public void App_FallsBackFromPromptToFirstResponse()
    {
        var withPrompt = new ConversationTurn { Prompt = new InteractionRow { App = "Copilot in Word" } };
        Assert.Equal("Copilot in Word", withPrompt.App);

        var noPrompt = new ConversationTurn();
        noPrompt.Responses.Add(new InteractionRow { App = "Copilot in Teams" });
        Assert.Equal("Copilot in Teams", noPrompt.App);
    }
}
