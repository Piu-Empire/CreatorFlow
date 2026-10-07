#nullable disable
using CreatorFlow.Controls;
using CreatorFlow.Theme;

namespace CreatorFlow.Forms.Board;

partial class BoardForm
{
    private System.ComponentModel.IContainer components = null;

    // Khung chính
    private SidebarControl _sidebarControl;
    private System.Windows.Forms.Panel _pnlMain;
    private TopHeaderControl _topHeaderControl;
    private System.Windows.Forms.Panel _pnlBoardArea;
    private ModulePageHeaderControl _modulePageHeader;
    private System.Windows.Forms.FlowLayoutPanel _flowColumns;   // Chứa 7 cột Kanban, cuộn ngang

    // Drawer chi tiết bên phải
    private System.Windows.Forms.Panel _pnlDrawerHost;
    private ContentDetailPanel _contentDetailPanel;

    private ToastNotification _toast;

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
        components = new System.ComponentModel.Container();
        _sidebarControl = new SidebarControl();
        _pnlMain = new System.Windows.Forms.Panel();
        _topHeaderControl = new TopHeaderControl();
        _pnlBoardArea = new System.Windows.Forms.Panel();
        _modulePageHeader = new ModulePageHeaderControl();
        _flowColumns = new System.Windows.Forms.FlowLayoutPanel();
        _pnlDrawerHost = new System.Windows.Forms.Panel();
        _contentDetailPanel = new ContentDetailPanel();
        _toast = new ToastNotification();
        SuspendLayout();
        _pnlMain.SuspendLayout();
        _pnlBoardArea.SuspendLayout();
        _pnlDrawerHost.SuspendLayout();
        // 
        // _sidebarControl  (trái, 260px, nền đen)
        // 
        _sidebarControl.Dock = DockStyle.Left;
        _sidebarControl.Name = "_sidebarControl";
        _sidebarControl.Width = 260;
        // 
        // _topHeaderControl  (breadcrumb / search / New Content / user)
        // 
        _topHeaderControl.Dock = DockStyle.Top;
        _topHeaderControl.Name = "_topHeaderControl";
        _topHeaderControl.Height = 68;
        // 
        // _modulePageHeader  (Production Board + bộ lọc + Add Ticket)
        // 
        _modulePageHeader.Dock = DockStyle.Top;
        _modulePageHeader.Name = "_modulePageHeader";
        _modulePageHeader.Height = 88;
        // 
        // _flowColumns  (các cột Kanban xếp ngang, thanh cuộn ngang ở đáy)
        // 
        _flowColumns.AutoScroll = true;
        _flowColumns.BackColor = UITheme.Neutral50;
        _flowColumns.Dock = DockStyle.Fill;
        _flowColumns.FlowDirection = FlowDirection.LeftToRight;
        _flowColumns.Name = "_flowColumns";
        _flowColumns.Padding = new Padding(0, 20, 32, 12);
        _flowColumns.WrapContents = false;
        // 
        // _pnlBoardArea  (lề trái 32, lề trên 24 như mẫu)
        // 
        _pnlBoardArea.BackColor = UITheme.Neutral50;
        _pnlBoardArea.Dock = DockStyle.Fill;
        _pnlBoardArea.Name = "_pnlBoardArea";
        _pnlBoardArea.Padding = new Padding(32, 24, 0, 0);
        _pnlBoardArea.Controls.Add(_flowColumns);        // Fill: Add trước
        _pnlBoardArea.Controls.Add(_modulePageHeader);   // Top: Add sau → dock trước
        // 
        // _pnlMain
        // 
        _pnlMain.BackColor = UITheme.Neutral50;
        _pnlMain.Dock = DockStyle.Fill;
        _pnlMain.Name = "_pnlMain";
        _pnlMain.Controls.Add(_pnlBoardArea);
        _pnlMain.Controls.Add(_topHeaderControl);
        // 
        // _contentDetailPanel
        // 
        _contentDetailPanel.Dock = DockStyle.Fill;
        _contentDetailPanel.Name = "_contentDetailPanel";
        // 
        // _pnlDrawerHost  (drawer phải 620px, ẩn tới khi chọn thẻ)
        // 
        _pnlDrawerHost.BackColor = UITheme.White;
        _pnlDrawerHost.Dock = DockStyle.Right;
        _pnlDrawerHost.Name = "_pnlDrawerHost";
        _pnlDrawerHost.Padding = new Padding(1, 0, 0, 0);   // chừa 1px cho đường viền trái
        _pnlDrawerHost.Width = 620;
        _pnlDrawerHost.Visible = false;
        _pnlDrawerHost.Controls.Add(_contentDetailPanel);
        // 
        // BoardForm
        // 
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = UITheme.Neutral50;
        ClientSize = new Size(1600, 900);
        Font = new Font("Segoe UI", 9F);
        MinimumSize = new Size(1100, 700);
        Name = "BoardForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow — Production Board";
        // Thứ tự Add: control Add SAU CÙNG được dock TRƯỚC → Sidebar (Left), Drawer (Right), rồi Main (Fill)
        Controls.Add(_pnlMain);
        Controls.Add(_pnlDrawerHost);
        Controls.Add(_sidebarControl);
        Controls.Add(_toast);
        _pnlDrawerHost.ResumeLayout(false);
        _pnlBoardArea.ResumeLayout(false);
        _pnlMain.ResumeLayout(false);
        ResumeLayout(false);
    }
}