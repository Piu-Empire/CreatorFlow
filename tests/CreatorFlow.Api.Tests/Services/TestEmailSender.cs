using CreatorFlow.Api.Services.Auth;
namespace CreatorFlow.Api.Tests.Services;
internal sealed class TestEmailSender : IEmailSender
{
    public bool IsConfigured => true;
    public bool Fail { get; set; }
    public string? VerificationCode { get; private set; }
    public string? ResetCode { get; private set; }
    public Task SendEmailVerificationCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new EmailDeliveryException();
        VerificationCode = code;
        return Task.CompletedTask;
    }
    public Task SendPasswordResetCodeAsync(string email, string code, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new EmailDeliveryException();
        ResetCode = code;
        return Task.CompletedTask;
    }
}
internal sealed class ImmediateClock : TimeProvider
{
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) => new Timer(callback, state);
    private sealed class Timer : ITimer
    {
        private volatile bool _disposed;
        public Timer(TimerCallback callback, object? state) => ThreadPool.QueueUserWorkItem(_ => { if (!_disposed) callback(state); });
        public bool Change(TimeSpan dueTime, TimeSpan period) => !_disposed;
        public void Dispose() => _disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
