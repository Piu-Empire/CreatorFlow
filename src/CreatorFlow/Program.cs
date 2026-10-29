using CreatorFlow.Data;
using CreatorFlow.AI;
using CreatorFlow.Forms.Board;
using CreatorFlow.Repositories.Implementations;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
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
    /// true  = kết nối PostgreSQL thật (cần đã chạy database/01_schema.sql + 02_seed.sql và đặt ConnectionStrings:CreatorFlow trong appsettings.Local.json).
    /// false = chạy tạm với dữ liệu trong RAM (Repositories/InMemory), dùng khi CHƯA có DB
    ///         để vẫn xem/test được giao diện Board + luồng Workflow.
    /// Đổi lại true khi đã setup xong PostgreSQL.
    /// </summary>
    private static readonly bool UseDatabase = false;

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

        IUnitOfWork uow;
        IContentRepository contentRepo;
        IContentStatusHistoryRepository historyRepo;
        IReviewRepository reviewRepo;
        IProjectMemberRepository memberRepo;
        IBoardRepository boardRepo;
        IReviewQueueRepository reviewQueueRepo;
        IActivityRepository activityRepo;
        IContentDetailsRepository detailsRepo;
        IMyTaskRepository myTaskRepo;
        IPlatformRepository platformRepo;
        IContentScriptRepository scriptRepo;
        IIdeaRepository ideaRepo;

        if (UseDatabase)
        {
            var npgsqlUow = new NpgsqlUnitOfWork(DbConfig.ConnectionString);
            uow = npgsqlUow;
            contentRepo = new ContentRepository(npgsqlUow);
            historyRepo = new ContentStatusHistoryRepository(npgsqlUow);
            reviewRepo = new ReviewRepository(npgsqlUow);
            memberRepo = new ProjectMemberRepository(npgsqlUow);
            boardRepo = new BoardRepository(npgsqlUow);
            reviewQueueRepo = new ReviewQueueRepository(npgsqlUow);
            activityRepo = new ActivityRepository(npgsqlUow);
            detailsRepo = new ContentDetailsRepository(npgsqlUow);
            myTaskRepo = new MyTaskRepository(npgsqlUow);
            platformRepo = new PlatformRepository(npgsqlUow);
            scriptRepo = new ContentScriptRepository(npgsqlUow);
            ideaRepo = new IdeaRepository(npgsqlUow);
        }
        else
        {
            uow = new InMemoryUnitOfWork();
            contentRepo = new InMemoryContentRepository();
            historyRepo = new InMemoryContentStatusHistoryRepository();
            reviewRepo = new InMemoryReviewRepository();
            memberRepo = new InMemoryProjectMemberRepository();
            boardRepo = new InMemoryBoardRepository();
            reviewQueueRepo = new InMemoryReviewQueueRepository();
            activityRepo = new InMemoryActivityRepository();
            detailsRepo = new InMemoryContentDetailsRepository();
            myTaskRepo = new InMemoryMyTaskRepository();
            platformRepo = new InMemoryPlatformRepository();
            scriptRepo = new InMemoryContentScriptRepository();
            ideaRepo = new InMemoryIdeaRepository(withSeed: true);
        }

        var workflowService = new WorkflowService(contentRepo, historyRepo, reviewRepo, memberRepo, uow);
        var contentService = new ContentService(contentRepo, detailsRepo, historyRepo, memberRepo, platformRepo, uow, myTaskRepo);
        var myTaskService = new MyTaskService(myTaskRepo, memberRepo, uow);
        var ideaService = new IdeaService(ideaRepo, memberRepo, historyRepo, uow);
        // SCRUM-57/58: lõi AI client đọc cấu hình từ .env ở client — POC, sẽ chuyển sang Backend sau.
        var aiService = AIFactory.Create();
        // SCRUM-59: AI đề xuất bản repurpose theo platform đích (dùng chung lõi AI client).
        var repurposeAi = new RepurposeAiService(aiService);
        // SCRUM-33 + SCRUM-58: cổng AI cho script dùng ScriptAiService (adapter sang lõi AI client).
        var scriptService = new ScriptService(scriptRepo, detailsRepo, platformRepo, memberRepo, myTaskRepo, uow, new ScriptAiService(aiService));

        // AuthenticationApplicationContext binds the authenticated user; project selection belongs to SCRUM-25.
        CurrentSession.CurrentUserId = 0;
        CurrentSession.CurrentUserName = string.Empty;
        CurrentSession.CurrentProjectId = 0;
        CurrentSession.CurrentProjectName = string.Empty;

        try
        {
            using var api = ApiClient.Create(ApiClientConfiguration.Load());
            var session = new UserSession();
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
                using var testForm = new BoardForm(workflowService, boardRepo, reviewQueueRepo,
                    activityRepo, memberRepo, contentService, myTaskService, scriptService, ideaService, auth, metricBackend, invitations, invitationInbox, reviewQueueBackend: reviewQueueBackend, notificationBackend: notificationBackend, statisticsBackend: statisticsBackend, repurposeAi: repurposeAi);
                var handle = testForm.Handle;
                testForm.Size = new Size(1440, 900);
                testForm.PerformLayout();
                Console.WriteLine("LAYOUT_OK: no-project Board shell created. Handle: " + handle);
                return;
            }
            using var context = new AuthenticationApplicationContext(auth, () =>
                new BoardForm(workflowService, boardRepo, reviewQueueRepo, activityRepo, memberRepo, contentService, myTaskService, scriptService, ideaService, auth, metricBackend, invitations, invitationInbox, projects: projects, taskBackend: taskBackend, ideaBackend: ideaBackend, boardBackend: boardBackend, detailBackend: detailBackend, assignmentBackend: assignmentBackend, reviewQueueBackend: reviewQueueBackend, notificationBackend: notificationBackend, statisticsBackend: statisticsBackend, aiService: aiService, repurposeAi: repurposeAi), projects, invitationInbox);
            Application.Run(context);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("Không thể đọc cấu hình API. Vui lòng kiểm tra Api:BaseUrl.", "CreatorFlow", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
