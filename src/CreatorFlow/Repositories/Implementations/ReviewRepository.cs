using CreatorFlow.Models.Enums;
using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories.Interfaces;
using Npgsql;

namespace CreatorFlow.Repositories.Implementations;

/// <summary>
/// Cài đặt Npgsql cho IReviewRepository (bảng Reviews — mục 5.4 bảng 19).
/// Mỗi Submit Review tạo 1 dòng mới, KHÔNG update đè lên Review cũ (Add luôn INSERT).
/// </summary>
public class ReviewRepository : IReviewRepository
{
    private readonly IDbSession _session;

    public ReviewRepository(IDbSession session) => _session = session;

    public int GetLatestReviewNo(long contentId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT COALESCE(MAX(reviewno), 0) FROM reviews WHERE contentid = @id");
        cmd.Parameters.AddWithValue("id", contentId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public Review GetPendingReview(long contentId)
    {
        using var cmd = _session.CreateCommand(
            "SELECT id, contentid, reviewno, submittedbyuserid, revieweruserid, status, feedback, submittedat, decidedat " +
            "FROM reviews WHERE contentid = @id AND status = 'Pending' ORDER BY reviewno DESC LIMIT 1");
        cmd.Parameters.AddWithValue("id", contentId);

        using var reader = cmd.ExecuteReader();
        return reader.Read() ? MapReview(reader) : null!;
    }

    public void Add(Review review)
    {
        using var cmd = _session.CreateCommand(
            "INSERT INTO reviews (contentid, reviewno, submittedbyuserid, status, submittedat) " +
            "VALUES (@contentId, @reviewNo, @submittedBy, @status, @submittedAt) RETURNING id");
        cmd.Parameters.AddWithValue("contentId", review.ContentId);
        cmd.Parameters.AddWithValue("reviewNo", review.ReviewNo);
        cmd.Parameters.AddWithValue("submittedBy", review.SubmittedByUserId);
        cmd.Parameters.AddWithValue("status", review.Status.ToString());
        cmd.Parameters.AddWithValue("submittedAt", review.SubmittedAt);
        review.Id = (long)cmd.ExecuteScalar()!;
    }

    public void Update(Review review)
    {
        using var cmd = _session.CreateCommand(
            "UPDATE reviews SET status = @status, revieweruserid = @reviewer, feedback = @feedback, decidedat = @decidedAt " +
            "WHERE id = @id");
        cmd.Parameters.AddWithValue("status", review.Status.ToString());
        cmd.Parameters.AddWithValue("reviewer", (object?)review.ReviewerUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("feedback", (object?)review.Feedback ?? DBNull.Value);
        cmd.Parameters.AddWithValue("decidedAt", (object?)review.DecidedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", review.Id);
        cmd.ExecuteNonQuery();
    }

    private static Review MapReview(NpgsqlDataReader reader) => new()
    {
        Id = reader.GetInt64(0),
        ContentId = reader.GetInt64(1),
        ReviewNo = reader.GetInt32(2),
        SubmittedByUserId = reader.GetInt64(3),
        ReviewerUserId = reader.IsDBNull(4) ? null : reader.GetInt64(4),
        Status = Enum.Parse<ReviewStatus>(reader.GetString(5)),
        Feedback = reader.IsDBNull(6) ? null : reader.GetString(6),
        SubmittedAt = reader.GetDateTime(7),
        DecidedAt = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
    };
}