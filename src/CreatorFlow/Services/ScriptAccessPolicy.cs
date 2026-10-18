using CreatorFlow.Models;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Services;

/// <summary>
/// Quy tắc "ai được sửa script" (thuần, không đụng DB). Xem script = là thành viên Project (ScriptService kiểm tra).
///   - Content đã Published hoặc Archived: không ai sửa được.
///   - Owner/Manager: sửa script của mọi Content trong Project.
///   - Creator: chỉ sửa script của Content do mình tạo HOẶC đang được giao cho mình.
/// </summary>
public static class ScriptAccessPolicy
{
    public static ScriptAccess Evaluate(ProjectRole role, ContentStatus status, bool isContentCreator, bool isAssignee)
    {
        if (status == ContentStatus.Published)
            return ScriptAccess.Locked("Nội dung đã Published nên không thể sửa kịch bản.");

        if (status == ContentStatus.Archived)
            return ScriptAccess.Locked("Nội dung đã Archived nên không thể sửa kịch bản. Hãy khôi phục nội dung trước.");

        if (role is ProjectRole.Owner or ProjectRole.Manager)
            return ScriptAccess.Editable;

        if (role == ProjectRole.Creator && (isContentCreator || isAssignee))
            return ScriptAccess.Editable;

        return ScriptAccess.Denied("Bạn chỉ được sửa kịch bản của nội dung do mình tạo hoặc được giao.");
    }
}
