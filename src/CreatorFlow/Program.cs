using CreatorFlow.AI;
using CreatorFlow.Forms.Board;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.ApiClients;
using CreatorFlow.Services.Backends;
using System.Runtime.Versioning;

namespace CreatorFlow;

[SupportedOSPlatform("windows")]
static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // Render directly at the monitor's DPI so Windows does not bitmap-scale the whole UI.
        // Set this before ApplicationConfiguration.Initialize().
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        ApplicationConfiguration.Initialize();

        // AuthenticationApplicationContext binds the authenticated user; project selection belongs to SCRUM-25.
        CurrentSession.CurrentUserId = 0;
        CurrentSession.CurrentUserName = string.Empty;
        CurrentSession.CurrentProjectId = 0;
        CurrentSession.CurrentProjectName = string.Empty;

        try
        {
            using var api = ApiClient.Create(ApiClientConfiguration.Load());
            var session = new UserSession();
            AIOptions aiOptions = AIFactory.FromEnvironment();
            var aiService = new AIService(new HttpClient(), aiOptions);
            var repurposeAi = new RepurposeAiService(aiService, aiOptions.Validate() is null);
            // SCRUM-60: AI nhận xét hiệu suất từ Metrics đã lưu (cùng lõi AI client).
            var performanceAi = new PerformanceAiService(aiService);
            var scriptService = new ScriptService(api, session, new ScriptAiService(aiService));
            using var projects = new ProjectApiFacade(api, session);
            var taskBackend = new ApiMyTaskBackend(api, session);
            var auth = new AuthApiFacade(api, session); var metricBackend = new ApiMetricBackend(api, session);
            var statisticsBackend = new ApiStatisticsBackend(api, session); // SCRUM-55
            var ideaBackend = new ApiIdeaBackend(api, session);
            var boardBackend = new ApiBoardBackend(api, session);
            var detailBackend = new ApiContentDetailBackend(api, session);
            var assignmentBackend = new ApiAssignmentBackend(api, session);
            var reviewQueueBackend = new ApiReviewQueueBackend(api, session);
            var notificationBackend = new ApiNotificationBackend(api, session);
            var invitations = new ProjectInvitationService(api, session);
            var invitationInbox = new InvitationInboxService(api, session);
            if (args.Length > 0 && args[0] == "--test")
            {
                // Diagnostic layout only: no authentication, project queries, or message loop.
                using var testForm = new BoardForm();
                var handle = testForm.Handle;
                testForm.Size = new Size(1440, 900);
                testForm.PerformLayout();
                Console.WriteLine("LAYOUT_OK: no-project Board shell created. Handle: " + handle);
                return;
            }
            using var context = new AuthenticationApplicationContext(auth, () =>
                new BoardForm(auth, projects, scriptService, taskBackend, ideaBackend, boardBackend,
                    detailBackend, assignmentBackend, reviewQueueBackend, notificationBackend,
                    metricBackend, invitations, invitationInbox,
                    statisticsBackend, aiService, repurposeAi, performanceAi), projects, invitationInbox);
            Application.Run(context);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("Không thể đọc cấu hình API. Vui lòng kiểm tra Api:BaseUrl.", "CreatorFlow", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
