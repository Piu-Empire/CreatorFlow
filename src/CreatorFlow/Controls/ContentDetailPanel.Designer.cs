#nullable disable
using System.Drawing;
using System.Windows.Forms;
using CreatorFlow.Models.Enums;
using CreatorFlow.Forms.Board;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.Backends;
using CreatorFlow.Services.Exceptions;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

partial class ContentDetailPanel
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

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.SuspendLayout();
        this._pnlHeader = new ContentDetailHeaderPanel();
        this._pnlTabBar = new ContentDetailTabBar();
        this._pnlContent = new Panel();
        this._pnlFooter = new Panel();
        this._lblFooterCode = new Label();
        this._lblSaveHint = new Label();
        this._btnSave = new ProjectActionButton();
        this._btnDone = new ProjectActionButton();
        this._pnlTabOverview = new Panel();
        this._overviewHost = new Panel();
        this._lblSecTitle = new Label();
        this._boxTitle = new RoundedPanel();
        this._padTitle = new Panel();
        this._txtTitleVal = new TextBox();
        this._lblSecHook = new Label();
        this._boxHook = new RoundedPanel();
        this._padHook = new Panel();
        this._txtHookVal = new TextBox();
        this._boxMeta = new RoundedPanel();
        this._lblMeta0 = new Label();
        this._lblMeta1 = new Label();
        this._lblMeta2 = new Label();
        this._lblMeta3 = new Label();
        this._lblMeta4 = new Label();
        this._lblMeta5 = new Label();
        this._lblMeta6 = new Label();
        this._lblMeta7 = new Label();
        this._cellMeta0 = new RoundedPanel();
        this._cellMeta1 = new RoundedPanel();
        this._cellMeta2 = new RoundedPanel();
        this._cellMeta3 = new RoundedPanel();
        this._cellMeta4 = new RoundedPanel();
        this._cellMeta5 = new RoundedPanel();
        this._cellMeta6 = new RoundedPanel();
        this._cellMeta7 = new RoundedPanel();
        this._lblStageVal = new Label();
        this._padPlatforms = new Panel();
        this._padPriority = new Panel();
        this._padSprint = new Panel();
        this._padTargetWeek = new Panel();
        this._padDuration = new Panel();
        this._padType = new Panel();
        this._padPublish = new Panel();
        this._btnPlatforms = new Button();
        this._mnuPlatforms = new ContextMenuStrip(this.components);
        this._cboPriorityEdit = new ComboBox();
        this._cboSprintEdit = new ComboBox();
        this._dtpTargetWeek = new DateTimePicker();
        this._txtDurationEdit = new TextBox();
        this._cboTypeEdit = new ComboBox();
        this._dtpPlannedPublish = new DateTimePicker();
        this._lblSecLead = new Label();
        this._boxLead = new RoundedPanel();
        this._cboAssigneeEdit = new ComboBox();
        this._lblSecWorkflow = new Label();
        this._flowActions = new FlowLayoutPanel();
        this._pnlTabScript = new Panel();
        this._scriptEditor = new ScriptEditorControl();
        this._pnlTabChecklist = new Panel();
        this._chkChecklist = new CheckedListBox();
        this._pnlTabActivity = new Panel();
        this._activityTimeline = new ActivityTimelineControl();
        this._pnlTabReferences = new Panel();
        this._contentReferences = new ContentReferencesControl();
        this._pnlTabMetrics = new Panel();
        this._metricsHost = new Panel();
        this._pnlEmptyState = new Panel();
        this._layoutRoot = new TableLayoutPanel();
        this._lblEmpty = new Label();

        // ===== Footer =====
        this._lblFooterCode.Font = UITheme.FontMono;
        this._lblFooterCode.ForeColor = UITheme.Neutral400;
        this._lblFooterCode.AutoSize = false;
        this._lblFooterCode.TextAlign = ContentAlignment.MiddleLeft;
        this._lblFooterCode.Location = new Point(24, 0);
        this._lblFooterCode.Size = new Size(96, 80);
        this._lblFooterCode.BackColor = UITheme.White;

        this._lblSaveHint.Text = "";
        this._lblSaveHint.Font = UITheme.FontLabel;
        this._lblSaveHint.ForeColor = UITheme.Neutral400;
        this._lblSaveHint.AutoSize = false;
        this._lblSaveHint.TextAlign = ContentAlignment.MiddleLeft;
        this._lblSaveHint.Location = new Point(120, 0);
        this._lblSaveHint.Size = new Size(150, 80);
        this._lblSaveHint.BackColor = UITheme.White;

        this._btnDone.Text = "Done & Close";
        this._btnDone.Style = RoundButtonStyle.Primary;
        this._btnDone.Size = new Size(130, 44);
        this._btnDone.Location = new Point(366, 18);
        this._btnDone.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        this._btnSave.Text = "Lưu thay đổi";
        this._btnSave.Style = RoundButtonStyle.Primary;
        this._btnSave.Size = new Size(130, 44);
        this._btnSave.Location = new Point(224, 18);
        this._btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        this._btnSave.Enabled = false;

        this._pnlFooter.BackColor = UITheme.White;
        this._pnlFooter.Dock = DockStyle.Fill;
        this._pnlFooter.Margin = new Padding(0);
        this._pnlFooter.Controls.Add(this._lblFooterCode);
        this._pnlFooter.Controls.Add(this._lblSaveHint);
        this._pnlFooter.Controls.Add(this._btnDone);
        this._pnlFooter.Controls.Add(this._btnSave);

        // ===== Header / Tab bar =====
        this._pnlHeader.Dock = DockStyle.Fill;
        this._pnlHeader.Margin = new Padding(0);
        this._pnlTabBar.Dock = DockStyle.Fill;
        this._pnlTabBar.Margin = new Padding(0);

        // ===== Tab 1: Overview =====

        this._lblSecTitle = new Label();
        this._lblSecTitle.Text = "PRODUCTION TITLE";
        this._lblSecTitle.Font = UITheme.FontLabelBold;
        this._lblSecTitle.ForeColor = UITheme.Neutral600;
        this._lblSecTitle.AutoSize = false;
        this._lblSecTitle.BackColor = UITheme.White;
        this._lblSecTitle.UseMnemonic = false;
        this._lblSecTitle.Location = new Point(24, 20);
        this._lblSecTitle.Size = new Size(472, 18);

        this._txtTitleVal.Dock = DockStyle.Fill;
        this._txtTitleVal.Font = new Font("Segoe UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
        this._txtTitleVal.ForeColor = UITheme.Ink;
        this._txtTitleVal.BorderStyle = BorderStyle.None;
        this._txtTitleVal.Multiline = true;

        this._padTitle = new Panel();
        this._padTitle.Dock = DockStyle.Fill;
        this._padTitle.BackColor = UITheme.White;
        this._padTitle.Padding = new Padding(16, 10, 16, 10);
        this._padTitle.Controls.Add(this._txtTitleVal);

        this._boxTitle.Location = new Point(24, 44);
        this._boxTitle.Size = new Size(472, 60);
        this._boxTitle.Controls.Add(this._padTitle);

        this._lblSecHook = new Label();
        this._lblSecHook.Text = "CONCEPT HOOK";
        this._lblSecHook.Font = UITheme.FontLabelBold;
        this._lblSecHook.ForeColor = UITheme.Neutral600;
        this._lblSecHook.AutoSize = false;
        this._lblSecHook.BackColor = UITheme.White;
        this._lblSecHook.UseMnemonic = false;
        this._lblSecHook.Location = new Point(24, 124);
        this._lblSecHook.Size = new Size(472, 18);

        this._txtHookVal.Dock = DockStyle.Fill;
        this._txtHookVal.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
        this._txtHookVal.ForeColor = UITheme.Ink;
        this._txtHookVal.BorderStyle = BorderStyle.None;
        this._txtHookVal.Multiline = true;

        this._padHook = new Panel();
        this._padHook.Dock = DockStyle.Fill;
        this._padHook.BackColor = UITheme.White;
        this._padHook.Padding = new Padding(16, 12, 16, 12);
        this._padHook.Controls.Add(this._txtHookVal);

        this._boxHook.Location = new Point(24, 148);
        this._boxHook.Size = new Size(472, 104);
        this._boxHook.Controls.Add(this._padHook);

        // Meta 0: Pipeline Stage (chỉ đọc)
        this._lblStageVal.Dock = DockStyle.Fill;
        this._lblStageVal.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._lblStageVal.ForeColor = UITheme.Neutral600;
        this._lblStageVal.TextAlign = ContentAlignment.MiddleLeft;
        this._lblStageVal.Padding = new Padding(14, 0, 14, 0);
        this._lblStageVal.BackColor = UITheme.Neutral100;
        this._lblStageVal.UseMnemonic = false;
        this._cellMeta0.FillColor = UITheme.Neutral100;
        this._cellMeta0.Controls.Add(this._lblStageVal);

        // Meta 1: Platform (menu nhiều lựa chọn)
        this._mnuPlatforms.ShowImageMargin = false;
        this._mnuPlatforms.ShowCheckMargin = true;
        this._mnuPlatforms.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._btnPlatforms.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._btnPlatforms.FlatStyle = FlatStyle.Flat;
        this._btnPlatforms.FlatAppearance.BorderSize = 0;
        this._btnPlatforms.BackColor = UITheme.White;
        this._btnPlatforms.TextAlign = ContentAlignment.MiddleLeft;
        this._btnPlatforms.AutoEllipsis = true;
        this._btnPlatforms.UseMnemonic = false;
        this._btnPlatforms.Dock = DockStyle.Fill;

        this._padPlatforms = new Panel();
        this._padPlatforms.Dock = DockStyle.Fill;
        this._padPlatforms.BackColor = UITheme.White;
        this._padPlatforms.Padding = new Padding(12, 7, 12, 7);
        this._padPlatforms.Controls.Add(this._btnPlatforms);
        this._cellMeta1.Controls.Add(this._padPlatforms);

        // Meta 2: Priority
        this._cboPriorityEdit.Dock = DockStyle.Fill;
        this._cboPriorityEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._cboPriorityEdit.DropDownStyle = ComboBoxStyle.DropDownList;
        this._cboPriorityEdit.FlatStyle = FlatStyle.Flat;

        this._padPriority = new Panel();
        this._padPriority.Dock = DockStyle.Fill;
        this._padPriority.BackColor = UITheme.White;
        this._padPriority.Padding = new Padding(12, 7, 12, 7);
        this._padPriority.Controls.Add(this._cboPriorityEdit);
        this._cellMeta2.Controls.Add(this._padPriority);

        // Meta 3: Sprint Allocation
        this._cboSprintEdit.Dock = DockStyle.Fill;
        this._cboSprintEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._cboSprintEdit.DropDownStyle = ComboBoxStyle.DropDownList;
        this._cboSprintEdit.FlatStyle = FlatStyle.Flat;

        this._padSprint = new Panel();
        this._padSprint.Dock = DockStyle.Fill;
        this._padSprint.BackColor = UITheme.White;
        this._padSprint.Padding = new Padding(12, 7, 12, 7);
        this._padSprint.Controls.Add(this._cboSprintEdit);
        this._cellMeta3.Controls.Add(this._padSprint);

        // Meta 4: Target Week (Deadline)
        this._dtpTargetWeek.Dock = DockStyle.Fill;
        this._dtpTargetWeek.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._dtpTargetWeek.Format = DateTimePickerFormat.Short;

        this._padTargetWeek = new Panel();
        this._padTargetWeek.Dock = DockStyle.Fill;
        this._padTargetWeek.BackColor = UITheme.White;
        this._padTargetWeek.Padding = new Padding(10, 7, 10, 7);
        this._padTargetWeek.Controls.Add(this._dtpTargetWeek);
        this._cellMeta4.Controls.Add(this._padTargetWeek);

        // Meta 5: Estimated Duration
        this._txtDurationEdit.Dock = DockStyle.Fill;
        this._txtDurationEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._txtDurationEdit.BorderStyle = BorderStyle.None;
        this._txtDurationEdit.PlaceholderText = "VD: 24 min";

        this._padDuration = new Panel();
        this._padDuration.Dock = DockStyle.Fill;
        this._padDuration.BackColor = UITheme.White;
        this._padDuration.Padding = new Padding(14, 11, 14, 11);
        this._padDuration.Controls.Add(this._txtDurationEdit);
        this._cellMeta5.Controls.Add(this._padDuration);

        // Meta 6: Content Type
        this._cboTypeEdit.Dock = DockStyle.Fill;
        this._cboTypeEdit.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._cboTypeEdit.DropDownStyle = ComboBoxStyle.DropDownList;
        this._cboTypeEdit.FlatStyle = FlatStyle.Flat;

        this._padType = new Panel();
        this._padType.Dock = DockStyle.Fill;
        this._padType.BackColor = UITheme.White;
        this._padType.Padding = new Padding(12, 7, 12, 7);
        this._padType.Controls.Add(this._cboTypeEdit);
        this._cellMeta6.Controls.Add(this._padType);

        // Meta 7: Planned Publish Date (tùy chọn)
        this._dtpPlannedPublish.Dock = DockStyle.Fill;
        this._dtpPlannedPublish.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._dtpPlannedPublish.Format = DateTimePickerFormat.Short;
        this._dtpPlannedPublish.ShowCheckBox = true;
        this._dtpPlannedPublish.Checked = false;

        this._padPublish = new Panel();
        this._padPublish.Dock = DockStyle.Fill;
        this._padPublish.BackColor = UITheme.White;
        this._padPublish.Padding = new Padding(10, 7, 10, 7);
        this._padPublish.Controls.Add(this._dtpPlannedPublish);
        this._cellMeta7.Controls.Add(this._padPublish);

        // Khung meta (lưới 2 cột x 4 hàng)
        this._boxMeta.Location = new Point(24, 272);
        this._boxMeta.Size = new Size(472, 336);
        this._boxMeta.FillColor = UITheme.Neutral50;
        this._boxMeta.Radius = 10;

        this._lblMeta0.Text = "Pipeline Stage";
        this._lblMeta0.Font = UITheme.FontLabelBold;
        this._lblMeta0.ForeColor = UITheme.Neutral700;
        this._lblMeta0.AutoSize = false;
        this._lblMeta0.BackColor = UITheme.Neutral50;
        this._lblMeta0.Location = new Point(20, 18);
        this._lblMeta0.Size = new Size(208, 18);
        this._cellMeta0.Location = new Point(20, 40);
        this._cellMeta0.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta0);
        this._boxMeta.Controls.Add(this._cellMeta0);

        this._lblMeta1.Text = "Platform";
        this._lblMeta1.Font = UITheme.FontLabelBold;
        this._lblMeta1.ForeColor = UITheme.Neutral700;
        this._lblMeta1.AutoSize = false;
        this._lblMeta1.BackColor = UITheme.Neutral50;
        this._lblMeta1.Location = new Point(244, 18);
        this._lblMeta1.Size = new Size(208, 18);
        this._cellMeta1.Location = new Point(244, 40);
        this._cellMeta1.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta1);
        this._boxMeta.Controls.Add(this._cellMeta1);

        this._lblMeta2.Text = "Priority";
        this._lblMeta2.Font = UITheme.FontLabelBold;
        this._lblMeta2.ForeColor = UITheme.Neutral700;
        this._lblMeta2.AutoSize = false;
        this._lblMeta2.BackColor = UITheme.Neutral50;
        this._lblMeta2.Location = new Point(20, 96);
        this._lblMeta2.Size = new Size(208, 18);
        this._cellMeta2.Location = new Point(20, 118);
        this._cellMeta2.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta2);
        this._boxMeta.Controls.Add(this._cellMeta2);

        this._lblMeta3.Text = "Sprint Allocation";
        this._lblMeta3.Font = UITheme.FontLabelBold;
        this._lblMeta3.ForeColor = UITheme.Neutral700;
        this._lblMeta3.AutoSize = false;
        this._lblMeta3.BackColor = UITheme.Neutral50;
        this._lblMeta3.Location = new Point(244, 96);
        this._lblMeta3.Size = new Size(208, 18);
        this._cellMeta3.Location = new Point(244, 118);
        this._cellMeta3.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta3);
        this._boxMeta.Controls.Add(this._cellMeta3);

        this._lblMeta4.Text = "Target Week";
        this._lblMeta4.Font = UITheme.FontLabelBold;
        this._lblMeta4.ForeColor = UITheme.Neutral700;
        this._lblMeta4.AutoSize = false;
        this._lblMeta4.BackColor = UITheme.Neutral50;
        this._lblMeta4.Location = new Point(20, 174);
        this._lblMeta4.Size = new Size(208, 18);
        this._cellMeta4.Location = new Point(20, 196);
        this._cellMeta4.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta4);
        this._boxMeta.Controls.Add(this._cellMeta4);

        this._lblMeta5.Text = "Estimated Duration";
        this._lblMeta5.Font = UITheme.FontLabelBold;
        this._lblMeta5.ForeColor = UITheme.Neutral700;
        this._lblMeta5.AutoSize = false;
        this._lblMeta5.BackColor = UITheme.Neutral50;
        this._lblMeta5.Location = new Point(244, 174);
        this._lblMeta5.Size = new Size(208, 18);
        this._cellMeta5.Location = new Point(244, 196);
        this._cellMeta5.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta5);
        this._boxMeta.Controls.Add(this._cellMeta5);

        this._lblMeta6.Text = "Content Type";
        this._lblMeta6.Font = UITheme.FontLabelBold;
        this._lblMeta6.ForeColor = UITheme.Neutral700;
        this._lblMeta6.AutoSize = false;
        this._lblMeta6.BackColor = UITheme.Neutral50;
        this._lblMeta6.Location = new Point(20, 252);
        this._lblMeta6.Size = new Size(208, 18);
        this._cellMeta6.Location = new Point(20, 274);
        this._cellMeta6.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta6);
        this._boxMeta.Controls.Add(this._cellMeta6);

        this._lblMeta7.Text = "Planned Publish Date";
        this._lblMeta7.Font = UITheme.FontLabelBold;
        this._lblMeta7.ForeColor = UITheme.Neutral700;
        this._lblMeta7.AutoSize = false;
        this._lblMeta7.BackColor = UITheme.Neutral50;
        this._lblMeta7.Location = new Point(244, 252);
        this._lblMeta7.Size = new Size(208, 18);
        this._cellMeta7.Location = new Point(244, 274);
        this._cellMeta7.Size = new Size(208, 44);
        this._boxMeta.Controls.Add(this._lblMeta7);
        this._boxMeta.Controls.Add(this._cellMeta7);

        this._lblSecLead = new Label();
        this._lblSecLead.Text = "DIRECTOR & CREATOR LEAD";
        this._lblSecLead.Font = UITheme.FontLabelBold;
        this._lblSecLead.ForeColor = UITheme.Neutral600;
        this._lblSecLead.AutoSize = false;
        this._lblSecLead.BackColor = UITheme.White;
        this._lblSecLead.UseMnemonic = false;
        this._lblSecLead.Location = new Point(24, 628);
        this._lblSecLead.Size = new Size(472, 18);

        this._cboAssigneeEdit.DropDownStyle = ComboBoxStyle.DropDownList;
        this._cboAssigneeEdit.Font = UITheme.FontBody;
        this._cboAssigneeEdit.DisplayMember = "Name";
        this._cboAssigneeEdit.Location = new Point(234, 23);
        this._cboAssigneeEdit.Size = new Size(220, 29);

        this._boxLead.Location = new Point(24, 652);
        this._boxLead.Size = new Size(472, 76);
        this._boxLead.FillColor = UITheme.Neutral50;
        this._boxLead.Radius = 10;
        this._boxLead.Controls.Add(this._cboAssigneeEdit);

        this._lblSecWorkflow = new Label();
        this._lblSecWorkflow.Text = "WORKFLOW ACTIONS";
        this._lblSecWorkflow.Font = UITheme.FontLabelBold;
        this._lblSecWorkflow.ForeColor = UITheme.Neutral600;
        this._lblSecWorkflow.AutoSize = false;
        this._lblSecWorkflow.BackColor = UITheme.White;
        this._lblSecWorkflow.UseMnemonic = false;
        this._lblSecWorkflow.Location = new Point(24, 748);
        this._lblSecWorkflow.Size = new Size(472, 18);

        this._flowActions.FlowDirection = FlowDirection.LeftToRight;
        this._flowActions.WrapContents = true;
        this._flowActions.AutoSize = false;
        this._flowActions.BackColor = UITheme.White;
        this._flowActions.Location = new Point(24, 772);
        this._flowActions.Size = new Size(472, 44);

        this._overviewHost.BackColor = UITheme.White;
        this._overviewHost.Location = new Point(0, 0);
        this._overviewHost.Size = new Size(520, 840);
        this._overviewHost.Controls.Add(this._lblSecTitle);
        this._overviewHost.Controls.Add(this._boxTitle);
        this._overviewHost.Controls.Add(this._lblSecHook);
        this._overviewHost.Controls.Add(this._boxHook);
        this._overviewHost.Controls.Add(this._boxMeta);
        this._overviewHost.Controls.Add(this._lblSecLead);
        this._overviewHost.Controls.Add(this._boxLead);
        this._overviewHost.Controls.Add(this._lblSecWorkflow);
        this._overviewHost.Controls.Add(this._flowActions);

        this._pnlTabOverview.Dock = DockStyle.Fill;
        this._pnlTabOverview.AutoScroll = true;
        this._pnlTabOverview.BackColor = UITheme.White;
        this._pnlTabOverview.Controls.Add(this._overviewHost);

        // ===== Tab 2: Script =====
        this._scriptEditor.Dock = DockStyle.Fill;
        this._pnlTabScript.Dock = DockStyle.Fill;
        this._pnlTabScript.Padding = new Padding(24, 20, 24, 20);
        this._pnlTabScript.Visible = false;
        this._pnlTabScript.Controls.Add(this._scriptEditor);

        // ===== Tab 3: Checklist =====
        this._chkChecklist.Dock = DockStyle.Fill;
        this._chkChecklist.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
        this._chkChecklist.BorderStyle = BorderStyle.FixedSingle;
        this._chkChecklist.Items.AddRange(new object[] {
            "1. Lên dàn ý kịch bản (Outline)",
            "2. Duyệt kịch bản chính thức",
            "3. Chuẩn bị đạo cụ & thiết bị quay",
            "4. Quay phân cảnh chính (Studio / Hiện trường)",
            "5. Thu âm lồng tiếng (Voiceover)",
            "6. Dựng nháp (Rough cut)",
            "7. Hậu kỳ màu sắc & âm thanh (Color & Mix)",
            "8. Kiểm tra bản quyền âm nhạc & nhãn dán"});
        this._pnlTabChecklist.Dock = DockStyle.Fill;
        this._pnlTabChecklist.Padding = new Padding(24, 20, 24, 20);
        this._pnlTabChecklist.Visible = false;
        this._pnlTabChecklist.Controls.Add(this._chkChecklist);

        // ===== Tab 4: Activity =====
        this._activityTimeline.Dock = DockStyle.Fill;
        this._pnlTabActivity.Dock = DockStyle.Fill;
        this._pnlTabActivity.Padding = new Padding(24, 20, 24, 20);
        this._pnlTabActivity.Visible = false;
        this._pnlTabActivity.Controls.Add(this._activityTimeline);

        // ===== Tab 5: References =====
        this._contentReferences.Dock = DockStyle.Fill;
        this._pnlTabReferences.Dock = DockStyle.Fill;
        this._pnlTabReferences.BackColor = UITheme.White;
        this._pnlTabReferences.Visible = false;
        this._pnlTabReferences.Controls.Add(this._contentReferences);

        // ===== Tab 6: Metrics =====
        this._metricsHost.Dock = DockStyle.Top;
        this._metricsHost.AutoSize = true;
        this._metricsHost.BackColor = UITheme.White;
        this._pnlTabMetrics.Dock = DockStyle.Fill;
        this._pnlTabMetrics.AutoScroll = true;
        this._pnlTabMetrics.BackColor = UITheme.White;
        this._pnlTabMetrics.Padding = new Padding(24, 20, 24, 20);
        this._pnlTabMetrics.Visible = false;
        this._pnlTabMetrics.Controls.Add(this._metricsHost);

        // ===== Vùng nội dung =====
        this._pnlContent.Dock = DockStyle.Fill;
        this._pnlContent.Margin = new Padding(0);
        this._pnlContent.BackColor = UITheme.White;
        this._pnlContent.Controls.Add(this._pnlTabOverview);
        this._pnlContent.Controls.Add(this._pnlTabScript);
        this._pnlContent.Controls.Add(this._pnlTabChecklist);
        this._pnlContent.Controls.Add(this._pnlTabActivity);
        this._pnlContent.Controls.Add(this._pnlTabReferences);
        this._pnlContent.Controls.Add(this._pnlTabMetrics);

        // ===== Empty state =====
        this._lblEmpty.Text = "Chưa chọn nội dung nào\r\n\r\nBấm vào một thẻ bất kỳ trên Board để xem chi tiết và thực hiện chuyển trạng thái.";
        this._lblEmpty.Font = UITheme.FontBody;
        this._lblEmpty.ForeColor = UITheme.Neutral600;
        this._lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
        this._lblEmpty.Dock = DockStyle.Fill;
        this._pnlEmptyState.Dock = DockStyle.Fill;
        this._pnlEmptyState.BackColor = UITheme.White;
        this._pnlEmptyState.Visible = false;
        this._pnlEmptyState.Controls.Add(this._lblEmpty);

        // ===== Bố cục 4 hàng: Header / Tab bar / Nội dung / Footer =====
        this._layoutRoot.Dock = DockStyle.Fill;
        this._layoutRoot.ColumnCount = 1;
        this._layoutRoot.RowCount = 4;
        this._layoutRoot.Margin = new Padding(0);
        this._layoutRoot.Padding = new Padding(0);
        this._layoutRoot.BackColor = UITheme.White;
        this._layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this._layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
        this._layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        this._layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this._layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 80F));
        this._layoutRoot.Controls.Add(this._pnlHeader, 0, 0);
        this._layoutRoot.Controls.Add(this._pnlTabBar, 0, 1);
        this._layoutRoot.Controls.Add(this._pnlContent, 0, 2);
        this._layoutRoot.Controls.Add(this._pnlFooter, 0, 3);

        // ===== UserControl =====
        this.BackColor = UITheme.White;
        this.Dock = DockStyle.Fill;
        this.Font = UITheme.FontBody;
        this.Size = new Size(520, 900);
        this.Controls.Add(this._layoutRoot);
        this.Controls.Add(this._pnlEmptyState);
        this.ResumeLayout(false);
    }

    #endregion

    private ContentDetailHeaderPanel _pnlHeader;
    private ContentDetailTabBar _pnlTabBar;
    private Panel _pnlContent;
    private Panel _pnlFooter;
    private Label _lblFooterCode;
    private Label _lblSaveHint;
    private ProjectActionButton _btnSave;
    private ProjectActionButton _btnDone;
    private Panel _pnlTabOverview;
    private Panel _overviewHost;
    private Label _lblSecTitle;
    private RoundedPanel _boxTitle;
    private Panel _padTitle;
    private TextBox _txtTitleVal;
    private Label _lblSecHook;
    private RoundedPanel _boxHook;
    private Panel _padHook;
    private TextBox _txtHookVal;
    private RoundedPanel _boxMeta;
    private Label _lblMeta0;
    private Label _lblMeta1;
    private Label _lblMeta2;
    private Label _lblMeta3;
    private Label _lblMeta4;
    private Label _lblMeta5;
    private Label _lblMeta6;
    private Label _lblMeta7;
    private RoundedPanel _cellMeta0;
    private RoundedPanel _cellMeta1;
    private RoundedPanel _cellMeta2;
    private RoundedPanel _cellMeta3;
    private RoundedPanel _cellMeta4;
    private RoundedPanel _cellMeta5;
    private RoundedPanel _cellMeta6;
    private RoundedPanel _cellMeta7;
    private Label _lblStageVal;
    private Panel _padPlatforms;
    private Panel _padPriority;
    private Panel _padSprint;
    private Panel _padTargetWeek;
    private Panel _padDuration;
    private Panel _padType;
    private Panel _padPublish;
    private Button _btnPlatforms;
    private ContextMenuStrip _mnuPlatforms;
    private ComboBox _cboPriorityEdit;
    private ComboBox _cboSprintEdit;
    private DateTimePicker _dtpTargetWeek;
    private TextBox _txtDurationEdit;
    private ComboBox _cboTypeEdit;
    private DateTimePicker _dtpPlannedPublish;
    private Label _lblSecLead;
    private RoundedPanel _boxLead;
    private ComboBox _cboAssigneeEdit;
    private Label _lblSecWorkflow;
    private FlowLayoutPanel _flowActions;
    private Panel _pnlTabScript;
    private ScriptEditorControl _scriptEditor;
    private Panel _pnlTabChecklist;
    private CheckedListBox _chkChecklist;
    private Panel _pnlTabActivity;
    private ActivityTimelineControl _activityTimeline;
    private Panel _pnlTabReferences;
    private ContentReferencesControl _contentReferences;
    private Panel _pnlTabMetrics;
    private Panel _metricsHost;
    private Panel _pnlEmptyState;
    private Label _lblEmpty;
    private TableLayoutPanel _layoutRoot;
}