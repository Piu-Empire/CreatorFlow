namespace CreatorFlow.AI;

public static class DotEnv
{
    public static void Load(string? filePath = null)
    {
        var path = filePath ?? FindDotEnv();
        if (path is null || !File.Exists(path))
        {
            return;
        }

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim().Trim('"', '\'');

            if (key.Length == 0)
            {
                continue;
            }

            // Không đè biến môi trường đã có.
            if (string.IsNullOrEmpty(
                    Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    private static string? FindDotEnv()
    {
        // Chạy từ bin/Debug -> trèo lên tới thư mục chứa .env
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
