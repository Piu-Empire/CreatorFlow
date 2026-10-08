namespace CreatorFlow.AI
{
    public partial class AiTestForm : Form
    {
        private IAIService? _aiService;
        private HttpClient? _ownedHttpClient;
        private CancellationTokenSource? _requestCts;

        public AiTestForm()
        {
            InitializeComponent();
        }

        public AiTestForm(IAIService aiService) : this()
        {
            _aiService = aiService;
        }

        private async void btnGenerate_Click(object? sender, EventArgs e)
        {
            if (_requestCts is not null)
                return;

            var prompt = txtPrompt.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt))
            {
                lblStatus.Text = "Vui lòng nhập yêu cầu.";
                txtPrompt.Focus();
                return;
            }

            using var cts = new CancellationTokenSource();
            _requestCts = cts;
            btnGenerate.Enabled = false;
            btnCancel.Enabled = true;
            txtPrompt.ReadOnly = true;
            lblStatus.Text = "Đang gọi AI…";
            txtResult.Clear();

            try
            {
                if (_aiService is null)
                {
                    _ownedHttpClient = new HttpClient();
                    _aiService = AIFactory.Create(_ownedHttpClient);
                }

                var result = await _aiService.AskAsync(prompt, cts.Token);
                if (IsDisposed || Disposing)
                    return;

                lblStatus.Text = cts.IsCancellationRequested
                    ? "Đã hủy."
                    : result.Success ? "Hoàn tất." : "Gọi AI thất bại.";
                txtResult.Text = cts.IsCancellationRequested
                    ? string.Empty
                    : result.Success ? result.Content : result.ErrorMessage;
            }
            catch (OperationCanceledException)
            {
                if (!IsDisposed && !Disposing)
                    lblStatus.Text = "Đã hủy.";
            }
            catch (Exception)
            {
                if (!IsDisposed && !Disposing)
                    lblStatus.Text = "Có lỗi khi gọi AI. Kiểm tra cấu hình .env.";
            }
            finally
            {
                _requestCts = null;
                if (!IsDisposed && !Disposing)
                {
                    btnGenerate.Enabled = true;
                    btnCancel.Enabled = false;
                    txtPrompt.ReadOnly = false;
                }
            }
        }

        private void btnCancel_Click(object? sender, EventArgs e)
        {
            _requestCts?.Cancel();
            btnCancel.Enabled = false;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _requestCts?.Cancel();
            _ownedHttpClient?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
