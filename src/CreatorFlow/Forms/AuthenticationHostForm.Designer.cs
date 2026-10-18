namespace CreatorFlow.Forms;

partial class AuthenticationHostForm
{
    private Panel pageHost = null!;

    private void InitializeComponent()
    {
        pageHost = new Panel();
        SuspendLayout();
        //
        // pageHost
        //
        pageHost.Dock = DockStyle.Fill;
        pageHost.Location = new Point(0, 0);
        pageHost.Name = "pageHost";
        pageHost.Size = new Size(1120, 900);
        pageHost.TabIndex = 0;
        //
        // AuthenticationHostForm
        //
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(1120, 900);
        Controls.Add(pageHost);
        MinimumSize = new Size(540, 620);
        Name = "AuthenticationHostForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CreatorFlow";
        WindowState = FormWindowState.Maximized;
        ResumeLayout(false);
    }
}
