using CreatorFlow.Api.Models.Metrics;
using CreatorFlow.Api.Repositories.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fixture = CreatorFlow.Api.Tests.Database.ContentReferenceRepositoryIntegrationTests.Fixture;

namespace CreatorFlow.Api.Tests.Database;

[TestClass]
public sealed class MetricSummaryPersistenceTests
{
    [TestMethod]
    public async Task ProjectWithoutMetrics_ReturnsProjectNameAndZeroTotals()
    {
        await using var fixture = await Fixture.CreateAsync();
        var repository = new MetricRepository(fixture.Factory);

        var rows = await repository.GetProjectSummaryAsync(fixture.ProjectId, MetricGroupBy.Project);

        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(fixture.ProjectId, rows[0].Key);
        Assert.AreEqual(await fixture.ScalarAsync<string>(
            "SELECT project_name FROM projects WHERE project_id=@id", ("id", fixture.ProjectId)), rows[0].Name);
        Assert.AreEqual(0L, rows[0].Views);
        Assert.AreEqual(0L, rows[0].Likes);
        Assert.AreEqual(0L, rows[0].Comments);
        Assert.AreEqual(0L, rows[0].Shares);
    }

    [TestMethod]
    public async Task ProjectSummary_UsesLatestSnapshotPerPlatform_AndExcludesOtherProjects()
    {
        await using var fixture = await Fixture.CreateAsync();
        await using var other = await Fixture.CreateAsync();
        var repository = new MetricRepository(fixture.Factory);
        long tikTok = await Link(fixture, "TikTok");
        long youTube = await Link(fixture, "YouTube");
        long otherLink = await Link(other, "TikTok");
        await Snapshot(fixture, tikTok, 100, 10, 2, 3, -1);
        await Snapshot(fixture, tikTok, 200, 20, 4, 6, 0);
        await Snapshot(fixture, youTube, 50, 5, 1, 2, 0);
        await Snapshot(other, otherLink, 9999, 999, 99, 9, 0);

        foreach (var dimension in new[] { MetricGroupBy.Project, MetricGroupBy.Content, MetricGroupBy.Creator, MetricGroupBy.Platform })
        {
            var rows = await repository.GetProjectSummaryAsync(fixture.ProjectId, dimension);
            Assert.AreEqual(250L, rows.Sum(row => row.Views), dimension.ToString());
            Assert.AreEqual(25L, rows.Sum(row => row.Likes), dimension.ToString());
            Assert.AreEqual(5L, rows.Sum(row => row.Comments), dimension.ToString());
            Assert.AreEqual(8L, rows.Sum(row => row.Shares), dimension.ToString());
            if (dimension == MetricGroupBy.Project)
            {
                Assert.AreEqual(1, rows.Count);
                Assert.AreEqual(fixture.ProjectId, rows[0].Key);
                Assert.AreEqual(await fixture.ScalarAsync<string>(
                    "SELECT project_name FROM projects WHERE project_id=@id", ("id", fixture.ProjectId)), rows[0].Name);
            }
        }
    }

    private static Task<long> Link(Fixture fixture, string platform) => fixture.ScalarAsync<long>("""
        INSERT INTO content_platforms(content_id,platform_id)
        SELECT @content,platform_id FROM platforms WHERE name=@platform RETURNING content_platform_id
        """, ("content", fixture.ContentId), ("platform", platform));

    private static Task<long> Snapshot(Fixture fixture, long link, long views, long likes, long comments, long shares, int days) =>
        fixture.ScalarAsync<long>("""
            INSERT INTO content_metrics(content_platform_id,views,likes,comments,shares,recorded_at)
            VALUES(@link,@views,@likes,@comments,@shares,CURRENT_TIMESTAMP + @days * INTERVAL '1 day')
            RETURNING metric_id
            """, ("link", link), ("views", views), ("likes", likes), ("comments", comments), ("shares", shares), ("days", days));
}
