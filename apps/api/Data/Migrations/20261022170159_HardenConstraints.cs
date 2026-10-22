using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaigonGuide.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class HardenConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_user_lock", table: "users");

            migrationBuilder.DropCheckConstraint(name: "ck_event_play", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_event_start", table: "analytics_events");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "lock_reason",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "revoke_reason",
                table: "user_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "verification_status",
                table: "places",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "places",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "places",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_license",
                table: "places",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_credit",
                table: "places",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "places",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "short_description",
                table: "place_translations",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "place_translations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_alt",
                table: "place_translations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "place_translations",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "languages",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "audio_assets",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "audio_assets",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "mime_type",
                table: "audio_assets",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "license",
                table: "audio_assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "credit",
                table: "audio_assets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "consent_policy_version",
                table: "analytics_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "close_reason",
                table: "analytics_sessions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "trigger",
                table: "analytics_events",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "page_key",
                table: "analytics_events",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "analytics_events",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "admin_audit_logs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "admin_audit_logs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "admin_audit_logs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_lock",
                table: "users",
                sql: "(status='active' AND locked_at IS NULL AND lock_reason IS NULL) OR (status='locked' AND locked_at IS NOT NULL AND lock_reason IS NOT NULL AND length(btrim(lock_reason))>0)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_time",
                table: "users",
                sql: "updated_at>=created_at AND length(btrim(email))>0 AND length(btrim(normalized_email))>0"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_session_revoke",
                table: "user_sessions",
                sql: "(revoked_at IS NULL AND revoke_reason IS NULL) OR (revoked_at IS NOT NULL AND revoked_at>=created_at AND revoke_reason IS NOT NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_sources",
                table: "places",
                sql: "jsonb_typeof(sources_json)='array' AND jsonb_array_length(sources_json)<=10 AND updated_at>=created_at AND code ~ '^[a-z0-9]+(-[a-z0-9]+)*$'"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_place_verified",
                table: "places",
                sql: "(verification_status='unverified' AND verified_at IS NULL AND verified_by IS NULL) OR (verification_status='verified' AND verified_at IS NOT NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_narration_sources",
                table: "narrations",
                sql: "jsonb_typeof(sources_json)='array' AND jsonb_array_length(sources_json)<=10 AND updated_at>=created_at"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_audio_rights",
                table: "audio_assets",
                sql: "length(btrim(credit))>0 AND length(btrim(license))>0 AND ((status='ready' AND retired_at IS NULL) OR (status='retired' AND retired_at IS NOT NULL AND retired_at>=created_at))"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_lang",
                table: "analytics_events",
                sql: "event_type<>'language_selected' OR language_code IS NOT NULL"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_page",
                table: "analytics_events",
                sql: "(event_type='page_view' AND page_key IS NOT NULL AND page_key IN ('explore','place_detail','privacy')) OR (event_type<>'page_view' AND page_key IS NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_place",
                table: "analytics_events",
                sql: "(event_type IN ('place_view','geofence_entered','narration_started','narration_completed') AND place_id IS NOT NULL AND language_code IS NOT NULL) OR (event_type NOT IN ('place_view','geofence_entered','narration_started','narration_completed') AND place_id IS NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_play",
                table: "analytics_events",
                sql: "(event_type IN ('narration_started','narration_completed') AND play_id IS NOT NULL AND audio_asset_id IS NOT NULL AND place_id IS NOT NULL AND language_code IS NOT NULL AND duration_seconds IS NOT NULL AND duration_seconds>0 AND duration_seconds<=600 AND play_started_at IS NOT NULL) OR (event_type NOT IN ('narration_started','narration_completed') AND play_id IS NULL AND audio_asset_id IS NULL AND duration_seconds IS NULL AND play_started_at IS NULL AND played_seconds IS NULL AND trigger IS NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_start",
                table: "analytics_events",
                sql: "event_type<>'narration_started' OR (play_started_at=received_at AND played_seconds IS NULL AND trigger IS NOT NULL AND trigger IN ('manual','auto'))"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit",
                table: "admin_audit_logs",
                sql: "jsonb_typeof(change_summary)='object' AND (entity_version IS NULL OR entity_version>0) AND (action<>'account_lock' OR (reason IS NOT NULL AND length(btrim(reason))>0))"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(name: "ck_user_lock", table: "users");

            migrationBuilder.DropCheckConstraint(name: "ck_user_time", table: "users");

            migrationBuilder.DropCheckConstraint(name: "ck_session_revoke", table: "user_sessions");

            migrationBuilder.DropCheckConstraint(name: "ck_place_sources", table: "places");

            migrationBuilder.DropCheckConstraint(name: "ck_place_verified", table: "places");

            migrationBuilder.DropCheckConstraint(name: "ck_narration_sources", table: "narrations");

            migrationBuilder.DropCheckConstraint(name: "ck_audio_rights", table: "audio_assets");

            migrationBuilder.DropCheckConstraint(name: "ck_event_lang", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_event_page", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_event_place", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_event_play", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_event_start", table: "analytics_events");

            migrationBuilder.DropCheckConstraint(name: "ck_audit", table: "admin_audit_logs");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10
            );

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10
            );

            migrationBuilder.AlterColumn<string>(
                name: "normalized_email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254
            );

            migrationBuilder.AlterColumn<string>(
                name: "lock_reason",
                table: "users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254
            );

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );

            migrationBuilder.AlterColumn<string>(
                name: "revoke_reason",
                table: "user_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "verification_status",
                table: "places",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(12)",
                oldMaxLength: 12
            );

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "places",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_url",
                table: "places",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2048)",
                oldMaxLength: 2048,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_license",
                table: "places",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_credit",
                table: "places",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "places",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(80)",
                oldMaxLength: 80
            );

            migrationBuilder.AlterColumn<string>(
                name: "short_description",
                table: "place_translations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "place_translations",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200
            );

            migrationBuilder.AlterColumn<string>(
                name: "image_alt",
                table: "place_translations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "address",
                table: "place_translations",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "display_name",
                table: "languages",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50
            );

            migrationBuilder.AlterColumn<string>(
                name: "storage_key",
                table: "audio_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200
            );

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "audio_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10
            );

            migrationBuilder.AlterColumn<string>(
                name: "mime_type",
                table: "audio_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32
            );

            migrationBuilder.AlterColumn<string>(
                name: "license",
                table: "audio_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500
            );

            migrationBuilder.AlterColumn<string>(
                name: "credit",
                table: "audio_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500
            );

            migrationBuilder.AlterColumn<string>(
                name: "consent_policy_version",
                table: "analytics_sessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20
            );

            migrationBuilder.AlterColumn<string>(
                name: "close_reason",
                table: "analytics_sessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "trigger",
                table: "analytics_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "page_key",
                table: "analytics_events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "event_type",
                table: "analytics_events",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40
            );

            migrationBuilder.AlterColumn<string>(
                name: "reason",
                table: "admin_audit_logs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true
            );

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "admin_audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20
            );

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "admin_audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_user_lock",
                table: "users",
                sql: "(status='active' AND locked_at IS NULL AND lock_reason IS NULL) OR (status='locked' AND locked_at IS NOT NULL AND length(btrim(lock_reason))>0)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_play",
                table: "analytics_events",
                sql: "(event_type IN ('narration_started','narration_completed') AND play_id IS NOT NULL AND audio_asset_id IS NOT NULL AND place_id IS NOT NULL AND language_code IS NOT NULL AND duration_seconds>0 AND duration_seconds<=600 AND play_started_at IS NOT NULL) OR (event_type NOT IN ('narration_started','narration_completed') AND play_id IS NULL AND audio_asset_id IS NULL AND duration_seconds IS NULL AND play_started_at IS NULL AND played_seconds IS NULL AND trigger IS NULL)"
            );

            migrationBuilder.AddCheckConstraint(
                name: "ck_event_start",
                table: "analytics_events",
                sql: "event_type<>'narration_started' OR (play_started_at=received_at AND played_seconds IS NULL AND trigger IN ('manual','auto'))"
            );
        }
    }
}
