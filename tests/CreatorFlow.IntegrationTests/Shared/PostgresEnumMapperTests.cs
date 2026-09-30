using CreatorFlow.Data;
using CreatorFlow.Models.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.Shared;

[TestClass]
public sealed class PostgresEnumMapperTests
{
    [TestMethod]
    public void SchemaEnumValues_RoundTrip()
    {
        AssertRoundTrip<AccountStatus>();
        AssertRoundTrip<ProjectStatus>();
        AssertRoundTrip<ProjectRole>();
        AssertRoundTrip<InvitationStatus>();
        AssertRoundTrip<IdeaStatus>();
        AssertRoundTrip<Priority>();
        AssertRoundTrip<ContentStatus>();
        AssertRoundTrip<PublicationStatus>();
        AssertRoundTrip<AssignmentStatus>();
        AssertRoundTrip<ReviewStatus>();
        AssertRoundTrip<RelationType>();
        AssertRoundTrip<NotificationType>();
        AssertRoundTrip<SubscriptionStatus>();
        AssertRoundTrip<ReportStatus>();
        AssertRoundTrip<AiRequestStatus>();
        AssertRoundTrip<AiRequestType>();
    }

    [TestMethod]
    public void ParseContentStatus_WithUnknownValue_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => PostgresEnumMapper.Parse<ContentStatus>("UNKNOWN"));
    }

    [TestMethod]
    public void ToDatabaseValue_WithUnknownPriority_Throws()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PostgresEnumMapper.ToDatabaseValue((Priority)999));
    }

    [TestMethod]
    public void MultiWordValues_MapToUpperSnakeCase()
    {
        Assert.AreEqual(
            "REVIEW_SUBMITTED",
            PostgresEnumMapper.ToDatabaseValue(NotificationType.ReviewSubmitted));
        Assert.AreEqual(
            "PERFORMANCE_ANALYSIS",
            PostgresEnumMapper.ToDatabaseValue(AiRequestType.PerformanceAnalysis));
    }

    private static void AssertRoundTrip<TEnum>()
        where TEnum : struct, Enum
    {
        foreach (TEnum value in Enum.GetValues<TEnum>())
        {
            string databaseValue = PostgresEnumMapper.ToDatabaseValue(value);
            Assert.AreEqual(value, PostgresEnumMapper.Parse<TEnum>(databaseValue));
        }
    }
}
