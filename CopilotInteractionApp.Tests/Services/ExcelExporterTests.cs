using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class ExcelExporterTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"excel-exporter-tests-{Guid.NewGuid():N}.xlsx");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    private static SessionGroup MakeSession()
    {
        var promptTime = new DateTime(2026, 1, 1, 9, 0, 0);
        var responseTime = new DateTime(2026, 1, 1, 9, 0, 30);

        var prompt = new InteractionRow
        {
            Type = "Prompt",
            Text = "What is our leave policy?",
            Timestamp = promptTime,
            App = "Copilot in Teams",
            AppClass = "IPM.SkypeTeams.Message.Copilot.Teams",
            RequestId = "req-1"
        };

        var response = new InteractionRow
        {
            Type = "Response",
            Text = "Here is the leave policy summary.",
            Timestamp = responseTime,
            App = "Copilot in Teams",
            AppClass = "IPM.SkypeTeams.Message.Copilot.Teams",
            RequestId = "req-1"
        };

        return new SessionGroup
        {
            SessionId = "session-1",
            User = "Ada Lovelace",
            UserPrincipalName = "ada@contoso.com",
            Start = promptTime,
            End = responseTime,
            Prompts = 1,
            Responses = 1,
            FirstPrompt = prompt.Text,
            App = "Copilot in Teams",
            AppClass = "IPM.SkypeTeams.Message.Copilot.Teams",
            Messages = new List<InteractionRow> { prompt, response }
        };
    }

    private static ExportContext MakeContext() => new()
    {
        Scope = "1 user",
        Endpoint = "https://graph.microsoft.com/v1.0",
        DateRange = "All available history",
        AppClassFilter = "All apps",
        TypeFilter = "All interactions",
        SearchFilter = "(none)",
        TotalLoaded = 2
    };

    [Fact]
    public void Write_CreatesAllFourSheetsInOrder()
    {
        ExcelExporter.Write(_path, new List<SessionGroup> { MakeSession() }, MakeContext());

        using var workbook = new XLWorkbook(_path);

        Assert.Equal(new[] { "Summary", "Conversations", "Messages", "Sessions" },
            workbook.Worksheets.Select(w => w.Name).ToArray());
    }

    [Fact]
    public void Write_ConversationsSheet_HasExpectedHeaderAndExchangeRow()
    {
        ExcelExporter.Write(_path, new List<SessionGroup> { MakeSession() }, MakeContext());

        using var workbook = new XLWorkbook(_path);
        var sheet = workbook.Worksheet("Conversations");

        Assert.Equal("#", sheet.Cell(1, 1).GetString());
        Assert.Equal("Prompt", sheet.Cell(1, 6).GetString());
        Assert.Equal("Response", sheet.Cell(1, 8).GetString());

        Assert.Equal("What is our leave policy?", sheet.Cell(2, 6).GetString());
        Assert.Equal("Here is the leave policy summary.", sheet.Cell(2, 8).GetString());
        Assert.Equal(30d, sheet.Cell(2, 9).GetValue<double>());
    }

    [Fact]
    public void Write_ReturnsCorrectSummaryCounts()
    {
        var summary = ExcelExporter.Write(_path, new List<SessionGroup> { MakeSession() }, MakeContext());

        Assert.Equal(1, summary.Conversations);
        Assert.Equal(1, summary.Exchanges);
        Assert.Equal(2, summary.Interactions);
    }

    [Fact]
    public void Write_TruncatesCellTextLongerThan32000Characters()
    {
        var longText = new string('x', 32010);
        var timestamp = new DateTime(2026, 1, 1, 9, 0, 0);

        var session = new SessionGroup
        {
            SessionId = "session-long",
            User = "Ada Lovelace",
            UserPrincipalName = "ada@contoso.com",
            Start = timestamp,
            End = timestamp,
            Prompts = 1,
            Responses = 0,
            FirstPrompt = longText,
            App = "Copilot in Teams",
            AppClass = "IPM.SkypeTeams.Message.Copilot.Teams",
            Messages = new List<InteractionRow>
            {
                new()
                {
                    Type = "Prompt",
                    Text = longText,
                    Timestamp = timestamp,
                    App = "Copilot in Teams",
                    AppClass = "IPM.SkypeTeams.Message.Copilot.Teams",
                    RequestId = "req-long"
                }
            }
        };

        ExcelExporter.Write(_path, new List<SessionGroup> { session }, MakeContext());

        using var workbook = new XLWorkbook(_path);
        var cellValue = workbook.Worksheet("Conversations").Cell(2, 6).GetString();

        Assert.Equal(32000 + "...".Length, cellValue.Length);
        Assert.EndsWith("...", cellValue);
    }
}
