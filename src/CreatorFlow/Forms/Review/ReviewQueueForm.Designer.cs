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
        panelTop.BackColor = Color.FromArgb(255, 255, 255);
        panelTop.Controls.Add(btnOpenReview);
        panelTop.Controls.Add(btnRefresh);
        panelTop.Controls.Add(lblHeader);
        panelTop.Dock = DockStyle.Top;
        panelTop.Location = new Point(0, 0);
        panelTop.Margin = new Padding(4, 5, 4, 5);
        panelTop.Name = "panelTop";
        panelTop.Padding = new Padding(25, 18, 25, 18);
        panelTop.Size = new Size(1086, 80);
        panelTop.TabIndex = 0;
        panelTop.Paint += panelTop_Paint;
        // 
        // btnOpenReview
        // 
        btnOpenReview.BorderRadius = 8;
        btnOpenReview.Dock = DockStyle.Right;
        btnOpenReview.Enabled = false;
        btnOpenReview.FlatStyle = FlatStyle.Flat;
        btnOpenReview.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnOpenReview.Location = new Point(747, 18);
        btnOpenReview.Margin = new Padding(4, 5, 4, 5);
        btnOpenReview.Name = "btnOpenReview";
        btnOpenReview.Padding = new Padding(15, 8, 15, 8);
        btnOpenReview.ShowPlusIcon = false;
        btnOpenReview.Size = new Size(188, 44);
        btnOpenReview.Style = RoundButtonStyle.Primary;
        btnOpenReview.TabIndex = 2;
        btnOpenReview.Text = "Kiểm duyệt";
        btnOpenReview.Click += btnOpenReview_Click;
        // 
        // btnRefresh
        // 
        btnRefresh.BorderRadius = 8;
        btnRefresh.Dock = DockStyle.Right;
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        btnRefresh.Location = new Point(935, 18);
        btnRefresh.Margin = new Padding(10, 5, 4, 5);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Padding = new Padding(15, 8, 15, 8);
        btnRefresh.ShowPlusIcon = false;
        btnRefresh.Size = new Size(126, 44);
        btnRefresh.Style = RoundButtonStyle.Secondary;
        btnRefresh.TabIndex = 1;
        btnRefresh.Text = "⟳ Làm mới";
        btnRefresh.Click += btnRefresh_Click;
        // 
        // lblHeader
        // 
        lblHeader.AutoSize = true;
        lblHeader.Dock = DockStyle.Left;
        lblHeader.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
        lblHeader.ForeColor = Color.FromArgb(21, 28, 39);
        lblHeader.Location = new Point(25, 18);
        lblHeader.Margin = new Padding(4, 0, 4, 0);
        lblHeader.Name = "lblHeader";
        lblHeader.Size = new Size(421, 31);
        lblHeader.TabIndex = 0;
        lblHeader.Text = "Review Queue — Danh sách chờ duyệt";
        // 
        // listViewQueue
        // 
        listViewQueue.BorderStyle = BorderStyle.None;
        listViewQueue.Columns.AddRange(new ColumnHeader[] { columnCode, columnTitle, columnSubmittedBy, columnSubmittedAt, columnReviewNo });
        listViewQueue.Dock = DockStyle.Fill;
        listViewQueue.Font = new Font("Segoe UI", 9.5F);
        listViewQueue.FullRowSelect = true;
        listViewQueue.GridLines = true;
        listViewQueue.Location = new Point(0, 80);
        listViewQueue.Margin = new Padding(4, 5, 4, 5);
        listViewQueue.MultiSelect = false;
        listViewQueue.Name = "listViewQueue";
        listViewQueue.Size = new Size(1086, 654);
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
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(255, 255, 255);
        ClientSize = new Size(1086, 734);
        Controls.Add(listViewQueue);
        Controls.Add(panelTop);
        Margin = new Padding(4, 5, 4, 5);
        MinimumSize = new Size(896, 588);
        Name = "ReviewQueueForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Review Queue — CreatorFlow";
        panelTop.ResumeLayout(false);
        panelTop.PerformLayout();
        ResumeLayout(false);
    }
}