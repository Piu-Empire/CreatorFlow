using CreatorFlow.Data;
using CreatorFlow.Forms.Board;
using CreatorFlow.Repositories.Implementations;
using CreatorFlow.Repositories.InMemory;
using CreatorFlow.Repositories.Interfaces;
using CreatorFlow.Services;

namespace CreatorFlow;

static class Program
{
    /// <summary>
    /// true  = kết nối PostgreSQL thật (cần đã chạy Database/schema.sql + sửa Data/DbConfig.cs).
    /// false = chạy tạm với dữ liệu trong RAM (Repositories/InMemory), dùng khi CHƯA có DB
    ///         để vẫn xem/test được giao diện Board + luồng Workflow.
    /// Đổi lại true khi đã setup xong PostgreSQL.
    /// </summary>
    private const bool UseDatabase = false;

    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        IUnitOfWork uow;
        IContentRepository contentRepo;
        IContentStatusHistoryRepository historyRepo;
        IReviewRepository reviewRepo;
        IProjectMemberRepository memberRepo;
        IBoardRepository boardRepo;
        IReviewQueueRepository reviewQueueRepo;
        IActivityRepository activityRepo;

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
        }

        var workflowService = new WorkflowService(contentRepo, historyRepo, reviewRepo, memberRepo, uow);

        // TODO: thay bằng màn Login + chọn Project thật (mục 18 UX spec: Login → Select Project).
        // Tạm hardcode User #1 / Project #1 — khớp seed data ở cả schema.sql lẫn InMemoryDataStore.
        CurrentSession.CurrentUserId = 1;
        CurrentSession.CurrentUserName = "Demo Owner";
        CurrentSession.CurrentProjectId = 1;
        CurrentSession.CurrentProjectName = "Creator Team";

        if (args.Length > 0 && args[0] == "--test")
        {
            using var testForm = new BoardForm(workflowService, boardRepo, reviewQueueRepo, activityRepo, memberRepo);
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

        Application.Run(new BoardForm(workflowService, boardRepo, reviewQueueRepo, activityRepo, memberRepo));
    }
}