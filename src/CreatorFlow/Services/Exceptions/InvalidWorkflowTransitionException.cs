using System;
using CreatorFlow.Models;

namespace CreatorFlow.Services.Exceptions;

/// <summary>Ném ra khi cố chuyển Content sang 1 Status không hợp lệ theo luồng chuẩn.</summary>
public class InvalidWorkflowTransitionException : Exception
{
    public ContentStatus From { get; }
    public ContentStatus To { get; }

    public InvalidWorkflowTransitionException(ContentStatus from, ContentStatus to)
        : base($"Không thể chuyển trạng thái từ {from} sang {to}.")
    {
        From = from;
        To = to;
    }
}