namespace CreatorFlow.Forms;

internal partial class AuthenticationHostForm : Form
{
    private Form? currentPage;

    public AuthenticationHostForm()
    {
        InitializeComponent();
    }

    public void ShowPage(Form page)
    {
        if (ReferenceEquals(currentPage, page)) return;

        Form? previousPage = currentPage;
        currentPage = null;
        if (previousPage is not null)
        {
            pageHost.Controls.Remove(previousPage);
            previousPage.Close();
            previousPage.Dispose();
        }

        currentPage = page;
        Text = page.Text;
        page.TopLevel = false;
        page.FormBorderStyle = FormBorderStyle.None;
        page.ShowInTaskbar = false;
        page.Dock = DockStyle.Fill;
        pageHost.Controls.Add(page);
        page.Show();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (e.Cancel || currentPage is null) return;

        Form page = currentPage;
        currentPage = null;
        pageHost.Controls.Remove(page);
        page.Close();
        page.Dispose();
    }
}
