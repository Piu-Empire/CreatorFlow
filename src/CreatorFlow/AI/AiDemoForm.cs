namespace CreatorFlow.AI;

// Form test AI 100% code, không dùng Designer.
// Không được tham chiếu từ Program.cs trên GitHub.
// Chỉ dùng local: tạm sửa Program.cs thành
// Application.Run(new AiDemoForm(AIFactory.Create()));
// rồi revert trước khi commit.
[System.ComponentModel.DesignerCategory("Code")]
public sealed class AiDemoForm : Form
{
    private readonly IAIService _aiService;
    private readonly TextBox _txtPrompt = new();
    private readonly Button _btnGenerate = new();
    private readonly TextBox _txtResult = new();

    public AiDemoForm(IAIService aiService)
    {
        _aiService = aiService;
        Text = "AI Demo (local only)";
        ClientSize = new Size(800, 450);

        var lblPrompt = new Label
        {
            Text = "Prompt",
            Location = new Point(30, 30),
            AutoSize = true
        };
        _txtPrompt.Location = new Point(30, 60);
        _txtPrompt.Size = new Size(700, 27);

        _btnGenerate.Text = "Generate";
        _btnGenerate.Location = new Point(30, 105);
        _btnGenerate.Size = new Size(120, 35);
        _btnGenerate.Click += BtnGenerate_Click;

        var lblResult = new Label
        {
            Text = "Result",
            Location = new Point(30, 165),
            AutoSize = true
        };
        _txtResult.Location = new Point(30, 195);
        _txtResult.Size = new Size(700, 200);
        _txtResult.Multiline = true;
        _txtResult.ReadOnly = true;
        _txtResult.ScrollBars = ScrollBars.Vertical;

        Controls.Add(lblPrompt);
        Controls.Add(_txtPrompt);
        Controls.Add(_btnGenerate);
        Controls.Add(lblResult);
        Controls.Add(_txtResult);
    }

    private async void BtnGenerate_Click(
        object? sender,
        EventArgs e)
    {
        _btnGenerate.Enabled = false;
        _txtResult.Clear();

        try
        {
            var result = await _aiService.AskAsync(
                _txtPrompt.Text);

            if (!result.Success)
            {
                MessageBox.Show(
                    result.ErrorMessage,
                    "AI Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _txtResult.Text = result.Content;
        }
        finally
        {
            _btnGenerate.Enabled = true;
        }
    }
}
