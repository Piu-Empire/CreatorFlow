using System;

namespace CreatorFlow.Services.Exceptions;

/// <summary>Ném ra khi User không có quyền (Role) thực hiện thao tác Workflow đang yêu cầu.</summary>
public class UnauthorizedWorkflowActionException : Exception
{
    public UnauthorizedWorkflowActionException(string message) : base(message)
    {
    }
}