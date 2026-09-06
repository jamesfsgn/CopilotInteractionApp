using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class AppClassCatalogTests
{
    [Fact]
    public void FriendlyName_KnownAppClass_ReturnsMappedName()
    {
        var result = AppClassCatalog.FriendlyName("IPM.SkypeTeams.Message.Copilot.Word");

        Assert.Equal("Copilot in Word", result);
    }

    [Fact]
    public void FriendlyName_IsCaseInsensitive()
    {
        var result = AppClassCatalog.FriendlyName("ipm.skypeteams.message.copilot.word");

        Assert.Equal("Copilot in Word", result);
    }

    [Fact]
    public void FriendlyName_UnknownAppClassWithKnownPrefix_FallsBackToSuffix()
    {
        var result = AppClassCatalog.FriendlyName("IPM.SkypeTeams.Message.Copilot.SomeNewApp");

        Assert.Equal("Copilot in SomeNewApp", result);
    }

    [Fact]
    public void FriendlyName_UnknownAppClassWithoutPrefix_ReturnsTrimmedValue()
    {
        var result = AppClassCatalog.FriendlyName("  Some.Other.Value  ");

        Assert.Equal("Some.Other.Value", result);
    }

    [Fact]
    public void FriendlyName_NullOrWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, AppClassCatalog.FriendlyName(null));
        Assert.Equal(string.Empty, AppClassCatalog.FriendlyName("   "));
    }

    [Fact]
    public void Find_KnownValue_ReturnsMatchingOption()
    {
        var option = AppClassCatalog.Find("IPM.SkypeTeams.Message.Copilot.Excel");

        Assert.Equal("Copilot in Excel", option.Name);
    }

    [Fact]
    public void Find_NullOrUnknownValue_FallsBackToAll()
    {
        Assert.Equal(AppClassCatalog.All, AppClassCatalog.Find(null));
        Assert.Equal(AppClassCatalog.All, AppClassCatalog.Find("not-a-real-appclass"));
    }

    [Fact]
    public void Options_StartsWithAllAppsOption()
    {
        Assert.Equal(AppClassCatalog.All, AppClassCatalog.Options[0]);
        Assert.True(AppClassCatalog.Options.Count > 1);
    }
}
