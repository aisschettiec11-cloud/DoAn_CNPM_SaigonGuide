using System.Text.Json.Serialization;

namespace SaigonGuide.Api.Contracts;

public record SourceRef(string Url, string Title, string AccessedOn, string Scope);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record LoginInput(string Email, string Password);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record RegisterInput(string Email, string Password, string DisplayName);

public record ProfileInput(string DisplayName, string PreferredLocale);

public record PasswordInput(string CurrentPassword, string NewPassword);

public record StatusInput(string Status, long Version, string? Reason);

public record TranslationInput(
    string LanguageCode,
    string Name,
    string? Address,
    string? ShortDescription,
    string? ImageAlt
);

public record NarrationInput(
    string LanguageCode,
    string? Transcript,
    List<SourceRef> Sources,
    Guid? AudioAssetId
);

public record PlaceInput(
    string Code,
    decimal? Latitude,
    decimal? Longitude,
    int TriggerRadiusM,
    string Status,
    string VerificationStatus,
    bool IsDemo,
    string? ImageUrl,
    string? ImageCredit,
    string? ImageLicense,
    List<SourceRef> Sources,
    List<TranslationInput> Translations,
    List<NarrationInput> Narrations,
    long Version
);

public record ConsentInput(bool Consent);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public record EventInput(
    Guid EventId,
    string Type,
    DateTimeOffset ClientTimestamp,
    Guid SessionId,
    Guid? PlaceId = null,
    string? LanguageCode = null,
    string? PageKey = null,
    Guid? PlayId = null,
    decimal? DurationSeconds = null,
    decimal? PlayedSeconds = null,
    string? Trigger = null
);

public record EventBatch(List<EventInput> Events);
