using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class ResetPasswordForm
{
    private TableLayoutPanel columns = null!, brandingStory = null!, formColumn = null!, bodyLayout = null!;
    private Panel brandingPanel = null!, formArea = null!;
    private Label brandName = null!, brandHeadline = null!, brandDescription = null!, brandFooter = null!;
    private Label formBrand = null!, formTitle = null!, formSubtitle = null!;
    private TextBox emailTextBox = null!;
    private TableLayoutPanel emailField = null!;
    private Panel emailFieldBorder = null!;
    private Label emailFieldCaption = null!, emailFieldError = null!;
    private TextBox codeTextBox = null!;
    private TableLayoutPanel codeField = null!;
    private Panel codeFieldBorder = null!;
    private Label codeFieldCaption = null!, codeFieldError = null!;
    private TextBox passwordTextBox = null!;
    private TableLayoutPanel passwordField = null!;
    private Panel passwordFieldBorder = null!;
    private Label passwordFieldCaption = null!, passwordFieldError = null!;
    private TextBox confirmTextBox = null!;
    private TableLayoutPanel confirmField = null!;
    private Panel confirmFieldBorder = null!;
    private Label confirmFieldCaption = null!, confirmFieldError = null!;
    private LinkLabel resendLink = null!;
    private CheckBox showPassword = null!;
    private Label policyLabel = null!;
    private Label messageLabel = null!;
    private Button resetButton = null!;
    private LinkLabel backLink = null!;
    private System.Windows.Forms.Timer cooldownTimer = null!;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
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
        codeField = new TableLayoutPanel();
        codeFieldCaption = new Label();
        codeFieldBorder = new Panel();
        codeTextBox = new TextBox();
        codeFieldError = new Label();
        resendLink = new LinkLabel();
        passwordField = new TableLayoutPanel();
        passwordFieldCaption = new Label();
        passwordFieldBorder = new Panel();
        passwordTextBox = new TextBox();
        passwordFieldError = new Label();
        confirmField = new TableLayoutPanel();
        confirmFieldCaption = new Label();
        confirmFieldBorder = new Panel();
        confirmTextBox = new TextBox();
        confirmFieldError = new Label();
        showPassword = new CheckBox();
        policyLabel = new Label();
        messageLabel = new Label();
        resetButton = new Button();
        backLink = new LinkLabel();
        cooldownTimer = new System.Windows.Forms.Timer(components);
        columns.SuspendLayout();
        brandingPanel.SuspendLayout();
        brandingStory.SuspendLayout();
        formArea.SuspendLayout();
        formColumn.SuspendLayout();
        bodyLayout.SuspendLayout();
        emailField.SuspendLayout();
        emailFieldBorder.SuspendLayout();
        codeField.SuspendLayout();
        codeFieldBorder.SuspendLayout();
        passwordField.SuspendLayout();
        passwordFieldBorder.SuspendLayout();
        confirmField.SuspendLayout();
        confirmFieldBorder.SuspendLayout();
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
        columns.Size = new Size(1120, 900);
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
        brandingPanel.Size = new Size(464, 894);
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
        brandingStory.Size = new Size(368, 798);
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
        formArea.Size = new Size(644, 894);
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
        formColumn.Size = new Size(427, 853);
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
        formBrand.Size = new Size(427, 19);
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
        formTitle.Size = new Size(427, 50);
        formTitle.TabIndex = 1;
        formTitle.Text = "Đặt lại mật khẩu";
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
        formSubtitle.Size = new Size(427, 46);
        formSubtitle.TabIndex = 2;
        formSubtitle.Text = "Nếu tài khoản đủ điều kiện, bạn sẽ nhận được mã xác nhận qua email. Mã có hiệu lực 10 phút.";
        // 
        // bodyLayout
        // 
        bodyLayout.AutoSize = true;
        bodyLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bodyLayout.ColumnCount = 1;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bodyLayout.Controls.Add(emailField, 0, 0);
        bodyLayout.Controls.Add(codeField, 0, 1);
        bodyLayout.Controls.Add(resendLink, 0, 2);
        bodyLayout.Controls.Add(passwordField, 0, 3);
        bodyLayout.Controls.Add(confirmField, 0, 4);
        bodyLayout.Controls.Add(showPassword, 0, 5);
        bodyLayout.Controls.Add(policyLabel, 0, 6);
        bodyLayout.Controls.Add(messageLabel, 0, 7);
        bodyLayout.Controls.Add(resetButton, 0, 8);
        bodyLayout.Controls.Add(backLink, 0, 9);
        bodyLayout.Dock = DockStyle.Top;
        bodyLayout.Location = new Point(0, 179);
        bodyLayout.Margin = new Padding(0);
        bodyLayout.Name = "bodyLayout";
        bodyLayout.RowCount = 10;
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.Size = new Size(427, 674);
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
        emailField.Size = new Size(427, 86);
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
        emailFieldCaption.Size = new Size(427, 19);
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
        emailFieldBorder.Size = new Size(427, 40);
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
        emailTextBox.ReadOnly = true;
        emailTextBox.Size = new Size(403, 22);
        emailTextBox.TabIndex = 0;
        emailTextBox.TabStop = false;
        // 
        // emailFieldError
        // 
        emailFieldError.AutoSize = true;
        emailFieldError.Dock = DockStyle.Top;
        emailFieldError.Font = new Font("Segoe UI", 8.25F);
        emailFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        emailFieldError.Location = new Point(3, 67);
        emailFieldError.Name = "emailFieldError";
        emailFieldError.Size = new Size(421, 19);
        emailFieldError.TabIndex = 2;
        emailFieldError.Visible = false;
        // 
        // codeField
        // 
        codeField.AutoSize = true;
        codeField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        codeField.ColumnCount = 1;
        codeField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        codeField.Controls.Add(codeFieldCaption, 0, 0);
        codeField.Controls.Add(codeFieldBorder, 0, 1);
        codeField.Controls.Add(codeFieldError, 0, 2);
        codeField.Dock = DockStyle.Top;
        codeField.Location = new Point(0, 102);
        codeField.Margin = new Padding(0, 0, 0, 16);
        codeField.Name = "codeField";
        codeField.RowCount = 3;
        codeField.RowStyles.Add(new RowStyle());
        codeField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        codeField.RowStyles.Add(new RowStyle());
        codeField.Size = new Size(427, 86);
        codeField.TabIndex = 1;
        // 
        // codeFieldCaption
        // 
        codeFieldCaption.AutoSize = true;
        codeFieldCaption.Dock = DockStyle.Top;
        codeFieldCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        codeFieldCaption.ForeColor = Color.FromArgb(82, 82, 82);
        codeFieldCaption.Location = new Point(0, 0);
        codeFieldCaption.Margin = new Padding(0, 0, 0, 4);
        codeFieldCaption.Name = "codeFieldCaption";
        codeFieldCaption.Size = new Size(427, 19);
        codeFieldCaption.TabIndex = 0;
        codeFieldCaption.Text = "Mã xác nhận (6 chữ số)";
        // 
        // codeFieldBorder
        // 
        codeFieldBorder.BackColor = Color.White;
        codeFieldBorder.Controls.Add(codeTextBox);
        codeFieldBorder.Dock = DockStyle.Fill;
        codeFieldBorder.Location = new Point(0, 23);
        codeFieldBorder.Margin = new Padding(0, 0, 0, 4);
        codeFieldBorder.Name = "codeFieldBorder";
        codeFieldBorder.Padding = new Padding(12, 10, 12, 8);
        codeFieldBorder.Size = new Size(427, 40);
        codeFieldBorder.TabIndex = 1;
        // 
        // codeTextBox
        // 
        codeTextBox.AccessibleName = "Mã xác nhận (6 chữ số)";
        codeTextBox.BackColor = Color.White;
        codeTextBox.BorderStyle = BorderStyle.None;
        codeTextBox.Dock = DockStyle.Fill;
        codeTextBox.Font = new Font("Segoe UI", 9.75F);
        codeTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        codeTextBox.Location = new Point(12, 10);
        codeTextBox.MaxLength = 6;
        codeTextBox.Name = "codeTextBox";
        codeTextBox.Size = new Size(403, 22);
        codeTextBox.TabIndex = 0;
        // 
        // codeFieldError
        // 
        codeFieldError.AutoSize = true;
        codeFieldError.Dock = DockStyle.Top;
        codeFieldError.Font = new Font("Segoe UI", 8.25F);
        codeFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        codeFieldError.Location = new Point(3, 67);
        codeFieldError.Name = "codeFieldError";
        codeFieldError.Size = new Size(421, 19);
        codeFieldError.TabIndex = 2;
        codeFieldError.Visible = false;
        // 
        // resendLink
        // 
        resendLink.ActiveLinkColor = Color.Black;
        resendLink.AutoSize = true;
        resendLink.Dock = DockStyle.Top;
        resendLink.Font = new Font("Segoe UI", 9.75F);
        resendLink.LinkBehavior = LinkBehavior.HoverUnderline;
        resendLink.LinkColor = Color.FromArgb(21, 28, 39);
        resendLink.Location = new Point(0, 204);
        resendLink.Margin = new Padding(0, 0, 0, 16);
        resendLink.Name = "resendLink";
        resendLink.Size = new Size(427, 23);
        resendLink.TabIndex = 2;
        resendLink.TabStop = true;
        resendLink.Text = "Gửi lại mã";
        resendLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // passwordField
        // 
        passwordField.AutoSize = true;
        passwordField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        passwordField.ColumnCount = 1;
        passwordField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        passwordField.Controls.Add(passwordFieldCaption, 0, 0);
        passwordField.Controls.Add(passwordFieldBorder, 0, 1);
        passwordField.Controls.Add(passwordFieldError, 0, 2);
        passwordField.Dock = DockStyle.Top;
        passwordField.Location = new Point(0, 243);
        passwordField.Margin = new Padding(0, 0, 0, 16);
        passwordField.Name = "passwordField";
        passwordField.RowCount = 3;
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.Size = new Size(427, 86);
        passwordField.TabIndex = 3;
        // 
        // passwordFieldCaption
        // 
        passwordFieldCaption.AutoSize = true;
        passwordFieldCaption.Dock = DockStyle.Top;
        passwordFieldCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        passwordFieldCaption.ForeColor = Color.FromArgb(82, 82, 82);
        passwordFieldCaption.Location = new Point(0, 0);
        passwordFieldCaption.Margin = new Padding(0, 0, 0, 4);
        passwordFieldCaption.Name = "passwordFieldCaption";
        passwordFieldCaption.Size = new Size(427, 19);
        passwordFieldCaption.TabIndex = 0;
        passwordFieldCaption.Text = "Mật khẩu mới";
        // 
        // passwordFieldBorder
        // 
        passwordFieldBorder.BackColor = Color.White;
        passwordFieldBorder.Controls.Add(passwordTextBox);
        passwordFieldBorder.Dock = DockStyle.Fill;
        passwordFieldBorder.Location = new Point(0, 23);
        passwordFieldBorder.Margin = new Padding(0, 0, 0, 4);
        passwordFieldBorder.Name = "passwordFieldBorder";
        passwordFieldBorder.Padding = new Padding(12, 10, 12, 8);
        passwordFieldBorder.Size = new Size(427, 40);
        passwordFieldBorder.TabIndex = 1;
        // 
        // passwordTextBox
        // 
        passwordTextBox.AccessibleName = "Mật khẩu mới";
        passwordTextBox.BackColor = Color.White;
        passwordTextBox.BorderStyle = BorderStyle.None;
        passwordTextBox.Dock = DockStyle.Fill;
        passwordTextBox.Font = new Font("Segoe UI", 9.75F);
        passwordTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        passwordTextBox.Location = new Point(12, 10);
        passwordTextBox.Name = "passwordTextBox";
        passwordTextBox.Size = new Size(403, 22);
        passwordTextBox.TabIndex = 0;
        passwordTextBox.UseSystemPasswordChar = true;
        // 
        // passwordFieldError
        // 
        passwordFieldError.AutoSize = true;
        passwordFieldError.Dock = DockStyle.Top;
        passwordFieldError.Font = new Font("Segoe UI", 8.25F);
        passwordFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        passwordFieldError.Location = new Point(3, 67);
        passwordFieldError.Name = "passwordFieldError";
        passwordFieldError.Size = new Size(421, 19);
        passwordFieldError.TabIndex = 2;
        passwordFieldError.Visible = false;
        // 
        // confirmField
        // 
        confirmField.AutoSize = true;
        confirmField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        confirmField.ColumnCount = 1;
        confirmField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        confirmField.Controls.Add(confirmFieldCaption, 0, 0);
        confirmField.Controls.Add(confirmFieldBorder, 0, 1);
        confirmField.Controls.Add(confirmFieldError, 0, 2);
        confirmField.Dock = DockStyle.Top;
        confirmField.Location = new Point(0, 345);
        confirmField.Margin = new Padding(0, 0, 0, 16);
        confirmField.Name = "confirmField";
        confirmField.RowCount = 3;
        confirmField.RowStyles.Add(new RowStyle());
        confirmField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        confirmField.RowStyles.Add(new RowStyle());
        confirmField.Size = new Size(427, 86);
        confirmField.TabIndex = 4;
        // 
        // confirmFieldCaption
        // 
        confirmFieldCaption.AutoSize = true;
        confirmFieldCaption.Dock = DockStyle.Top;
        confirmFieldCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        confirmFieldCaption.ForeColor = Color.FromArgb(82, 82, 82);
        confirmFieldCaption.Location = new Point(0, 0);
        confirmFieldCaption.Margin = new Padding(0, 0, 0, 4);
        confirmFieldCaption.Name = "confirmFieldCaption";
        confirmFieldCaption.Size = new Size(427, 19);
        confirmFieldCaption.TabIndex = 0;
        confirmFieldCaption.Text = "Xác nhận mật khẩu mới";
        // 
        // confirmFieldBorder
        // 
        confirmFieldBorder.BackColor = Color.White;
        confirmFieldBorder.Controls.Add(confirmTextBox);
        confirmFieldBorder.Dock = DockStyle.Fill;
        confirmFieldBorder.Location = new Point(0, 23);
        confirmFieldBorder.Margin = new Padding(0, 0, 0, 4);
        confirmFieldBorder.Name = "confirmFieldBorder";
        confirmFieldBorder.Padding = new Padding(12, 10, 12, 8);
        confirmFieldBorder.Size = new Size(427, 40);
        confirmFieldBorder.TabIndex = 1;
        // 
        // confirmTextBox
        // 
        confirmTextBox.AccessibleName = "Xác nhận mật khẩu mới";
        confirmTextBox.BackColor = Color.White;
        confirmTextBox.BorderStyle = BorderStyle.None;
        confirmTextBox.Dock = DockStyle.Fill;
        confirmTextBox.Font = new Font("Segoe UI", 9.75F);
        confirmTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        confirmTextBox.Location = new Point(12, 10);
        confirmTextBox.Name = "confirmTextBox";
        confirmTextBox.Size = new Size(403, 22);
        confirmTextBox.TabIndex = 0;
        confirmTextBox.UseSystemPasswordChar = true;
        // 
        // confirmFieldError
        // 
        confirmFieldError.AutoSize = true;
        confirmFieldError.Dock = DockStyle.Top;
        confirmFieldError.Font = new Font("Segoe UI", 8.25F);
        confirmFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        confirmFieldError.Location = new Point(3, 67);
        confirmFieldError.Name = "confirmFieldError";
        confirmFieldError.Size = new Size(421, 19);
        confirmFieldError.TabIndex = 2;
        confirmFieldError.Visible = false;
        // 
        // showPassword
        // 
        showPassword.AutoSize = true;
        showPassword.Location = new Point(0, 447);
        showPassword.Margin = new Padding(0, 0, 0, 8);
        showPassword.Name = "showPassword";
        showPassword.Size = new Size(144, 27);
        showPassword.TabIndex = 5;
        showPassword.Text = "Hiện mật khẩu";
        // 
        // policyLabel
        // 
        policyLabel.AutoSize = true;
        policyLabel.Dock = DockStyle.Top;
        policyLabel.ForeColor = Color.FromArgb(82, 82, 82);
        policyLabel.Location = new Point(0, 482);
        policyLabel.Margin = new Padding(0, 0, 0, 16);
        policyLabel.MaximumSize = new Size(440, 0);
        policyLabel.Name = "policyLabel";
        policyLabel.Size = new Size(427, 46);
        policyLabel.TabIndex = 6;
        policyLabel.Text = "Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tối đa 72 byte UTF-8.";
        // 
        // messageLabel
        // 
        messageLabel.AutoSize = true;
        messageLabel.Dock = DockStyle.Top;
        messageLabel.ForeColor = Color.FromArgb(220, 38, 38);
        messageLabel.Location = new Point(0, 544);
        messageLabel.Margin = new Padding(0, 0, 0, 16);
        messageLabel.MaximumSize = new Size(440, 0);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(427, 23);
        messageLabel.TabIndex = 7;
        // 
        // resetButton
        // 
        resetButton.Cursor = Cursors.Hand;
        resetButton.Dock = DockStyle.Top;
        resetButton.Location = new Point(0, 583);
        resetButton.Margin = new Padding(0, 0, 0, 24);
        resetButton.Name = "resetButton";
        resetButton.Size = new Size(427, 44);
        resetButton.TabIndex = 8;
        resetButton.Text = "Đặt lại mật khẩu";
        // 
        // backLink
        // 
        backLink.ActiveLinkColor = Color.Black;
        backLink.AutoSize = true;
        backLink.Dock = DockStyle.Top;
        backLink.Font = new Font("Segoe UI", 9.75F);
        backLink.LinkBehavior = LinkBehavior.HoverUnderline;
        backLink.LinkColor = Color.FromArgb(21, 28, 39);
        backLink.Location = new Point(0, 651);
        backLink.Margin = new Padding(0);
        backLink.Name = "backLink";
        backLink.Size = new Size(427, 23);
        backLink.TabIndex = 9;
        backLink.TabStop = true;
        backLink.Text = "← Quay về đăng nhập";
        backLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // cooldownTimer
        // 
        cooldownTimer.Interval = 1000;
        // 
        // ResetPasswordForm
        // 
        AcceptButton = resetButton;
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 900);
        Controls.Add(columns);
        Font = new Font("Segoe UI", 9.75F);
        MinimumSize = new Size(540, 700);
        Name = "ResetPasswordForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Đặt lại mật khẩu";
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
        codeField.ResumeLayout(false);
        codeField.PerformLayout();
        codeFieldBorder.ResumeLayout(false);
        codeFieldBorder.PerformLayout();
        passwordField.ResumeLayout(false);
        passwordField.PerformLayout();
        passwordFieldBorder.ResumeLayout(false);
        passwordFieldBorder.PerformLayout();
        confirmField.ResumeLayout(false);
        confirmField.PerformLayout();
        confirmFieldBorder.ResumeLayout(false);
        confirmFieldBorder.PerformLayout();
        ResumeLayout(false);
    }

    private System.ComponentModel.IContainer components;
}
