using CreatorFlow.Api.Models.Search;
using CreatorFlow.Data;
using Npgsql;
using NpgsqlTypes;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CreatorFlow.Api.Repositories.Search;

/// <summary>
/// Tìm các mục thuộc Project mà người gọi đang là thành viên. Quyền truy cập được giới hạn
/// ngay trong câu truy vấn, không tải toàn bộ dữ liệu về client để lọc.
/// </summary>
public sealed class GlobalSearchRepository(IDbConnectionFactory connectionFactory) : IGlobalSearchRepository
{
    private const string VietnameseAccentSource = "áàảãạăắằẳẵặâấầẩẫậđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ";
    private static readonly string VietnameseAccentTarget =
        new string('a', 17) + "d" + new string('e', 11) + new string('i', 5) +
        new string('o', 17) + new string('u', 11) + new string('y', 5);

    private static readonly string Sql = $"""
        WITH authorized_projects AS (
            SELECT p.project_id, p.project_name
            FROM projects p
            JOIN project_members pm ON pm.project_id = p.project_id
            WHERE pm.user_id = @user_id
              AND pm.is_active
              AND p.status = 'ACTIVE'
        ), matches AS (
            SELECT
                'Project'::text AS entity_type,
                p.project_id AS entity_id,
                p.project_id,
                p.project_name,
                p.project_name AS title,
                'Project'::text AS subtitle,
                p.status::text AS status,
                CASE WHEN {NormalizeSql("p.project_name")} = @normalized_query THEN 0
                     WHEN LEFT({NormalizeSql("p.project_name")}, CHAR_LENGTH(@normalized_query)) = @normalized_query THEN 1
                     ELSE 2 + CARDINALITY(@fuzzy_grams) - (
                         SELECT COUNT(*) FROM UNNEST(@fuzzy_grams::text[]) AS gram(value)
                         WHERE STRPOS({NormalizeSql("p.project_name")}, gram.value) > 0
                     ) END AS relevance,
                0 AS type_order,
                p.updated_at AS modified_at
            FROM authorized_projects ap
            JOIN projects p ON p.project_id = ap.project_id
            WHERE p.project_name ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(p.description, '') ILIKE @pattern ESCAPE CHR(92)
               OR EXISTS (SELECT 1 FROM users u WHERE u.user_id = p.owner_id
                          AND u.display_name ILIKE @pattern ESCAPE CHR(92))
               OR {HasFuzzyMatchSql("concat_ws(' ', p.project_name, p.description, (SELECT u.display_name FROM users u WHERE u.user_id = p.owner_id))")}

            UNION ALL

            SELECT
                'Content'::text,
                c.content_id,
                c.project_id,
                ap.project_name,
                c.title,
                'CNT-' || LPAD(c.content_id::text, 3, '0') || ' · ' ||
                    REPLACE(c.status::text, '_', ' ') || ' · ' || REPLACE(c.priority::text, '_', ' '),
                c.status::text,
                CASE WHEN {NormalizeSql("c.title")} = @normalized_query THEN 0
                     WHEN LEFT({NormalizeSql("c.title")}, CHAR_LENGTH(@normalized_query)) = @normalized_query THEN 1
                     ELSE 2 + CARDINALITY(@fuzzy_grams) - (
                         SELECT COUNT(*) FROM UNNEST(@fuzzy_grams::text[]) AS gram(value)
                         WHERE STRPOS({NormalizeSql("c.title")}, gram.value) > 0
                     ) END,
                1,
                c.updated_at
            FROM contents c
            JOIN authorized_projects ap ON ap.project_id = c.project_id
            WHERE c.title ILIKE @pattern ESCAPE CHR(92)
               OR ('CNT-' || LPAD(c.content_id::text, 3, '0')) ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(c.description, '') ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(c.script, '') ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(c.content_type, '') ILIKE @pattern ESCAPE CHR(92)
               OR c.status::text ILIKE @pattern ESCAPE CHR(92)
               OR c.priority::text ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(c.deadline::date::text, '') ILIKE @pattern ESCAPE CHR(92)
               OR EXISTS (SELECT 1 FROM content_tags ct JOIN tags t ON t.tag_id = ct.tag_id
                          WHERE ct.content_id = c.content_id AND t.name ILIKE @pattern ESCAPE CHR(92))
               OR EXISTS (SELECT 1 FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
                          WHERE cp.content_id = c.content_id AND p.name ILIKE @pattern ESCAPE CHR(92))
               OR EXISTS (SELECT 1 FROM content_assignments ca JOIN users u ON u.user_id = ca.assignee_id
                          WHERE ca.content_id = c.content_id AND ca.status <> 'CANCELLED'
                            AND u.display_name ILIKE @pattern ESCAPE CHR(92))
               OR EXISTS (SELECT 1 FROM users u WHERE u.user_id = c.created_by
                          AND u.display_name ILIKE @pattern ESCAPE CHR(92))
               OR EXISTS (SELECT 1 FROM content_attachments a
                          WHERE a.content_id = c.content_id
                            AND (a.display_name ILIKE @pattern ESCAPE CHR(92)
                                 OR a.resource_url ILIKE @pattern ESCAPE CHR(92)))
               OR {HasFuzzyMatchSql("concat_ws(' ', c.title, 'CNT-' || LPAD(c.content_id::text, 3, '0'), c.description, c.script, c.content_type, c.status::text, c.priority::text, c.deadline::date::text, (SELECT string_agg(t.name, ' ') FROM content_tags ct JOIN tags t ON t.tag_id = ct.tag_id WHERE ct.content_id = c.content_id), (SELECT string_agg(p.name, ' ') FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id WHERE cp.content_id = c.content_id), (SELECT string_agg(u.display_name, ' ') FROM content_assignments ca JOIN users u ON u.user_id = ca.assignee_id WHERE ca.content_id = c.content_id AND ca.status <> 'CANCELLED'), (SELECT u.display_name FROM users u WHERE u.user_id = c.created_by), (SELECT string_agg(concat_ws(' ', a.display_name, a.resource_url), ' ') FROM content_attachments a WHERE a.content_id = c.content_id))")}

            UNION ALL

            SELECT
                'Idea'::text,
                i.idea_id,
                i.project_id,
                ap.project_name,
                i.title,
                'IDEA-' || LPAD(i.idea_id::text, 3, '0') || ' · ' || REPLACE(i.status::text, '_', ' '),
                i.status::text,
                CASE WHEN {NormalizeSql("i.title")} = @normalized_query THEN 0
                     WHEN LEFT({NormalizeSql("i.title")}, CHAR_LENGTH(@normalized_query)) = @normalized_query THEN 1
                     ELSE 2 + CARDINALITY(@fuzzy_grams) - (
                         SELECT COUNT(*) FROM UNNEST(@fuzzy_grams::text[]) AS gram(value)
                         WHERE STRPOS({NormalizeSql("i.title")}, gram.value) > 0
                     ) END,
                2,
                i.updated_at
            FROM ideas i
            JOIN authorized_projects ap ON ap.project_id = i.project_id
            WHERE i.title ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(i.description, '') ILIKE @pattern ESCAPE CHR(92)
               OR COALESCE(i.note, '') ILIKE @pattern ESCAPE CHR(92)
               OR ('IDEA-' || LPAD(i.idea_id::text, 3, '0')) ILIKE @pattern ESCAPE CHR(92)
               OR i.status::text ILIKE @pattern ESCAPE CHR(92)
               OR EXISTS (SELECT 1 FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id
                          WHERE it.idea_id = i.idea_id AND t.name ILIKE @pattern ESCAPE CHR(92))
               OR EXISTS (SELECT 1 FROM users u WHERE u.user_id = i.created_by
                          AND u.display_name ILIKE @pattern ESCAPE CHR(92))
               OR {HasFuzzyMatchSql("concat_ws(' ', i.title, 'IDEA-' || LPAD(i.idea_id::text, 3, '0'), i.description, i.note, i.status::text, (SELECT string_agg(t.name, ' ') FROM idea_tags it JOIN tags t ON t.tag_id = it.tag_id WHERE it.idea_id = i.idea_id), (SELECT u.display_name FROM users u WHERE u.user_id = i.created_by))")}

            UNION ALL

            SELECT
                'Task'::text,
                c.content_id,
                c.project_id,
                ap.project_name,
                c.title,
                'My Task · ' || REPLACE(ca.status::text, '_', ' ') || ' · CNT-' ||
                    LPAD(c.content_id::text, 3, '0'),
                ca.status::text,
                CASE WHEN {NormalizeSql("c.title")} = @normalized_query THEN 0
                     WHEN LEFT({NormalizeSql("c.title")}, CHAR_LENGTH(@normalized_query)) = @normalized_query THEN 1
                     ELSE 2 + CARDINALITY(@fuzzy_grams) - (
                         SELECT COUNT(*) FROM UNNEST(@fuzzy_grams::text[]) AS gram(value)
                         WHERE STRPOS({NormalizeSql("c.title")}, gram.value) > 0
                     ) END,
                3,
                GREATEST(c.updated_at, ca.assigned_at)
            FROM content_assignments ca
            JOIN contents c ON c.content_id = ca.content_id
            JOIN authorized_projects ap ON ap.project_id = c.project_id
            WHERE ca.assignee_id = @user_id
              AND ca.status <> 'CANCELLED'
              AND c.status <> 'ARCHIVED'
              AND (c.title ILIKE @pattern ESCAPE CHR(92)
                   OR ('CNT-' || LPAD(c.content_id::text, 3, '0')) ILIKE @pattern ESCAPE CHR(92)
                   OR COALESCE(c.description, '') ILIKE @pattern ESCAPE CHR(92)
                   OR ca.status::text ILIKE @pattern ESCAPE CHR(92)
                   OR EXISTS (SELECT 1 FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id
                              WHERE cp.content_id = c.content_id AND p.name ILIKE @pattern ESCAPE CHR(92))
                   OR {HasFuzzyMatchSql("concat_ws(' ', c.title, 'CNT-' || LPAD(c.content_id::text, 3, '0'), c.description, ca.status::text, (SELECT string_agg(p.name, ' ') FROM content_platforms cp JOIN platforms p ON p.platform_id = cp.platform_id WHERE cp.content_id = c.content_id))")})
        )
        SELECT entity_type, entity_id, project_id, project_name, title, subtitle, status
        FROM matches
        ORDER BY relevance, type_order, modified_at DESC, project_id, entity_id
        LIMIT @limit;
        """;

    private static string NormalizeSql(string expression) =>
        $"TRANSLATE(LOWER(COALESCE({expression}, '')), @accent_source, @accent_target)";

    private static string HasFuzzyMatchSql(string expression) =>
        $"(SELECT COUNT(*) FROM UNNEST(@fuzzy_grams::text[]) AS fuzzy_gram(value) " +
        $"WHERE STRPOS({NormalizeSql(expression)}, fuzzy_gram.value) > 0) >= @minimum_fuzzy_gram_matches";

    public async Task<IReadOnlyList<GlobalSearchRecord>> SearchAsync(
        long userId, string query, int limit, CancellationToken cancellationToken = default)
    {
        string trimmed = query.Trim();
        string normalized = NormalizeSearchText(trimmed);
        string[] fuzzyGrams = BuildFuzzyGrams(normalized);
        string escaped = EscapeLike(trimmed);
        string pattern = $"%{escaped}%";

        await using NpgsqlConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("user_id", NpgsqlDbType.Bigint, userId);
        command.Parameters.AddWithValue("pattern", NpgsqlDbType.Text, pattern);
        command.Parameters.AddWithValue("normalized_query", NpgsqlDbType.Text, normalized);
        command.Parameters.AddWithValue("fuzzy_grams", NpgsqlDbType.Array | NpgsqlDbType.Text, fuzzyGrams);
        command.Parameters.AddWithValue("minimum_fuzzy_gram_matches", NpgsqlDbType.Integer,
            MinimumFuzzyGramMatches(fuzzyGrams.Length));
        command.Parameters.AddWithValue("accent_source", NpgsqlDbType.Text, VietnameseAccentSource);
        command.Parameters.AddWithValue("accent_target", NpgsqlDbType.Text, VietnameseAccentTarget);
        command.Parameters.AddWithValue("limit", NpgsqlDbType.Integer, limit);

        var results = new List<GlobalSearchRecord>();
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new GlobalSearchRecord(
                Type: reader.GetString(0),
                EntityId: reader.GetInt64(1),
                ProjectId: reader.GetInt64(2),
                ProjectName: reader.GetString(3),
                Title: reader.GetString(4),
                Subtitle: reader.GetString(5),
                Status: reader.GetString(6)));
        }
        return results;
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
             .Replace("%", "\\%", StringComparison.Ordinal)
             .Replace("_", "\\_", StringComparison.Ordinal);

    internal static string NormalizeSearchText(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            result.Append(character switch
            {
                'đ' or 'Đ' => 'd',
                _ => char.ToLowerInvariant(character)
            });
        }
        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    internal static string[] BuildFuzzyGrams(string normalizedQuery)
    {
        var grams = new HashSet<string>(StringComparer.Ordinal);
        string[] terms = Regex.Split(normalizedQuery, @"[^\p{L}\p{N}]+")
            .Where(term => term.Length >= 2)
            .ToArray();

        foreach (string term in terms)
        {
            AddBigrams(term, grams);
            for (int index = 0; index < term.Length; index++)
                AddBigrams(term.Remove(index, 1), grams);
        }

        return grams.ToArray();
    }

    internal static int MinimumFuzzyGramMatches(int fuzzyGramCount) =>
        Math.Max(1, (int)Math.Ceiling(fuzzyGramCount * 0.2));

    private static void AddBigrams(string value, ISet<string> grams)
    {
        for (int index = 0; index < value.Length - 1; index++)
            grams.Add(value.Substring(index, 2));
    }
}
