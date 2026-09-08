using CopilotInteractionApp.Models;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class InteractionRowTests
{
    [Theory]
    [InlineData("userPrompt", "Prompt")]
    [InlineData("aiResponse", "Response")]
    [InlineData(null, "Unknown")]
    [InlineData("", "Unknown")]
    [InlineData("somethingElse", "somethingElse")]
    public void FromInteraction_MapsInteractionTypeToFriendlyType(string? interactionType, string expected)
    {
        var interaction = new AiInteraction { InteractionType = interactionType };

        var row = InteractionRow.FromInteraction(interaction);

        Assert.Equal(expected, row.Type);
    }

    [Fact]
    public void FromInteraction_AuthorFallsBackFromUserToApplicationToIds()
    {
        var userNamed = InteractionRow.FromInteraction(new AiInteraction
        {
            From = new AiInteractionFrom { User = new IdentityInfo { DisplayName = "Ada Lovelace", Id = "user-1" } }
        });
        Assert.Equal("Ada Lovelace", userNamed.Author);

        var appNamed = InteractionRow.FromInteraction(new AiInteraction
        {
            From = new AiInteractionFrom { Application = new IdentityInfo { DisplayName = "Copilot", Id = "app-1" } }
        });
        Assert.Equal("Copilot", appNamed.Author);

        var userIdOnly = InteractionRow.FromInteraction(new AiInteraction
        {
            From = new AiInteractionFrom { User = new IdentityInfo { Id = "user-2" } }
        });
        Assert.Equal("user-2", userIdOnly.Author);

        var appIdOnly = InteractionRow.FromInteraction(new AiInteraction
        {
            From = new AiInteractionFrom { Application = new IdentityInfo { Id = "app-2" } }
        });
        Assert.Equal("app-2", appIdOnly.Author);

        var noFrom = InteractionRow.FromInteraction(new AiInteraction());
        Assert.Equal(string.Empty, noFrom.Author);
    }

    [Fact]
    public void ToPlainText_StripsHtmlTags()
    {
        var result = InteractionRow.ToPlainText("<p>Hello, <b>Ada</b>!</p>");

        Assert.Equal("Hello, Ada !", result);
    }

    [Fact]
    public void ToPlainText_DecodesHtmlEntities()
    {
        var result = InteractionRow.ToPlainText("Q&amp;A session &mdash; done");

        Assert.Equal("Q&A session — done", result);
    }

    [Fact]
    public void ToPlainText_CollapsesWhitespaceAndTrims()
    {
        var result = InteractionRow.ToPlainText("  Line one\n\n  Line   two  ");

        Assert.Equal("Line one Line two", result);
    }

    [Fact]
    public void ToPlainText_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, InteractionRow.ToPlainText(null));
        Assert.Equal(string.Empty, InteractionRow.ToPlainText("   "));
    }

    [Fact]
    public void FromInteraction_CopiesAppClassAndResolvesFriendlyAppName()
    {
        var interaction = new AiInteraction { AppClass = "IPM.SkypeTeams.Message.Copilot.Teams" };

        var row = InteractionRow.FromInteraction(interaction);

        Assert.Equal("IPM.SkypeTeams.Message.Copilot.Teams", row.AppClass);
        Assert.Equal("Copilot in Teams", row.App);
    }
}
