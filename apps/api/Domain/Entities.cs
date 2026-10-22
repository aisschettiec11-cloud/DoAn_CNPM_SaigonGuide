namespace SaigonGuide.Api.Domain;

public class Language
{
    public string Code { get; set; } = "vi";
    public string DisplayName { get; set; } = "";
}

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "User";
    public string Status { get; set; } = "active";
    public string PreferredLocale { get; set; } = "vi";
    public DateTimeOffset? LockedAt { get; set; }
    public string? LockReason { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public byte[] HandleHash { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset AbsoluteExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokeReason { get; set; }
}

public class Place
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int TriggerRadiusM { get; set; } = 75;
    public string Status { get; set; } = "draft";
    public string VerificationStatus { get; set; } = "unverified";
    public bool IsDemo { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageCredit { get; set; }
    public string? ImageLicense { get; set; }
    public string SourcesJson { get; set; } = "[]";
    public DateTimeOffset? VerifiedAt { get; set; }
    public Guid? VerifiedBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public long Version { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<PlaceTranslation> Translations { get; set; } = [];
    public List<Narration> Narrations { get; set; } = [];
}

public class PlaceTranslation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaceId { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? ShortDescription { get; set; }
    public string? ImageAlt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class Narration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaceId { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public string? Transcript { get; set; }
    public Guid? AudioAssetId { get; set; }
    public string SourcesJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AudioAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PlaceId { get; set; }
    public string LanguageCode { get; set; } = "vi";
    public string StorageKey { get; set; } = "";
    public string MimeType { get; set; } = "audio/mpeg";
    public long ByteSize { get; set; }
    public decimal DurationSeconds { get; set; }
    public byte[] Sha256 { get; set; } = [];
    public string Credit { get; set; } = "";
    public string License { get; set; } = "";
    public string Status { get; set; } = "ready";
    public Guid? UploadedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RetiredAt { get; set; }
}

public class AnalyticsSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VisitorId { get; set; }
    public string ConsentPolicyVersion { get; set; } = "1.0";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastEventAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? CloseReason { get; set; }
}

public class AnalyticsEvent
{
    public Guid Id { get; set; }
    public short SchemaVersion { get; set; } = 1;
    public string EventType { get; set; } = "";
    public Guid SessionId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PlaceId { get; set; }
    public string? LanguageCode { get; set; }
    public string? PageKey { get; set; }
    public Guid? PlayId { get; set; }
    public Guid? AudioAssetId { get; set; }
    public DateTimeOffset? PlayStartedAt { get; set; }
    public decimal? DurationSeconds { get; set; }
    public decimal? PlayedSeconds { get; set; }
    public string? Trigger { get; set; }
    public DateTimeOffset ClientTimestamp { get; set; }
    public DateTimeOffset ReceivedAt { get; set; } = DateTimeOffset.UtcNow;
    public byte[] PayloadHash { get; set; } = [];
}

public class AdminAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "Place";
    public Guid EntityId { get; set; }
    public long? EntityVersion { get; set; }
    public string? Reason { get; set; }
    public string ChangeSummary { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
