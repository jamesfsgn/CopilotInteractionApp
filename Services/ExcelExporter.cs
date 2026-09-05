using ClosedXML.Excel;

namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// Details about the pull that produced an export, written to the workbook's Summary sheet.
    /// </summary>
    public sealed class ExportContext
    {
        public string Scope { get; init; } = string.Empty;
        public string DateRange { get; init; } = "All available history";
        public string AppClassFilter { get; init; } = "(none)";
        public string TypeFilter { get; init; } = "All interactions";
        public string SearchFilter { get; init; } = "(none)";
        public string Endpoint { get; init; } = string.Empty;
        public int TotalLoaded { get; init; }
        public IReadOnlyList<string> Issues { get; init; } = Array.Empty<string>();
    }

    /// <summary>Row counts produced by an export: conversations, prompt/reply exchanges, and messages.</summary>
    public sealed record ExportSummary(int Conversations, int Exchanges, int Interactions);

    /// <summary>
    /// Writes Copilot interactions to a formatted Excel workbook built around conversations.
    /// </summary>
    public static class ExcelExporter
    {
        private const int MaxCellLength = 32000;

        private static readonly XLColor HeaderFill = XLColor.FromHtml("#1F4E79");
        private static readonly XLColor PromptFill = XLColor.FromHtml("#DDEBF7");
        private static readonly XLColor ResponseFill = XLColor.FromHtml("#E2EFDA");
        private static readonly XLColor BandFill = XLColor.FromHtml("#F7F7F7");
        private static readonly XLColor TitleColor = XLColor.FromHtml("#1F4E79");

        /// <summary>
        /// Writes the workbook to <paramref name="path"/>, overwriting any existing file.
        /// Conversations are ordered by user and then chronologically so the sheets read top to bottom.
        /// </summary>
        /// <returns>Counts of what was written, for the status bar.</returns>
        public static ExportSummary Write(string path, IReadOnlyList<SessionGroup> sessions, ExportContext context)
        {
            var ordered = sessions
                .OrderBy(s => s.User, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(s => s.Start ?? DateTime.MinValue)
                .ToList();

            using var workbook = new XLWorkbook();

            var summarySheet = workbook.Worksheets.Add("Summary");
            var conversationsSheet = workbook.Worksheets.Add("Conversations");
            var messagesSheet = workbook.Worksheets.Add("Messages");
            var sessionsSheet = workbook.Worksheets.Add("Sessions");

            var exchanges = WriteConversations(conversationsSheet, ordered);
            var interactions = WriteMessages(messagesSheet, ordered);
            WriteSessions(sessionsSheet, ordered);
            WriteSummary(summarySheet, ordered, context, exchanges, interactions);

            workbook.SaveAs(path);
            return new ExportSummary(ordered.Count, exchanges, interactions);
        }

        /// <summary>
        /// One row per exchange, with the prompt and its reply side by side and each conversation banded.
        /// </summary>
        private static int WriteConversations(IXLWorksheet sheet, IReadOnlyList<SessionGroup> sessions)
        {
            string[] headers =
            {
                "#", "User", "Conversation started", "Turn", "Prompt time", "Prompt",
                "Response time", "Response", "Reply (sec)", "App", "Chat type",
                "Attachments", "Links", "Session ID", "App class (raw)"
            };

            WriteHeader(sheet, headers);

            var row = 2;
            var conversationNumber = 0;
            var exchanges = 0;

            foreach (var session in sessions)
            {
                conversationNumber++;
                var firstRowOfConversation = row;

                foreach (var turn in session.BuildTurns())
                {
                    sheet.Cell(row, 1).Value = conversationNumber;
                    sheet.Cell(row, 2).Value = Text(session.User);
                    SetDate(sheet.Cell(row, 3), session.Start);
                    sheet.Cell(row, 4).Value = turn.TurnNumber;
                    SetDate(sheet.Cell(row, 5), turn.PromptTime);
                    sheet.Cell(row, 6).Value = Text(turn.PromptText);
                    SetDate(sheet.Cell(row, 7), turn.ResponseTime);
                    sheet.Cell(row, 8).Value = Text(turn.ResponseText);

                    if (turn.PromptTime.HasValue && turn.ResponseTime.HasValue)
                    {
                        sheet.Cell(row, 9).Value = Math.Round((turn.ResponseTime.Value - turn.PromptTime.Value).TotalSeconds, 1);
                    }

                    sheet.Cell(row, 10).Value = Text(turn.App);
                    sheet.Cell(row, 11).Value = Text(turn.Conversation);
                    sheet.Cell(row, 12).Value = turn.PromptAttachments + turn.ResponseAttachments;
                    sheet.Cell(row, 13).Value = turn.ResponseLinks;
                    sheet.Cell(row, 14).Value = Text(session.DisplaySessionId);
                    sheet.Cell(row, 15).Value = Text(turn.AppClass);

                    sheet.Cell(row, 6).Style.Fill.BackgroundColor = PromptFill;
                    sheet.Cell(row, 8).Style.Fill.BackgroundColor = ResponseFill;

                    row++;
                    exchanges++;
                }

                if (row == firstRowOfConversation)
                {
                    continue;
                }

                var block = sheet.Range(firstRowOfConversation, 1, row - 1, headers.Length);
                if (conversationNumber % 2 == 0)
                {
                    block.Style.Fill.BackgroundColor = BandFill;
                    sheet.Range(firstRowOfConversation, 6, row - 1, 6).Style.Fill.BackgroundColor = PromptFill;
                    sheet.Range(firstRowOfConversation, 8, row - 1, 8).Style.Fill.BackgroundColor = ResponseFill;
                }

                // Visually separate conversations.
                sheet.Range(firstRowOfConversation, 1, firstRowOfConversation, headers.Length)
                     .Style.Border.TopBorder = XLBorderStyleValues.Medium;

                // Repeat the conversation number only on its first row so blocks read cleanly.
                for (var r = firstRowOfConversation + 1; r < row; r++)
                {
                    sheet.Cell(r, 1).Style.Font.FontColor = XLColor.FromHtml("#BFBFBF");
                }
            }

            FinishSheet(sheet, headers.Length, row - 1);

            sheet.Column(1).Width = 6;
            sheet.Column(2).Width = 24;
            sheet.Column(3).Width = 18;
            sheet.Column(4).Width = 6;
            sheet.Column(5).Width = 18;
            sheet.Column(6).Width = 70;
            sheet.Column(7).Width = 18;
            sheet.Column(8).Width = 90;
            sheet.Column(9).Width = 11;
            sheet.Column(10).Width = 34;
            sheet.Column(11).Width = 12;
            sheet.Column(12).Width = 12;
            sheet.Column(13).Width = 8;
            sheet.Column(14).Width = 30;
            sheet.Column(15).Width = 36;

            if (row > 2)
            {
                sheet.Range(2, 6, row - 1, 6).Style.Alignment.WrapText = true;
                sheet.Range(2, 8, row - 1, 8).Style.Alignment.WrapText = true;
                sheet.Range(2, 1, row - 1, headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            return exchanges;
        }

        /// <summary>
        /// One row per interaction, kept in conversation and turn order, colour coded by prompt/response.
        /// </summary>
        private static int WriteMessages(IXLWorksheet sheet, IReadOnlyList<SessionGroup> sessions)
        {
            string[] headers =
            {
                "#", "User", "Turn", "Seq", "Timestamp", "Type", "Text", "From",
                "App", "Chat type", "Attachments", "Links", "Session ID",
                "Request ID", "Interaction ID", "App class (raw)"
            };

            WriteHeader(sheet, headers);

            var row = 2;
            var conversationNumber = 0;
            var interactions = 0;

            foreach (var session in sessions)
            {
                conversationNumber++;
                var sequence = 0;
                var firstRowOfConversation = row;

                foreach (var turn in session.BuildTurns())
                {
                    var messages = new List<InteractionRow>();
                    if (turn.Prompt is not null)
                    {
                        messages.Add(turn.Prompt);
                    }
                    messages.AddRange(turn.Responses);

                    foreach (var message in messages)
                    {
                        sequence++;

                        sheet.Cell(row, 1).Value = conversationNumber;
                        sheet.Cell(row, 2).Value = Text(session.User);
                        sheet.Cell(row, 3).Value = turn.TurnNumber;
                        sheet.Cell(row, 4).Value = sequence;
                        SetDate(sheet.Cell(row, 5), message.Timestamp);
                        sheet.Cell(row, 6).Value = Text(message.Type);
                        sheet.Cell(row, 7).Value = Text(message.Text);
                        sheet.Cell(row, 8).Value = Text(message.Author);
                        sheet.Cell(row, 9).Value = Text(message.App);
                        sheet.Cell(row, 10).Value = Text(message.Conversation);
                        sheet.Cell(row, 11).Value = message.Attachments;
                        sheet.Cell(row, 12).Value = message.Links;
                        sheet.Cell(row, 13).Value = Text(session.DisplaySessionId);
                        sheet.Cell(row, 14).Value = Text(message.RequestId);
                        sheet.Cell(row, 15).Value = Text(message.Id);
                        sheet.Cell(row, 16).Value = Text(message.AppClass);

                        var isPrompt = string.Equals(message.Type, "Prompt", StringComparison.OrdinalIgnoreCase);
                        sheet.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor =
                            isPrompt ? PromptFill : ResponseFill;

                        if (isPrompt)
                        {
                            sheet.Cell(row, 7).Style.Font.Bold = true;
                        }

                        row++;
                        interactions++;
                    }
                }

                if (row > firstRowOfConversation)
                {
                    sheet.Range(firstRowOfConversation, 1, firstRowOfConversation, headers.Length)
                         .Style.Border.TopBorder = XLBorderStyleValues.Medium;
                }
            }

            FinishSheet(sheet, headers.Length, row - 1);

            sheet.Column(1).Width = 6;
            sheet.Column(2).Width = 24;
            sheet.Column(3).Width = 6;
            sheet.Column(4).Width = 6;
            sheet.Column(5).Width = 18;
            sheet.Column(6).Width = 10;
            sheet.Column(7).Width = 100;
            sheet.Column(8).Width = 22;
            sheet.Column(9).Width = 34;
            sheet.Column(10).Width = 12;
            sheet.Column(11).Width = 12;
            sheet.Column(12).Width = 8;
            sheet.Column(13).Width = 30;
            sheet.Column(14).Width = 28;
            sheet.Column(15).Width = 20;
            sheet.Column(16).Width = 36;

            if (row > 2)
            {
                sheet.Range(2, 7, row - 1, 7).Style.Alignment.WrapText = true;
                sheet.Range(2, 1, row - 1, headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            return interactions;
        }

        private static void WriteSessions(IXLWorksheet sheet, IReadOnlyList<SessionGroup> sessions)
        {
            string[] headers =
            {
                "#", "User", "User principal name", "Started", "Ended", "Duration (min)",
                "Messages", "Prompts", "Responses", "First prompt", "App", "Session ID",
                "App class (raw)"
            };

            WriteHeader(sheet, headers);

            var row = 2;
            var conversationNumber = 0;

            foreach (var session in sessions)
            {
                conversationNumber++;

                sheet.Cell(row, 1).Value = conversationNumber;
                sheet.Cell(row, 2).Value = Text(session.User);
                sheet.Cell(row, 3).Value = Text(session.UserPrincipalName);
                SetDate(sheet.Cell(row, 4), session.Start);
                SetDate(sheet.Cell(row, 5), session.End);

                if (session.Start.HasValue && session.End.HasValue)
                {
                    sheet.Cell(row, 6).Value = Math.Round((session.End.Value - session.Start.Value).TotalMinutes, 1);
                }

                sheet.Cell(row, 7).Value = session.MessageCount;
                sheet.Cell(row, 8).Value = session.Prompts;
                sheet.Cell(row, 9).Value = session.Responses;
                sheet.Cell(row, 10).Value = Text(session.FirstPrompt);
                sheet.Cell(row, 11).Value = Text(session.App);
                sheet.Cell(row, 12).Value = Text(session.DisplaySessionId);
                sheet.Cell(row, 13).Value = Text(session.AppClass);

                row++;
            }

            FinishSheet(sheet, headers.Length, row - 1);

            sheet.Column(1).Width = 6;
            sheet.Column(2).Width = 24;
            sheet.Column(3).Width = 28;
            sheet.Column(4).Width = 18;
            sheet.Column(5).Width = 18;
            sheet.Column(6).Width = 14;
            sheet.Column(7).Width = 10;
            sheet.Column(8).Width = 10;
            sheet.Column(9).Width = 11;
            sheet.Column(10).Width = 80;
            sheet.Column(11).Width = 34;
            sheet.Column(12).Width = 30;
            sheet.Column(13).Width = 36;

            if (row > 2)
            {
                sheet.Range(2, 10, row - 1, 10).Style.Alignment.WrapText = true;
                sheet.Range(2, 1, row - 1, headers.Length).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }
        }

        private static void WriteSummary(
            IXLWorksheet sheet,
            IReadOnlyList<SessionGroup> sessions,
            ExportContext context,
            int exchanges,
            int interactions)
        {
            sheet.Cell(1, 1).Value = "Microsoft 365 Copilot interaction export";
            sheet.Cell(1, 1).Style.Font.Bold = true;
            sheet.Cell(1, 1).Style.Font.FontSize = 16;
            sheet.Cell(1, 1).Style.Font.FontColor = TitleColor;

            var row = 3;
            void Detail(string label, string value)
            {
                sheet.Cell(row, 1).Value = label;
                sheet.Cell(row, 1).Style.Font.Bold = true;
                sheet.Cell(row, 2).Value = Text(value);
                row++;
            }

            Detail("Generated", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            Detail("Scope", context.Scope);
            Detail("Endpoint", context.Endpoint);
            Detail("Date range", context.DateRange);
            Detail("App filter", context.AppClassFilter);
            Detail("Interaction type filter", context.TypeFilter);
            Detail("Search filter", context.SearchFilter);
            row++;

            Detail("Users with activity", sessions.Select(s => s.UserPrincipalName).Distinct().Count().ToString());
            Detail("Conversations", sessions.Count.ToString());
            Detail("Exchanges (prompt + reply)", exchanges.ToString());
            Detail("Interactions exported", interactions.ToString());
            Detail("Interactions loaded in app", context.TotalLoaded.ToString());
            row++;

            sheet.Cell(row, 1).Value = "Activity by user";
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 12;
            sheet.Cell(row, 1).Style.Font.FontColor = TitleColor;
            row++;

            string[] headers = { "User", "User principal name", "Conversations", "Prompts", "Responses", "Avg reply (sec)", "First activity", "Last activity" };
            var headerRow = row;
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = sheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = HeaderFill;
            }
            row++;

            var perUser = sessions
                .GroupBy(s => (s.User, s.UserPrincipalName))
                .OrderByDescending(g => g.Sum(s => s.Prompts));

            foreach (var group in perUser)
            {
                var replyTimes = group
                    .SelectMany(s => s.BuildTurns())
                    .Where(t => t.PromptTime.HasValue && t.ResponseTime.HasValue)
                    .Select(t => (t.ResponseTime!.Value - t.PromptTime!.Value).TotalSeconds)
                    .ToList();

                sheet.Cell(row, 1).Value = Text(group.Key.User);
                sheet.Cell(row, 2).Value = Text(group.Key.UserPrincipalName);
                sheet.Cell(row, 3).Value = group.Count();
                sheet.Cell(row, 4).Value = group.Sum(s => s.Prompts);
                sheet.Cell(row, 5).Value = group.Sum(s => s.Responses);

                if (replyTimes.Count > 0)
                {
                    sheet.Cell(row, 6).Value = Math.Round(replyTimes.Average(), 1);
                }

                SetDate(sheet.Cell(row, 7), group.Min(s => s.Start));
                SetDate(sheet.Cell(row, 8), group.Max(s => s.End));
                row++;
            }

            if (context.Issues.Count > 0)
            {
                row++;
                sheet.Cell(row, 1).Value = $"Users skipped ({context.Issues.Count})";
                sheet.Cell(row, 1).Style.Font.Bold = true;
                sheet.Cell(row, 1).Style.Font.FontSize = 12;
                sheet.Cell(row, 1).Style.Font.FontColor = TitleColor;
                row++;

                foreach (var issue in context.Issues)
                {
                    sheet.Cell(row, 1).Value = Text(issue);
                    row++;
                }
            }

            sheet.Column(1).Width = 30;
            sheet.Column(2).Width = 34;
            for (var i = 3; i <= 8; i++)
            {
                sheet.Column(i).Width = 16;
            }
        }

        private static void WriteHeader(IXLWorksheet sheet, IReadOnlyList<string> headers)
        {
            for (var i = 0; i < headers.Count; i++)
            {
                var cell = sheet.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = HeaderFill;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }

            sheet.SheetView.FreezeRows(1);
            sheet.Row(1).Height = 22;
        }

        private static void FinishSheet(IXLWorksheet sheet, int columnCount, int lastRow)
        {
            var headerRange = sheet.Range(1, 1, Math.Max(lastRow, 1), columnCount);
            headerRange.SetAutoFilter();
        }

        private static void SetDate(IXLCell cell, DateTime? value)
        {
            if (!value.HasValue)
            {
                return;
            }

            cell.Value = value.Value;
            cell.Style.NumberFormat.Format = "yyyy-mm-dd hh:mm:ss";
        }

        private static string Text(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Length <= MaxCellLength ? value : value[..MaxCellLength] + "...";
        }
    }
}
