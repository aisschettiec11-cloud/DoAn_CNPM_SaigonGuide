using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SaigonGuide.Api.Domain;

namespace SaigonGuide.Api.Data;

public class GuideDb(DbContextOptions<GuideDb> options) : DbContext(options)
{
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PlaceTranslation> PlaceTranslations => Set<PlaceTranslation>();
    public DbSet<Narration> Narrations => Set<Narration>();
    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();
    public DbSet<AnalyticsSession> AnalyticsSessions => Set<AnalyticsSession>();
    public DbSet<AnalyticsEvent> AnalyticsEvents => Set<AnalyticsEvent>();
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        m.Entity<Language>().HasKey(x => x.Code);
        m.Entity<Language>().Property(x => x.Code).HasMaxLength(2);
        m.Entity<Language>()
            .HasData(
                new Language { Code = "vi", DisplayName = "Tiếng Việt" },
                new Language { Code = "en", DisplayName = "English" }
            );
        m.Entity<User>().HasIndex(x => x.NormalizedEmail).IsUnique();
        m.Entity<User>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<User>()
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(x => x.PreferredLocale)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<User>()
            .HasIndex(x => new
            {
                x.Role,
                x.Status,
                x.CreatedAt,
                x.Id,
            });
        m.Entity<UserSession>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        m.Entity<UserSession>().HasIndex(x => x.HandleHash).IsUnique();
        m.Entity<UserSession>().HasIndex(x => x.AbsoluteExpiresAt);
        m.Entity<Place>().HasIndex(x => x.Code).IsUnique();
        m.Entity<Place>().HasIndex(x => new { x.Status, x.Code });
        m.Entity<Place>().Property(x => x.Version).IsConcurrencyToken();
        m.Entity<Place>().Property(x => x.Latitude).HasPrecision(9, 6);
        m.Entity<Place>().Property(x => x.Longitude).HasPrecision(10, 6);
        m.Entity<Place>().Property(x => x.SourcesJson).HasColumnType("jsonb");
        m.Entity<Place>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
        m.Entity<Place>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);
        m.Entity<Place>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.VerifiedBy)
            .OnDelete(DeleteBehavior.SetNull);
        m.Entity<Place>()
            .HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<Place>()
            .HasMany(x => x.Narrations)
            .WithOne()
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<PlaceTranslation>().HasIndex(x => new { x.PlaceId, x.LanguageCode }).IsUnique();
        m.Entity<PlaceTranslation>()
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<Narration>().HasIndex(x => new { x.PlaceId, x.LanguageCode }).IsUnique();
        m.Entity<Narration>().HasIndex(x => x.AudioAssetId).IsUnique();
        m.Entity<Narration>().Property(x => x.SourcesJson).HasColumnType("jsonb");
        m.Entity<Narration>()
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<AudioAsset>()
            .HasAlternateKey(x => new
            {
                x.Id,
                x.PlaceId,
                x.LanguageCode,
            });
        m.Entity<AudioAsset>().HasIndex(x => x.StorageKey).IsUnique();
        m.Entity<AudioAsset>()
            .HasOne<Place>()
            .WithMany()
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<AudioAsset>()
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<AudioAsset>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);
        m.Entity<AudioAsset>().Property(x => x.DurationSeconds).HasPrecision(9, 3);
        m.Entity<Narration>()
            .HasOne<AudioAsset>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.AudioAssetId,
                x.PlaceId,
                x.LanguageCode,
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.PlaceId,
                x.LanguageCode,
            })
            .OnDelete(DeleteBehavior.Restrict);
        m.Entity<AnalyticsSession>()
            .HasIndex(x => x.VisitorId)
            .IsUnique()
            .HasFilter("closed_at IS NULL");
        m.Entity<AnalyticsSession>().HasIndex(x => x.StartedAt);
        var ev = m.Entity<AnalyticsEvent>();
        ev.HasOne<AnalyticsSession>()
            .WithMany()
            .HasForeignKey(x => x.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
        ev.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        ev.HasOne<Place>()
            .WithMany()
            .HasForeignKey(x => x.PlaceId)
            .OnDelete(DeleteBehavior.Restrict);
        ev.HasOne<Language>()
            .WithMany()
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
        ev.HasOne<AudioAsset>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.AudioAssetId,
                x.PlaceId,
                x.LanguageCode,
            })
            .HasPrincipalKey(x => new
            {
                x.Id,
                x.PlaceId,
                x.LanguageCode,
            })
            .OnDelete(DeleteBehavior.Restrict);
        ev.HasIndex(x => new
            {
                x.SessionId,
                x.PlayId,
                x.EventType,
            })
            .IsUnique()
            .HasFilter("play_id IS NOT NULL");
        ev.HasIndex(x => new { x.ReceivedAt, x.EventType });
        ev.HasIndex(x => new
        {
            x.PlaceId,
            x.EventType,
            x.ReceivedAt,
        });
        ev.HasIndex(x => new { x.SessionId, x.ReceivedAt });
        ev.HasIndex(x => new { x.UserId, x.ReceivedAt });
        ev.HasIndex(x => new
        {
            x.LanguageCode,
            x.EventType,
            x.ReceivedAt,
        });
        ev.Property(x => x.DurationSeconds).HasPrecision(9, 3);
        ev.Property(x => x.PlayedSeconds).HasPrecision(9, 3);
        m.Entity<AdminAuditLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
        m.Entity<AdminAuditLog>().Property(x => x.ChangeSummary).HasColumnType("jsonb");
        m.Entity<AdminAuditLog>()
            .HasIndex(x => new
            {
                x.EntityType,
                x.EntityId,
                x.OccurredAt,
            });
        m.Entity<AdminAuditLog>().HasIndex(x => x.OccurredAt);
        void Limits<T>(params (string Name, int Max)[] limits)
            where T : class
        {
            foreach (var (name, max) in limits)
                m.Entity<T>().Property(name).HasMaxLength(max);
        }
        Limits<Language>(("DisplayName", 50));
        Limits<User>(
            ("Email", 254),
            ("NormalizedEmail", 254),
            ("DisplayName", 100),
            ("Role", 10),
            ("Status", 10),
            ("PreferredLocale", 2),
            ("LockReason", 500)
        );
        Limits<UserSession>(("RevokeReason", 32));
        Limits<Place>(
            ("Code", 80),
            ("Status", 10),
            ("VerificationStatus", 12),
            ("ImageUrl", 2048),
            ("ImageCredit", 500),
            ("ImageLicense", 500)
        );
        Limits<PlaceTranslation>(
            ("LanguageCode", 2),
            ("Name", 200),
            ("Address", 500),
            ("ShortDescription", 1000),
            ("ImageAlt", 500)
        );
        Limits<Narration>(("LanguageCode", 2));
        Limits<AudioAsset>(
            ("LanguageCode", 2),
            ("StorageKey", 200),
            ("MimeType", 32),
            ("Credit", 500),
            ("License", 500),
            ("Status", 10)
        );
        Limits<AnalyticsSession>(("ConsentPolicyVersion", 20), ("CloseReason", 20));
        Limits<AnalyticsEvent>(
            ("EventType", 40),
            ("LanguageCode", 2),
            ("PageKey", 30),
            ("Trigger", 10)
        );
        Limits<AdminAuditLog>(("Action", 40), ("EntityType", 20), ("Reason", 500));
        foreach (var entity in m.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
                property.SetColumnName(
                    Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant()
                );
        }
        m.Entity<Language>()
            .ToTable("languages", t => t.HasCheckConstraint("ck_language", "code IN ('vi','en')"));
        m.Entity<User>()
            .ToTable(
                "users",
                t =>
                {
                    t.HasCheckConstraint(
                        "ck_user",
                        "role IN ('Admin','User') AND status IN ('active','locked') AND version>0 AND length(display_name) BETWEEN 1 AND 100 AND length(email)<=254"
                    );
                    t.HasCheckConstraint(
                        "ck_user_lock",
                        "(status='active' AND locked_at IS NULL AND lock_reason IS NULL) OR (status='locked' AND locked_at IS NOT NULL AND lock_reason IS NOT NULL AND length(btrim(lock_reason))>0)"
                    );
                }
            );
        m.Entity<UserSession>()
            .ToTable(
                "user_sessions",
                t =>
                    t.HasCheckConstraint(
                        "ck_session",
                        "octet_length(handle_hash)=32 AND last_seen_at>=created_at AND absolute_expires_at=created_at+interval '8 hours'"
                    )
            );
        m.Entity<Place>()
            .ToTable(
                "places",
                t =>
                {
                    t.HasCheckConstraint(
                        "ck_place",
                        "trigger_radius_m BETWEEN 25 AND 500 AND (latitude IS NULL OR latitude BETWEEN -90 AND 90) AND (longitude IS NULL OR longitude BETWEEN -180 AND 180) AND ((latitude IS NULL)=(longitude IS NULL)) AND status IN ('draft','published','hidden') AND verification_status IN ('unverified','verified') AND version>0"
                    );
                    t.HasCheckConstraint(
                        "ck_publish",
                        "status<>'published' OR (latitude IS NOT NULL AND longitude IS NOT NULL AND verification_status='verified')"
                    );
                }
            );
        m.Entity<PlaceTranslation>()
            .ToTable(
                "place_translations",
                t =>
                    t.HasCheckConstraint(
                        "ck_translation",
                        "length(btrim(name)) BETWEEN 1 AND 200 AND (address IS NULL OR length(address)<=500) AND (short_description IS NULL OR length(short_description)<=1000)"
                    )
            );
        m.Entity<Narration>()
            .ToTable(
                "narrations",
                t =>
                    t.HasCheckConstraint(
                        "ck_transcript",
                        "transcript IS NULL OR length(transcript)<=20000"
                    )
            );
        m.Entity<AudioAsset>()
            .ToTable(
                "audio_assets",
                t =>
                    t.HasCheckConstraint(
                        "ck_audio",
                        "byte_size BETWEEN 1 AND 20000000 AND duration_seconds>0 AND duration_seconds<=600 AND mime_type='audio/mpeg' AND octet_length(sha256)=32 AND status IN ('ready','retired')"
                    )
            );
        m.Entity<AnalyticsSession>()
            .ToTable(
                "analytics_sessions",
                t =>
                    t.HasCheckConstraint(
                        "ck_analytics_session",
                        "(last_event_at IS NULL OR last_event_at>=started_at) AND (closed_at IS NULL OR closed_at>=COALESCE(last_event_at,started_at)) AND ((closed_at IS NULL)=(close_reason IS NULL))"
                    )
            );
        ev.ToTable(
            "analytics_events",
            t =>
            {
                t.HasCheckConstraint(
                    "ck_event_type",
                    "event_type IN ('page_view','place_view','location_permission_granted','location_permission_denied','geofence_entered','narration_started','narration_completed','language_selected') AND schema_version=1 AND octet_length(payload_hash)=32"
                );
                t.HasCheckConstraint(
                    "ck_event_play",
                    "(event_type IN ('narration_started','narration_completed') AND play_id IS NOT NULL AND audio_asset_id IS NOT NULL AND place_id IS NOT NULL AND language_code IS NOT NULL AND duration_seconds IS NOT NULL AND duration_seconds>0 AND duration_seconds<=600 AND play_started_at IS NOT NULL) OR (event_type NOT IN ('narration_started','narration_completed') AND play_id IS NULL AND audio_asset_id IS NULL AND duration_seconds IS NULL AND play_started_at IS NULL AND played_seconds IS NULL AND trigger IS NULL)"
                );
                t.HasCheckConstraint(
                    "ck_event_complete",
                    "event_type<>'narration_completed' OR (played_seconds IS NOT NULL AND played_seconds>=duration_seconds*0.9 AND played_seconds<=duration_seconds AND trigger IS NULL AND received_at>=play_started_at+duration_seconds*0.9*interval '1 second')"
                );
                t.HasCheckConstraint(
                    "ck_event_start",
                    "event_type<>'narration_started' OR (play_started_at=received_at AND played_seconds IS NULL AND trigger IS NOT NULL AND trigger IN ('manual','auto'))"
                );
            }
        );
        m.Entity<AdminAuditLog>()
            .ToTable(
                "admin_audit_logs",
                t =>
                    t.HasCheckConstraint(
                        "ck_audit",
                        "jsonb_typeof(change_summary)='object' AND (entity_version IS NULL OR entity_version>0) AND (action<>'account_lock' OR (reason IS NOT NULL AND length(btrim(reason))>0))"
                    )
            );
        m.Entity<User>()
            .ToTable(
                "users",
                t =>
                    t.HasCheckConstraint(
                        "ck_user_time",
                        "updated_at>=created_at AND length(btrim(email))>0 AND length(btrim(normalized_email))>0"
                    )
            );
        m.Entity<UserSession>()
            .ToTable(
                "user_sessions",
                t =>
                    t.HasCheckConstraint(
                        "ck_session_revoke",
                        "(revoked_at IS NULL AND revoke_reason IS NULL) OR (revoked_at IS NOT NULL AND revoked_at>=created_at AND revoke_reason IS NOT NULL)"
                    )
            );
        m.Entity<Place>()
            .ToTable(
                "places",
                t =>
                {
                    t.HasCheckConstraint(
                        "ck_place_verified",
                        "(verification_status='unverified' AND verified_at IS NULL AND verified_by IS NULL) OR (verification_status='verified' AND verified_at IS NOT NULL)"
                    );
                    t.HasCheckConstraint(
                        "ck_place_sources",
                        "jsonb_typeof(sources_json)='array' AND jsonb_array_length(sources_json)<=10 AND updated_at>=created_at AND code ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"
                    );
                }
            );
        m.Entity<Narration>()
            .ToTable(
                "narrations",
                t =>
                    t.HasCheckConstraint(
                        "ck_narration_sources",
                        "jsonb_typeof(sources_json)='array' AND jsonb_array_length(sources_json)<=10 AND updated_at>=created_at"
                    )
            );
        m.Entity<AudioAsset>()
            .ToTable(
                "audio_assets",
                t =>
                    t.HasCheckConstraint(
                        "ck_audio_rights",
                        "length(btrim(credit))>0 AND length(btrim(license))>0 AND ((status='ready' AND retired_at IS NULL) OR (status='retired' AND retired_at IS NOT NULL AND retired_at>=created_at))"
                    )
            );
        ev.ToTable(
            "analytics_events",
            t =>
            {
                t.HasCheckConstraint(
                    "ck_event_place",
                    "(event_type IN ('place_view','geofence_entered','narration_started','narration_completed') AND place_id IS NOT NULL AND language_code IS NOT NULL) OR (event_type NOT IN ('place_view','geofence_entered','narration_started','narration_completed') AND place_id IS NULL)"
                );
                t.HasCheckConstraint(
                    "ck_event_page",
                    "(event_type='page_view' AND page_key IS NOT NULL AND page_key IN ('explore','place_detail','privacy')) OR (event_type<>'page_view' AND page_key IS NULL)"
                );
                t.HasCheckConstraint(
                    "ck_event_lang",
                    "event_type<>'language_selected' OR language_code IS NOT NULL"
                );
            }
        );
    }
}
