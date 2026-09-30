using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Review;

partial class ReviewQueueForm
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Panel panelTop;
    private System.Windows.Forms.Label lblHeader;
    private CreatorFlow.Controls.RoundedButton btnRefresh;
    private CreatorFlow.Controls.RoundedButton btnOpenReview;
    private System.Windows.Forms.ListView listViewQueue;
    private System.Windows.Forms.ColumnHeader columnCode;
    private System.Windows.Forms.ColumnHeader columnTitle;
    private System.Windows.Forms.ColumnHeader columnSubmittedBy;
    private System.Windows.Forms.ColumnHeader columnSubmittedAt;
    private System.Windows.Forms.ColumnHeader columnReviewNo;

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
        panelTop = new Panel();
        btnOpenReview = new RoundedButton();
        btnRefresh = new RoundedButton();
        lblHeader = new Label();
        listViewQueue = new ListView();
        columnCode = new ColumnHeader();
        columnTitle = new ColumnHeader();
        columnSubmittedBy = new ColumnHeader();
        columnSubmittedAt = new ColumnHeader();
        columnReviewNo = new ColumnHeader();
        panelTop.SuspendLayout();
        SuspendLayout();
        // 
        // panelTop
        // 
        panelTop.BackColor = UITheme.White;
        panelTop.Controls.Add(btnOpenReview);
        panelTop.Controls.Add(btnRefresh);
        panelTop.Controls.Add(lblHeader);
        panelTop.Dock = DockStyle.Top;
        panelTop.Location = new Point(0, 0);
        panelTop.Margin = new Padding(3, 4, 3, 4);
        panelTop.Name = "panelTop";
        panelTop.Padding = new Padding(20, 14, 20, 14);
        panelTop.Size = new Size(869, 64);
        panelTop.TabIndex = 0;
        panelTop.Paint += panelTop_Paint;
        // 
        // btnOpenReview
        // 
        btnOpenReview.Dock = DockStyle.Right;
        btnOpenReview.Enabled = false;
        btnOpenReview.Location = new Point(590, 14);
        btnOpenReview.Margin = new Padding(3, 4, 3, 4);
        btnOpenReview.Name = "btnOpenReview";
        btnOpenReview.Size = new Size(150, 36);
        btnOpenReview.TabIndex = 2;
        btnOpenReview.Text = "Kiểm duyệt";
        btnOpenReview.Style = RoundButtonStyle.Primary;
        btnOpenReview.Click += btnOpenReview_Click;
        // 
        // btnRefresh
        // 
        btnRefresh.Dock = DockStyle.Right;
        btnRefresh.Location = new Point(748, 14);
        btnRefresh.Margin = new Padding(8, 4, 3, 4);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(101, 36);
        btnRefresh.TabIndex = 1;
        btnRefresh.Text = "⟳ Làm mới";
        btnRefresh.Style = RoundButtonStyle.Secondary;
        btnRefresh.Click += btnRefresh_Click;
        // 
        // lblHeader
        // 
        lblHeader.AutoSize = true;
        lblHeader.Dock = DockStyle.Left;
        lblHeader.Font = UITheme.FontH2;
        lblHeader.ForeColor = UITheme.Ink;
        lblHeader.Location = new Point(20, 14);
        lblHeader.Name = "lblHeader";
        lblHeader.Size = new Size(147, 28);
        lblHeader.TabIndex = 0;
        lblHeader.Text = "Review Queue — Danh sách chờ duyệt";
        // 
        // listViewQueue
        // 
        listViewQueue.BorderStyle = BorderStyle.None;
        listViewQueue.Columns.AddRange(new ColumnHeader[] { columnCode, columnTitle, columnSubmittedBy, columnSubmittedAt, columnReviewNo });
        listViewQueue.Dock = DockStyle.Fill;
        listViewQueue.Font = UITheme.FontBody;
        listViewQueue.FullRowSelect = true;
        listViewQueue.GridLines = true;
        listViewQueue.Location = new Point(0, 64);
        listViewQueue.Margin = new Padding(3, 4, 3, 4);
        listViewQueue.MultiSelect = false;
        listViewQueue.Name = "listViewQueue";
        listViewQueue.Size = new Size(869, 523);
        listViewQueue.TabIndex = 1;
        listViewQueue.UseCompatibleStateImageBehavior = false;
        listViewQueue.View = View.Details;
        listViewQueue.SelectedIndexChanged += listViewQueue_SelectedIndexChanged;
        listViewQueue.DoubleClick += btnOpenReview_Click;
        // 
        // columnCode
        // 
        columnCode.Text = "Mã";
        columnCode.Width = 90;
        // 
        // columnTitle
        // 
        columnTitle.Text = "Tiêu đề";
        columnTitle.Width = 320;
        // 
        // columnSubmittedBy
        // 
        columnSubmittedBy.Text = "Người gửi duyệt";
        columnSubmittedBy.Width = 160;
        // 
        // columnSubmittedAt
        // 
        columnSubmittedAt.Text = "Thời gian gửi";
        columnSubmittedAt.Width = 150;
        // 
        // columnReviewNo
        // 
        columnReviewNo.Text = "Lần duyệt";
        columnReviewNo.Width = 90;
        // 
        // ReviewQueueForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UITheme.White;
        ClientSize = new Size(869, 587);
        Controls.Add(listViewQueue);
        Controls.Add(panelTop);
        Margin = new Padding(3, 4, 3, 4);
        MinimumSize = new Size(720, 480);
        Name = "ReviewQueueForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Review Queue — CreatorFlow";
        panelTop.ResumeLayout(false);
        panelTop.PerformLayout();
        ResumeLayout(false);
    }
}