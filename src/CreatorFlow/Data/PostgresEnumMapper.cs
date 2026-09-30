using System.Text;
using CreatorFlow.Models.Enums;

namespace CreatorFlow.Data;

public static class PostgresEnumMapper
{
    private static readonly HashSet<Type> SupportedTypes =
    [
        typeof(AccountStatus),
        typeof(ProjectStatus),
        typeof(ProjectRole),
        typeof(InvitationStatus),
        typeof(IdeaStatus),
        typeof(Priority),
        typeof(ContentStatus),
        typeof(PublicationStatus),
        typeof(AssignmentStatus),
        typeof(ReviewStatus),
        typeof(RelationType),
        typeof(NotificationType),
        typeof(SubscriptionStatus),
        typeof(ReportStatus),
        typeof(AiRequestStatus),
        typeof(AiRequestType)
    ];

    public static string ToDatabaseValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        EnsureSupported<TEnum>();

        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown enum value.");
        }

        return ToUpperSnakeCase(value.ToString());
    }

    public static TEnum Parse<TEnum>(string databaseValue)
        where TEnum : struct, Enum
    {
        EnsureSupported<TEnum>();
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseValue);

        foreach (TEnum value in Enum.GetValues<TEnum>())
        {
            if (ToDatabaseValue(value) == databaseValue)
            {
                return value;
            }
        }

        throw new ArgumentException(
            $"Unknown PostgreSQL value for {typeof(TEnum).Name}.",
            nameof(databaseValue));
    }

    private static void EnsureSupported<TEnum>()
        where TEnum : struct, Enum
    {
        if (!SupportedTypes.Contains(typeof(TEnum)))
        {
            throw new NotSupportedException(
                $"Enum type {typeof(TEnum).Name} is not mapped to PostgreSQL.");
        }
    }

    private static string ToUpperSnakeCase(string value)
    {
        var result = new StringBuilder(value.Length + 4);

        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];

            if (index > 0 && char.IsUpper(character))
            {
                result.Append('_');
            }

            result.Append(char.ToUpperInvariant(character));
        }

        return result.ToString();
    }
}
