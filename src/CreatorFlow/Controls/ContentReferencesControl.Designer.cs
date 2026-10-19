#nullable disable
using System.Drawing;
using System.Windows.Forms;
using CreatorFlow.Theme;

namespace CreatorFlow.Controls;

partial class ContentReferencesControl
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this._root = new TableLayoutPanel();
        this._lblTitle = new Label();
        this._editor = new TableLayoutPanel();
        this._txtName = new TextBox();
        this._txtUrl = new TextBox();
        this._btnAdd = new ProjectActionButton();
        this._addButtonHost = new Panel();
        this._lblStatus = new Label();
        this._references = new ListView();
        this._colName = new ColumnHeader();
        this._colUrl = new ColumnHeader();
        this._actions = new FlowLayoutPanel();
        this._btnDelete = new ProjectActionButton();
        this._btnOpen = new ProjectActionButton();
        this._listHost = new Panel();
        this.SuspendLayout();

        // Tên tài liệu
        this._txtName.PlaceholderText = "Tên tài liệu tham khảo";
        this._txtName.Dock = DockStyle.Fill;
        this._txtName.Font = UITheme.FontBody;
        this._txtName.BorderStyle = BorderStyle.FixedSingle;
        this._txtName.MaxLength = 255;

        // URL
        this._txtUrl.PlaceholderText = "https://...";
        this._txtUrl.Dock = DockStyle.Fill;
        this._txtUrl.Font = UITheme.FontBody;
        this._txtUrl.BorderStyle = BorderStyle.FixedSingle;

        // Nút thêm
        this._btnAdd.Text = "Thêm reference";
        this._btnAdd.Style = RoundButtonStyle.Primary;
        this._btnAdd.Size = new Size(150, 32);
        this._btnAdd.Location = new Point(0, 0);
        this._btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        this._addButtonHost.Dock = DockStyle.Fill;
        this._addButtonHost.BackColor = UITheme.White;
        this._addButtonHost.Controls.Add(this._btnAdd);

        // Khối nhập liệu (3 hàng)
        this._editor.Dock = DockStyle.Fill;
        this._editor.ColumnCount = 1;
        this._editor.RowCount = 3;
        this._editor.Padding = new Padding(0, 6, 0, 4);
        this._editor.Margin = new Padding(0);
        this._editor.BackColor = UITheme.White;
        this._editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this._editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        this._editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        this._editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this._editor.Controls.Add(this._txtName, 0, 0);
        this._editor.Controls.Add(this._txtUrl, 0, 1);
        this._editor.Controls.Add(this._addButtonHost, 0, 2);

        // Tiêu đề + trạng thái
        this._lblTitle.Text = "REFERENCE LINKS";
        this._lblTitle.Dock = DockStyle.Fill;
        this._lblTitle.Font = UITheme.FontLabelBold;
        this._lblTitle.ForeColor = UITheme.Neutral600;
        this._lblTitle.TextAlign = ContentAlignment.MiddleLeft;

        this._lblStatus.Dock = DockStyle.Fill;
        this._lblStatus.Font = UITheme.FontLabel;
        this._lblStatus.ForeColor = UITheme.Neutral600;
        this._lblStatus.TextAlign = ContentAlignment.MiddleLeft;
        this._lblStatus.AutoEllipsis = true;

        // Danh sách reference
        this._colName.Text = "Tên";
        this._colName.Width = 190;
        this._colUrl.Text = "URL";
        this._colUrl.Width = 450;
        this._references.Dock = DockStyle.Fill;
        this._references.View = System.Windows.Forms.View.Details;
        this._references.FullRowSelect = true;
        this._references.MultiSelect = false;
        this._references.HideSelection = false;
        this._references.BorderStyle = BorderStyle.FixedSingle;
        this._references.Font = UITheme.FontBody;
        this._references.BackColor = UITheme.White;
        this._references.Columns.AddRange(new ColumnHeader[] { this._colName, this._colUrl });

        // Nút Xóa / Mở link
        this._btnDelete.Text = "Xóa reference";
        this._btnDelete.Style = RoundButtonStyle.Secondary;
        this._btnDelete.Size = new Size(145, 32);
        this._btnDelete.Enabled = false;

        this._btnOpen.Text = "Mở link";
        this._btnOpen.Style = RoundButtonStyle.Secondary;
        this._btnOpen.Size = new Size(110, 32);
        this._btnOpen.Enabled = false;

        this._actions.Dock = DockStyle.Bottom;
        this._actions.Height = 42;
        this._actions.FlowDirection = FlowDirection.RightToLeft;
        this._actions.WrapContents = false;
        this._actions.BackColor = UITheme.White;
        this._actions.Padding = new Padding(0, 6, 0, 0);
        this._actions.Controls.Add(this._btnDelete);
        this._actions.Controls.Add(this._btnOpen);

        this._listHost.Dock = DockStyle.Fill;
        this._listHost.BackColor = UITheme.White;
        this._listHost.Controls.Add(this._references);
        this._listHost.Controls.Add(this._actions);

        // Khung 4 hàng: tiêu đề / nhập liệu / trạng thái / danh sách
        this._root.Dock = DockStyle.Fill;
        this._root.ColumnCount = 1;
        this._root.RowCount = 4;
        this._root.Margin = new Padding(0);
        this._root.Padding = new Padding(0);
        this._root.BackColor = UITheme.White;
        this._root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        this._root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        this._root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        this._root.Controls.Add(this._lblTitle, 0, 0);
        this._root.Controls.Add(this._editor, 0, 1);
        this._root.Controls.Add(this._lblStatus, 0, 2);
        this._root.Controls.Add(this._listHost, 0, 3);

        // UserControl
        this.Dock = DockStyle.Fill;
        this.BackColor = UITheme.White;
        this.Padding = new Padding(24, 20, 24, 20);
        this.Size = new Size(680, 520);
        this.Controls.Add(this._root);
        this.ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel _root;
    private Label _lblTitle;
    private TableLayoutPanel _editor;
    private TextBox _txtName;
    private TextBox _txtUrl;
    private ProjectActionButton _btnAdd;
    private Panel _addButtonHost;
    private Label _lblStatus;
    private ListView _references;
    private ColumnHeader _colName;
    private ColumnHeader _colUrl;
    private FlowLayoutPanel _actions;
    private ProjectActionButton _btnDelete;
    private ProjectActionButton _btnOpen;
    private Panel _listHost;
}
