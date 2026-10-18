using CreatorFlow.Controls.ContentLibrary;
using CreatorFlow.Forms.Board;
using CreatorFlow.Models;
using CreatorFlow.Models.Enums;
using CreatorFlow.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows.Forms;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[DoNotParallelize]
[TestCategory("ContentLibrary")]
public sealed class ContentLibraryTests
{
    [TestMethod]
    public void Library_ShowsArchivedStatusPlatformAndAssignee_AndRaisesDetailRequest()
    {
        RunSta(() =>
        {
            using var library = new ContentLibraryControl("Project Alpha");
            var card = new ContentBoardCard
            {
                ContentId = 901,
                ProjectId = 81,
                Code = "CNT-901",
                Title = "Archived video",
                Status = ContentStatus.Archived,
                Platforms = ["TikTok", "YouTube"],
                Assignees = [new CardAssignee(7, "Creator One", AssignmentStatus.InProgress, 45, null)],
            };

            library.SetContents([card]);
            ContentLibraryRowControl row = Descendants(library).OfType<ContentLibraryRowControl>().Single();
            var labels = Descendants(row).OfType<Label>().Select(label => label.Text).ToArray();
            CollectionAssert.Contains(labels, "CNT-901");
            CollectionAssert.Contains(labels, "Archived video");
            CollectionAssert.Contains(labels, string.Join(" · ", card.Platforms));
            CollectionAssert.Contains(labels, "Creator One");
            Assert.AreEqual(ContentStatus.Archived, row.Content.Status);
            Assert.IsTrue(Descendants(row).OfType<Label>().Any(label => !string.IsNullOrWhiteSpace(label.AccessibleName) && label.AccessibleName != label.Text));

            long? requestedId = null;
            library.OpenContentRequested += id => requestedId = id;
            row.OpenButton.PerformClick();
            Assert.AreEqual(901L, requestedId);
        });
    }

    [TestMethod]
    public void Library_SearchAndStatusFilter_ShowOnlyMatchingContent()
    {
        RunSta(() =>
        {
            using var library = new ContentLibraryControl("Project Alpha");
            library.SetContents(
            [
                new ContentBoardCard { ContentId = 901, Code = "CNT-901", Title = "Archived video", Status = ContentStatus.Archived, Platforms = ["TikTok"] },
                new ContentBoardCard { ContentId = 900, Code = "CNT-900", Title = "New script", Status = ContentStatus.Script, Platforms = ["YouTube"] },
            ]);
            TableLayoutPanel rows = Descendants(library).OfType<TableLayoutPanel>().Single(control => control.Name == "rows");
            TextBox search = Descendants(library).OfType<TextBox>().Single(control => control.Name == "search");
            ComboBox status = Descendants(library).OfType<ComboBox>().Single(control => control.Name == "statusFilter");

            search.Text = "CNT-901";
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual("CNT-901", ((ContentLibraryRowControl)rows.Controls[0]).Content.Code);

            search.Clear();
            status.SelectedIndex = Array.IndexOf(Enum.GetValues<ContentStatus>(), ContentStatus.Archived) + 1;
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual(ContentStatus.Archived, ((ContentLibraryRowControl)rows.Controls[0]).Content.Status);

            status.SelectedIndex = 0;
            ComboBox platform = Descendants(library).OfType<ComboBox>().Single(control => control.Name == "platformFilter");
            platform.SelectedIndex = platform.Items.IndexOf("TikTok");
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual("CNT-901", ((ContentLibraryRowControl)rows.Controls[0]).Content.Code);
        });
    }

    [TestMethod]
    public void Library_LoadingAndFailureStatesAreVisibleAndDoNotShowFalseEmptyResult()
    {
        RunSta(() =>
        {
            using var library = new ContentLibraryControl("Project Alpha");
            Label state = Descendants(library).OfType<Label>().Single(control => control.Name == "stateLabel");

            library.SetLoading();
            Assert.IsTrue(state.Visible);
            StringAssert.Contains(state.Text, "Content");
            Assert.AreEqual(0, library.TotalContentCount);

            library.SetLoadError("API temporarily unavailable");
            Assert.IsTrue(state.Visible);
            Assert.AreEqual("API temporarily unavailable", state.Text);
            Assert.AreEqual(0, library.TotalContentCount);
        });
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (Control descendant in Descendants(child)) yield return descendant;
        }
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception error) { failure = error; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "Content library UI fixture did not finish.");
        if (failure is not null) throw new AssertFailedException(failure.ToString());
    }
}
