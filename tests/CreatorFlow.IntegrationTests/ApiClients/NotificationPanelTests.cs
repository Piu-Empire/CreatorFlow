using System.Drawing;
using System.Windows.Forms;
using CreatorFlow.Controls;
using CreatorFlow.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CreatorFlow.IntegrationTests.ApiClients;

[TestClass]
[DoNotParallelize]
[TestCategory("ProjectShell")]
public sealed class NotificationPanelTests
{
    [TestMethod]
    public void Constructor_EmptyItemsAndResize_ShowEmptyStateWithoutThrowing()
    {
        RunSta(() =>
        {
            using var panel = new NotificationPanelControl();
            panel.CreateControl();
            panel.SetItems(Array.Empty<NotificationItem>());
            panel.Size = new Size(480, 460);
            panel.PerformLayout();

            var heading = Descendants(panel).OfType<Label>()
                .Single(label => label.Text == "Notifications");
            Assert.IsTrue(heading.Height >= heading.Font.Height + heading.Padding.Vertical,
                "Notification heading must have room for its font and vertical padding.");

            Assert.IsTrue(Descendants(panel).OfType<Label>()
                .Single(label => label.Text == "No notifications.").Visible);
            Assert.AreEqual(0, Descendants(panel).Count(control => control.Tag is long));
        });
    }

    [TestMethod]
    public void PopulatedItems_ResizeKeepsRowsWithinHost_ThenCanReturnToEmptyState()
    {
        RunSta(() =>
        {
            using var panel = new NotificationPanelControl();
            panel.CreateControl();
            panel.SetItems(new[]
            {
                new NotificationItem { NotificationId = 1, Title = "Assignment", Message = "Fixture", CreatedAt = new DateTime(2026, 10, 27) },
                new NotificationItem { NotificationId = 2, Title = "Approved", IsRead = true, CreatedAt = new DateTime(2026, 10, 27) },
            });

            foreach (int width in new[] { 480, 300 })
            {
                panel.Size = new Size(width, 460);
                panel.PerformLayout();
                var rows = Descendants(panel).Where(control => control.Tag is long).ToArray();
                Assert.AreEqual(2, rows.Length);
                foreach (Control row in rows)
                {
                    Assert.IsNotNull(row.Parent);
                    Assert.AreEqual(row.Parent.ClientSize.Width, row.Width);
                }
            }

            panel.SetItems(Array.Empty<NotificationItem>());
            Assert.IsTrue(Descendants(panel).OfType<Label>()
                .Single(label => label.Text == "No notifications.").Visible);
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
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(15)), "Notification panel UI fixture did not finish.");
        if (failure is not null) throw new AssertFailedException(failure.ToString());
    }
}
