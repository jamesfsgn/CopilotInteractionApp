using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CopilotInteractionApp.Models;
using CopilotInteractionApp.Services;

namespace CopilotInteractionApp
{
    /// <summary>
    /// Main window. Collects connection details and filters, runs a pull through
    /// <see cref="GraphInteractionClient"/>, presents the results as conversations
    /// (sessions grid) and messages (message grid), and exports to Excel.
    /// </summary>
    public partial class Form1 : Form
    {
        /// <summary>Where non-secret preferences are stored between runs.</summary>
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CopilotInteractionApp",
            "settings.json");

        private readonly GraphInteractionClient _client = new();

        /// <summary>Bound to the message grid: the messages of the selected conversation.</summary>
        private readonly BindingSource _bindingSource = new();

        /// <summary>Bound to the sessions grid: one entry per conversation.</summary>
        private readonly BindingSource _sessionsBinding = new();

        /// <summary>Everything returned by the last pull, before UI filtering.</summary>
        private readonly List<InteractionRow> _allRows = new();

        /// <summary>Rows surviving the search and type filters; this is what gets exported.</summary>
        private List<InteractionRow> _filteredRows = new();

        // Captured from the last pull so the export's Summary sheet can describe it.
        private string _lastScope = "(no pull yet)";
        private string _lastEndpoint = string.Empty;
        private IReadOnlyList<string> _lastIssues = Array.Empty<string>();

        private CancellationTokenSource? _cts;

        public Form1()
        {
            InitializeComponent();

            cboTypeFilter.Items.AddRange(new object[] { "All interactions", "Prompts only", "Responses only" });
            cboTypeFilter.SelectedIndex = 0;

            cboAppClass.Items.AddRange(AppClassCatalog.Options.Cast<object>().ToArray());
            cboAppClass.SelectedIndex = 0;

            dtpFrom.Value = DateTime.Today.AddDays(-7);
            dtpTo.Value = DateTime.Today;
            chkUseBeta.Checked = true;

            dgvInteractions.DataSource = _bindingSource;
            dgvSessions.DataSource = _sessionsBinding;
            splitTop.Panel1Collapsed = !chkGroupBySession.Checked;
            UpdateDateControls();
            chkDateFilter.CheckedChanged += (_, _) => UpdateDateControls();
            dtpFrom.ValueChanged += DtpFrom_ValueChanged;
            dtpTo.ValueChanged += DtpTo_ValueChanged;

            LoadSettings();
            SetIssues(Array.Empty<string>());
        }

        /// <summary>
        /// Keeps the range valid by nudging the other picker instead of failing the pull later.
        /// </summary>
        private void DtpFrom_ValueChanged(object? sender, EventArgs e)
        {
            if (dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                dtpTo.Value = dtpFrom.Value.Date;
            }
        }

        private void DtpTo_ValueChanged(object? sender, EventArgs e)
        {
            if (dtpTo.Value.Date < dtpFrom.Value.Date)
            {
                dtpFrom.Value = dtpTo.Value.Date;
            }
        }

        private string? SelectedAppClass()
        {
            var value = (cboAppClass.SelectedItem as AppClassOption)?.Value;
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private InteractionQueryOptions BuildOptions()
        {
            if (string.IsNullOrWhiteSpace(txtTenantId.Text)) throw new ArgumentException("Tenant ID is required.");
            if (string.IsNullOrWhiteSpace(txtClientId.Text)) throw new ArgumentException("Client ID is required.");
            if (string.IsNullOrWhiteSpace(txtClientSecret.Text)) throw new ArgumentException("Client secret is required.");

            DateTimeOffset? fromUtc = null;
            DateTimeOffset? toUtc = null;
            if (chkDateFilter.Checked)
            {
                var (from, to) = DateRangeFilter.BuildUtcBoundaries(dtpFrom.Value, dtpTo.Value);
                fromUtc = from;
                toUtc = to;
            }

            return new InteractionQueryOptions
            {
                TenantId = txtTenantId.Text.Trim(),
                ClientId = txtClientId.Text.Trim(),
                ClientSecret = txtClientSecret.Text,
                UserId = txtUserId.Text.Trim(),
                CopilotLicensedOnly = chkLicensedOnly.Checked,
                UseBeta = chkUseBeta.Checked,
                Top = (int)numTop.Value,
                MaxItems = (int)numMaxItems.Value,
                AppClass = SelectedAppClass(),
                FromDateUtc = fromUtc,
                ToDateUtc = toUtc
            };
        }

        private void UpdateDateControls()
        {
            dtpFrom.Enabled = chkDateFilter.Checked;
            dtpTo.Enabled = chkDateFilter.Checked;
        }

        private async void BtnFetch_Click(object? sender, EventArgs e)
        {
            if (chkDateFilter.Checked && dtpFrom.Value.Date > dtpTo.Value.Date)
            {
                MessageBox.Show(this, "The 'from' date must be on or before the 'to' date.",
                    "Invalid date range", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUserId.Text))
            {
                var message = chkLicensedOnly.Checked
                    ? "No user was specified, so every user in the tenant will be checked for a Microsoft 365 " +
                      "Copilot license and interactions will be collected for those who have one."
                    : "No user was specified, so interactions will be collected for every enabled user in the tenant, " +
                      "including users without a Copilot license.";

                var confirm = MessageBox.Show(this,
                    message + Environment.NewLine + Environment.NewLine +
                    "This enumerates all users (requires the User.Read.All application permission) and can take a long time. Continue?",
                    "Collect interactions for all users", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes)
                {
                    return;
                }
            }

            InteractionQueryOptions options;
            try
            {
                options = BuildOptions();
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(this, ex.Message, "Connection details required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            SetBusy(true);
            var progress = new Progress<string>(message => lblStatus.Text = message);

            try
            {
                var result = await _client.GetInteractionsAsync(options, progress, _cts.Token);

                _allRows.Clear();
                _allRows.AddRange(result.Items
                    .OrderByDescending(i => i.Interaction.CreatedDateTime ?? DateTimeOffset.MinValue)
                    .Select(InteractionRow.FromUserInteraction));

                ApplyFilter();

                var scope = !string.IsNullOrWhiteSpace(options.UserId)
                    ? options.UserId
                    : options.CopilotLicensedOnly
                        ? $"{result.UsersQueried} Copilot-licensed user(s) of {result.UsersScanned} scanned"
                        : $"{result.UsersQueried} user(s)";

                var status = $"Loaded {_allRows.Count} interaction(s) for {scope}.";
                if (result.LimitReached)
                {
                    status += $" Stopped at the {options.MaxItems} item limit.";
                }
                if (result.Errors.Count > 0)
                {
                    status += $" {result.Errors.Count} user(s) skipped - see the Issues tab.";
                }
                lblStatus.Text = status;

                _lastScope = scope;
                _lastEndpoint = options.UseBeta
                    ? "https://graph.microsoft.com/beta"
                    : "https://graph.microsoft.com/v1.0";

                var issues = new List<string>(result.Errors);
                if (result.UsersWithoutCopilotLicense > 0)
                {
                    issues.Insert(0,
                        $"{result.UsersWithoutCopilotLicense} of {result.UsersScanned} enabled user(s) were skipped " +
                        "because they do not have an enabled Microsoft 365 Copilot service plan.");
                }
                if (result.UsedClientSideFilterFallback)
                {
                    issues.Insert(0,
                        "Microsoft Graph rejected the server-side $filter for at least one user. " +
                        "The date and app filters were applied locally instead, so results may be capped early for those users.");
                }

                _lastIssues = issues;

                SetIssues(issues);
                SaveSettings();
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Request canceled.";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Request failed.";
                MessageBox.Show(this, ex.Message, "Unable to retrieve interactions",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                _cts?.Dispose();
                _cts = null;
            }
        }

        private void BtnCancel_Click(object? sender, EventArgs e) => _cts?.Cancel();

        /// <summary>
        /// Surfaces per-user failures (unlicensed users, access denied, ...) in the Issues tab
        /// instead of interrupting the run with a dialog.
        /// </summary>
        private void SetIssues(IReadOnlyList<string> errors)
        {
            tabIssues.Text = errors.Count == 0 ? "Issues" : $"Issues ({errors.Count})";

            txtIssues.Text = errors.Count == 0
                ? "No issues. Skipped users and filter warnings are reported here."
                : "The following issues were reported during the last pull:" +
                  Environment.NewLine + Environment.NewLine +
                  string.Join(Environment.NewLine, errors);

            txtIssues.SelectionStart = 0;
            txtIssues.ScrollToCaret();
        }

        private void Filter_Changed(object? sender, EventArgs e) => ApplyFilter();

        private void ApplyFilter()
        {
            var search = txtSearch.Text.Trim();
            IEnumerable<InteractionRow> rows = _allRows;

            rows = cboTypeFilter.SelectedIndex switch
            {
                1 => rows.Where(r => string.Equals(r.Type, "Prompt", StringComparison.OrdinalIgnoreCase)),
                2 => rows.Where(r => string.Equals(r.Type, "Response", StringComparison.OrdinalIgnoreCase)),
                _ => rows
            };

            if (search.Length > 0)
            {
                rows = rows.Where(r =>
                    Contains(r.Text, search) ||
                    Contains(r.User, search) ||
                    Contains(r.UserPrincipalName, search) ||
                    Contains(r.Author, search) ||
                    Contains(r.App, search) ||
                    Contains(r.AppClass, search) ||
                    Contains(r.SessionId, search) ||
                    Contains(r.RequestId, search));
            }

            var list = rows.ToList();
            _filteredRows = list;
            btnExport.Enabled = list.Count > 0;

            var totalText = list.Count == _allRows.Count
                ? $"{list.Count} interactions"
                : $"{list.Count} of {_allRows.Count} interactions";

            if (chkGroupBySession.Checked)
            {
                splitTop.Panel1Collapsed = false;

                var previousKey = CurrentSessionKey();
                var sessions = SessionGroup.Build(list);
                _sessionsBinding.DataSource = new BindingList<SessionGroup>(sessions);

                var index = previousKey is null
                    ? -1
                    : sessions.FindIndex(s => SessionKey(s) == previousKey);
                if (sessions.Count > 0)
                {
                    _sessionsBinding.Position = index >= 0 ? index : 0;
                }

                ShowSelectedSessionMessages();
                lblCount.Text = $"{totalText} in {sessions.Count} session(s)";
            }
            else
            {
                splitTop.Panel1Collapsed = true;
                _bindingSource.DataSource = new BindingList<InteractionRow>(list);
                lblCount.Text = totalText;
            }

            ShowDetails();
        }

        private static string SessionKey(SessionGroup session) =>
            session.UserPrincipalName + "|" + session.SessionId;

        private string? CurrentSessionKey() =>
            _sessionsBinding.Current is SessionGroup current ? SessionKey(current) : null;

        private void DgvSessions_SelectionChanged(object? sender, EventArgs e)
        {
            if (!chkGroupBySession.Checked)
            {
                return;
            }

            ShowSelectedSessionMessages();
            ShowDetails();
        }

        private void ChkGroupBySession_CheckedChanged(object? sender, EventArgs e) => ApplyFilter();

        /// <summary>
        /// Binds the message grid to the conversation selected in the sessions list, in chronological order.
        /// </summary>
        private void ShowSelectedSessionMessages()
        {
            var messages = (dgvSessions.CurrentRow?.DataBoundItem as SessionGroup)?.Messages
                           ?? (_sessionsBinding.Current as SessionGroup)?.Messages
                           ?? new List<InteractionRow>();

            _bindingSource.DataSource = new BindingList<InteractionRow>(messages.ToList());
        }

        private static bool Contains(string? value, string search) =>
            value is not null && value.Contains(search, StringComparison.OrdinalIgnoreCase);

        private void DgvInteractions_SelectionChanged(object? sender, EventArgs e) => ShowDetails();

        private void ShowDetails()
        {
            if (dgvInteractions.CurrentRow?.DataBoundItem is not InteractionRow row)
            {
                txtDetails.Text = string.Empty;
                return;
            }

            var i = row.Source;
            var sb = new StringBuilder();
            sb.AppendLine($"Timestamp:      {i.CreatedDateTime?.ToLocalTime():F}");
            sb.AppendLine($"User:           {row.User} {(string.IsNullOrEmpty(row.UserPrincipalName) ? string.Empty : $"<{row.UserPrincipalName}>")}");
            sb.AppendLine($"Type:           {row.Type} ({i.InteractionType})");
            sb.AppendLine($"From:           {row.Author}");
            sb.AppendLine($"App:            {row.App}");
            sb.AppendLine($"App class:      {i.AppClass}");
            sb.AppendLine($"Conversation:   {i.ConversationType}");
            sb.AppendLine($"Locale:         {i.Locale}");
            sb.AppendLine($"Interaction ID: {i.Id}");
            sb.AppendLine($"Session ID:     {i.SessionId}");
            sb.AppendLine($"Request ID:     {i.RequestId}");
            if (chkGroupBySession.Checked && dgvInteractions.CurrentRow is not null)
            {
                sb.AppendLine($"Position:       message {dgvInteractions.CurrentRow.Index + 1} of {dgvInteractions.RowCount} in this session");
            }
            sb.AppendLine();
            sb.AppendLine($"--- Body ({i.Body?.ContentType}) ---");
            sb.AppendLine(i.Body?.Content ?? "(empty)");

            if (i.Contexts is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine("--- Contexts ---");
                foreach (var context in i.Contexts)
                {
                    sb.AppendLine($"[{context.ContextType}] {context.DisplayName} -> {context.ContextReference}");
                }
            }

            if (i.Attachments is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine("--- Attachments ---");
                foreach (var attachment in i.Attachments)
                {
                    sb.AppendLine($"[{attachment.ContentType}] {attachment.Name ?? attachment.AttachmentId}");
                    if (!string.IsNullOrWhiteSpace(attachment.ContentUrl))
                    {
                        sb.AppendLine($"    {attachment.ContentUrl}");
                    }
                    if (!string.IsNullOrWhiteSpace(attachment.Content))
                    {
                        sb.AppendLine($"    {InteractionRow.ToPlainText(attachment.Content)}");
                    }
                }
            }

            if (i.Links is { Count: > 0 })
            {
                sb.AppendLine();
                sb.AppendLine("--- Links (resources used) ---");
                foreach (var link in i.Links)
                {
                    sb.AppendLine($"[{link.LinkType}] {link.DisplayName}");
                    sb.AppendLine($"    {link.LinkUrl}");
                }
            }

            txtDetails.Text = sb.ToString();
            txtDetails.SelectionStart = 0;
            txtDetails.ScrollToCaret();
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            if (_filteredRows.Count == 0)
            {
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "Excel workbook (*.xlsx)|*.xlsx",
                DefaultExt = "xlsx",
                FileName = $"copilot-conversations-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var sessions = SessionGroup.Build(_filteredRows);
                var context = new ExportContext
                {
                    Scope = _lastScope,
                    Endpoint = _lastEndpoint,
                    DateRange = chkDateFilter.Checked
                        ? $"{dtpFrom.Value:yyyy-MM-dd} to {dtpTo.Value:yyyy-MM-dd}"
                        : "All available history",
                    AppClassFilter = cboAppClass.SelectedItem?.ToString() ?? AppClassCatalog.All.Name,
                    TypeFilter = cboTypeFilter.SelectedItem?.ToString() ?? "All interactions",
                    SearchFilter = string.IsNullOrWhiteSpace(txtSearch.Text) ? "(none)" : txtSearch.Text.Trim(),
                    TotalLoaded = _allRows.Count,
                    Issues = _lastIssues
                };

                var summary = ExcelExporter.Write(dialog.FileName, sessions, context);

                lblStatus.Text =
                    $"Exported {summary.Exchanges} exchange(s) across {summary.Conversations} conversation(s) " +
                    $"to {dialog.FileName}.";

                if (MessageBox.Show(this, "Export complete. Open the workbook now?", "Export complete",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Export failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetBusy(bool busy)
        {
            btnFetch.Enabled = !busy;
            btnCancel.Enabled = busy;
            txtTenantId.ReadOnly = busy;
            txtClientId.ReadOnly = busy;
            txtClientSecret.ReadOnly = busy;
            txtUserId.ReadOnly = busy;
            progressBar.Visible = busy;
            Cursor = busy ? Cursors.AppStarting : Cursors.Default;
        }

        private sealed class AppSettings
        {
            public string? TenantId { get; set; }
            public string? ClientId { get; set; }
            public string? UserId { get; set; }
            public bool CopilotLicensedOnly { get; set; } = true;
            public string? AppClass { get; set; }
            public bool UseBeta { get; set; }
            public int Top { get; set; } = 100;
            public int MaxItems { get; set; } = 1000;
        }

        /// <summary>
        /// Persists non-secret connection details. The client secret is never written to disk.
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    TenantId = txtTenantId.Text.Trim(),
                    ClientId = txtClientId.Text.Trim(),
                    UserId = txtUserId.Text.Trim(),
                    CopilotLicensedOnly = chkLicensedOnly.Checked,
                    AppClass = SelectedAppClass() ?? string.Empty,
                    UseBeta = chkUseBeta.Checked,
                    Top = (int)numTop.Value,
                    MaxItems = (int)numMaxItems.Value
                };

                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch
            {
                // Persisting preferences is best effort only.
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath))
                {
                    return;
                }

                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (settings is null)
                {
                    return;
                }

                txtTenantId.Text = settings.TenantId ?? string.Empty;
                txtClientId.Text = settings.ClientId ?? string.Empty;
                txtUserId.Text = settings.UserId ?? string.Empty;
                chkLicensedOnly.Checked = settings.CopilotLicensedOnly;
                cboAppClass.SelectedItem = AppClassCatalog.Find(settings.AppClass);
                chkUseBeta.Checked = settings.UseBeta;
                numTop.Value = Math.Clamp(settings.Top, (int)numTop.Minimum, (int)numTop.Maximum);
                numMaxItems.Value = Math.Clamp(settings.MaxItems, (int)numMaxItems.Minimum, (int)numMaxItems.Maximum);
            }
            catch
            {
                // Ignore corrupt settings and start with defaults.
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _cts?.Cancel();
            _client.Dispose();
            base.OnFormClosed(e);
        }
    }
}
