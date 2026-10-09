namespace CreatorFlow.Models;

/// <summary>Một đoạn của script, tách theo dấu [Hook] / [Body] / [CTA]... ở đầu dòng.</summary>
/// <param name="Name">Tên đoạn, VD "Hook 0-3s". Phần chữ nằm trước dấu mốc đầu tiên có tên "General".</param>
/// <param name="Text">Nội dung đoạn (không gồm dấu mốc).</param>
public sealed record ScriptSection(string Name, string Text);
