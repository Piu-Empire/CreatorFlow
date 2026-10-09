using CreatorFlow.Data;
using CreatorFlow.Forms.Board;
using CreatorFlow.Repositories.Implementations;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;
using CreatorFlow.Services.AI;
using CreatorFlow.ApiClients;
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
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        // Toàn bộ UI vẽ tay theo pixel cố định (thiết kế ở 100%). Chạy DPI-unaware để layout không bị scale kép
        // khi Windows để 125%/150%. Phải gọi TRƯỚC ApplicationConfiguration.Initialize().
        Application.SetHighDpiMode(HighDpiMode.DpiUnaware);
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
        }

        var workflowService = new WorkflowService(contentRepo, historyRepo, reviewRepo, memberRepo, uow);
        var contentService = new ContentService(contentRepo, detailsRepo, historyRepo, memberRepo, platformRepo, uow, myTaskRepo);
        var myTaskService = new MyTaskService(myTaskRepo, memberRepo, uow);
        // SCRUM-33: AI chưa có cài đặt thật nên dùng NotConfiguredAiService; thay bằng AIService thật khi có.
        var scriptService = new ScriptService(scriptRepo, detailsRepo, platformRepo, memberRepo, myTaskRepo, uow, new NotConfiguredAiService());

        // AuthenticationApplicationContext binds the authenticated user; project selection belongs to SCRUM-25.
        CurrentSession.CurrentUserId = 0;
        CurrentSession.CurrentUserName = "Demo Owner";
        CurrentSession.CurrentProjectId = 1;
        CurrentSession.CurrentProjectName = "Creator Team";

        if (args.Length > 0 && args[0] == "--test")
        {
            using var testForm = new BoardForm(workflowService, boardRepo, reviewQueueRepo, activityRepo, memberRepo, contentService, myTaskService, scriptService);
            var handle = testForm.Handle;
            testForm.Size = new Size(1440, 900);
            testForm.PerformLayout();

            Console.WriteLine("TEST_OK: BoardForm created and loaded successfully. Handle: " + handle);
            foreach (Control c in testForm.Controls)
            {
                Console.WriteLine($"Control: {c.GetType().Name}, Bounds: {c.Bounds}, Dock: {c.Dock}");
            }
            return;
        }

        try
        {
            using var api = ApiClient.Create(ApiClientConfiguration.Load());
            var session = new UserSession();
            var auth = new AuthApiFacade(api, session);
            using var context = new AuthenticationApplicationContext(auth, () =>
                new BoardForm(workflowService, boardRepo, reviewQueueRepo, activityRepo, memberRepo, contentService, myTaskService, scriptService, auth));
            Application.Run(context);
        }
        catch (InvalidOperationException)
        {
            MessageBox.Show("Không thể đọc cấu hình API. Vui lòng kiểm tra Api:BaseUrl.", "CreatorFlow", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}