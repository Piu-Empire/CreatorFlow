using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.IdeaBank;

partial class IdeaBankForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblPageTitle;
    private System.Windows.Forms.Label lblSubtitle;
    private CreatorFlow.Controls.RoundedButton btnNew;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.ComboBox cboStatus;
    private System.Windows.Forms.ComboBox cboTag;
    private CreatorFlow.Controls.RoundedButton btnClearFilter;
    private CreatorFlow.Controls.BufferedListView listViewIdeas;
    private System.Windows.Forms.ColumnHeader colCode;
    private System.Windows.Forms.ColumnHeader colTitle;
    private System.Windows.Forms.ColumnHeader colStatus;
    private System.Windows.Forms.ColumnHeader colTags;
    private System.Windows.Forms.ColumnHeader colCreator;
    private System.Windows.Forms.ColumnHeader colUpdated;
    private System.Windows.Forms.TextBox txtDetail;
    private System.Windows.Forms.Label lblCount;
    private CreatorFlow.Controls.RoundedButton btnEdit;
    private CreatorFlow.Controls.RoundedButton btnDelete;
    private CreatorFlow.Controls.RoundedButton btnClose;

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
        this.lblPageTitle = new System.Windows.Forms.Label();
        this.lblSubtitle = new System.Windows.Forms.Label();
        this.btnNew = new CreatorFlow.Controls.RoundedButton();
        this.txtSearch = new System.Windows.Forms.TextBox();
        this.cboStatus = new System.Windows.Forms.ComboBox();
        this.cboTag = new System.Windows.Forms.ComboBox();
        this.btnClearFilter = new CreatorFlow.Controls.RoundedButton();
        this.listViewIdeas = new CreatorFlow.Controls.BufferedListView();
        this.colCode = new System.Windows.Forms.ColumnHeader();
        this.colTitle = new System.Windows.Forms.ColumnHeader();
        this.colStatus = new System.Windows.Forms.ColumnHeader();
        this.colTags = new System.Windows.Forms.ColumnHeader();
        this.colCreator = new System.Windows.Forms.ColumnHeader();
        this.colUpdated = new System.Windows.Forms.ColumnHeader();
        this.txtDetail = new System.Windows.Forms.TextBox();
        this.lblCount = new System.Windows.Forms.Label();
        this.btnEdit = new CreatorFlow.Controls.RoundedButton();
        this.btnDelete = new CreatorFlow.Controls.RoundedButton();
        this.btnClose = new CreatorFlow.Controls.RoundedButton();
        this.SuspendLayout();
        //
        // lblPageTitle
        //
        this.lblPageTitle.AutoSize = true;
        this.lblPageTitle.Font = UITheme.FontPageTitle;
        this.lblPageTitle.ForeColor = UITheme.Ink;
        this.lblPageTitle.Location = new System.Drawing.Point(24, 14);
        this.lblPageTitle.Name = "lblPageTitle";
        this.lblPageTitle.Text = "Idea Bank";
        //
        // lblSubtitle
        //
        this.lblSubtitle.Font = UITheme.FontBody;
        this.lblSubtitle.ForeColor = UITheme.Neutral600;
        this.lblSubtitle.Location = new System.Drawing.Point(26, 56);
        this.lblSubtitle.Name = "lblSubtitle";
        this.lblSubtitle.Size = new System.Drawing.Size(640, 22);
        this.lblSubtitle.Text = "Kho ý tưởng của Project: thêm, sửa, gắn tag, ghi chú, tìm kiếm và lọc theo trạng thái.";
        //
        // btnNew
        //
        this.btnNew.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
        this.btnNew.Location = new System.Drawing.Point(806, 20);
        this.btnNew.Name = "btnNew";
        this.btnNew.ShowPlusIcon = true;
        this.btnNew.Size = new System.Drawing.Size(170, 40);
        this.btnNew.Style = RoundButtonStyle.Primary;
        this.btnNew.TabIndex = 0;
        this.btnNew.Text = "Thêm ý tưởng";
        this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
        //
        // txtSearch
        //
        this.txtSearch.Font = UITheme.FontBody;
        this.txtSearch.Location = new System.Drawing.Point(24, 90);
        this.txtSearch.Name = "txtSearch";
        this.txtSearch.PlaceholderText = "Tìm theo tiêu đề, mô tả, ghi chú, tag...";
        this.txtSearch.Size = new System.Drawing.Size(340, 29);
        this.txtSearch.TabIndex = 1;
        //
        // cboStatus
        //
        this.cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboStatus.Font = UITheme.FontBody;
        this.cboStatus.Location = new System.Drawing.Point(376, 90);
        this.cboStatus.Name = "cboStatus";
        this.cboStatus.Size = new System.Drawing.Size(180, 29);
        this.cboStatus.TabIndex = 2;
        //
        // cboTag
        //
        this.cboTag.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cboTag.Font = UITheme.FontBody;
        this.cboTag.Location = new System.Drawing.Point(568, 90);
        this.cboTag.Name = "cboTag";
        this.cboTag.Size = new System.Drawing.Size(180, 29);
        this.cboTag.TabIndex = 3;
        //
        // btnClearFilter
        //
        this.btnClearFilter.Location = new System.Drawing.Point(760, 86);
        this.btnClearFilter.Name = "btnClearFilter";
        this.btnClearFilter.Size = new System.Drawing.Size(110, 36);
        this.btnClearFilter.Style = RoundButtonStyle.Secondary;
        this.btnClearFilter.TabIndex = 4;
        this.btnClearFilter.Text = "Xóa lọc";
        this.btnClearFilter.Click += new System.EventHandler(this.btnClearFilter_Click);
        //
        // listViewIdeas
        //
        this.listViewIdeas.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
        this.listViewIdeas.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.listViewIdeas.Columns.AddRange(new System.Windows.Forms.ColumnHeader[]
        {
            this.colCode, this.colTitle, this.colStatus, this.colTags, this.colCreator, this.colUpdated,
        });
        this.listViewIdeas.Font = UITheme.FontBody;
        this.listViewIdeas.FullRowSelect = true;
        this.listViewIdeas.HideSelection = false;
        this.listViewIdeas.Location = new System.Drawing.Point(24, 132);
        this.listViewIdeas.MultiSelect = false;
        this.listViewIdeas.Name = "listViewIdeas";
        this.listViewIdeas.Size = new System.Drawing.Size(952, 318);
        this.listViewIdeas.TabIndex = 5;
        this.listViewIdeas.UseCompatibleStateImageBehavior = false;
        this.listViewIdeas.View = System.Windows.Forms.View.Details;
        this.listViewIdeas.SelectedIndexChanged += new System.EventHandler(this.listViewIdeas_SelectedIndexChanged);
        this.listViewIdeas.DoubleClick += new System.EventHandler(this.listViewIdeas_DoubleClick);
        //
        // columns
        //
        this.colCode.Text = "Mã";
        this.colCode.Width = 80;
        this.colTitle.Text = "Tiêu đề";
        this.colTitle.Width = 300;
        this.colStatus.Text = "Trạng thái";
        this.colStatus.Width = 130;
        this.colTags.Text = "Tag";
        this.colTags.Width = 200;
        this.colCreator.Text = "Người tạo";
        this.colCreator.Width = 120;
        this.colUpdated.Text = "Cập nhật";
        this.colUpdated.Width = 110;
        //
        // txtDetail
        //
        this.txtDetail.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left
            | System.Windows.Forms.AnchorStyles.Right;
        this.txtDetail.BackColor = UITheme.White;
        this.txtDetail.Font = UITheme.FontBody;
        this.txtDetail.Location = new System.Drawing.Point(24, 460);
        this.txtDetail.Multiline = true;
        this.txtDetail.Name = "txtDetail";
        this.txtDetail.ReadOnly = true;
        this.txtDetail.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtDetail.Size = new System.Drawing.Size(952, 92);
        this.txtDetail.TabIndex = 6;
        //
        // lblCount
        //
        this.lblCount.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
        this.lblCount.Font = UITheme.FontBody;
        this.lblCount.ForeColor = UITheme.Neutral600;
        this.lblCount.Location = new System.Drawing.Point(24, 572);
        this.lblCount.Name = "lblCount";
        this.lblCount.Size = new System.Drawing.Size(360, 36);
        this.lblCount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // btnEdit
        //
        this.btnEdit.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.btnEdit.Location = new System.Drawing.Point(620, 570);
        this.btnEdit.Name = "btnEdit";
        this.btnEdit.Size = new System.Drawing.Size(110, 40);
        this.btnEdit.Style = RoundButtonStyle.Secondary;
        this.btnEdit.TabIndex = 7;
        this.btnEdit.Text = "Sửa";
        this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
        //
        // btnDelete
        //
        this.btnDelete.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.btnDelete.Location = new System.Drawing.Point(740, 570);
        this.btnDelete.Name = "btnDelete";
        this.btnDelete.Size = new System.Drawing.Size(110, 40);
        this.btnDelete.Style = RoundButtonStyle.Danger;
        this.btnDelete.TabIndex = 8;
        this.btnDelete.Text = "Xóa";
        this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
        //
        // btnClose
        //
        this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
        this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnClose.Location = new System.Drawing.Point(866, 570);
        this.btnClose.Name = "btnClose";
        this.btnClose.Size = new System.Drawing.Size(110, 40);
        this.btnClose.Style = RoundButtonStyle.Secondary;
        this.btnClose.TabIndex = 9;
        this.btnClose.Text = "Đóng";
        //
        // IdeaBankForm
        //
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
        this.BackColor = UITheme.Neutral50;
        this.CancelButton = this.btnClose;
        this.ClientSize = new System.Drawing.Size(1000, 630);
        this.Controls.Add(this.lblPageTitle);
        this.Controls.Add(this.lblSubtitle);
        this.Controls.Add(this.btnNew);
        this.Controls.Add(this.txtSearch);
        this.Controls.Add(this.cboStatus);
        this.Controls.Add(this.cboTag);
        this.Controls.Add(this.btnClearFilter);
        this.Controls.Add(this.listViewIdeas);
        this.Controls.Add(this.txtDetail);
        this.Controls.Add(this.lblCount);
        this.Controls.Add(this.btnEdit);
        this.Controls.Add(this.btnDelete);
        this.Controls.Add(this.btnClose);
        this.Font = UITheme.FontBody;
        this.MinimumSize = new System.Drawing.Size(900, 560);
        this.Name = "IdeaBankForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "CreatorFlow — Idea Bank";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
