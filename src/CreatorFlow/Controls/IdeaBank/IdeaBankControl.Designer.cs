#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls.IdeaBank;

partial class IdeaBankControl
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel pageLayout;
    private TableLayoutPanel headerLayout;
    private TableLayoutPanel titleLayout;
    private Label lblPageTitle;
    private Label lblSubtitle;
    private ProjectActionButton btnNew;
    private TableLayoutPanel filterLayout;
    private IdeaCardPanel searchSurface;
    private TableLayoutPanel searchLayout;
    private Label lblSearchIcon;
    private TextBox txtSearch;
    private IdeaCardPanel statusSurface;
    private ComboBox cboStatus;
    private IdeaCardPanel tagSurface;
    private ComboBox cboTag;
    private ProjectActionButton btnClearFilter;
    private IdeaCardPanel listSurface;
    private BufferedListView listViewIdeas;
    private ColumnHeader colCode;
    private ColumnHeader colTitle;
    private ColumnHeader colStatus;
    private ColumnHeader colTags;
    private ColumnHeader colCreator;
    private ColumnHeader colUpdated;
    private ImageList rowHeightImageList;
    private Label lblEmptyState;
    private IdeaCardPanel detailsSurface;
    private Panel detailPlaceholder;
    private Label lblDetailIcon;
    private Label lblDetailEmpty;
    private TextBox txtDetail;
    private Panel footerPanel;
    private Label lblCount;
    private FlowLayoutPanel footerActions;
    private ProjectActionButton btnConvert;
    private ProjectActionButton btnEdit;
    private ProjectActionButton btnDelete;
    private ProjectActionButton btnClose;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
            components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pageLayout = new TableLayoutPanel();
        headerLayout = new TableLayoutPanel();
        titleLayout = new TableLayoutPanel();
        lblPageTitle = new Label();
        lblSubtitle = new Label();
        btnNew = new ProjectActionButton();
        filterLayout = new TableLayoutPanel();
        searchSurface = new IdeaCardPanel();
        searchLayout = new TableLayoutPanel();
        lblSearchIcon = new Label();
        txtSearch = new TextBox();
        statusSurface = new IdeaCardPanel();
        cboStatus = new ComboBox();
        tagSurface = new IdeaCardPanel();
        cboTag = new ComboBox();
        btnClearFilter = new ProjectActionButton();
        listSurface = new IdeaCardPanel();
        listViewIdeas = new BufferedListView();
        colCode = new ColumnHeader();
        colTitle = new ColumnHeader();
        colStatus = new ColumnHeader();
        colTags = new ColumnHeader();
        colCreator = new ColumnHeader();
        colUpdated = new ColumnHeader();
        rowHeightImageList = new ImageList(components);
        lblEmptyState = new Label();
        detailsSurface = new IdeaCardPanel();
        detailPlaceholder = new Panel();
        lblDetailIcon = new Label();
        lblDetailEmpty = new Label();
        txtDetail = new TextBox();
        footerPanel = new Panel();
        lblCount = new Label();
        footerActions = new FlowLayoutPanel();
        btnConvert = new ProjectActionButton();
        btnEdit = new ProjectActionButton();
        btnDelete = new ProjectActionButton();
        btnClose = new ProjectActionButton();
        pageLayout.SuspendLayout();
        headerLayout.SuspendLayout();
        titleLayout.SuspendLayout();
        filterLayout.SuspendLayout();
        searchSurface.SuspendLayout();
        searchLayout.SuspendLayout();
        statusSurface.SuspendLayout();
        tagSurface.SuspendLayout();
        listSurface.SuspendLayout();
        detailsSurface.SuspendLayout();
        detailPlaceholder.SuspendLayout();
        footerPanel.SuspendLayout();
        footerActions.SuspendLayout();
        SuspendLayout();
        //
        // pageLayout
        //
        pageLayout.BackColor = Color.White;
        pageLayout.ColumnCount = 1;
        pageLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        pageLayout.Controls.Add(headerLayout, 0, 0);
        pageLayout.Controls.Add(filterLayout, 0, 1);
        pageLayout.Controls.Add(listSurface, 0, 2);
        pageLayout.Controls.Add(detailsSurface, 0, 3);
        pageLayout.Controls.Add(footerPanel, 0, 4);
        pageLayout.Dock = DockStyle.Fill;
        pageLayout.Padding = new Padding(24, 24, 24, 4);
        pageLayout.RowCount = 5;
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 108F));
        pageLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        pageLayout.Name = "pageLayout";
        pageLayout.TabIndex = 0;
        //
        // headerLayout
        //
        headerLayout.BackColor = Color.White;
        headerLayout.ColumnCount = 2;
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 176F));
        headerLayout.Controls.Add(titleLayout, 0, 0);
        headerLayout.Controls.Add(btnNew, 1, 0);
        headerLayout.Dock = DockStyle.Fill;
        headerLayout.Margin = new Padding(0, 0, 0, 7);
        headerLayout.Name = "headerLayout";
        headerLayout.RowCount = 1;
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        headerLayout.TabIndex = 0;
        //
        // titleLayout
        //
        titleLayout.BackColor = Color.White;
        titleLayout.ColumnCount = 1;
        titleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        titleLayout.Controls.Add(lblPageTitle, 0, 0);
        titleLayout.Controls.Add(lblSubtitle, 0, 1);
        titleLayout.Dock = DockStyle.Fill;
        titleLayout.Margin = Padding.Empty;
        titleLayout.Name = "titleLayout";
        titleLayout.RowCount = 2;
        titleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        titleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        titleLayout.TabIndex = 0;
        //
        // lblPageTitle
        //
        lblPageTitle.Dock = DockStyle.Fill;
        lblPageTitle.Font = UITheme.FontPageTitle;
        lblPageTitle.ForeColor = UITheme.Ink;
        lblPageTitle.Margin = Padding.Empty;
        lblPageTitle.Name = "lblPageTitle";
        lblPageTitle.Text = "Idea Bank";
        lblPageTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // lblSubtitle
        //
        lblSubtitle.AutoEllipsis = true;
        lblSubtitle.Dock = DockStyle.Fill;
        lblSubtitle.Font = UITheme.FontBody;
        lblSubtitle.ForeColor = Color.FromArgb(107, 114, 128);
        lblSubtitle.Margin = Padding.Empty;
        lblSubtitle.Name = "lblSubtitle";
        lblSubtitle.Text = "Kho ý tưởng của Project: thêm, sửa, gắn tag, ghi chú, tìm kiếm và lọc theo trạng thái.";
        lblSubtitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // btnNew
        //
        btnNew.Anchor = AnchorStyles.Right;
        btnNew.Margin = new Padding(0, 0, 0, 0);
        btnNew.Name = "btnNew";
        btnNew.ShowPlusIcon = true;
        btnNew.Size = new Size(170, 42);
        btnNew.Style = RoundButtonStyle.Primary;
        btnNew.TabIndex = 0;
        btnNew.Text = "Thêm ý tưởng";
        btnNew.Click += btnNew_Click;
        //
        // filterLayout
        //
        filterLayout.BackColor = Color.White;
        filterLayout.ColumnCount = 4;
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
        filterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        filterLayout.Controls.Add(searchSurface, 0, 0);
        filterLayout.Controls.Add(statusSurface, 1, 0);
        filterLayout.Controls.Add(tagSurface, 2, 0);
        filterLayout.Controls.Add(btnClearFilter, 3, 0);
        filterLayout.Dock = DockStyle.Fill;
        filterLayout.Margin = new Padding(0, 0, 0, 14);
        filterLayout.Name = "filterLayout";
        filterLayout.RowCount = 1;
        filterLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        filterLayout.TabIndex = 1;
        //
        // searchSurface
        //
        searchSurface.BackColor = Color.White;
        searchSurface.BorderColor = Color.FromArgb(229, 231, 235);
        searchSurface.Dock = DockStyle.Fill;
        searchSurface.Margin = new Padding(0, 0, 10, 0);
        searchSurface.Padding = new Padding(8, 4, 10, 4);
        searchSurface.Radius = 9;
        searchSurface.Controls.Add(searchLayout);
        searchSurface.Name = "searchSurface";
        searchSurface.TabIndex = 0;
        //
        // searchLayout
        //
        searchLayout.BackColor = Color.White;
        searchLayout.ColumnCount = 2;
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchLayout.Controls.Add(lblSearchIcon, 0, 0);
        searchLayout.Controls.Add(txtSearch, 1, 0);
        searchLayout.Dock = DockStyle.Fill;
        searchLayout.Margin = Padding.Empty;
        searchLayout.Name = "searchLayout";
        searchLayout.RowCount = 1;
        searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        //
        // lblSearchIcon
        //
        lblSearchIcon.Dock = DockStyle.Fill;
        lblSearchIcon.Margin = Padding.Empty;
        lblSearchIcon.Name = "lblSearchIcon";
        lblSearchIcon.Paint += (_, e) => UIIcons.Search(e.Graphics,
            new Rectangle((lblSearchIcon.Width - 17) / 2, (lblSearchIcon.Height - 17) / 2, 17, 17),
            Color.FromArgb(148, 163, 184));
        //
        // txtSearch
        //
        txtSearch.AccessibleName = "Tìm ý tưởng";
        txtSearch.BackColor = Color.White;
        txtSearch.BorderStyle = BorderStyle.None;
        txtSearch.Dock = DockStyle.Fill;
        txtSearch.Font = UITheme.FontBody;
        txtSearch.Margin = Padding.Empty;
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = "Tìm theo tiêu đề, mô tả, ghi chú, tag...";
        txtSearch.TabIndex = 1;
        //
        // statusSurface
        //
        statusSurface.BackColor = Color.White;
        statusSurface.BorderColor = Color.FromArgb(229, 231, 235);
        statusSurface.Dock = DockStyle.Fill;
        statusSurface.Margin = new Padding(0, 0, 10, 0);
        statusSurface.Padding = new Padding(7, 2, 6, 2);
        statusSurface.Radius = 9;
        statusSurface.Controls.Add(cboStatus);
        statusSurface.Name = "statusSurface";
        statusSurface.TabIndex = 1;
        //
        // cboStatus
        //
        cboStatus.AccessibleName = "Lọc theo trạng thái";
        cboStatus.BackColor = Color.White;
        cboStatus.Dock = DockStyle.Fill;
        cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
        cboStatus.FlatStyle = FlatStyle.Flat;
        cboStatus.Font = UITheme.FontBody;
        cboStatus.Margin = Padding.Empty;
        cboStatus.Name = "cboStatus";
        cboStatus.TabIndex = 2;
        //
        // tagSurface
        //
        tagSurface.BackColor = Color.White;
        tagSurface.BorderColor = Color.FromArgb(229, 231, 235);
        tagSurface.Dock = DockStyle.Fill;
        tagSurface.Margin = new Padding(0, 0, 10, 0);
        tagSurface.Padding = new Padding(7, 2, 6, 2);
        tagSurface.Radius = 9;
        tagSurface.Controls.Add(cboTag);
        tagSurface.Name = "tagSurface";
        tagSurface.TabIndex = 2;
        //
        // cboTag
        //
        cboTag.AccessibleName = "Lọc theo tag";
        cboTag.BackColor = Color.White;
        cboTag.Dock = DockStyle.Fill;
        cboTag.DropDownStyle = ComboBoxStyle.DropDownList;
        cboTag.FlatStyle = FlatStyle.Flat;
        cboTag.Font = UITheme.FontBody;
        cboTag.Margin = Padding.Empty;
        cboTag.Name = "cboTag";
        cboTag.TabIndex = 3;
        //
        // btnClearFilter
        //
        btnClearFilter.Dock = DockStyle.Fill;
        btnClearFilter.Margin = Padding.Empty;
        btnClearFilter.Name = "btnClearFilter";
        btnClearFilter.Style = RoundButtonStyle.Secondary;
        btnClearFilter.TabIndex = 4;
        btnClearFilter.Text = "Xóa lọc";
        btnClearFilter.Click += btnClearFilter_Click;
        //
        // listSurface
        //
        listSurface.BackColor = Color.White;
        listSurface.BorderColor = Color.FromArgb(228, 231, 235);
        listSurface.Dock = DockStyle.Fill;
        listSurface.Margin = new Padding(0, 0, 0, 20);
        listSurface.Padding = new Padding(1);
        listSurface.Radius = 12;
        listSurface.Controls.Add(listViewIdeas);
        listSurface.Controls.Add(lblEmptyState);
        listSurface.Name = "listSurface";
        listSurface.TabIndex = 2;
        //
        // listViewIdeas
        //
        listViewIdeas.AccessibleName = "Danh sách ý tưởng trong Project";
        listViewIdeas.BorderStyle = BorderStyle.None;
        listViewIdeas.Columns.AddRange(new ColumnHeader[] { colCode, colTitle, colStatus, colTags, colCreator, colUpdated });
        listViewIdeas.Dock = DockStyle.Fill;
        listViewIdeas.Font = UITheme.FontBody;
        listViewIdeas.FullRowSelect = true;
        listViewIdeas.GridLines = false;
        listViewIdeas.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        listViewIdeas.HideSelection = false;
        listViewIdeas.MultiSelect = false;
        listViewIdeas.Name = "listViewIdeas";
        listViewIdeas.OwnerDraw = true;
        listViewIdeas.SmallImageList = rowHeightImageList;
        listViewIdeas.TabIndex = 5;
        listViewIdeas.UseCompatibleStateImageBehavior = false;
        listViewIdeas.View = View.Details;
        listViewIdeas.DrawColumnHeader += listViewIdeas_DrawColumnHeader;
        listViewIdeas.DrawItem += listViewIdeas_DrawItem;
        listViewIdeas.DrawSubItem += listViewIdeas_DrawSubItem;
        listViewIdeas.SelectedIndexChanged += listViewIdeas_SelectedIndexChanged;
        listViewIdeas.DoubleClick += listViewIdeas_DoubleClick;
        listViewIdeas.SizeChanged += (_, _) => ResizeIdeaColumns();
        //
        // columns
        //
        colCode.Text = "MÃ";
        colTitle.Text = "TIÊU ĐỀ";
        colStatus.Text = "TRẠNG THÁI";
        colTags.Text = "TAG";
        colCreator.Text = "NGƯỜI TẠO";
        colUpdated.Text = "CẬP NHẬT";
        //
        // rowHeightImageList
        //
        rowHeightImageList.ColorDepth = ColorDepth.Depth32Bit;
        rowHeightImageList.ImageSize = new Size(1, 60);
        rowHeightImageList.TransparentColor = Color.Transparent;
        //
        // lblEmptyState
        //
        lblEmptyState.BackColor = Color.White;
        lblEmptyState.Dock = DockStyle.Fill;
        lblEmptyState.Font = UITheme.FontH2;
        lblEmptyState.ForeColor = UITheme.Neutral600;
        lblEmptyState.Name = "lblEmptyState";
        lblEmptyState.Text = "Chưa có ý tưởng phù hợp\r\nThử thay đổi bộ lọc hoặc thêm ý tưởng mới.";
        lblEmptyState.TextAlign = ContentAlignment.MiddleCenter;
        lblEmptyState.Visible = false;
        //
        // detailsSurface
        //
        detailsSurface.BackColor = Color.White;
        detailsSurface.BorderColor = Color.FromArgb(228, 231, 235);
        detailsSurface.Dock = DockStyle.Fill;
        detailsSurface.Margin = new Padding(0, 0, 0, 12);
        detailsSurface.Padding = new Padding(12, 5, 12, 5);
        detailsSurface.Radius = 12;
        detailsSurface.Controls.Add(txtDetail);
        detailsSurface.Controls.Add(detailPlaceholder);
        detailsSurface.Name = "detailsSurface";
        detailsSurface.TabIndex = 3;
        //
        // detailPlaceholder
        //
        detailPlaceholder.BackColor = Color.White;
        detailPlaceholder.Controls.Add(lblDetailEmpty);
        detailPlaceholder.Controls.Add(lblDetailIcon);
        detailPlaceholder.Dock = DockStyle.Fill;
        detailPlaceholder.Name = "detailPlaceholder";
        detailPlaceholder.TabIndex = 0;
        //
        // lblDetailIcon
        //
        lblDetailIcon.Dock = DockStyle.Top;
        lblDetailIcon.Font = new Font("Segoe UI Symbol", 15F, FontStyle.Regular, GraphicsUnit.Point);
        lblDetailIcon.ForeColor = Color.FromArgb(210, 214, 220);
        lblDetailIcon.Height = 42;
        lblDetailIcon.Name = "lblDetailIcon";
        lblDetailIcon.Text = "▤";
        lblDetailIcon.TextAlign = ContentAlignment.BottomCenter;
        //
        // lblDetailEmpty
        //
        lblDetailEmpty.Dock = DockStyle.Fill;
        lblDetailEmpty.Font = UITheme.FontBodyBold;
        lblDetailEmpty.ForeColor = Color.FromArgb(173, 178, 188);
        lblDetailEmpty.Name = "lblDetailEmpty";
        lblDetailEmpty.Text = "Ghi chú và nội dung chi tiết ý tưởng hiển thị tại đây";
        lblDetailEmpty.TextAlign = ContentAlignment.MiddleCenter;
        //
        // txtDetail
        //
        txtDetail.BackColor = Color.White;
        txtDetail.BorderStyle = BorderStyle.None;
        txtDetail.Dock = DockStyle.Fill;
        txtDetail.Font = UITheme.FontBody;
        txtDetail.Multiline = true;
        txtDetail.Name = "txtDetail";
        txtDetail.ReadOnly = true;
        txtDetail.ScrollBars = ScrollBars.Vertical;
        txtDetail.Visible = false;
        txtDetail.TabIndex = 6;
        //
        // footerPanel
        //
        footerPanel.BackColor = Color.FromArgb(250, 250, 251);
        footerPanel.Controls.Add(footerActions);
        footerPanel.Controls.Add(lblCount);
        footerPanel.Dock = DockStyle.Fill;
        footerPanel.Margin = Padding.Empty;
        footerPanel.Padding = new Padding(16, 0, 16, 0);
        footerPanel.Name = "footerPanel";
        footerPanel.TabIndex = 4;
        footerPanel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(238, 240, 243));
            e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
        };
        //
        // lblCount
        //
        lblCount.Dock = DockStyle.Left;
        lblCount.Font = UITheme.FontBody;
        lblCount.ForeColor = Color.FromArgb(107, 114, 128);
        lblCount.Name = "lblCount";
        lblCount.Size = new Size(250, 58);
        lblCount.TextAlign = ContentAlignment.MiddleLeft;
        //
        // footerActions
        //
        footerActions.Dock = DockStyle.Right;
        footerActions.BackColor = footerPanel.BackColor;
        footerActions.FlowDirection = FlowDirection.RightToLeft;
        footerActions.Margin = Padding.Empty;
        footerActions.Name = "footerActions";
        footerActions.WrapContents = false;
        footerActions.Width = 470;
        footerActions.Controls.Add(btnClose);
        footerActions.Controls.Add(btnDelete);
        footerActions.Controls.Add(btnEdit);
        footerActions.Controls.Add(btnConvert);
        //
        // btnConvert
        //
        btnConvert.Margin = new Padding(0, 8, 8, 8);
        btnConvert.Name = "btnConvert";
        btnConvert.Size = new Size(180, 40);
        btnConvert.Style = RoundButtonStyle.Secondary;
        btnConvert.TabIndex = 10;
        btnConvert.Text = "Chuyển thành Content";
        btnConvert.Click += btnConvert_Click;
        //
        // btnEdit
        //
        btnEdit.Margin = new Padding(0, 8, 8, 8);
        btnEdit.Name = "btnEdit";
        btnEdit.Size = new Size(82, 40);
        btnEdit.Style = RoundButtonStyle.Secondary;
        btnEdit.TabIndex = 7;
        btnEdit.Text = "Sửa";
        btnEdit.Click += btnEdit_Click;
        //
        // btnDelete
        //
        btnDelete.Margin = new Padding(0, 8, 8, 8);
        btnDelete.Name = "btnDelete";
        btnDelete.Size = new Size(82, 40);
        btnDelete.Style = RoundButtonStyle.DangerGhost;
        btnDelete.TabIndex = 8;
        btnDelete.Text = "Xóa";
        btnDelete.Click += btnDelete_Click;
        //
        // btnClose
        //
        btnClose.Margin = new Padding(0, 8, 0, 8);
        btnClose.Name = "btnClose";
        btnClose.Size = new Size(82, 40);
        btnClose.Style = RoundButtonStyle.Primary;
        btnClose.TabIndex = 9;
        btnClose.Text = "Đóng";
        btnClose.Click += btnClose_Click;
        //
        // IdeaBankControl
        //
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.White;
        Controls.Add(pageLayout);
        Font = UITheme.FontBody;
        Name = "IdeaBankControl";
        Size = new Size(1200, 760);
        pageLayout.ResumeLayout(false);
        headerLayout.ResumeLayout(false);
        titleLayout.ResumeLayout(false);
        filterLayout.ResumeLayout(false);
        searchSurface.ResumeLayout(false);
        searchLayout.ResumeLayout(false);
        searchSurface.PerformLayout();
        statusSurface.ResumeLayout(false);
        tagSurface.ResumeLayout(false);
        listSurface.ResumeLayout(false);
        detailsSurface.ResumeLayout(false);
        detailPlaceholder.ResumeLayout(false);
        footerPanel.ResumeLayout(false);
        footerActions.ResumeLayout(false);
        ResumeLayout(false);
    }
}
