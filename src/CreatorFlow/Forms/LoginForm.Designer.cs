using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class LoginForm
{
    private TableLayoutPanel columns = null!, brandingStory = null!, formColumn = null!, bodyLayout = null!,
        passwordActions = null!;
    private Panel brandingPanel = null!, formArea = null!, emailBorder = null!, passwordBorder = null!;
    private Label emailCaption = null!, passwordCaption = null!, emailError = null!, passwordError = null!;
    private Label brandName = null!, brandHeadline = null!, brandDescription = null!, brandFooter = null!;
    private Label formBrand = null!, formTitle = null!, formSubtitle = null!;
    private TextBox emailTextBox = null!, passwordTextBox = null!;
    private Button loginButton = null!;
    private Label messageLabel = null!;
    private CheckBox showPassword = null!;
    private LinkLabel registerLink = null!;
    private LinkLabel forgotLink = null!;
    private TableLayoutPanel emailField = null!, passwordField = null!;

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
        emailCaption = new Label();
        emailBorder = new Panel();
        emailTextBox = new TextBox();
        emailError = new Label();
        passwordField = new TableLayoutPanel();
        passwordCaption = new Label();
        passwordBorder = new Panel();
        passwordTextBox = new TextBox();
        passwordError = new Label();
        passwordActions = new TableLayoutPanel();
        showPassword = new CheckBox();
        forgotLink = new LinkLabel();
        messageLabel = new Label();
        loginButton = new Button();
        registerLink = new LinkLabel();
        columns.SuspendLayout();
        brandingPanel.SuspendLayout();
        brandingStory.SuspendLayout();
        formArea.SuspendLayout();
        formColumn.SuspendLayout();
        bodyLayout.SuspendLayout();
        emailField.SuspendLayout();
        emailBorder.SuspendLayout();
        passwordField.SuspendLayout();
        passwordBorder.SuspendLayout();
        passwordActions.SuspendLayout();
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
        formColumn.Size = new Size(338, 533);
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
        formBrand.Size = new Size(338, 19);
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
        formTitle.Size = new Size(338, 50);
        formTitle.TabIndex = 1;
        formTitle.Text = "Chào mừng trở lại";
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
        formSubtitle.Size = new Size(338, 23);
        formSubtitle.TabIndex = 2;
        formSubtitle.Text = "Đăng nhập để tiếp tục công việc của bạn.";
        // 
        // bodyLayout
        // 
        bodyLayout.AutoSize = true;
        bodyLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bodyLayout.ColumnCount = 1;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bodyLayout.Controls.Add(emailField, 0, 0);
        bodyLayout.Controls.Add(passwordField, 0, 1);
        bodyLayout.Controls.Add(passwordActions, 0, 2);
        bodyLayout.Controls.Add(messageLabel, 0, 3);
        bodyLayout.Controls.Add(loginButton, 0, 4);
        bodyLayout.Controls.Add(registerLink, 0, 5);
        bodyLayout.Dock = DockStyle.Top;
        bodyLayout.Location = new Point(0, 156);
        bodyLayout.Margin = new Padding(0);
        bodyLayout.Name = "bodyLayout";
        bodyLayout.RowCount = 6;
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.Size = new Size(338, 377);
        bodyLayout.TabIndex = 3;
        // 
        // emailField
        // 
        emailField.AutoSize = true;
        emailField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        emailField.ColumnCount = 1;
        emailField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        emailField.Controls.Add(emailCaption, 0, 0);
        emailField.Controls.Add(emailBorder, 0, 1);
        emailField.Controls.Add(emailError, 0, 2);
        emailField.Dock = DockStyle.Top;
        emailField.Location = new Point(0, 0);
        emailField.Margin = new Padding(0, 0, 0, 16);
        emailField.Name = "emailField";
        emailField.RowCount = 3;
        emailField.RowStyles.Add(new RowStyle());
        emailField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        emailField.RowStyles.Add(new RowStyle());
        emailField.Size = new Size(338, 86);
        emailField.TabIndex = 0;
        // 
        // emailCaption
        // 
        emailCaption.AutoSize = true;
        emailCaption.Dock = DockStyle.Top;
        emailCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        emailCaption.ForeColor = Color.FromArgb(82, 82, 82);
        emailCaption.Location = new Point(0, 0);
        emailCaption.Margin = new Padding(0, 0, 0, 4);
        emailCaption.Name = "emailCaption";
        emailCaption.Size = new Size(338, 19);
        emailCaption.TabIndex = 0;
        emailCaption.Text = "Email";
        // 
        // emailBorder
        // 
        emailBorder.BackColor = Color.White;
        emailBorder.Controls.Add(emailTextBox);
        emailBorder.Dock = DockStyle.Fill;
        emailBorder.Location = new Point(0, 23);
        emailBorder.Margin = new Padding(0, 0, 0, 4);
        emailBorder.Name = "emailBorder";
        emailBorder.Padding = new Padding(12, 10, 12, 8);
        emailBorder.Size = new Size(338, 40);
        emailBorder.TabIndex = 1;
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
        emailTextBox.Size = new Size(314, 22);
        emailTextBox.TabIndex = 0;
        // 
        // emailError
        // 
        emailError.AutoSize = true;
        emailError.Dock = DockStyle.Top;
        emailError.Font = new Font("Segoe UI", 8.25F);
        emailError.ForeColor = Color.FromArgb(220, 38, 38);
        emailError.Location = new Point(3, 67);
        emailError.Name = "emailError";
        emailError.Size = new Size(332, 19);
        emailError.TabIndex = 2;
        emailError.Visible = false;
        // 
        // passwordField
        // 
        passwordField.AutoSize = true;
        passwordField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        passwordField.ColumnCount = 1;
        passwordField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        passwordField.Controls.Add(passwordCaption, 0, 0);
        passwordField.Controls.Add(passwordBorder, 0, 1);
        passwordField.Controls.Add(passwordError, 0, 2);
        passwordField.Dock = DockStyle.Top;
        passwordField.Location = new Point(0, 102);
        passwordField.Margin = new Padding(0, 0, 0, 16);
        passwordField.Name = "passwordField";
        passwordField.RowCount = 3;
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.Size = new Size(338, 86);
        passwordField.TabIndex = 1;
        // 
        // passwordCaption
        // 
        passwordCaption.AutoSize = true;
        passwordCaption.Dock = DockStyle.Top;
        passwordCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        passwordCaption.ForeColor = Color.FromArgb(82, 82, 82);
        passwordCaption.Location = new Point(0, 0);
        passwordCaption.Margin = new Padding(0, 0, 0, 4);
        passwordCaption.Name = "passwordCaption";
        passwordCaption.Size = new Size(338, 19);
        passwordCaption.TabIndex = 0;
        passwordCaption.Text = "Mật khẩu";
        // 
        // passwordBorder
        // 
        passwordBorder.BackColor = Color.White;
        passwordBorder.Controls.Add(passwordTextBox);
        passwordBorder.Dock = DockStyle.Fill;
        passwordBorder.Location = new Point(0, 23);
        passwordBorder.Margin = new Padding(0, 0, 0, 4);
        passwordBorder.Name = "passwordBorder";
        passwordBorder.Padding = new Padding(12, 10, 12, 8);
        passwordBorder.Size = new Size(338, 40);
        passwordBorder.TabIndex = 1;
        // 
        // passwordTextBox
        // 
        passwordTextBox.AccessibleName = "Mật khẩu";
        passwordTextBox.BackColor = Color.White;
        passwordTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.Dock = DockStyle.Fill;
        passwordTextBox.Font = new Font("Segoe UI", 9.75F);
        passwordTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        passwordTextBox.Location = new Point(12, 10);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(314, 22);
        passwordTextBox.TabIndex = 0;
        passwordTextBox.UseSystemPasswordChar = true;
        // 
        // passwordError
        // 
        passwordError.AutoSize = true;
        passwordError.Dock = DockStyle.Top;
        passwordError.Font = new Font("Segoe UI", 8.25F);
        passwordError.ForeColor = Color.FromArgb(220, 38, 38);
        passwordError.Location = new Point(3, 67);
        passwordError.Name = "passwordError";
        passwordError.Size = new Size(332, 19);
        passwordError.TabIndex = 2;
        passwordError.Visible = false;
        // 
        // passwordActions
        // 
        passwordActions.AutoSize = true;
        passwordActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        passwordActions.ColumnCount = 2;
        passwordActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        passwordActions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        passwordActions.Controls.Add(showPassword, 0, 0);
        passwordActions.Controls.Add(forgotLink, 1, 0);
        passwordActions.Dock = DockStyle.Top;
        passwordActions.Location = new Point(0, 204);
        passwordActions.Margin = new Padding(0, 0, 0, 16);
        passwordActions.Name = "passwordActions";
        passwordActions.RowCount = 1;
        passwordActions.RowStyles.Add(new RowStyle());
        passwordActions.Size = new Size(338, 27);
        passwordActions.TabIndex = 2;
        // 
        // showPassword
        // 
        showPassword.AutoSize = true;
        showPassword.Location = new Point(0, 0);
        showPassword.Margin = new Padding(0);
        showPassword.Name = "showPassword";
        showPassword.Size = new Size(144, 27);
        showPassword.TabIndex = 0;
        showPassword.Text = "Hiện mật khẩu";
        // 
        // forgotLink
        // 
        forgotLink.ActiveLinkColor = Color.Black;
        forgotLink.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        forgotLink.AutoSize = true;
        forgotLink.Enabled = false;
        forgotLink.Font = new Font("Segoe UI", 9.75F);
        forgotLink.LinkBehavior = LinkBehavior.HoverUnderline;
        forgotLink.LinkColor = Color.FromArgb(21, 28, 39);
        forgotLink.Location = new Point(198, 0);
        forgotLink.Name = "forgotLink";
        forgotLink.Size = new Size(137, 23);
        forgotLink.TabIndex = 1;
        forgotLink.TabStop = true;
        forgotLink.Text = "Quên mật khẩu?";
        forgotLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // messageLabel
        // 
        messageLabel.AutoSize = true;
        messageLabel.Dock = DockStyle.Top;
        messageLabel.ForeColor = Color.FromArgb(220, 38, 38);
        messageLabel.Location = new Point(0, 247);
        messageLabel.Margin = new Padding(0, 0, 0, 16);
        messageLabel.MaximumSize = new Size(440, 0);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(338, 23);
        messageLabel.TabIndex = 3;
        // 
        // loginButton
        // 
        loginButton.Cursor = Cursors.Hand;
        loginButton.Dock = DockStyle.Top;
        loginButton.Location = new Point(0, 286);
        loginButton.Margin = new Padding(0, 0, 0, 24);
        loginButton.Name = "loginButton";
        loginButton.Size = new Size(338, 44);
        loginButton.TabIndex = 4;
        loginButton.Text = "Đăng nhập";
        // 
        // registerLink
        // 
        registerLink.ActiveLinkColor = Color.Black;
        registerLink.AutoSize = true;
        registerLink.Dock = DockStyle.Top;
        registerLink.Font = new Font("Segoe UI", 9.75F);
        registerLink.LinkBehavior = LinkBehavior.HoverUnderline;
        registerLink.LinkColor = Color.FromArgb(21, 28, 39);
        registerLink.Location = new Point(0, 354);
        registerLink.Margin = new Padding(0);
        registerLink.Name = "registerLink";
        registerLink.Size = new Size(338, 23);
        registerLink.TabIndex = 5;
        registerLink.TabStop = true;
        registerLink.Text = "Chưa có tài khoản? Đăng ký";
        registerLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // LoginForm
        // 
        AcceptButton = loginButton;
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 760);
        Controls.Add(columns);
        Font = new Font("Segoe UI", 9.75F);
        MinimumSize = new Size(540, 620);
        Name = "LoginForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Đăng nhập";
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
        emailBorder.ResumeLayout(false);
        emailBorder.PerformLayout();
        passwordField.ResumeLayout(false);
        passwordField.PerformLayout();
        passwordBorder.ResumeLayout(false);
        passwordBorder.PerformLayout();
        passwordActions.ResumeLayout(false);
        passwordActions.PerformLayout();
        ResumeLayout(false);
    }
}
