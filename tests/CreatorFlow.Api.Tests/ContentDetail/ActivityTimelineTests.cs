using CreatorFlow.Contracts.ContentDetail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.Api.Tests.ContentDetail;

/// <summary>
/// SCRUM-46 Lịch sử Review và Status: gộp dòng trạng thái của luồng Review với dòng Review
/// để timeline không bị trùng, đồng thời giữ đủ old/new status, actor, thời gian và feedback.
/// </summary>
[TestClass]
public sealed class ActivityTimelineTests
{
    private static readonly DateTime At = new(2026, 10, 19, 9, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void Compose_MergesSubmitStatusRowIntoReviewEvent_AndKeepsSubmitNote()
    {
        var entries = new[]
        {
            new ActivityItemResponse(At, "Creator A", "Editing → Review", "kịch bản đã xong",
                ActivityKinds.StatusChange, "Editing", "Review"),
            new ActivityItemResponse(At, "Creator A", "submitted Review #1", null,
                ActivityKinds.ReviewSubmitted, "Editing", "Review", 1, "Pending"),
        };

        var result = ActivityTimeline.Compose(entries);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(ActivityKinds.ReviewSubmitted, result[0].Kind);
        Assert.AreEqual("Editing", result[0].FromStatus);
        Assert.AreEqual("Review", result[0].ToStatus);
        Assert.AreEqual(1, result[0].ReviewNo);
        Assert.AreEqual("kịch bản đã xong", result[0].Feedback);
        Assert.AreEqual("Creator A", result[0].ActorName);
        Assert.AreEqual(At, result[0].Timestamp);
    }

    [TestMethod]
    public void Compose_MergesDecisionRow_AndKeepsReviewFeedback()
    {
        var decidedAt = At.AddHours(2);
        var entries = new[]
        {
            new ActivityItemResponse(At, "Creator A", "Editing → Review", null,
                ActivityKinds.StatusChange, "Editing", "Review"),
            new ActivityItemResponse(At, "Creator A", "submitted Review #1", null,
                ActivityKinds.ReviewSubmitted, "Editing", "Review", 1, "Pending"),
            new ActivityItemResponse(decidedAt, "Owner", "Review → Editing", "cần sửa hook",
                ActivityKinds.StatusChange, "Review", "Editing"),
            new ActivityItemResponse(decidedAt, "Owner", "rejected Review #1", "cần sửa hook",
                ActivityKinds.ReviewDecided, "Review", "Editing", 1, "Rejected"),
        };

        var result = ActivityTimeline.Compose(entries);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(ActivityKinds.ReviewSubmitted, result[0].Kind);
        Assert.AreEqual(ActivityKinds.ReviewDecided, result[1].Kind);
        Assert.AreEqual("Rejected", result[1].ReviewStatus);
        Assert.AreEqual("Review", result[1].FromStatus);
        Assert.AreEqual("Editing", result[1].ToStatus);
        Assert.AreEqual("cần sửa hook", result[1].Feedback);
        Assert.AreEqual("Owner", result[1].ActorName);
    }

    [TestMethod]
    public void Compose_KeepsPlainStatusChanges_SortsAscending_AndDoesNotMergeOtherActors()
    {
        var entries = new[]
        {
            new ActivityItemResponse(At, "Creator A", "submitted Review #1", null,
                ActivityKinds.ReviewSubmitted, "Editing", "Review", 1, "Pending"),
            new ActivityItemResponse(At.AddHours(3), "Creator A", "Ready → Published", null,
                ActivityKinds.StatusChange, "Ready", "Published"),
            new ActivityItemResponse(At.AddHours(1), "Manager B", "Review → Ready", null,
                ActivityKinds.StatusChange, "Review", "Ready"),
        };

        var result = ActivityTimeline.Compose(entries);

        // Dòng "Review → Ready" của Manager B không khớp actor với Review của Creator A nên vẫn được giữ.
        Assert.AreEqual(3, result.Count);
        CollectionAssert.AreEqual(
            new[] { At, At.AddHours(1), At.AddHours(3) },
            result.Select(i => i.Timestamp).ToArray());
        Assert.AreEqual(ActivityKinds.StatusChange, result[1].Kind);
    }
}
