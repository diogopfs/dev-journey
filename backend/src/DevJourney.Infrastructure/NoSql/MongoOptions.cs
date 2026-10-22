namespace DevJourney.Infrastructure.NoSql;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    /// <summary>Ex.: mongodb://localhost:27017 ou a string "mongodb+srv://..." do MongoDB Atlas (free tier).</summary>
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";
    public string Database { get; init; } = "devjourney";
    public string AuditCollection { get; init; } = "audit_logs";
}
