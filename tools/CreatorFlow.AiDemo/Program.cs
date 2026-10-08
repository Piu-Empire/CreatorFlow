using CreatorFlow.AI;

namespace CreatorFlow.AiDemo;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new AiTestForm());
    }
}
