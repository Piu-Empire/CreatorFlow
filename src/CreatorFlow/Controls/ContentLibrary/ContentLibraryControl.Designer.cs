#nullable disable

namespace CreatorFlow.Controls.ContentLibrary;

partial class ContentLibraryControl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        frame = new LibraryFramePanel();
        layout = new TableLayoutPanel();
        heading = new TableLayoutPanel();
        titleLine = new FlowLayoutPanel();
        titleLabel = new Label();
        workspaceBadge = new WorkspaceBadgeLabel();
        subtitleLine = new FlowLayoutPanel();
        projectNameLabel = new Label();
        dotLabel = new Label();
        descriptionLabel = new Label();
        toolbar = new RoundedSurfacePanel();
        filters = new TableLayoutPanel();
        searchSurface = new RoundedSurfacePanel();
        searchContent = new TableLayoutPanel();
        searchGlyph = new SearchGlyphControl();
        search = new TextBox();
        statusSurface = new RoundedSurfacePanel();
        statusFilter = new ComboBox();
        platformSurface = new RoundedSurfacePanel();
        platformFilter = new ComboBox();
        countSurface = new RoundedSurfacePanel();
        countLabel = new Label();
        tableSurface = new RoundedSurfacePanel();
        tableLayout = new TableLayoutPanel();
        columnHeader = new TableLayoutPanel();
        headerContent = new Label();
        headerStatus = new Label();
        headerPlatform = new Label();
        headerAssignee = new Label();
        headerAction = new Label();
        viewport = new Panel();
        rows = new TableLayoutPanel();
        stateLabel = new Label();
        footer = new TableLayoutPanel();
        summaryLabel = new Label();
        brandLabel = new Label();
        frame.SuspendLayout();
        layout.SuspendLayout();
        heading.SuspendLayout();
        titleLine.SuspendLayout();
        subtitleLine.SuspendLayout();
        toolbar.SuspendLayout();
        filters.SuspendLayout();
        searchSurface.SuspendLayout();
        searchContent.SuspendLayout();
        statusSurface.SuspendLayout();
        platformSurface.SuspendLayout();
        countSurface.SuspendLayout();
        tableSurface.SuspendLayout();
        tableLayout.SuspendLayout();
        columnHeader.SuspendLayout();
        viewport.SuspendLayout();
        footer.SuspendLayout();
        SuspendLayout();
        // 
        // frame
        // 
        frame.BackColor = Color.White;
        frame.Controls.Add(layout);
        frame.Dock = DockStyle.Fill;
        frame.Location = new Point(0, 0);
        frame.Margin = new Padding(1);
        frame.Name = "frame";
        frame.Padding = new Padding(34, 25, 34, 22);
        frame.Size = new Size(1200, 700);
        frame.TabIndex = 0;
        // 
        // layout
        // 
        layout.BackColor = Color.White;
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(heading, 0, 0);
        layout.Controls.Add(toolbar, 0, 1);
        layout.Controls.Add(tableSurface, 0, 2);
        layout.Dock = DockStyle.Fill;
        layout.Location = new Point(34, 25);
        layout.Margin = new Padding(0);
        layout.Name = "layout";
        layout.RowCount = 3;
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.Size = new Size(1132, 653);
        layout.TabIndex = 0;
        // 
        // heading
        // 
        heading.BackColor = Color.White;
        heading.ColumnCount = 1;
        heading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        heading.Controls.Add(titleLine, 0, 0);
        heading.Controls.Add(subtitleLine, 0, 1);
        heading.Dock = DockStyle.Fill;
        heading.Location = new Point(0, 0);
        heading.Margin = new Padding(0, 0, 0, 2);
        heading.Name = "heading";
        heading.RowCount = 2;
        heading.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
        heading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        heading.Size = new Size(1132, 70);
        heading.TabIndex = 0;
        // 
        // titleLine
        // 
        titleLine.BackColor = Color.White;
        titleLine.Controls.Add(titleLabel);
        titleLine.Controls.Add(workspaceBadge);
        titleLine.Dock = DockStyle.Fill;
        titleLine.Location = new Point(0, 0);
        titleLine.Margin = new Padding(0);
        titleLine.Name = "titleLine";
        titleLine.Size = new Size(1132, 39);
        titleLine.TabIndex = 0;
        titleLine.WrapContents = false;
        // 
        // titleLabel
        // 
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(0, 0);
        titleLabel.Margin = new Padding(0);
        titleLabel.Name = "titleLabel";
        titleLabel.Size = new Size(91, 20);
        titleLabel.TabIndex = 0;
        titleLabel.Text = "Kho Content";
        titleLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // workspaceBadge
        // 
        workspaceBadge.BackColor = Color.FromArgb(243, 238, 255);
        workspaceBadge.ForeColor = Color.FromArgb(109, 40, 217);
        workspaceBadge.Location = new Point(103, 8);
        workspaceBadge.Margin = new Padding(12, 8, 0, 0);
        workspaceBadge.Name = "workspaceBadge";
        workspaceBadge.Size = new Size(74, 20);
        workspaceBadge.TabIndex = 1;
        workspaceBadge.Text = "Workspace";
        workspaceBadge.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // subtitleLine
        // 
        subtitleLine.BackColor = Color.White;
        subtitleLine.Controls.Add(projectNameLabel);
        subtitleLine.Controls.Add(dotLabel);
        subtitleLine.Controls.Add(descriptionLabel);
        subtitleLine.Dock = DockStyle.Fill;
        subtitleLine.Location = new Point(0, 39);
        subtitleLine.Margin = new Padding(0);
        subtitleLine.Name = "subtitleLine";
        subtitleLine.Size = new Size(1132, 31);
        subtitleLine.TabIndex = 1;
        subtitleLine.WrapContents = false;
        // 
        // projectNameLabel
        // 
        projectNameLabel.AutoSize = true;
        projectNameLabel.ForeColor = Color.FromArgb(86, 111, 157);
        projectNameLabel.Location = new Point(0, 2);
        projectNameLabel.Margin = new Padding(0, 2, 0, 0);
        projectNameLabel.Name = "projectNameLabel";
        projectNameLabel.Size = new Size(0, 20);
        projectNameLabel.TabIndex = 0;
        projectNameLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // dotLabel
        // 
        dotLabel.AutoSize = true;
        dotLabel.ForeColor = Color.FromArgb(195, 205, 220);
        dotLabel.Location = new Point(8, 2);
        dotLabel.Margin = new Padding(8, 2, 8, 0);
        dotLabel.Name = "dotLabel";
        dotLabel.Size = new Size(12, 20);
        dotLabel.TabIndex = 1;
        dotLabel.Text = "·";
        // 
        // descriptionLabel
        // 
        descriptionLabel.AutoSize = true;
        descriptionLabel.ForeColor = Color.FromArgb(107, 125, 153);
        descriptionLabel.Location = new Point(28, 2);
        descriptionLabel.Margin = new Padding(0, 2, 0, 0);
        descriptionLabel.Name = "descriptionLabel";
        descriptionLabel.Size = new Size(399, 20);
        descriptionLabel.TabIndex = 2;
        descriptionLabel.Text = "Toàn bộ Content của Project, bao gồm nội dung đã lưu trữ.";
        // 
        // toolbar
        // 
        toolbar.BackColor = Color.White;
        toolbar.BorderColor = Color.FromArgb(229, 235, 244);
        toolbar.Controls.Add(filters);
        toolbar.Dock = DockStyle.Fill;
        toolbar.Location = new Point(0, 73);
        toolbar.Margin = new Padding(0, 1, 0, 10);
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(10, 7, 10, 7);
        toolbar.Radius = 12;
        toolbar.Size = new Size(1132, 45);
        toolbar.TabIndex = 1;
        // 
        // filters
        // 
        filters.BackColor = Color.White;
        filters.ColumnCount = 4;
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 146F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 154F));
        filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68F));
        filters.Controls.Add(searchSurface, 0, 0);
        filters.Controls.Add(statusSurface, 1, 0);
        filters.Controls.Add(platformSurface, 2, 0);
        filters.Controls.Add(countSurface, 3, 0);
        filters.Dock = DockStyle.Fill;
        filters.Location = new Point(10, 7);
        filters.Margin = new Padding(0);
        filters.Name = "filters";
        filters.RowCount = 1;
        filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        filters.Size = new Size(1112, 31);
        filters.TabIndex = 0;
        // 
        // searchSurface
        // 
        searchSurface.BackColor = Color.FromArgb(250, 251, 253);
        searchSurface.BorderColor = Color.FromArgb(231, 236, 245);
        searchSurface.Controls.Add(searchContent);
        searchSurface.Dock = DockStyle.Fill;
        searchSurface.Location = new Point(1, 1);
        searchSurface.Margin = new Padding(1, 1, 12, 1);
        searchSurface.Name = "searchSurface";
        searchSurface.Padding = new Padding(10, 2, 8, 2);
        searchSurface.Radius = 9;
        searchSurface.Size = new Size(731, 29);
        searchSurface.TabIndex = 0;
        // 
        // searchContent
        // 
        searchContent.BackColor = Color.FromArgb(250, 251, 253);
        searchContent.ColumnCount = 2;
        searchContent.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
        searchContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        searchContent.Controls.Add(searchGlyph, 0, 0);
        searchContent.Controls.Add(search, 1, 0);
        searchContent.Dock = DockStyle.Fill;
        searchContent.Location = new Point(10, 2);
        searchContent.Margin = new Padding(0);
        searchContent.Name = "searchContent";
        searchContent.RowCount = 1;
        searchContent.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        searchContent.Size = new Size(713, 25);
        searchContent.TabIndex = 0;
        // 
        // searchGlyph
        // 
        searchGlyph.Dock = DockStyle.Fill;
        searchGlyph.Location = new Point(0, 0);
        searchGlyph.Margin = new Padding(0);
        searchGlyph.Name = "searchGlyph";
        searchGlyph.Size = new Size(22, 25);
        searchGlyph.TabIndex = 0;
        // 
        // search
        // 
        search.AccessibleName = "Tìm Content trong Project";
        search.BackColor = Color.FromArgb(250, 251, 253);
        search.BorderStyle = BorderStyle.None;
        search.Dock = DockStyle.Fill;
        search.Location = new Point(22, 4);
        search.Margin = new Padding(0, 4, 0, 3);
        search.Name = "search";
        search.PlaceholderText = "Tìm theo mã hoặc tên Content";
        search.Size = new Size(691, 20);
        search.TabIndex = 1;
        search.TextChanged += Filter_Changed;
        // 
        // statusSurface
        // 
        statusSurface.BackColor = Color.White;
        statusSurface.BorderColor = Color.FromArgb(231, 236, 245);
        statusSurface.Controls.Add(statusFilter);
        statusSurface.Dock = DockStyle.Fill;
        statusSurface.Location = new Point(749, 1);
        statusSurface.Margin = new Padding(5, 1, 2, 1);
        statusSurface.Name = "statusSurface";
        statusSurface.Padding = new Padding(5, 2, 3, 2);
        statusSurface.Radius = 9;
        statusSurface.Size = new Size(139, 29);
        statusSurface.TabIndex = 1;
        // 
        // statusFilter
        // 
        statusFilter.AccessibleName = "Lọc theo trạng thái";
        statusFilter.BackColor = Color.White;
        statusFilter.Dock = DockStyle.Fill;
        statusFilter.DrawMode = DrawMode.OwnerDrawFixed;
        statusFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        statusFilter.FlatStyle = FlatStyle.Flat;
        statusFilter.ForeColor = Color.FromArgb(42, 57, 81);
        statusFilter.IntegralHeight = false;
        statusFilter.ItemHeight = 24;
        statusFilter.Location = new Point(5, 2);
        statusFilter.Margin = new Padding(0);
        statusFilter.Name = "statusFilter";
        statusFilter.Size = new Size(131, 30);
        statusFilter.TabIndex = 0;
        statusFilter.DrawItem += Filter_DrawItem;
        statusFilter.SelectedIndexChanged += Filter_Changed;
        // 
        // platformSurface
        // 
        platformSurface.BackColor = Color.White;
        platformSurface.BorderColor = Color.FromArgb(231, 236, 245);
        platformSurface.Controls.Add(platformFilter);
        platformSurface.Dock = DockStyle.Fill;
        platformSurface.Location = new Point(895, 1);
        platformSurface.Margin = new Padding(5, 1, 2, 1);
        platformSurface.Name = "platformSurface";
        platformSurface.Padding = new Padding(5, 2, 3, 2);
        platformSurface.Radius = 9;
        platformSurface.Size = new Size(147, 29);
        platformSurface.TabIndex = 2;
        // 
        // platformFilter
        // 
        platformFilter.AccessibleName = "Lọc theo nền tảng";
        platformFilter.BackColor = Color.White;
        platformFilter.Dock = DockStyle.Fill;
        platformFilter.DrawMode = DrawMode.OwnerDrawFixed;
        platformFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        platformFilter.FlatStyle = FlatStyle.Flat;
        platformFilter.ForeColor = Color.FromArgb(42, 57, 81);
        platformFilter.IntegralHeight = false;
        platformFilter.ItemHeight = 24;
        platformFilter.Location = new Point(5, 2);
        platformFilter.Margin = new Padding(0);
        platformFilter.Name = "platformFilter";
        platformFilter.Size = new Size(139, 30);
        platformFilter.TabIndex = 0;
        platformFilter.DrawItem += Filter_DrawItem;
        platformFilter.SelectedIndexChanged += Filter_Changed;
        // 
        // countSurface
        // 
        countSurface.BackColor = Color.FromArgb(244, 247, 251);
        countSurface.BorderColor = Color.FromArgb(231, 236, 245);
        countSurface.Controls.Add(countLabel);
        countSurface.Dock = DockStyle.Fill;
        countSurface.Location = new Point(1045, 1);
        countSurface.Margin = new Padding(1, 1, 0, 1);
        countSurface.Name = "countSurface";
        countSurface.Radius = 9;
        countSurface.Size = new Size(67, 29);
        countSurface.TabIndex = 3;
        // 
        // countLabel
        // 
        countLabel.Dock = DockStyle.Fill;
        countLabel.ForeColor = Color.FromArgb(76, 94, 123);
        countLabel.Location = new Point(0, 0);
        countLabel.Margin = new Padding(0);
        countLabel.Name = "countLabel";
        countLabel.Size = new Size(67, 29);
        countLabel.TabIndex = 0;
        countLabel.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // tableSurface
        // 
        tableSurface.BackColor = Color.White;
        tableSurface.BorderColor = Color.FromArgb(228, 234, 243);
        tableSurface.Controls.Add(tableLayout);
        tableSurface.Dock = DockStyle.Fill;
        tableSurface.Location = new Point(0, 136);
        tableSurface.Margin = new Padding(0, 8, 0, 0);
        tableSurface.Name = "tableSurface";
        tableSurface.Padding = new Padding(1);
        tableSurface.Radius = 13;
        tableSurface.Size = new Size(1132, 517);
        tableSurface.TabIndex = 2;
        // 
        // tableLayout
        // 
        tableLayout.BackColor = Color.White;
        tableLayout.ColumnCount = 1;
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        tableLayout.Controls.Add(columnHeader, 0, 0);
        tableLayout.Controls.Add(viewport, 0, 1);
        tableLayout.Controls.Add(footer, 0, 2);
        tableLayout.Dock = DockStyle.Fill;
        tableLayout.Location = new Point(1, 1);
        tableLayout.Margin = new Padding(0);
        tableLayout.Name = "tableLayout";
        tableLayout.RowCount = 3;
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        tableLayout.Size = new Size(1130, 515);
        tableLayout.TabIndex = 0;
        // 
        // columnHeader
        // 
        columnHeader.BackColor = Color.FromArgb(249, 251, 254);
        columnHeader.ColumnCount = 5;
        columnHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29F));
        columnHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 18F));
        columnHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        columnHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24F));
        columnHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 9F));
        columnHeader.Controls.Add(headerContent, 0, 0);
        columnHeader.Controls.Add(headerStatus, 1, 0);
        columnHeader.Controls.Add(headerPlatform, 2, 0);
        columnHeader.Controls.Add(headerAssignee, 3, 0);
        columnHeader.Controls.Add(headerAction, 4, 0);
        columnHeader.Dock = DockStyle.Fill;
        columnHeader.Location = new Point(0, 0);
        columnHeader.Margin = new Padding(0);
        columnHeader.Name = "columnHeader";
        columnHeader.Padding = new Padding(19, 0, 19, 0);
        columnHeader.RowCount = 1;
        columnHeader.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        columnHeader.Size = new Size(1130, 40);
        columnHeader.TabIndex = 0;
        // 
        // headerContent
        // 
        headerContent.Dock = DockStyle.Fill;
        headerContent.ForeColor = Color.FromArgb(91, 111, 143);
        headerContent.Location = new Point(19, 0);
        headerContent.Margin = new Padding(0, 0, 6, 0);
        headerContent.Name = "headerContent";
        headerContent.Size = new Size(310, 40);
        headerContent.TabIndex = 0;
        headerContent.Text = "Content";
        headerContent.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerStatus
        // 
        headerStatus.Dock = DockStyle.Fill;
        headerStatus.ForeColor = Color.FromArgb(91, 111, 143);
        headerStatus.Location = new Point(341, 0);
        headerStatus.Margin = new Padding(6, 0, 6, 0);
        headerStatus.Name = "headerStatus";
        headerStatus.Size = new Size(184, 40);
        headerStatus.TabIndex = 1;
        headerStatus.Text = "Trạng thái";
        headerStatus.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerPlatform
        // 
        headerPlatform.Dock = DockStyle.Fill;
        headerPlatform.ForeColor = Color.FromArgb(91, 111, 143);
        headerPlatform.Location = new Point(537, 0);
        headerPlatform.Margin = new Padding(6, 0, 6, 0);
        headerPlatform.Name = "headerPlatform";
        headerPlatform.Size = new Size(206, 40);
        headerPlatform.TabIndex = 2;
        headerPlatform.Text = "Nền tảng";
        headerPlatform.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerAssignee
        // 
        headerAssignee.Dock = DockStyle.Fill;
        headerAssignee.ForeColor = Color.FromArgb(91, 111, 143);
        headerAssignee.Location = new Point(755, 0);
        headerAssignee.Margin = new Padding(6, 0, 6, 0);
        headerAssignee.Name = "headerAssignee";
        headerAssignee.Size = new Size(250, 40);
        headerAssignee.TabIndex = 3;
        headerAssignee.Text = "Người được giao";
        headerAssignee.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // headerAction
        // 
        headerAction.Dock = DockStyle.Fill;
        headerAction.ForeColor = Color.FromArgb(91, 111, 143);
        headerAction.Location = new Point(1017, 0);
        headerAction.Margin = new Padding(6, 0, 6, 0);
        headerAction.Name = "headerAction";
        headerAction.Size = new Size(88, 40);
        headerAction.TabIndex = 4;
        headerAction.Text = "Thao tác";
        headerAction.TextAlign = ContentAlignment.MiddleRight;
        // 
        // viewport
        // 
        viewport.AutoScroll = true;
        viewport.BackColor = Color.White;
        viewport.Controls.Add(rows);
        viewport.Controls.Add(stateLabel);
        viewport.Dock = DockStyle.Fill;
        viewport.Location = new Point(0, 40);
        viewport.Margin = new Padding(0);
        viewport.Name = "viewport";
        viewport.Size = new Size(1130, 447);
        viewport.TabIndex = 1;
        // 
        // rows
        // 
        rows.AutoSize = true;
        rows.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        rows.BackColor = Color.White;
        rows.ColumnCount = 1;
        rows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rows.Dock = DockStyle.Top;
        rows.Location = new Point(0, 0);
        rows.Margin = new Padding(0);
        rows.Name = "rows";
        rows.Size = new Size(1130, 0);
        rows.TabIndex = 0;
        // 
        // stateLabel
        // 
        stateLabel.Dock = DockStyle.Fill;
        stateLabel.Location = new Point(0, 0);
        stateLabel.Margin = new Padding(0);
        stateLabel.Name = "stateLabel";
        stateLabel.Size = new Size(1130, 447);
        stateLabel.TabIndex = 1;
        stateLabel.TextAlign = ContentAlignment.MiddleCenter;
        stateLabel.Visible = false;
        // 
        // footer
        // 
        footer.BackColor = Color.FromArgb(251, 252, 254);
        footer.ColumnCount = 2;
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        footer.ColumnStyles.Add(new ColumnStyle());
        footer.Controls.Add(summaryLabel, 0, 0);
        footer.Controls.Add(brandLabel, 1, 0);
        footer.Dock = DockStyle.Fill;
        footer.Location = new Point(0, 487);
        footer.Margin = new Padding(0);
        footer.Name = "footer";
        footer.Padding = new Padding(18, 0, 18, 0);
        footer.RowCount = 1;
        footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        footer.Size = new Size(1130, 28);
        footer.TabIndex = 2;
        // 
        // summaryLabel
        // 
        summaryLabel.Dock = DockStyle.Fill;
        summaryLabel.ForeColor = Color.FromArgb(125, 143, 170);
        summaryLabel.Location = new Point(18, 0);
        summaryLabel.Margin = new Padding(0);
        summaryLabel.Name = "summaryLabel";
        summaryLabel.Size = new Size(912, 28);
        summaryLabel.TabIndex = 0;
        summaryLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // brandLabel
        // 
        brandLabel.AutoSize = true;
        brandLabel.Dock = DockStyle.Fill;
        brandLabel.ForeColor = Color.FromArgb(91, 112, 145);
        brandLabel.Location = new Point(930, 0);
        brandLabel.Margin = new Padding(0);
        brandLabel.Name = "brandLabel";
        brandLabel.Size = new Size(182, 28);
        brandLabel.TabIndex = 1;
        brandLabel.Text = "CreatorFlow Content Suite";
        brandLabel.TextAlign = ContentAlignment.MiddleRight;
        // 
        // ContentLibraryControl
        // 
        BackColor = Color.FromArgb(248, 250, 253);
        Controls.Add(frame);
        Name = "ContentLibraryControl";
        Size = new Size(1200, 700);
        frame.ResumeLayout(false);
        layout.ResumeLayout(false);
        heading.ResumeLayout(false);
        titleLine.ResumeLayout(false);
        titleLine.PerformLayout();
        subtitleLine.ResumeLayout(false);
        subtitleLine.PerformLayout();
        toolbar.ResumeLayout(false);
        filters.ResumeLayout(false);
        searchSurface.ResumeLayout(false);
        searchContent.ResumeLayout(false);
        searchContent.PerformLayout();
        statusSurface.ResumeLayout(false);
        platformSurface.ResumeLayout(false);
        countSurface.ResumeLayout(false);
        tableSurface.ResumeLayout(false);
        tableLayout.ResumeLayout(false);
        columnHeader.ResumeLayout(false);
        viewport.ResumeLayout(false);
        viewport.PerformLayout();
        footer.ResumeLayout(false);
        footer.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private LibraryFramePanel frame;
    private TableLayoutPanel layout;
    private TableLayoutPanel heading;
    private FlowLayoutPanel titleLine;
    private Label titleLabel;
    private WorkspaceBadgeLabel workspaceBadge;
    private FlowLayoutPanel subtitleLine;
    private Label projectNameLabel;
    private Label dotLabel;
    private Label descriptionLabel;
    private RoundedSurfacePanel toolbar;
    private TableLayoutPanel filters;
    private RoundedSurfacePanel searchSurface;
    private TableLayoutPanel searchContent;
    private SearchGlyphControl searchGlyph;
    private TextBox search;
    private RoundedSurfacePanel statusSurface;
    private ComboBox statusFilter;
    private RoundedSurfacePanel platformSurface;
    private ComboBox platformFilter;
    private RoundedSurfacePanel countSurface;
    private Label countLabel;
    private RoundedSurfacePanel tableSurface;
    private TableLayoutPanel tableLayout;
    private TableLayoutPanel columnHeader;
    private Label headerContent;
    private Label headerStatus;
    private Label headerPlatform;
    private Label headerAssignee;
    private Label headerAction;
    private Panel viewport;
    private TableLayoutPanel rows;
    private Label stateLabel;
    private TableLayoutPanel footer;
    private Label summaryLabel;
    private Label brandLabel;
}
