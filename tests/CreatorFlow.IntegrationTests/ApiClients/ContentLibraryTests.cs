using CreatorFlow.Controls.ContentLibrary;
using CreatorFlow.Contracts.Board;
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
            status.SelectedItem = FindItem(status, "Đã lưu trữ");
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual(ContentStatus.Archived, ((ContentLibraryRowControl)rows.Controls[0]).Content.Status);

            status.SelectedIndex = 0;
            ComboBox platform = Descendants(library).OfType<ComboBox>().Single(control => control.Name == "platformFilter");
            platform.SelectedItem = FindItem(platform, "TikTok");
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual("CNT-901", ((ContentLibraryRowControl)rows.Controls[0]).Content.Code);
        });
    }

    [TestMethod]
    public void Library_CombinesLiveProjectFiltersAndDeadlineRange()
    {
        RunSta(() =>
        {
            using var library = new ContentLibraryControl("Project Alpha");
            library.SetContents(
            [
                new ContentBoardCard
                {
                    ContentId = 901, Code = "CNT-901", Title = "Weekly education video", Status = ContentStatus.Ready,
                    Priority = Priority.High, Platforms = ["YouTube"], Tags = ["education"], Deadline = new DateTime(2026, 11, 10),
                    Assignees = [new CardAssignee(7, "Creator One", AssignmentStatus.InProgress, 45, null)],
                },
                new ContentBoardCard
                {
                    ContentId = 902, Code = "CNT-902", Title = "Weekly education draft", Status = ContentStatus.Ready,
                    Priority = Priority.High, Platforms = ["YouTube"], Tags = ["education"], Deadline = new DateTime(2026, 11, 10),
                    Assignees = [new CardAssignee(8, "Creator Two", AssignmentStatus.InProgress, 10, null)],
                },
                new ContentBoardCard
                {
                    ContentId = 903, Code = "CNT-903", Title = "Weekly education video", Status = ContentStatus.Script,
                    Priority = Priority.High, Platforms = ["TikTok"], Tags = ["education"], Deadline = new DateTime(2026, 11, 10),
                    Assignees = [new CardAssignee(7, "Creator One", AssignmentStatus.InProgress, 45, null)],
                },
            ]);

            ComboBox status = Combo(library, "statusFilter");
            ComboBox platform = Combo(library, "platformFilter");
            ComboBox creator = Combo(library, "creatorFilter");
            ComboBox priority = Combo(library, "priorityFilter");
            ComboBox deadlineMode = Combo(library, "deadlineModeFilter");
            ComboBox tag = Combo(library, "tagFilter");
            TextBox search = Descendants(library).OfType<TextBox>().Single(control => control.Name == "search");
            DateTimePicker from = Descendants(library).OfType<DateTimePicker>().Single(control => control.Name == "deadlineFromFilter");
            DateTimePicker to = Descendants(library).OfType<DateTimePicker>().Single(control => control.Name == "deadlineToFilter");

            search.Text = "weekly education video";
            status.SelectedItem = FindItem(status, "Sẵn sàng");
            platform.SelectedItem = FindItem(platform, "YouTube");
            creator.SelectedItem = FindItem(creator, "Creator One");
            priority.SelectedItem = FindItem(priority, "Cao");
            deadlineMode.SelectedItem = FindItem(deadlineMode, "Có deadline");
            from.Checked = true;
            from.Value = new DateTime(2026, 11, 1);
            to.Checked = true;
            to.Value = new DateTime(2026, 11, 30);
            tag.SelectedItem = FindItem(tag, "education");

            TableLayoutPanel rows = Descendants(library).OfType<TableLayoutPanel>().Single(control => control.Name == "rows");
            Assert.AreEqual(1, rows.Controls.Count);
            Assert.AreEqual(901L, ((ContentLibraryRowControl)rows.Controls[0]).Content.ContentId);
            CollectionAssert.Contains(tag.Items.Cast<object>().Select(item => item.ToString()).ToArray(), "education");
        });
    }

    [TestMethod]
    public void Library_SortsByTitleInBothDirections()
    {
        RunSta(() =>
        {
            using var library = new ContentLibraryControl("Project Alpha");
            library.SetContents(
            [
                new ContentBoardCard { ContentId = 3, Code = "CNT-003", Title = "Charlie" },
                new ContentBoardCard { ContentId = 1, Code = "CNT-001", Title = "Alpha" },
                new ContentBoardCard { ContentId = 2, Code = "CNT-002", Title = "Bravo" },
            ]);
            ComboBox sort = Combo(library, "sortFilter");
            sort.SelectedItem = FindItem(sort, "Tiêu đề");
            TableLayoutPanel rows = Descendants(library).OfType<TableLayoutPanel>().Single(control => control.Name == "rows");

            CollectionAssert.AreEqual(new[] { "Alpha", "Bravo", "Charlie" }, rows.Controls.Cast<ContentLibraryRowControl>().Select(row => row.Content.Title).ToArray());
            Descendants(library).OfType<Button>().Single(button => button.Name == "sortDirection").PerformClick();
            CollectionAssert.AreEqual(new[] { "Charlie", "Bravo", "Alpha" }, rows.Controls.Cast<ContentLibraryRowControl>().Select(row => row.Content.Title).ToArray());
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

    private static ComboBox Combo(Control parent, string name) =>
        Descendants(parent).OfType<ComboBox>().Single(control => control.Name == name);

    private static object FindItem(ComboBox combo, string display) =>
        combo.Items.Cast<object>().Single(item => item.ToString() == display);

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
