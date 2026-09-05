namespace CopilotInteractionApp
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            grpConnection = new GroupBox();
            layoutConnection = new TableLayoutPanel();
            lblTenantId = new Label();
            txtTenantId = new TextBox();
            lblClientId = new Label();
            txtClientId = new TextBox();
            lblClientSecret = new Label();
            txtClientSecret = new TextBox();
            lblUserId = new Label();
            txtUserId = new TextBox();
            lblAppClass = new Label();
            cboAppClass = new ComboBox();
            lblTop = new Label();
            numTop = new NumericUpDown();
            chkLicensedOnly = new CheckBox();
            flowActions = new FlowLayoutPanel();
            chkDateFilter = new CheckBox();
            dtpFrom = new DateTimePicker();
            lblTo = new Label();
            dtpTo = new DateTimePicker();
            chkUseBeta = new CheckBox();
            lblMaxItems = new Label();
            numMaxItems = new NumericUpDown();
            btnFetch = new Button();
            btnCancel = new Button();
            btnExport = new Button();

            flowFilter = new FlowLayoutPanel();
            lblSearch = new Label();
            txtSearch = new TextBox();
            cboTypeFilter = new ComboBox();
            lblCount = new Label();
            chkGroupBySession = new CheckBox();

            splitMain = new SplitContainer();
            splitTop = new SplitContainer();
            dgvSessions = new DataGridView();
            colSessionUser = new DataGridViewTextBoxColumn();
            colSessionStart = new DataGridViewTextBoxColumn();
            colSessionMessages = new DataGridViewTextBoxColumn();
            colSessionPrompts = new DataGridViewTextBoxColumn();
            colSessionFirstPrompt = new DataGridViewTextBoxColumn();
            colSessionApp = new DataGridViewTextBoxColumn();
            colSessionKey = new DataGridViewTextBoxColumn();
            tabsBottom = new TabControl();
            tabDetails = new TabPage();
            tabIssues = new TabPage();
            txtIssues = new TextBox();
            dgvInteractions = new DataGridView();
            colTimestamp = new DataGridViewTextBoxColumn();
            colUser = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colAuthor = new DataGridViewTextBoxColumn();
            colApp = new DataGridViewTextBoxColumn();
            colConversation = new DataGridViewTextBoxColumn();
            colText = new DataGridViewTextBoxColumn();
            colAttachments = new DataGridViewTextBoxColumn();
            colLinks = new DataGridViewTextBoxColumn();
            colSessionId = new DataGridViewTextBoxColumn();
            colRequestId = new DataGridViewTextBoxColumn();
            colId = new DataGridViewTextBoxColumn();
            txtDetails = new TextBox();

            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel();
            progressBar = new ToolStripProgressBar();

            grpConnection.SuspendLayout();
            layoutConnection.SuspendLayout();
            flowActions.SuspendLayout();
            flowFilter.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTop).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxItems).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitTop).BeginInit();
            splitTop.Panel1.SuspendLayout();
            splitTop.Panel2.SuspendLayout();
            splitTop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSessions).BeginInit();
            tabsBottom.SuspendLayout();
            tabDetails.SuspendLayout();
            tabIssues.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvInteractions).BeginInit();
            statusStrip.SuspendLayout();
            SuspendLayout();

            // grpConnection
            grpConnection.AutoSize = true;
            grpConnection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            grpConnection.Controls.Add(layoutConnection);
            grpConnection.Dock = DockStyle.Top;
            grpConnection.Location = new Point(0, 0);
            grpConnection.Name = "grpConnection";
            grpConnection.Padding = new Padding(8);
            grpConnection.TabIndex = 0;
            grpConnection.TabStop = false;
            grpConnection.Text = "Microsoft Graph connection (app-only: AiEnterpriseInteraction.Read.All, plus User.Read.All for all-user pulls)";

            // layoutConnection
            layoutConnection.AutoSize = true;
            layoutConnection.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layoutConnection.ColumnCount = 6;
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layoutConnection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layoutConnection.Dock = DockStyle.Top;
            layoutConnection.Location = new Point(8, 24);
            layoutConnection.Name = "layoutConnection";
            layoutConnection.RowCount = 3;
            layoutConnection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutConnection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutConnection.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layoutConnection.TabIndex = 0;

            lblTenantId.Anchor = AnchorStyles.Left;
            lblTenantId.AutoSize = true;
            lblTenantId.Margin = new Padding(3, 6, 3, 6);
            lblTenantId.Name = "lblTenantId";
            lblTenantId.Text = "Tenant ID:";

            txtTenantId.Dock = DockStyle.Fill;
            txtTenantId.Name = "txtTenantId";
            txtTenantId.PlaceholderText = "contoso.onmicrosoft.com or GUID";
            txtTenantId.TabIndex = 1;

            lblClientId.Anchor = AnchorStyles.Left;
            lblClientId.AutoSize = true;
            lblClientId.Margin = new Padding(12, 6, 3, 6);
            lblClientId.Name = "lblClientId";
            lblClientId.Text = "Client ID:";

            txtClientId.Dock = DockStyle.Fill;
            txtClientId.Name = "txtClientId";
            txtClientId.PlaceholderText = "App registration (client) ID";
            txtClientId.TabIndex = 2;

            lblClientSecret.Anchor = AnchorStyles.Left;
            lblClientSecret.AutoSize = true;
            lblClientSecret.Margin = new Padding(12, 6, 3, 6);
            lblClientSecret.Name = "lblClientSecret";
            lblClientSecret.Text = "Client secret:";

            txtClientSecret.Dock = DockStyle.Fill;
            txtClientSecret.Name = "txtClientSecret";
            txtClientSecret.PlaceholderText = "Not saved to disk";
            txtClientSecret.UseSystemPasswordChar = true;
            txtClientSecret.TabIndex = 3;

            lblUserId.Anchor = AnchorStyles.Left;
            lblUserId.AutoSize = true;
            lblUserId.Margin = new Padding(3, 6, 3, 6);
            lblUserId.Name = "lblUserId";
            lblUserId.Text = "User (blank = all):";

            txtUserId.Dock = DockStyle.Fill;
            txtUserId.Name = "txtUserId";
            txtUserId.PlaceholderText = "Leave blank for all users in the tenant";
            txtUserId.TabIndex = 4;

            lblAppClass.Anchor = AnchorStyles.Left;
            lblAppClass.AutoSize = true;
            lblAppClass.Margin = new Padding(12, 6, 3, 6);
            lblAppClass.Name = "lblAppClass";
            lblAppClass.Text = "App filter:";

            cboAppClass.Dock = DockStyle.Fill;
            cboAppClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAppClass.Name = "cboAppClass";
            cboAppClass.TabIndex = 5;

            lblTop.Anchor = AnchorStyles.Left;
            lblTop.AutoSize = true;
            lblTop.Margin = new Padding(12, 6, 3, 6);
            lblTop.Name = "lblTop";
            lblTop.Text = "Page size ($top):";

            numTop.Anchor = AnchorStyles.Left;
            numTop.Maximum = 1000;
            numTop.Minimum = 1;
            numTop.Name = "numTop";
            numTop.TabIndex = 6;
            numTop.Value = 100;
            numTop.Width = 80;

            chkLicensedOnly.Anchor = AnchorStyles.Left;
            chkLicensedOnly.AutoSize = true;
            chkLicensedOnly.Checked = true;
            chkLicensedOnly.Margin = new Padding(3, 8, 12, 3);
            chkLicensedOnly.Name = "chkLicensedOnly";
            chkLicensedOnly.TabIndex = 7;
            chkLicensedOnly.Text = "Only users with a Copilot license";

            // flowActions
            flowActions.AutoSize = true;
            flowActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowActions.Margin = new Padding(0, 6, 0, 0);
            flowActions.Name = "flowActions";
            flowActions.WrapContents = true;

            chkDateFilter.Anchor = AnchorStyles.Left;
            chkDateFilter.AutoSize = true;
            chkDateFilter.Margin = new Padding(3, 8, 6, 3);
            chkDateFilter.Name = "chkDateFilter";
            chkDateFilter.Text = "Date range:";
            chkDateFilter.TabIndex = 8;

            dtpFrom.Format = DateTimePickerFormat.Short;
            dtpFrom.Margin = new Padding(3, 5, 3, 3);
            dtpFrom.MinDate = new DateTime(2015, 1, 1);
            dtpFrom.Name = "dtpFrom";
            dtpFrom.TabIndex = 9;
            dtpFrom.Width = 110;

            lblTo.Anchor = AnchorStyles.Left;
            lblTo.AutoSize = true;
            lblTo.Margin = new Padding(3, 9, 3, 3);
            lblTo.Name = "lblTo";
            lblTo.Text = "to";

            dtpTo.Format = DateTimePickerFormat.Short;
            dtpTo.Margin = new Padding(3, 5, 12, 3);
            dtpTo.MinDate = new DateTime(2015, 1, 1);
            dtpTo.Name = "dtpTo";
            dtpTo.TabIndex = 10;
            dtpTo.Width = 110;

            chkUseBeta.Anchor = AnchorStyles.Left;
            chkUseBeta.AutoSize = true;
            chkUseBeta.Margin = new Padding(3, 8, 12, 3);
            chkUseBeta.Name = "chkUseBeta";
            chkUseBeta.Text = "Use /beta endpoint";
            chkUseBeta.TabIndex = 11;

            lblMaxItems.Anchor = AnchorStyles.Left;
            lblMaxItems.AutoSize = true;
            lblMaxItems.Margin = new Padding(3, 9, 3, 3);
            lblMaxItems.Name = "lblMaxItems";
            lblMaxItems.Text = "Max items:";

            numMaxItems.Anchor = AnchorStyles.Left;
            numMaxItems.Increment = 100;
            numMaxItems.Margin = new Padding(3, 5, 12, 3);
            numMaxItems.Maximum = 100000;
            numMaxItems.Minimum = 1;
            numMaxItems.Name = "numMaxItems";
            numMaxItems.TabIndex = 12;
            numMaxItems.Value = 1000;
            numMaxItems.Width = 90;

            btnFetch.AutoSize = true;
            btnFetch.Name = "btnFetch";
            btnFetch.Padding = new Padding(8, 2, 8, 2);
            btnFetch.TabIndex = 13;
            btnFetch.Text = "Get interactions";
            btnFetch.UseVisualStyleBackColor = true;
            btnFetch.Click += BtnFetch_Click;

            btnCancel.AutoSize = true;
            btnCancel.Enabled = false;
            btnCancel.Name = "btnCancel";
            btnCancel.Padding = new Padding(8, 2, 8, 2);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += BtnCancel_Click;

            btnExport.AutoSize = true;
            btnExport.Enabled = false;
            btnExport.Name = "btnExport";
            btnExport.Padding = new Padding(8, 2, 8, 2);
            btnExport.TabIndex = 15;
            btnExport.Text = "Export to Excel...";
            btnExport.UseVisualStyleBackColor = true;
            btnExport.Click += BtnExport_Click;

            flowActions.Controls.Add(chkLicensedOnly);
            flowActions.Controls.Add(chkDateFilter);
            flowActions.Controls.Add(dtpFrom);
            flowActions.Controls.Add(lblTo);
            flowActions.Controls.Add(dtpTo);
            flowActions.Controls.Add(chkUseBeta);
            flowActions.Controls.Add(lblMaxItems);
            flowActions.Controls.Add(numMaxItems);
            flowActions.Controls.Add(btnFetch);
            flowActions.Controls.Add(btnCancel);
            flowActions.Controls.Add(btnExport);

            layoutConnection.Controls.Add(lblTenantId, 0, 0);
            layoutConnection.Controls.Add(txtTenantId, 1, 0);
            layoutConnection.Controls.Add(lblClientId, 2, 0);
            layoutConnection.Controls.Add(txtClientId, 3, 0);
            layoutConnection.Controls.Add(lblClientSecret, 4, 0);
            layoutConnection.Controls.Add(txtClientSecret, 5, 0);
            layoutConnection.Controls.Add(lblUserId, 0, 1);
            layoutConnection.Controls.Add(txtUserId, 1, 1);
            layoutConnection.Controls.Add(lblAppClass, 2, 1);
            layoutConnection.Controls.Add(cboAppClass, 3, 1);
            layoutConnection.Controls.Add(lblTop, 4, 1);
            layoutConnection.Controls.Add(numTop, 5, 1);
            layoutConnection.Controls.Add(flowActions, 0, 2);
            layoutConnection.SetColumnSpan(flowActions, 6);

            // flowFilter
            flowFilter.AutoSize = true;
            flowFilter.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flowFilter.Dock = DockStyle.Top;
            flowFilter.Name = "flowFilter";
            flowFilter.Padding = new Padding(6, 4, 6, 4);
            flowFilter.TabIndex = 1;
            flowFilter.WrapContents = false;

            lblSearch.Anchor = AnchorStyles.Left;
            lblSearch.AutoSize = true;
            lblSearch.Margin = new Padding(3, 8, 3, 3);
            lblSearch.Name = "lblSearch";
            lblSearch.Text = "Search:";

            txtSearch.Margin = new Padding(3, 4, 12, 3);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Filter loaded rows...";
            txtSearch.TabIndex = 15;
            txtSearch.Width = 260;
            txtSearch.TextChanged += Filter_Changed;

            cboTypeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTypeFilter.Margin = new Padding(3, 4, 12, 3);
            cboTypeFilter.Name = "cboTypeFilter";
            cboTypeFilter.TabIndex = 16;
            cboTypeFilter.Width = 170;
            cboTypeFilter.SelectedIndexChanged += Filter_Changed;

            lblCount.Anchor = AnchorStyles.Left;
            lblCount.AutoSize = true;
            lblCount.Margin = new Padding(3, 8, 3, 3);
            lblCount.Name = "lblCount";
            lblCount.Text = "0 interactions";

            chkGroupBySession.Anchor = AnchorStyles.Left;
            chkGroupBySession.AutoSize = true;
            chkGroupBySession.Checked = true;
            chkGroupBySession.Margin = new Padding(12, 7, 3, 3);
            chkGroupBySession.Name = "chkGroupBySession";
            chkGroupBySession.TabIndex = 21;
            chkGroupBySession.Text = "Group by session";
            chkGroupBySession.CheckedChanged += ChkGroupBySession_CheckedChanged;

            flowFilter.Controls.Add(lblSearch);
            flowFilter.Controls.Add(txtSearch);
            flowFilter.Controls.Add(cboTypeFilter);
            flowFilter.Controls.Add(chkGroupBySession);
            flowFilter.Controls.Add(lblCount);

            // splitMain
            splitMain.Dock = DockStyle.Fill;
            splitMain.Name = "splitMain";
            splitMain.Orientation = Orientation.Horizontal;
            splitMain.Panel1.Controls.Add(splitTop);
            splitMain.Panel2.Controls.Add(tabsBottom);
            splitMain.Size = new Size(1180, 500);
            splitMain.SplitterDistance = 320;
            splitMain.TabIndex = 2;

            // splitTop
            splitTop.Dock = DockStyle.Fill;
            splitTop.Name = "splitTop";
            splitTop.Orientation = Orientation.Vertical;
            splitTop.Panel1.Controls.Add(dgvSessions);
            splitTop.Panel2.Controls.Add(dgvInteractions);
            splitTop.Size = new Size(1180, 320);
            splitTop.SplitterDistance = 420;
            splitTop.TabIndex = 0;

            // dgvSessions
            dgvSessions.AllowUserToAddRows = false;
            dgvSessions.AllowUserToDeleteRows = false;
            dgvSessions.AllowUserToResizeRows = false;
            dgvSessions.AutoGenerateColumns = false;
            dgvSessions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSessions.Dock = DockStyle.Fill;
            dgvSessions.MultiSelect = false;
            dgvSessions.Name = "dgvSessions";
            dgvSessions.ReadOnly = true;
            dgvSessions.RowHeadersVisible = false;
            dgvSessions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSessions.TabIndex = 22;
            dgvSessions.Columns.AddRange(new DataGridViewColumn[]
            {
                colSessionStart, colSessionUser, colSessionMessages, colSessionPrompts,
                colSessionFirstPrompt, colSessionApp, colSessionKey
            });
            dgvSessions.SelectionChanged += DgvSessions_SelectionChanged;

            colSessionStart.DataPropertyName = "Start";
            colSessionStart.HeaderText = "Started";
            colSessionStart.Name = "colSessionStart";
            colSessionStart.Width = 130;
            colSessionStart.DefaultCellStyle.Format = "g";

            colSessionUser.DataPropertyName = "User";
            colSessionUser.HeaderText = "User";
            colSessionUser.Name = "colSessionUser";
            colSessionUser.Width = 140;

            colSessionMessages.DataPropertyName = "MessageCount";
            colSessionMessages.HeaderText = "Msgs";
            colSessionMessages.Name = "colSessionMessages";
            colSessionMessages.Width = 50;

            colSessionPrompts.DataPropertyName = "Prompts";
            colSessionPrompts.HeaderText = "Prompts";
            colSessionPrompts.Name = "colSessionPrompts";
            colSessionPrompts.Width = 60;

            colSessionFirstPrompt.DataPropertyName = "FirstPrompt";
            colSessionFirstPrompt.HeaderText = "First prompt";
            colSessionFirstPrompt.Name = "colSessionFirstPrompt";
            colSessionFirstPrompt.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colSessionFirstPrompt.MinimumWidth = 150;

            colSessionApp.DataPropertyName = "App";
            colSessionApp.HeaderText = "App";
            colSessionApp.Name = "colSessionApp";
            colSessionApp.Visible = false;

            colSessionKey.DataPropertyName = "DisplaySessionId";
            colSessionKey.HeaderText = "Session ID";
            colSessionKey.Name = "colSessionKey";
            colSessionKey.Visible = false;

            // dgvInteractions
            dgvInteractions.AllowUserToAddRows = false;
            dgvInteractions.AllowUserToDeleteRows = false;
            dgvInteractions.AutoGenerateColumns = false;
            dgvInteractions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvInteractions.Dock = DockStyle.Fill;
            dgvInteractions.MultiSelect = false;
            dgvInteractions.Name = "dgvInteractions";
            dgvInteractions.ReadOnly = true;
            dgvInteractions.RowHeadersVisible = false;
            dgvInteractions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvInteractions.TabIndex = 17;
            dgvInteractions.Columns.AddRange(new DataGridViewColumn[]
            {
                colTimestamp, colUser, colType, colAuthor, colApp, colConversation, colText,
                colAttachments, colLinks, colSessionId, colRequestId, colId
            });
            dgvInteractions.SelectionChanged += DgvInteractions_SelectionChanged;

            colTimestamp.DataPropertyName = "Timestamp";
            colTimestamp.HeaderText = "Timestamp";
            colTimestamp.Name = "colTimestamp";
            colTimestamp.Width = 145;
            colTimestamp.DefaultCellStyle.Format = "g";

            colUser.DataPropertyName = "User";
            colUser.HeaderText = "User";
            colUser.Name = "colUser";
            colUser.Width = 160;

            colType.DataPropertyName = "Type";
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.Width = 80;

            colAuthor.DataPropertyName = "Author";
            colAuthor.HeaderText = "From";
            colAuthor.Name = "colAuthor";
            colAuthor.Width = 160;

            colApp.DataPropertyName = "App";
            colApp.HeaderText = "App";
            colApp.Name = "colApp";
            colApp.Width = 200;

            colConversation.DataPropertyName = "Conversation";
            colConversation.HeaderText = "Conversation";
            colConversation.Name = "colConversation";
            colConversation.Width = 100;

            colText.DataPropertyName = "Text";
            colText.HeaderText = "Prompt / Response";
            colText.Name = "colText";
            colText.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colText.MinimumWidth = 250;

            colAttachments.DataPropertyName = "Attachments";
            colAttachments.HeaderText = "Att.";
            colAttachments.Name = "colAttachments";
            colAttachments.Width = 50;

            colLinks.DataPropertyName = "Links";
            colLinks.HeaderText = "Links";
            colLinks.Name = "colLinks";
            colLinks.Width = 50;

            colSessionId.DataPropertyName = "SessionId";
            colSessionId.HeaderText = "Session ID";
            colSessionId.Name = "colSessionId";
            colSessionId.Visible = false;

            colRequestId.DataPropertyName = "RequestId";
            colRequestId.HeaderText = "Request ID";
            colRequestId.Name = "colRequestId";
            colRequestId.Visible = false;

            colId.DataPropertyName = "Id";
            colId.HeaderText = "Interaction ID";
            colId.Name = "colId";
            colId.Visible = false;

            // txtDetails
            txtDetails.Dock = DockStyle.Fill;
            txtDetails.Multiline = true;
            txtDetails.Name = "txtDetails";
            txtDetails.ReadOnly = true;
            txtDetails.ScrollBars = ScrollBars.Both;
            txtDetails.TabIndex = 18;
            txtDetails.WordWrap = true;

            // tabsBottom
            tabsBottom.Dock = DockStyle.Fill;
            tabsBottom.Name = "tabsBottom";
            tabsBottom.TabIndex = 19;
            tabsBottom.Controls.Add(tabDetails);
            tabsBottom.Controls.Add(tabIssues);

            tabDetails.Name = "tabDetails";
            tabDetails.Padding = new Padding(3);
            tabDetails.Text = "Details";
            tabDetails.UseVisualStyleBackColor = true;
            tabDetails.Controls.Add(txtDetails);

            tabIssues.Name = "tabIssues";
            tabIssues.Padding = new Padding(3);
            tabIssues.Text = "Issues";
            tabIssues.UseVisualStyleBackColor = true;
            tabIssues.Controls.Add(txtIssues);

            txtIssues.Dock = DockStyle.Fill;
            txtIssues.Multiline = true;
            txtIssues.Name = "txtIssues";
            txtIssues.ReadOnly = true;
            txtIssues.ScrollBars = ScrollBars.Both;
            txtIssues.TabIndex = 20;
            txtIssues.WordWrap = false;

            // statusStrip
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, progressBar });
            statusStrip.Name = "statusStrip";
            statusStrip.TabIndex = 3;

            lblStatus.Name = "lblStatus";
            lblStatus.Spring = true;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Text = "Ready.";

            progressBar.Name = "progressBar";
            progressBar.Style = ProgressBarStyle.Marquee;
            progressBar.Visible = false;

            // Form1
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1180, 720);
            Controls.Add(splitMain);
            Controls.Add(flowFilter);
            Controls.Add(grpConnection);
            Controls.Add(statusStrip);
            MinimumSize = new Size(900, 560);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Microsoft 365 Copilot Interaction Viewer";

            ((System.ComponentModel.ISupportInitialize)dgvInteractions).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvSessions).EndInit();
            splitTop.Panel1.ResumeLayout(false);
            splitTop.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitTop).EndInit();
            splitTop.ResumeLayout(false);
            tabDetails.ResumeLayout(false);
            tabDetails.PerformLayout();
            tabIssues.ResumeLayout(false);
            tabIssues.PerformLayout();
            tabsBottom.ResumeLayout(false);
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMaxItems).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTop).EndInit();
            flowFilter.ResumeLayout(false);
            flowFilter.PerformLayout();
            flowActions.ResumeLayout(false);
            flowActions.PerformLayout();
            layoutConnection.ResumeLayout(false);
            layoutConnection.PerformLayout();
            grpConnection.ResumeLayout(false);
            grpConnection.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private GroupBox grpConnection;
        private TableLayoutPanel layoutConnection;
        private Label lblTenantId;
        private TextBox txtTenantId;
        private Label lblClientId;
        private TextBox txtClientId;
        private Label lblClientSecret;
        private TextBox txtClientSecret;
        private Label lblUserId;
        private TextBox txtUserId;
        private Label lblAppClass;
        private ComboBox cboAppClass;
        private Label lblTop;
        private NumericUpDown numTop;
        private CheckBox chkLicensedOnly;
        private FlowLayoutPanel flowActions;
        private CheckBox chkDateFilter;
        private DateTimePicker dtpFrom;
        private Label lblTo;
        private DateTimePicker dtpTo;
        private CheckBox chkUseBeta;
        private Label lblMaxItems;
        private NumericUpDown numMaxItems;
        private Button btnFetch;
        private Button btnCancel;
        private Button btnExport;
        private FlowLayoutPanel flowFilter;
        private Label lblSearch;
        private TextBox txtSearch;
        private ComboBox cboTypeFilter;
        private Label lblCount;
        private CheckBox chkGroupBySession;
        private SplitContainer splitMain;
        private SplitContainer splitTop;
        private DataGridView dgvSessions;
        private DataGridViewTextBoxColumn colSessionUser;
        private DataGridViewTextBoxColumn colSessionStart;
        private DataGridViewTextBoxColumn colSessionMessages;
        private DataGridViewTextBoxColumn colSessionPrompts;
        private DataGridViewTextBoxColumn colSessionFirstPrompt;
        private DataGridViewTextBoxColumn colSessionApp;
        private DataGridViewTextBoxColumn colSessionKey;
        private DataGridView dgvInteractions;
        private DataGridViewTextBoxColumn colTimestamp;
        private DataGridViewTextBoxColumn colUser;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colAuthor;
        private DataGridViewTextBoxColumn colApp;
        private DataGridViewTextBoxColumn colConversation;
        private DataGridViewTextBoxColumn colText;
        private DataGridViewTextBoxColumn colAttachments;
        private DataGridViewTextBoxColumn colLinks;
        private DataGridViewTextBoxColumn colSessionId;
        private DataGridViewTextBoxColumn colRequestId;
        private DataGridViewTextBoxColumn colId;
        private TextBox txtDetails;
        private TabControl tabsBottom;
        private TabPage tabDetails;
        private TabPage tabIssues;
        private TextBox txtIssues;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblStatus;
        private ToolStripProgressBar progressBar;
    }
}
