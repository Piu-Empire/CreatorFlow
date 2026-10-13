#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Tasks;

partial class MyTasksForm
{
    private System.ComponentModel.IContainer components = null;

    // Page Header (style guide mục 26)
    private System.Windows.Forms.Panel panelHeader;
    private CreatorFlow.Controls.SegmentedControl segStatus;
    private System.Windows.Forms.ComboBox cboDeadline;
    private CreatorFlow.Controls.RoundedButton btnRefresh;
    private System.Windows.Forms.Label lblSummary;

    // Bảng trong Card (mục 11, 12)
    private CreatorFlow.Controls.RoundedPanel cardList;
    private CreatorFlow.Controls.BufferedListView listViewTasks;
    private System.Windows.Forms.ImageList imageListRow;
    private System.Windows.Forms.ColumnHeader columnCode;
    private System.Windows.Forms.ColumnHeader columnTitle;
    private System.Windows.Forms.ColumnHeader columnStatus;
    private System.Windows.Forms.ColumnHeader columnPriority;
    private System.Windows.Forms.ColumnHeader columnDeadline;
    private System.Windows.Forms.ColumnHeader columnProgress;
    private System.Windows.Forms.ColumnHeader columnTeam;
    private System.Windows.Forms.ColumnHeader columnAssignedBy;
    private System.Windows.Forms.Panel pnlEmpty;

    // Thanh cập nhật Progress / Deadline + Mở Content
    private System.Windows.Forms.Panel panelBottomHost;
    private CreatorFlow.Controls.RoundedPanel panelEdit;
    private System.Windows.Forms.Label lblProgressEdit;
    private System.Windows.Forms.NumericUpDown nudProgress;
    private CreatorFlow.Controls.RoundedButton btnSaveProgress;
    private System.Windows.Forms.Label lblDeadlineEdit;
    private System.Windows.Forms.DateTimePicker dtpDeadline;
    private CreatorFlow.Controls.RoundedButton btnSaveDeadline;
    private CreatorFlow.Controls.RoundedButton btnOpenContent;
    private System.Windows.Forms.Label lblHint;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        panelHeader = new Panel();
        lblSummary = new Label();
        btnRefresh = new RoundedButton();
        cboDeadline = new ComboBox();
        segStatus = new SegmentedControl();
        cardList = new RoundedPanel();
        listViewTasks = new BufferedListView();
        columnCode = new ColumnHeader();
        columnTitle = new ColumnHeader();
        columnStatus = new ColumnHeader();
        columnPriority = new ColumnHeader();
        columnDeadline = new ColumnHeader();
        columnProgress = new ColumnHeader();
        columnTeam = new ColumnHeader();
        columnAssignedBy = new ColumnHeader();
        imageListRow = new ImageList(components);
        pnlEmpty = new Panel();
        panelBottomHost = new Panel();
        panelEdit = new RoundedPanel();
        btnOpenContent = new RoundedButton();
        lblHint = new Label();
        btnSaveDeadline = new RoundedButton();
        dtpDeadline = new DateTimePicker();
        lblDeadlineEdit = new Label();
        btnSaveProgress = new RoundedButton();
        nudProgress = new NumericUpDown();
        lblProgressEdit = new Label();
        panelHeader.SuspendLayout();
        cardList.SuspendLayout();
        panelBottomHost.SuspendLayout();
        panelEdit.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)nudProgress).BeginInit();
        SuspendLayout();
        // 
        // panelHeader  (tiêu đề / badge / mô tả được vẽ trong panelHeader_Paint)
        // 
        panelHeader.BackColor = UITheme.Neutral50;
        panelHeader.Controls.Add(lblSummary);
        panelHeader.Controls.Add(btnRefresh);
        panelHeader.Controls.Add(cboDeadline);
        panelHeader.Controls.Add(segStatus);
        panelHeader.Dock = DockStyle.Top;
        panelHeader.Name = "panelHeader";
        panelHeader.Size = new Size(1056, 88);
        panelHeader.TabIndex = 0;
        panelHeader.Paint += panelHeader_Paint;
        panelHeader.Resize += panelHeader_Resize;
        // 
        // segStatus  (bộ lọc nhanh theo trạng thái — Segmented Control, mục 18)
        // 
        segStatus.Height = 34;
        segStatus.Location = new Point(420, 2);
        segStatus.Name = "segStatus";
        segStatus.TabIndex = 0;
        segStatus.SelectedIndexChanged += segStatus_SelectedIndexChanged;
        // 
        // cboDeadline  (bộ lọc thêm — ComboBox)
        // 
        cboDeadline.DropDownStyle = ComboBoxStyle.DropDownList;
        cboDeadline.Font = UITheme.FontBody;
        cboDeadline.Location = new Point(764, 5);
        cboDeadline.Name = "cboDeadline";
        cboDeadline.Size = new Size(170, 28);
        cboDeadline.TabIndex = 1;
        cboDeadline.SelectedIndexChanged += cboDeadline_SelectedIndexChanged;
        // 
        // btnRefresh  (Ghost: hành động phụ nhẹ trong toolbar, mục 8)
        // 
        btnRefresh.Location = new Point(946, 0);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(110, 38);
        btnRefresh.Style = RoundButtonStyle.Ghost;
        btnRefresh.TabIndex = 2;
        btnRefresh.Text = "Làm mới";
        btnRefresh.Click += btnRefresh_Click;
        // 
        // lblSummary
        // 
        lblSummary.Font = UITheme.FontLabelBold;
        lblSummary.ForeColor = UITheme.Neutral600;
        lblSummary.Location = new Point(636, 48);
        lblSummary.Name = "lblSummary";
        lblSummary.Size = new Size(420, 22);
        lblSummary.TabIndex = 3;
        lblSummary.TextAlign = ContentAlignment.MiddleRight;
        // 
        // cardList  (Card: nền trắng, viền #E5E5E5, bo 12px — mục 12)
        // 
        cardList.BorderColor = UITheme.Neutral200;
        cardList.Controls.Add(listViewTasks);
        cardList.Controls.Add(pnlEmpty);
        cardList.Dock = DockStyle.Fill;
        cardList.FillColor = UITheme.White;
        cardList.Name = "cardList";
        cardList.Padding = new Padding(1);
        cardList.Radius = 12;
        cardList.TabIndex = 1;
        // 
        // listViewTasks
        // 
        listViewTasks.BackColor = UITheme.White;
        listViewTasks.BorderStyle = BorderStyle.None;
        listViewTasks.Columns.AddRange(new ColumnHeader[] { columnCode, columnTitle, columnStatus, columnPriority, columnDeadline, columnProgress, columnTeam, columnAssignedBy });
        listViewTasks.Dock = DockStyle.Fill;
        listViewTasks.Font = UITheme.FontBody;
        listViewTasks.FullRowSelect = true;
        listViewTasks.GridLines = false;
        listViewTasks.HideSelection = false;
        listViewTasks.MultiSelect = false;
        listViewTasks.Name = "listViewTasks";
        listViewTasks.OwnerDraw = true;
        listViewTasks.SmallImageList = imageListRow;
        listViewTasks.TabIndex = 0;
        listViewTasks.UseCompatibleStateImageBehavior = false;
        listViewTasks.View = View.Details;
        listViewTasks.DrawColumnHeader += listViewTasks_DrawColumnHeader;
        listViewTasks.DrawSubItem += listViewTasks_DrawSubItem;
        listViewTasks.SelectedIndexChanged += listViewTasks_SelectedIndexChanged;
        listViewTasks.DoubleClick += listViewTasks_DoubleClick;
        listViewTasks.KeyDown += listViewTasks_KeyDown;
        listViewTasks.MouseMove += listViewTasks_MouseMove;
        listViewTasks.MouseLeave += listViewTasks_MouseLeave;
        listViewTasks.SizeChanged += listViewTasks_SizeChanged;
        // 
        // columns
        // 
        columnCode.Text = "Mã";
        columnCode.Width = 90;
        columnTitle.Text = "Tiêu đề";
        columnTitle.Width = 300;
        columnStatus.Text = "Trạng thái";
        columnStatus.Width = 120;
        columnPriority.Text = "Ưu tiên";
        columnPriority.Width = 100;
        columnDeadline.Text = "Deadline";
        columnDeadline.Width = 170;
        columnProgress.Text = "Tiến độ";
        columnProgress.Width = 170;
        columnTeam.Text = "Nhóm";
        columnTeam.Width = 80;
        columnAssignedBy.Text = "Giao bởi";
        columnAssignedBy.Width = 110;
        // 
        // imageListRow  (chỉ để nới chiều cao dòng của ListView — không chứa ảnh)
        // 
        imageListRow.ColorDepth = ColorDepth.Depth32Bit;
        imageListRow.ImageSize = new Size(1, 46);
        // 
        // pnlEmpty  (Empty state, mục 13 — nội dung vẽ trong pnlEmpty_Paint)
        // 
        pnlEmpty.BackColor = UITheme.White;
        pnlEmpty.Dock = DockStyle.Fill;
        pnlEmpty.Name = "pnlEmpty";
        pnlEmpty.TabIndex = 1;
        pnlEmpty.Visible = false;
        pnlEmpty.Paint += pnlEmpty_Paint;
        // 
        // panelBottomHost  (chừa 16px khoảng cách phía trên thẻ chỉnh sửa)
        // 
        panelBottomHost.BackColor = UITheme.Neutral50;
        panelBottomHost.Controls.Add(panelEdit);
        panelBottomHost.Dock = DockStyle.Bottom;
        panelBottomHost.Name = "panelBottomHost";
        panelBottomHost.Padding = new Padding(0, 16, 0, 0);
        panelBottomHost.Size = new Size(1056, 100);
        panelBottomHost.TabIndex = 2;
        // 
        // panelEdit  (Card chứa ô Progress / Deadline / Mở Content)
        // 
        panelEdit.BorderColor = UITheme.Neutral200;
        panelEdit.Controls.Add(lblHint);
        panelEdit.Controls.Add(btnOpenContent);
        panelEdit.Controls.Add(btnSaveDeadline);
        panelEdit.Controls.Add(dtpDeadline);
        panelEdit.Controls.Add(lblDeadlineEdit);
        panelEdit.Controls.Add(btnSaveProgress);
        panelEdit.Controls.Add(nudProgress);
        panelEdit.Controls.Add(lblProgressEdit);
        panelEdit.Dock = DockStyle.Fill;
        panelEdit.FillColor = UITheme.White;
        panelEdit.Name = "panelEdit";
        panelEdit.Radius = 12;
        panelEdit.TabIndex = 0;
        // 
        // lblProgressEdit
        // 
        lblProgressEdit.AutoSize = true;
        lblProgressEdit.Font = UITheme.FontLabelBold;
        lblProgressEdit.ForeColor = UITheme.Neutral600;
        lblProgressEdit.Location = new Point(20, 24);
        lblProgressEdit.Name = "lblProgressEdit";
        lblProgressEdit.TabIndex = 0;
        lblProgressEdit.Text = "Tiến độ (%)";
        // 
        // nudProgress
        // 
        nudProgress.Enabled = false;
        nudProgress.Font = UITheme.FontBody;
        nudProgress.Location = new Point(104, 18);
        nudProgress.Maximum = 100;
        nudProgress.Minimum = 0;
        nudProgress.Name = "nudProgress";
        nudProgress.Size = new Size(70, 28);
        nudProgress.TabIndex = 1;
        nudProgress.TextAlign = HorizontalAlignment.Right;
        nudProgress.KeyDown += nudProgress_KeyDown;
        // 
        // btnSaveProgress  (Primary: hành động chính của màn hình)
        // 
        btnSaveProgress.Enabled = false;
        btnSaveProgress.Location = new Point(186, 14);
        btnSaveProgress.Name = "btnSaveProgress";
        btnSaveProgress.Size = new Size(150, 36);
        btnSaveProgress.Style = RoundButtonStyle.Primary;
        btnSaveProgress.TabIndex = 2;
        btnSaveProgress.Text = "Cập nhật tiến độ";
        btnSaveProgress.Click += btnSaveProgress_Click;
        // 
        // lblDeadlineEdit
        // 
        lblDeadlineEdit.AutoSize = true;
        lblDeadlineEdit.Font = UITheme.FontLabelBold;
        lblDeadlineEdit.ForeColor = UITheme.Neutral600;
        lblDeadlineEdit.Location = new Point(366, 24);
        lblDeadlineEdit.Name = "lblDeadlineEdit";
        lblDeadlineEdit.TabIndex = 3;
        lblDeadlineEdit.Text = "Deadline";
        // 
        // dtpDeadline
        // 
        dtpDeadline.Enabled = false;
        dtpDeadline.Font = UITheme.FontBody;
        dtpDeadline.Format = DateTimePickerFormat.Short;
        dtpDeadline.Location = new Point(430, 18);
        dtpDeadline.Name = "dtpDeadline";
        dtpDeadline.ShowCheckBox = true;
        dtpDeadline.Size = new Size(140, 28);
        dtpDeadline.TabIndex = 4;
        // 
        // btnSaveDeadline  (Secondary)
        // 
        btnSaveDeadline.Enabled = false;
        btnSaveDeadline.Location = new Point(582, 14);
        btnSaveDeadline.Name = "btnSaveDeadline";
        btnSaveDeadline.Size = new Size(130, 36);
        btnSaveDeadline.Style = RoundButtonStyle.Secondary;
        btnSaveDeadline.TabIndex = 5;
        btnSaveDeadline.Text = "Đổi deadline";
        btnSaveDeadline.Click += btnSaveDeadline_Click;
        // 
        // btnOpenContent  (Secondary, canh phải)
        // 
        btnOpenContent.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnOpenContent.Enabled = false;
        btnOpenContent.Location = new Point(886, 14);
        btnOpenContent.Name = "btnOpenContent";
        btnOpenContent.Size = new Size(150, 36);
        btnOpenContent.Style = RoundButtonStyle.Secondary;
        btnOpenContent.TabIndex = 6;
        btnOpenContent.Text = "Mở Content";
        btnOpenContent.Click += btnOpenContent_Click;
        // 
        // lblHint  (dòng gợi ý / báo lỗi ngay dưới các ô nhập: đỏ, 11px, đậm khi là lỗi — mục 13)
        // 
        lblHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblHint.Font = UITheme.FontLabel;
        lblHint.ForeColor = UITheme.Neutral600;
        lblHint.Location = new Point(20, 58);
        lblHint.Name = "lblHint";
        lblHint.Size = new Size(1016, 18);
        lblHint.TabIndex = 7;
        lblHint.UseMnemonic = false;
        // 
        // MyTasksForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UITheme.Neutral50;
        ClientSize = new Size(1120, 700);
        // Add SAU CÙNG được dock TRƯỚC: panelHeader (Top) → panelBottomHost (Bottom) → cardList (Fill)
        Controls.Add(cardList);
        Controls.Add(panelBottomHost);
        Controls.Add(panelHeader);
        MinimumSize = new Size(960, 600);
        Name = "MyTasksForm";
        Padding = new Padding(32, 24, 32, 24); // Content area: padding 32px (mục 27)
        StartPosition = FormStartPosition.CenterParent;
        Text = "My Tasks — CreatorFlow";
        panelHeader.ResumeLayout(false);
        cardList.ResumeLayout(false);
        panelEdit.ResumeLayout(false);
        panelEdit.PerformLayout();
        panelBottomHost.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)nudProgress).EndInit();
        ResumeLayout(false);
    }
}