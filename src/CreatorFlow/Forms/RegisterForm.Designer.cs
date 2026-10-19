using CreatorFlow.Helpers;

namespace CreatorFlow.Forms;

partial class RegisterForm
{
    private TableLayoutPanel columns = null!, brandingStory = null!, formColumn = null!, bodyLayout = null!;
    private Panel brandingPanel = null!, formArea = null!;
    private Label brandName = null!, brandHeadline = null!, brandDescription = null!, brandFooter = null!;
    private Label formBrand = null!, formTitle = null!, formSubtitle = null!;
    private TextBox nameTextBox = null!;
    private TableLayoutPanel nameField = null!;
    private Panel nameFieldBorder = null!;
    private Label nameFieldCaption = null!, nameFieldError = null!;
    private TextBox emailTextBox = null!;
    private TableLayoutPanel emailField = null!;
    private Panel emailFieldBorder = null!;
    private Label emailFieldCaption = null!, emailFieldError = null!;
    private TextBox passwordTextBox = null!;
    private TableLayoutPanel passwordField = null!;
    private Panel passwordFieldBorder = null!;
    private Label passwordFieldCaption = null!, passwordFieldError = null!;
    private TextBox confirmTextBox = null!;
    private TableLayoutPanel confirmField = null!;
    private Panel confirmFieldBorder = null!;
    private Label confirmFieldCaption = null!, confirmFieldError = null!;
    private CheckBox showPassword = null!;
    private Label policyLabel = null!;
    private Label messageLabel = null!;
    private Button registerButton = null!;
    private LinkLabel loginLink = null!;

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
        nameField = new TableLayoutPanel();
        nameFieldCaption = new Label();
        nameFieldBorder = new Panel();
        nameTextBox = new TextBox();
        nameFieldError = new Label();
        emailField = new TableLayoutPanel();
        emailFieldCaption = new Label();
        emailFieldBorder = new Panel();
        emailTextBox = new TextBox();
        emailFieldError = new Label();
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
        registerButton = new Button();
        loginLink = new LinkLabel();
        columns.SuspendLayout();
        brandingPanel.SuspendLayout();
        brandingStory.SuspendLayout();
        formArea.SuspendLayout();
        formColumn.SuspendLayout();
        bodyLayout.SuspendLayout();
        nameField.SuspendLayout();
        nameFieldBorder.SuspendLayout();
        emailField.SuspendLayout();
        emailFieldBorder.SuspendLayout();
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
        formColumn.Size = new Size(421, 791);
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
        formBrand.Size = new Size(421, 19);
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
        formTitle.Size = new Size(421, 50);
        formTitle.TabIndex = 1;
        formTitle.Text = "Tạo tài khoản";
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
        formSubtitle.Size = new Size(421, 23);
        formSubtitle.TabIndex = 2;
        formSubtitle.Text = "Bắt đầu làm việc cùng CreatorFlow.";
        // 
        // bodyLayout
        // 
        bodyLayout.AutoSize = true;
        bodyLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        bodyLayout.ColumnCount = 1;
        bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        bodyLayout.Controls.Add(nameField, 0, 0);
        bodyLayout.Controls.Add(emailField, 0, 1);
        bodyLayout.Controls.Add(passwordField, 0, 2);
        bodyLayout.Controls.Add(confirmField, 0, 3);
        bodyLayout.Controls.Add(showPassword, 0, 4);
        bodyLayout.Controls.Add(policyLabel, 0, 5);
        bodyLayout.Controls.Add(messageLabel, 0, 6);
        bodyLayout.Controls.Add(registerButton, 0, 7);
        bodyLayout.Controls.Add(loginLink, 0, 8);
        bodyLayout.Dock = DockStyle.Top;
        bodyLayout.Location = new Point(0, 156);
        bodyLayout.Margin = new Padding(0);
        bodyLayout.Name = "bodyLayout";
        bodyLayout.RowCount = 9;
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.RowStyles.Add(new RowStyle());
        bodyLayout.Size = new Size(421, 635);
        bodyLayout.TabIndex = 3;
        // 
        // nameField
        // 
        nameField.AutoSize = true;
        nameField.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        nameField.ColumnCount = 1;
        nameField.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        nameField.Controls.Add(nameFieldCaption, 0, 0);
        nameField.Controls.Add(nameFieldBorder, 0, 1);
        nameField.Controls.Add(nameFieldError, 0, 2);
        nameField.Dock = DockStyle.Top;
        nameField.Location = new Point(0, 0);
        nameField.Margin = new Padding(0, 0, 0, 16);
        nameField.Name = "nameField";
        nameField.RowCount = 3;
        nameField.RowStyles.Add(new RowStyle());
        nameField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        nameField.RowStyles.Add(new RowStyle());
        nameField.Size = new Size(421, 86);
        nameField.TabIndex = 0;
        // 
        // nameFieldCaption
        // 
        nameFieldCaption.AutoSize = true;
        nameFieldCaption.Dock = DockStyle.Top;
        nameFieldCaption.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        nameFieldCaption.ForeColor = Color.FromArgb(82, 82, 82);
        nameFieldCaption.Location = new Point(0, 0);
        nameFieldCaption.Margin = new Padding(0, 0, 0, 4);
        nameFieldCaption.Name = "nameFieldCaption";
        nameFieldCaption.Size = new Size(421, 19);
        nameFieldCaption.TabIndex = 0;
        nameFieldCaption.Text = "Tên hiển thị";
        // 
        // nameFieldBorder
        // 
        nameFieldBorder.BackColor = Color.White;
        nameFieldBorder.Controls.Add(nameTextBox);
        nameFieldBorder.Dock = DockStyle.Fill;
        nameFieldBorder.Location = new Point(0, 23);
        nameFieldBorder.Margin = new Padding(0, 0, 0, 4);
        nameFieldBorder.Name = "nameFieldBorder";
        nameFieldBorder.Padding = new Padding(12, 10, 12, 8);
        nameFieldBorder.Size = new Size(421, 40);
        nameFieldBorder.TabIndex = 1;
        // 
        // nameTextBox
        // 
        nameTextBox.AccessibleName = "Tên hiển thị";
        nameTextBox.BackColor = Color.White;
        nameTextBox.BorderStyle = BorderStyle.None;
        nameTextBox.Dock = DockStyle.Fill;
        nameTextBox.Font = new Font("Segoe UI", 9.75F);
        nameTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        nameTextBox.Location = new Point(12, 10);
        nameTextBox.MaxLength = 150;
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(397, 22);
        nameTextBox.TabIndex = 0;
        // 
        // nameFieldError
        // 
        nameFieldError.AutoSize = true;
        nameFieldError.Dock = DockStyle.Top;
        nameFieldError.Font = new Font("Segoe UI", 8.25F);
        nameFieldError.ForeColor = Color.FromArgb(220, 38, 38);
        nameFieldError.Location = new Point(3, 67);
        nameFieldError.Name = "nameFieldError";
        nameFieldError.Size = new Size(415, 19);
        nameFieldError.TabIndex = 2;
        nameFieldError.Visible = false;
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
        emailField.Location = new Point(0, 102);
        emailField.Margin = new Padding(0, 0, 0, 16);
        emailField.Name = "emailField";
        emailField.RowCount = 3;
        emailField.RowStyles.Add(new RowStyle());
        emailField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        emailField.RowStyles.Add(new RowStyle());
        emailField.Size = new Size(421, 86);
        emailField.TabIndex = 1;
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
        emailFieldCaption.Size = new Size(421, 19);
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
        emailFieldBorder.Size = new Size(421, 40);
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
        emailTextBox.Size = new Size(397, 22);
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
        emailFieldError.Size = new Size(415, 19);
        emailFieldError.TabIndex = 2;
        emailFieldError.Visible = false;
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
        passwordField.Location = new Point(0, 204);
        passwordField.Margin = new Padding(0, 0, 0, 16);
        passwordField.Name = "passwordField";
        passwordField.RowCount = 3;
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        passwordField.RowStyles.Add(new RowStyle());
        passwordField.Size = new Size(421, 86);
        passwordField.TabIndex = 2;
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
        passwordFieldCaption.Size = new Size(421, 19);
        passwordFieldCaption.TabIndex = 0;
        passwordFieldCaption.Text = "Mật khẩu";
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
        passwordFieldBorder.Size = new Size(421, 40);
        passwordFieldBorder.TabIndex = 1;
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
        passwordTextBox.Size = new Size(397, 22);
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
        passwordFieldError.Size = new Size(415, 19);
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
        confirmField.Location = new Point(0, 306);
        confirmField.Margin = new Padding(0, 0, 0, 16);
        confirmField.Name = "confirmField";
        confirmField.RowCount = 3;
        confirmField.RowStyles.Add(new RowStyle());
        confirmField.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        confirmField.RowStyles.Add(new RowStyle());
        confirmField.Size = new Size(421, 86);
        confirmField.TabIndex = 3;
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
        confirmFieldCaption.Size = new Size(421, 19);
        confirmFieldCaption.TabIndex = 0;
        confirmFieldCaption.Text = "Xác nhận mật khẩu";
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
        confirmFieldBorder.Size = new Size(421, 40);
        confirmFieldBorder.TabIndex = 1;
        // 
        // confirmTextBox
        // 
        confirmTextBox.AccessibleName = "Xác nhận mật khẩu";
        confirmTextBox.BackColor = Color.White;
        confirmTextBox.BorderStyle = BorderStyle.None;
        confirmTextBox.Dock = DockStyle.Fill;
        confirmTextBox.Font = new Font("Segoe UI", 9.75F);
        confirmTextBox.ForeColor = Color.FromArgb(21, 28, 39);
        confirmTextBox.Location = new Point(12, 10);
        confirmTextBox.Name = "confirmTextBox";
        confirmTextBox.Size = new Size(397, 22);
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
        confirmFieldError.Size = new Size(415, 19);
        confirmFieldError.TabIndex = 2;
        confirmFieldError.Visible = false;
        // 
        // showPassword
        // 
        showPassword.AutoSize = true;
        showPassword.Location = new Point(0, 408);
        showPassword.Margin = new Padding(0, 0, 0, 8);
        showPassword.Name = "showPassword";
        showPassword.Size = new Size(144, 27);
        showPassword.TabIndex = 4;
        showPassword.Text = "Hiện mật khẩu";
        // 
        // policyLabel
        // 
        policyLabel.AutoSize = true;
        policyLabel.Dock = DockStyle.Top;
        policyLabel.ForeColor = Color.FromArgb(82, 82, 82);
        policyLabel.Location = new Point(0, 443);
        policyLabel.Margin = new Padding(0, 0, 0, 16);
        policyLabel.MaximumSize = new Size(440, 0);
        policyLabel.Name = "policyLabel";
        policyLabel.Size = new Size(421, 46);
        policyLabel.TabIndex = 5;
        policyLabel.Text = "Ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt. Tối đa 72 byte UTF-8.";
        // 
        // messageLabel
        // 
        messageLabel.AutoSize = true;
        messageLabel.Dock = DockStyle.Top;
        messageLabel.ForeColor = Color.FromArgb(220, 38, 38);
        messageLabel.Location = new Point(0, 505);
        messageLabel.Margin = new Padding(0, 0, 0, 16);
        messageLabel.MaximumSize = new Size(440, 0);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(421, 23);
        messageLabel.TabIndex = 6;
        // 
        // registerButton
        // 
        registerButton.Cursor = Cursors.Hand;
        registerButton.Dock = DockStyle.Top;
        registerButton.Location = new Point(0, 544);
        registerButton.Margin = new Padding(0, 0, 0, 24);
        registerButton.Name = "registerButton";
        registerButton.Size = new Size(421, 44);
        registerButton.TabIndex = 7;
        registerButton.Text = "Tạo tài khoản";
        // 
        // loginLink
        // 
        loginLink.ActiveLinkColor = Color.Black;
        loginLink.AutoSize = true;
        loginLink.Dock = DockStyle.Top;
        loginLink.Font = new Font("Segoe UI", 9.75F);
        loginLink.LinkBehavior = LinkBehavior.HoverUnderline;
        loginLink.LinkColor = Color.FromArgb(21, 28, 39);
        loginLink.Location = new Point(0, 612);
        loginLink.Margin = new Padding(0);
        loginLink.Name = "loginLink";
        loginLink.Size = new Size(421, 23);
        loginLink.TabIndex = 8;
        loginLink.TabStop = true;
        loginLink.Text = "Đã có tài khoản? Đăng nhập";
        loginLink.VisitedLinkColor = Color.FromArgb(21, 28, 39);
        // 
        // RegisterForm
        // 
        AcceptButton = registerButton;
        AutoScaleDimensions = new SizeF(120F, 120F);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 900);
        Controls.Add(columns);
        Font = new Font("Segoe UI", 9.75F);
        MinimumSize = new Size(540, 700);
        Name = "RegisterForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow · Tạo tài khoản";
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
        nameField.ResumeLayout(false);
        nameField.PerformLayout();
        nameFieldBorder.ResumeLayout(false);
        nameFieldBorder.PerformLayout();
        emailField.ResumeLayout(false);
        emailField.PerformLayout();
        emailFieldBorder.ResumeLayout(false);
        emailFieldBorder.PerformLayout();
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
}
