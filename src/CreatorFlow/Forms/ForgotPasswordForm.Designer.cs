using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class ForgotPasswordForm
{
    private TableLayoutPanel columns = null!, brandingStory = null!, formColumn = null!, bodyLayout = null!;
    private Panel brandingPanel = null!, formArea = null!;
    private Label brandName = null!, brandHeadline = null!, brandDescription = null!, brandFooter = null!;
    private Label formBrand = null!, formTitle = null!, formSubtitle = null!;
    private TextBox emailTextBox = null!;
    private TableLayoutPanel emailField = null!;
    private Panel emailFieldBorder = null!;
    private Label emailFieldCaption = null!, emailFieldError = null!;
    private Label noteLabel = null!;
    private Label messageLabel = null!;
    private Button sendButton = null!;
    private LinkLabel backLink = null!;

    private void InitializeComponent()
    {
        columns = new TableLayoutPanel();
        brandingPanel = new Panel();
        brandingStory = new TableLayoutPanel();
        brandName = new Label();
        brandHeadline = new Label();
        brandDescription = new Label();
        brandFooter = new Label();
        formArea = new Panel();
        formColumn = new TableLayoutPanel();
        formBrand = new Label();
        formTitle = new Label();
        formSubtitle = new Label();
        bodyLayout = new TableLayoutPanel();
        emailField = new TableLayoutPanel();
        emailFieldCaption = new Label();
        emailFieldBorder = new Panel();
        emailTextBox = new TextBox();
        emailFieldError = new Label();
        noteLabel = new Label();
        messageLabel = new Label();
        sendButton = new Button();
        backLink = new LinkLabel();
        columns.SuspendLayout();
        brandingPanel.SuspendLayout();
        brandingStory.SuspendLayout();
        formArea.SuspendLayout();
        formColumn.SuspendLayout();
        bodyLayout.SuspendLayout();
        emailField.SuspendLayout();
        emailFieldBorder.SuspendLayout();
        SuspendLayout();
        // 
        // columns
        // 
        columns.ColumnCount = 2;
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        columns.Controls.Add(brandingPanel, 0, 0);
        columns.Controls.Add(formArea, 1, 0);
        columns.Dock = DockStyle.Fill;
        columns.Location = new Point(0, 0);
        columns.Margin = new Padding(0);
        columns.Name = "columns";
        columns.RowCount = 1;
        columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        columns.Size = new Size(1120, 760);
        columns.TabIndex = 0;
        // 
        // brandingPanel
        // 
        brandingPanel.BackColor = Color.Black;
        brandingPanel.Controls.Add(brandingStory);
        brandingPanel.Dock = DockStyle.Fill;
        brandingPanel.Location = new Point(3, 3);
        brandingPanel.Name = "brandingPanel";
        brandingPanel.Padding = new Padding(48);
        brandingPanel.Size = new Size(464, 754);
        brandingPanel.TabIndex = 0;
        // 
        // brandingStory
        // 
        brandingStory.ColumnCount = 1;
        brandingStory.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        brandingStory.Controls.Add(brandName, 0, 0);
        brandingStory.Controls.Add(brandHeadline, 0, 1);
        brandingStory.Controls.Add(brandDescription, 0, 2);
        brandingStory.Controls.Add(brandFooter, 0, 3);
        brandingStory.Dock = DockStyle.Fill;
        brandingStory.Location = new Point(48, 48);
        brandingStory.Margin = new Padding(0);
        brandingStory.Name = "brandingStory";
        brandingStory.RowCount = 4;
        brandingStory.RowStyles.Add(new RowStyle());
        brandingStory.RowStyles.Add(new RowStyle());
        brandingStory.RowStyles.Add(new RowStyle());
        brandingStory.RowStyles.Add(new RowStyle());
        brandingStory.Size = new Size(368, 658);
        brandingStory.TabIndex = 0;
        // 
        // brandName
        // 
        brandName.AutoSize = true;
        brandName.Dock = DockStyle.Top;
        brandName.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        brandName.ForeColor = Color.White;
        brandName.Location = new Point(0, 0);
        brandName.Margin = new Padding(0, 0, 0, 48);
        brandName.Name = "brandName";
        brandName.Size = new Size(368, 50);
        brandName.TabIndex = 0;
        brandName.Text = "CreatorFlow";
        // 
        // brandHeadline
        // 
        brandHeadline.AutoSize = true;
        brandHeadline.Dock = DockStyle.Top;
        brandHeadline.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        brandHeadline.ForeColor = Color.White;
        brandHeadline.Location = new Point(0, 98);
        brandHeadline.Margin = new Padding(0, 0, 0, 24);
        brandHeadline.MaximumSize = new Size(320, 0);
        brandHeadline.Name = "brandHeadline";
        brandHeadline.Size = new Size(320, 150);
        brandHeadline.TabIndex = 1;
        brandHeadline.Text = "Từ ý tưởng\nđến nội dung hoàn chỉnh.";
        // 
        // brandDescription
        // 
        brandDescription.AutoSize = true;
        brandDescription.Dock = DockStyle.Top;
        brandDescription.ForeColor = Color.FromArgb(207, 196, 197);
        brandDescription.Location = new Point(0, 272);
        brandDescription.Margin = new Padding(0, 0, 0, 32);
        brandDescription.MaximumSize = new Size(320, 0);
        brandDescription.Name = "brandDescription";
        brandDescription.Size = new Size(320, 46);
        brandDescription.TabIndex = 2;
        brandDescription.Text = "Lên kế hoạch, phối hợp cùng nhóm và theo dõi hành trình sáng tạo ở một nơi.";
        // 
        // brandFooter
        // 
        brandFooter.AutoSize = true;
        brandFooter.Dock = DockStyle.Top;
        brandFooter.ForeColor = Color.FromArgb(163, 163, 163);
        brandFooter.Location = new Point(3, 350);
        brandFooter.Name = "brandFooter";
        brandFooter.Size = new Size(362, 23);
        brandFooter.TabIndex = 3;
        brandFooter.Text = "Ý tưởng  ·  Sản xuất  ·  Kiểm duyệt";
        // 
        // formArea
        // 
        formArea.AutoScroll = true;
        formArea.BackColor = Color.White;
        formArea.Controls.Add(formColumn);
        formArea.Dock = DockStyle.Fill;
        formArea.Location = new Point(473, 3);
        formArea.Name = "formArea";
        formArea.Padding = new Padding(48, 32, 48, 32);
        formArea.Size = new Size(644, 754);
        formArea.TabIndex = 1;
        // 
        // formColumn
        // 
        formColumn.AutoSize = true;
        formColumn.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        formColumn.ColumnCount = 1;
        formColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formColumn.Controls.Add(formBrand, 0, 0);
        formColumn.Controls.Add(formTitle, 0, 1);
        formColumn.Controls.Add(formSubtitle, 0, 2);
        formColumn.Controls.Add(bodyLayout, 0, 3);
        formColumn.Location = new Point(0, 0);
        formColumn.Margin = new Padding(0);
        formColumn.Name = "formColumn";
        formColumn.RowCount = 4;
        formColumn.RowStyles.Add(new RowStyle());
        formColumn.RowStyles.Add(new RowStyle());
        formColumn.RowStyles.Add(new RowStyle());
        formColumn.RowStyles.Add(new RowStyle());
        formColumn.Size = new Size(439, 473);
        formColumn.TabIndex = 0;
        // 
        // formBrand
        // 
        formBrand.AutoSize = true;
        formBrand.Dock = DockStyle.Top;
        formBrand.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        formBrand.ForeColor = Color.FromArgb(82, 82, 82);
        formBrand.Location = new Point(0, 0);
        formBrand.Margin = new Padding(0, 0, 0, 24);
        formBrand.Name = "formBrand";
        formBrand.Size = new Size(439, 19);
        formBrand.TabIndex = 0;
        formBrand.Text = "CREATORFLOW";
        // 
        // formTitle
        // 
        formTitle.AutoSize = true;
        formTitle.Dock = DockStyle.Top;
        formTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
        formTitle.ForeColor = Color.FromArgb(21, 28, 39);
        formTitle.Location = new Point(0, 43);
        formTitle.Margin = new Padding(0, 0, 0, 8);
        formTitle.Name = "formTitle";
        formTitle.Size = new Size(439, 50);
        formTitle.TabIndex = 1;
        formTitle.Text = "Quên mật khẩu?";
        // 
        // formSubtitle
        // 
        formSubtitle.AutoSize = true;
        formSubtitle.Dock = DockStyle.Top;
        formSubtitle.ForeColor = Color.FromArgb(82, 82, 82);
        formSubtitle.Location = new Point(0, 101);
        formSubtitle.Margin = new Padding(0, 0, 0, 32);
        formSubtitle.MaximumSize = new Size(440, 0);
        formSubtitle.Name = "formSubtitle";
        formSubtitle.Size = new Size(439, 46);
        formSubtitle.TabIndex = 2;
        formSubtitle.Text = "Nhập email tài khoản để yêu cầu mã xác nhận và đặt lại mật khẩu.";
        // 
        // bodyLayout
        // 
        bodyLayout.AutoSize = true;
        bodyLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bodyLayout.ColumnCount = 1;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bodyLayout.Controls.Add(emailField, 0, 0);
        bodyLayout.Controls.Add(noteLabel, 0, 1);
        bodyLayout.Controls.Add(messageLabel, 0, 2);
        bodyLayout.Controls.Add(sendButton, 0, 3);
        bodyLayout.Controls.Add(backLink, 0, 4);
        bodyLayout.Dock = DockStyle.Top;
        bodyLayout.Location = new Point(0, 179);
        bodyLayout.Margin = new Padding(0);
        bodyLayout.Name = "bodyLayout";
        bodyLayout.RowCount = 5;
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.Size = new Size(439, 294);
        bodyLayout.TabIndex = 3;
        // 
        // emailField
        // 
        emailField.AutoSize = true;
        emailField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        emailField.ColumnCount = 1;
        emailField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        emailField.Controls.Add(emailFieldCaption, 0, 0);
        emailField.Controls.Add(emailFieldBorder, 0, 1);
        emailField.Controls.Add(emailFieldError, 0, 2);
        emailField.Dock = DockStyle.Top;
        emailField.Location = new Point(0, 0);
        emailField.Margin = new Padding(0, 0, 0, 16);
        emailField.Name = "emailField";
        emailField.RowCount = 3;
        emailField.RowStyles.Add(new RowStyle());
        emailField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        emailField.RowStyles.Add(new RowStyle());
        emailField.Size = new Size(439, 86);
        emailField.TabIndex = 0;
        // 
        // emailFieldCaption
        // 
        emailFieldCaption.AutoSize = true;
        emailFieldCaption.Dock = DockStyle.Top;
        emailFieldCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        emailFieldCaption.ForeColor = Color.FromArgb(82, 82, 82);
        emailFieldCaption.Location = new Point(0, 0);
        emailFieldCaption.Margin = new Padding(0, 0, 0, 4);
        emailFieldCaption.Name = "emailFieldCaption";
        emailFieldCaption.Size = new Size(439, 19);
        emailFieldCaption.TabIndex = 0;
        emailFieldCaption.Text = "Email";
        // 
        // emailFieldBorder
        // 
        emailFieldBorder.BackColor = Color.White;
        emailFieldBorder.Controls.Add(emailTextBox);
        emailFieldBorder.Dock = DockStyle.Fill;
        emailFieldBorder.Location = new Point(0, 23);
        emailFieldBorder.Margin = new Padding(0, 0, 0, 4);
        emailFieldBorder.Name = "emailFieldBorder";
        emailFieldBorder.Padding = new Padding(12, 10, 12, 8);
        emailFieldBorder.Size = new Size(439, 40);
        emailFieldBorder.TabIndex = 1;
        // 
        // emailTextBox
        // 
        emailTextBox.AccessibleName = "Email";
        emailTextBox.BackColor = Color.White;
        emailTextBox.BorderStyle = BorderStyle.None;
        emailTextBox.Dock = DockStyle.Fill;
        emailTextBox.Font = new Font("Segoe UI", 9.75F);
        emailTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        emailTextBox.Location = new Point(12, 10);
        emailTextBox.MaxLength = 255;
        emailTextBox.Name = "emailTextBox";
        emailTextBox.Size = new Size(415, 22);
        emailTextBox.TabIndex = 0;
        // 
        // emailFieldError
        // 
        emailFieldError.AutoSize = true;
        emailFieldError.Dock = DockStyle.Top;
        emailFieldError.Font = new Font("Segoe UI", 8.25F);
        emailFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        emailFieldError.Location = new Point(3, 67);
        emailFieldError.Name = "emailFieldError";
        emailFieldError.Size = new Size(433, 19);
        emailFieldError.TabIndex = 2;
        emailFieldError.Visible = false;
        // 
        // noteLabel
        // 
        noteLabel.AutoSize = true;
        noteLabel.Dock = DockStyle.Top;
        noteLabel.ForeColor = Color.FromArgb(82, 82, 82);
        noteLabel.Location = new Point(0, 102);
        noteLabel.Margin = new Padding(0, 0, 0, 16);
        noteLabel.MaximumSize = new Size(440, 0);
        noteLabel.Name = "noteLabel";
        noteLabel.Size = new Size(439, 46);
        noteLabel.TabIndex = 1;
        noteLabel.Text = "Mã gồm 6 chữ số, có hiệu lực trong 10 phút. Không chia sẻ mã với người khác.";
        // 
        // messageLabel
        // 
        messageLabel.AutoSize = true;
        messageLabel.Dock = DockStyle.Top;
        messageLabel.ForeColor = Color.FromArgb(220, 38, 38);
        messageLabel.Location = new Point(0, 164);
        messageLabel.Margin = new Padding(0, 0, 0, 16);
        messageLabel.MaximumSize = new Size(440, 0);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(439, 23);
        messageLabel.TabIndex = 2;
        // 
        // sendButton
        // 
        sendButton.Cursor = Cursors.Hand;
        sendButton.Dock = DockStyle.Top;
        sendButton.Location = new Point(0, 203);
        sendButton.Margin = new Padding(0, 0, 0, 24);
        sendButton.Name = "sendButton";
        sendButton.Size = new Size(439, 44);
        sendButton.TabIndex = 3;
        sendButton.Text = "Gửi mã xác nhận";
        // 
        // backLink
        // 
        backLink.ActiveLinkColor = Color.Black;
        backLink.AutoSize = true;
        backLink.Dock = DockStyle.Top;
        backLink.Font = new Font("Segoe UI", 9.75F);
        backLink.LinkBehavior = LinkBehavior.HoverUnderline;
        backLink.LinkColor = Color.FromArgb(21, 28, 39);
        backLink.Location = new Point(0, 271);
        backLink.Margin = new Padding(0);
        backLink.Name = "backLink";
        backLink.Size = new Size(439, 23);
        backLink.TabIndex = 4;
        backLink.TabStop = true;
        backLink.Text = "← Quay về đăng nhập";
        backLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // ForgotPasswordForm
        // 
        AcceptButton = sendButton;
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 760);
        Controls.Add(columns);
        Font = new Font("Segoe UI", 9.75F);
        MinimumSize = new Size(540, 620);
        Name = "ForgotPasswordForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Quên mật khẩu?";
        columns.ResumeLayout(false);
        brandingPanel.ResumeLayout(false);
        brandingStory.ResumeLayout(false);
        brandingStory.PerformLayout();
        formArea.ResumeLayout(false);
        formArea.PerformLayout();
        formColumn.ResumeLayout(false);
        formColumn.PerformLayout();
        bodyLayout.ResumeLayout(false);
        bodyLayout.PerformLayout();
        emailField.ResumeLayout(false);
        emailField.PerformLayout();
        emailFieldBorder.ResumeLayout(false);
        emailFieldBorder.PerformLayout();
        ResumeLayout(false);
    }
}
