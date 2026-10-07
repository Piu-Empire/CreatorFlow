using CreatorFlow.Api.Models.Auth;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.Api.Repositories.Auth;

public sealed partial class UserRepository
{
    public async Task<UserAvatar?> GetAvatarAsync(long userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT a.image_data, a.width, a.height FROM user_avatars a JOIN users u USING(user_id)
            WHERE a.user_id = @id AND u.account_status = 'ACTIVE' AND u.email_verified_at IS NOT NULL;
            """, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, userId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new UserAvatar(reader.GetFieldValue<byte[]>(0), reader.GetInt16(1), reader.GetInt16(2)) : null;
    }

    public async Task<UserProfile?> SaveProfileAvatarAsync(long userId, string displayName, string? url,
        AvatarChange change, UserAvatar? avatar, long expectedTokenVersion, CancellationToken cancellationToken = default)
    {
        if (change == AvatarChange.Upload && avatar is null) throw new ArgumentException("Missing avatar.");
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var update = new NpgsqlCommand("""
            UPDATE users SET display_name = @name,
                avatar_url = CASE WHEN @keep THEN avatar_url ELSE @url END
            WHERE user_id = @id AND token_version = @expected_version AND account_status = 'ACTIVE' AND email_verified_at IS NOT NULL
            RETURNING user_id, email, display_name, avatar_url,
                account_status::text AS account_status, is_system_admin;
            """, connection, transaction);
        update.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, userId);
        update.Parameters.AddWithValue("expected_version", NpgsqlDbType.Bigint, expectedTokenVersion);
        update.Parameters.AddWithValue("name", NpgsqlDbType.Varchar, displayName);
        update.Parameters.AddWithValue("keep", NpgsqlDbType.Boolean, change == AvatarChange.Keep);
        update.Parameters.AddWithValue("url", NpgsqlDbType.Text, change == AvatarChange.Url ? (object?)url ?? DBNull.Value : DBNull.Value);
        UserProfile profile;
        await using (var reader = await update.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            profile = MapProfile(reader);
        }
        if (change != AvatarChange.Keep)
        {
            string sql = change == AvatarChange.Upload ? """
                INSERT INTO user_avatars(user_id, image_data, width, height)
                VALUES(@id, @data, @width, @height)
                ON CONFLICT(user_id) DO UPDATE SET image_data=EXCLUDED.image_data,
                    width=EXCLUDED.width, height=EXCLUDED.height, updated_at=clock_timestamp();
                """ : "DELETE FROM user_avatars WHERE user_id=@id;";
            await using var save = new NpgsqlCommand(sql, connection, transaction);
            save.Parameters.AddWithValue("id", NpgsqlDbType.Bigint, userId);
            if (change == AvatarChange.Upload)
            {
                save.Parameters.AddWithValue("data", NpgsqlDbType.Bytea, avatar!.ImageData);
                save.Parameters.AddWithValue("width", NpgsqlDbType.Smallint, (short)avatar.Width);
                save.Parameters.AddWithValue("height", NpgsqlDbType.Smallint, (short)avatar.Height);
            }
            await save.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return profile;
    }
}
