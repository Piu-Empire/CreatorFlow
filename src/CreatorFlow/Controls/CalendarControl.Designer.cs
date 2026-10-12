namespace CreatorFlow.Controls
{
    partial class CalendarControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// Khung do dev viết tay đúng chuẩn VS sinh: mở Designer vẫn thấy và sửa được.
        /// Font/màu chữ do CalendarControl.cs ép UITheme lúc chạy.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlHeaderCard = new RoundedPanel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.badgeKind = new BadgeLabel();
            this.lblDesc = new System.Windows.Forms.Label();
            this.flpToolbar = new System.Windows.Forms.FlowLayoutPanel();
            this.segProject = new SegmentedControl();
            this.pnlViewSwitch = new System.Windows.Forms.Panel();
            this.btnToday = new RoundedButton();
            this.segPlatform = new SegmentedControl();
            this.cboStatus = new System.Windows.Forms.ComboBox();
            this.pnlGridHost = new System.Windows.Forms.Panel();
            this.pnlState = new System.Windows.Forms.Panel();
            this.lblStateTitle = new System.Windows.Forms.Label();
            this.lblStateSub = new System.Windows.Forms.Label();
            this.btnState = new RoundedButton();
            this.pnlPager = new System.Windows.Forms.Panel();
            this.lblSummary = new System.Windows.Forms.Label();
            this.flpPager = new System.Windows.Forms.FlowLayoutPanel();
            this.btnPrev = new RoundedButton();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnNext = new RoundedButton();
            this.pnlHeaderCard.SuspendLayout();
            this.flpToolbar.SuspendLayout();
            this.pnlState.SuspendLayout();
            this.pnlPager.SuspendLayout();
            this.flpPager.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlHeaderCard
            //
            this.pnlHeaderCard.BackColor = System.Drawing.Color.White;
            this.pnlHeaderCard.BorderColor = System.Drawing.Color.FromArgb(229, 229, 229);
            this.pnlHeaderCard.Controls.Add(this.lblTitle);
            this.pnlHeaderCard.Controls.Add(this.badgeKind);
            this.pnlHeaderCard.Controls.Add(this.lblDesc);
            this.pnlHeaderCard.Controls.Add(this.flpToolbar);
            this.pnlHeaderCard.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeaderCard.FillColor = System.Drawing.Color.White;
            this.pnlHeaderCard.Location = new System.Drawing.Point(24, 20);
            this.pnlHeaderCard.Name = "pnlHeaderCard";
            this.pnlHeaderCard.Radius = 12;
            this.pnlHeaderCard.Size = new System.Drawing.Size(1552, 168);
            this.pnlHeaderCard.TabIndex = 0;
            //
            // lblTitle
            //
            this.lblTitle.AutoSize = true;
            this.lblTitle.Location = new System.Drawing.Point(20, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(120, 20);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Lịch Content";
            //
            // badgeKind
            //
            this.badgeKind.Location = new System.Drawing.Point(250, 22);
            this.badgeKind.Name = "badgeKind";
            this.badgeKind.Size = new System.Drawing.Size(90, 20);
            this.badgeKind.TabIndex = 1;
            this.badgeKind.Text = "Lịch";
            //
            // lblDesc
            //
            this.lblDesc.AutoSize = true;
            this.lblDesc.Location = new System.Drawing.Point(20, 52);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(350, 20);
            this.lblDesc.TabIndex = 2;
            this.lblDesc.Text = "Xem content theo hạn sản xuất và lịch đăng.";
            //
            // flpToolbar
            //
            this.flpToolbar.AutoSize = true;
            this.flpToolbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpToolbar.Controls.Add(this.segProject);
            this.flpToolbar.Controls.Add(this.pnlViewSwitch);
            this.flpToolbar.Controls.Add(this.btnToday);
            this.flpToolbar.Controls.Add(this.segPlatform);
            this.flpToolbar.Controls.Add(this.cboStatus);
            this.flpToolbar.Location = new System.Drawing.Point(20, 84);
            this.flpToolbar.Name = "flpToolbar";
            this.flpToolbar.Size = new System.Drawing.Size(700, 42);
            this.flpToolbar.TabIndex = 3;
            this.flpToolbar.WrapContents = false;
            //
            // pnlViewSwitch
            //
            this.pnlViewSwitch.Location = new System.Drawing.Point(3, 3);
            this.pnlViewSwitch.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.pnlViewSwitch.Name = "pnlViewSwitch";
            this.pnlViewSwitch.Size = new System.Drawing.Size(220, 36);
            this.pnlViewSwitch.TabIndex = 0;
            //
            // segProject (TẠM-LOCAL: nút project mẫu để test, xóa khi push)
            //
            this.segProject.Location = new System.Drawing.Point(3, 3);
            this.segProject.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.segProject.Name = "segProject";
            this.segProject.Size = new System.Drawing.Size(220, 36);
            this.segProject.TabIndex = 4;
            //
            // btnToday
            //
            this.btnToday.Location = new System.Drawing.Point(235, 3);
            this.btnToday.Margin = new System.Windows.Forms.Padding(0, 0, 12, 0);
            this.btnToday.Name = "btnToday";
            this.btnToday.Size = new System.Drawing.Size(110, 36);
            this.btnToday.Style = RoundButtonStyle.Secondary;
            this.btnToday.TabIndex = 1;
            this.btnToday.Text = "‹ Hôm nay ›";
            //
            // segPlatform
            //
            this.segPlatform.Location = new System.Drawing.Point(357, 3);
            this.segPlatform.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            this.segPlatform.Name = "segPlatform";
            this.segPlatform.Size = new System.Drawing.Size(320, 36);
            this.segPlatform.TabIndex = 2;
            //
            // cboStatus
            //
            this.cboStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboStatus.Location = new System.Drawing.Point(689, 3);
            this.cboStatus.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
            this.cboStatus.Name = "cboStatus";
            this.cboStatus.Size = new System.Drawing.Size(170, 28);
            this.cboStatus.TabIndex = 3;
            //
            // pnlGridHost
            //
            this.pnlGridHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlGridHost.Location = new System.Drawing.Point(24, 20);
            this.pnlGridHost.Name = "pnlGridHost";
            this.pnlGridHost.Size = new System.Drawing.Size(1552, 600);
            this.pnlGridHost.TabIndex = 3;
            //
            // pnlState
            //
            this.pnlState.BackColor = System.Drawing.Color.White;
            this.pnlState.Controls.Add(this.lblStateTitle);
            this.pnlState.Controls.Add(this.lblStateSub);
            this.pnlState.Controls.Add(this.btnState);
            this.pnlState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlState.Location = new System.Drawing.Point(24, 20);
            this.pnlState.Name = "pnlState";
            this.pnlState.Size = new System.Drawing.Size(1552, 600);
            this.pnlState.TabIndex = 4;
            this.pnlState.Visible = false;
            //
            // lblStateTitle
            //
            this.lblStateTitle.AutoSize = true;
            this.lblStateTitle.Location = new System.Drawing.Point(700, 250);
            this.lblStateTitle.Name = "lblStateTitle";
            this.lblStateTitle.Size = new System.Drawing.Size(150, 20);
            this.lblStateTitle.TabIndex = 0;
            this.lblStateTitle.Text = "Trạng thái";
            this.lblStateSub.AutoSize = true;
            this.lblStateSub.Location = new System.Drawing.Point(700, 280);
            this.lblStateSub.Name = "lblStateSub";
            this.lblStateSub.Size = new System.Drawing.Size(150, 20);
            this.lblStateSub.TabIndex = 1;
            this.lblStateSub.Text = "Chi tiết";
            //
            // btnState
            //
            this.btnState.Location = new System.Drawing.Point(711, 310);
            this.btnState.Name = "btnState";
            this.btnState.Size = new System.Drawing.Size(130, 36);
            this.btnState.Style = RoundButtonStyle.Secondary;
            this.btnState.TabIndex = 2;
            this.btnState.Text = "Thử lại";
            //
            // pnlPager
            //
            this.pnlPager.Controls.Add(this.lblSummary);
            this.pnlPager.Controls.Add(this.flpPager);
            this.pnlPager.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlPager.Location = new System.Drawing.Point(24, 620);
            this.pnlPager.Name = "pnlPager";
            this.pnlPager.Size = new System.Drawing.Size(1552, 48);
            this.pnlPager.TabIndex = 2;
            //
            // lblSummary
            //
            this.lblSummary.AutoSize = true;
            this.lblSummary.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblSummary.Location = new System.Drawing.Point(0, 0);
            this.lblSummary.Name = "lblSummary";
            this.lblSummary.Size = new System.Drawing.Size(120, 20);
            this.lblSummary.TabIndex = 0;
            this.lblSummary.Text = "Đang hiện…";
            this.lblSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // flpPager
            //
            this.flpPager.AutoSize = true;
            this.flpPager.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpPager.Controls.Add(this.btnPrev);
            this.flpPager.Controls.Add(this.lblPage);
            this.flpPager.Controls.Add(this.btnNext);
            this.flpPager.Dock = System.Windows.Forms.DockStyle.Right;
            this.flpPager.Location = new System.Drawing.Point(1300, 0);
            this.flpPager.Name = "flpPager";
            this.flpPager.Size = new System.Drawing.Size(252, 44);
            this.flpPager.TabIndex = 1;
            this.flpPager.WrapContents = false;
            //
            // btnPrev
            //
            this.btnPrev.Location = new System.Drawing.Point(3, 6);
            this.btnPrev.Margin = new System.Windows.Forms.Padding(0, 6, 8, 0);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Size = new System.Drawing.Size(90, 32);
            this.btnPrev.Style = RoundButtonStyle.Secondary;
            this.btnPrev.TabIndex = 0;
            this.btnPrev.Text = "Trước";
            //
            // lblPage
            //
            this.lblPage.AutoSize = true;
            this.lblPage.Location = new System.Drawing.Point(101, 6);
            this.lblPage.Margin = new System.Windows.Forms.Padding(0, 12, 8, 0);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(60, 20);
            this.lblPage.TabIndex = 1;
            this.lblPage.Text = "Trang";
            //
            // btnNext
            //
            this.btnNext.Location = new System.Drawing.Point(169, 6);
            this.btnNext.Margin = new System.Windows.Forms.Padding(0, 6, 0, 0);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(90, 32);
            this.btnNext.Style = RoundButtonStyle.Secondary;
            this.btnNext.TabIndex = 2;
            this.btnNext.Text = "Sau";
            //
            // CalendarControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.Color.FromArgb(250, 250, 250);
            this.Controls.Add(this.pnlGridHost);
            this.Controls.Add(this.pnlState);
            this.Controls.Add(this.pnlPager);
            this.Controls.Add(this.pnlHeaderCard);
            this.Name = "CalendarControl";
            this.Padding = new System.Windows.Forms.Padding(24, 20, 24, 20);
            this.Size = new System.Drawing.Size(1600, 900);
            this.pnlHeaderCard.ResumeLayout(false);
            this.pnlHeaderCard.PerformLayout();
            this.flpToolbar.ResumeLayout(false);
            this.flpToolbar.PerformLayout();
            this.pnlState.ResumeLayout(false);
            this.pnlState.PerformLayout();
            this.pnlPager.ResumeLayout(false);
            this.pnlPager.PerformLayout();
            this.flpPager.ResumeLayout(false);
            this.flpPager.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion

        private RoundedPanel pnlHeaderCard;
        private System.Windows.Forms.Label lblTitle;
        private BadgeLabel badgeKind;
        private System.Windows.Forms.Label lblDesc;
        private System.Windows.Forms.FlowLayoutPanel flpToolbar;
        private SegmentedControl segProject;
        private System.Windows.Forms.Panel pnlViewSwitch;
        private RoundedButton btnToday;
        private SegmentedControl segPlatform;
        private System.Windows.Forms.ComboBox cboStatus;
        private System.Windows.Forms.Panel pnlGridHost;
        private System.Windows.Forms.Panel pnlState;
        private System.Windows.Forms.Label lblStateTitle;
        private System.Windows.Forms.Label lblStateSub;
        private RoundedButton btnState;
        private System.Windows.Forms.Panel pnlPager;
        private System.Windows.Forms.Label lblSummary;
        private System.Windows.Forms.FlowLayoutPanel flpPager;
        private RoundedButton btnPrev;
        private System.Windows.Forms.Label lblPage;
        private RoundedButton btnNext;
    }
}
