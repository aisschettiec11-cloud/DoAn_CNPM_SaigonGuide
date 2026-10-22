using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SaigonGuide.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytics_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    visitor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_policy_version = table.Column<string>(type: "text", nullable: false),
                    started_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    last_event_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    closed_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    close_reason = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_sessions", x => x.id);
                    table.CheckConstraint(
                        "ck_analytics_session",
                        "(last_event_at IS NULL OR last_event_at>=started_at) AND (closed_at IS NULL OR closed_at>=COALESCE(last_event_at,started_at)) AND ((closed_at IS NULL)=(close_reason IS NULL))"
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "languages",
                columns: table => new
                {
                    code = table.Column<string>(
                        type: "character varying(2)",
                        maxLength: 2,
                        nullable: false
                    ),
                    display_name = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.code);
                    table.CheckConstraint("ck_language", "code IN ('vi','en')");
                }
            );

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    normalized_email = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    preferred_locale = table.Column<string>(
                        type: "character varying(2)",
                        nullable: false
                    ),
                    locked_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    lock_reason = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.CheckConstraint(
                        "ck_user",
                        "role IN ('Admin','User') AND status IN ('active','locked') AND version>0 AND length(display_name) BETWEEN 1 AND 100 AND length(email)<=254"
                    );
                    table.CheckConstraint(
                        "ck_user_lock",
                        "(status='active' AND locked_at IS NULL AND lock_reason IS NULL) OR (status='locked' AND locked_at IS NOT NULL AND length(btrim(lock_reason))>0)"
                    );
                    table.ForeignKey(
                        name: "FK_users_languages_preferred_locale",
                        column: x => x.preferred_locale,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_version = table.Column<long>(type: "bigint", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    change_summary = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_admin_audit_logs_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "places",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<decimal>(
                        type: "numeric(9,6)",
                        precision: 9,
                        scale: 6,
                        nullable: true
                    ),
                    longitude = table.Column<decimal>(
                        type: "numeric(10,6)",
                        precision: 10,
                        scale: 6,
                        nullable: true
                    ),
                    trigger_radius_m = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    verification_status = table.Column<string>(type: "text", nullable: false),
                    is_demo = table.Column<bool>(type: "boolean", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    image_credit = table.Column<string>(type: "text", nullable: true),
                    image_license = table.Column<string>(type: "text", nullable: true),
                    sources_json = table.Column<string>(type: "jsonb", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    verified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_places", x => x.id);
                    table.CheckConstraint(
                        "ck_place",
                        "trigger_radius_m BETWEEN 25 AND 500 AND (latitude IS NULL OR latitude BETWEEN -90 AND 90) AND (longitude IS NULL OR longitude BETWEEN -180 AND 180) AND ((latitude IS NULL)=(longitude IS NULL)) AND status IN ('draft','published','hidden') AND verification_status IN ('unverified','verified') AND version>0"
                    );
                    table.CheckConstraint(
                        "ck_publish",
                        "status<>'published' OR (latitude IS NOT NULL AND longitude IS NOT NULL AND verification_status='verified')"
                    );
                    table.ForeignKey(
                        name: "FK_places_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_places_users_updated_by",
                        column: x => x.updated_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                    table.ForeignKey(
                        name: "FK_places_users_verified_by",
                        column: x => x.verified_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handle_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    last_seen_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    absolute_expires_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    revoked_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    revoke_reason = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.id);
                    table.CheckConstraint(
                        "ck_session",
                        "octet_length(handle_hash)=32 AND last_seen_at>=created_at AND absolute_expires_at=created_at+interval '8 hours'"
                    );
                    table.ForeignKey(
                        name: "FK_user_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "audio_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(
                        type: "character varying(2)",
                        nullable: false
                    ),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    byte_size = table.Column<long>(type: "bigint", nullable: false),
                    duration_seconds = table.Column<decimal>(
                        type: "numeric(9,3)",
                        precision: 9,
                        scale: 3,
                        nullable: false
                    ),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    credit = table.Column<string>(type: "text", nullable: false),
                    license = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    retired_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audio_assets", x => x.id);
                    table.UniqueConstraint(
                        "AK_audio_assets_id_place_id_language_code",
                        x => new
                        {
                            x.id,
                            x.place_id,
                            x.language_code,
                        }
                    );
                    table.CheckConstraint(
                        "ck_audio",
                        "byte_size BETWEEN 1 AND 20000000 AND duration_seconds>0 AND duration_seconds<=600 AND mime_type='audio/mpeg' AND octet_length(sha256)=32 AND status IN ('ready','retired')"
                    );
                    table.ForeignKey(
                        name: "FK_audio_assets_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_audio_assets_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_audio_assets_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "place_translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(
                        type: "character varying(2)",
                        nullable: false
                    ),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: true),
                    short_description = table.Column<string>(type: "text", nullable: true),
                    image_alt = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_place_translations", x => x.id);
                    table.CheckConstraint(
                        "ck_translation",
                        "length(btrim(name)) BETWEEN 1 AND 200 AND (address IS NULL OR length(address)<=500) AND (short_description IS NULL OR length(short_description)<=1000)"
                    );
                    table.ForeignKey(
                        name: "FK_place_translations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_place_translations_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "analytics_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    schema_version = table.Column<short>(type: "smallint", nullable: false),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    place_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language_code = table.Column<string>(
                        type: "character varying(2)",
                        nullable: true
                    ),
                    page_key = table.Column<string>(type: "text", nullable: true),
                    play_id = table.Column<Guid>(type: "uuid", nullable: true),
                    audio_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    play_started_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    duration_seconds = table.Column<decimal>(
                        type: "numeric(9,3)",
                        precision: 9,
                        scale: 3,
                        nullable: true
                    ),
                    played_seconds = table.Column<decimal>(
                        type: "numeric(9,3)",
                        precision: 9,
                        scale: 3,
                        nullable: true
                    ),
                    trigger = table.Column<string>(type: "text", nullable: true),
                    client_timestamp = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    received_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    payload_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytics_events", x => x.id);
                    table.CheckConstraint(
                        "ck_event_complete",
                        "event_type<>'narration_completed' OR (played_seconds IS NOT NULL AND played_seconds>=duration_seconds*0.9 AND played_seconds<=duration_seconds AND trigger IS NULL AND received_at>=play_started_at+duration_seconds*0.9*interval '1 second')"
                    );
                    table.CheckConstraint(
                        "ck_event_play",
                        "(event_type IN ('narration_started','narration_completed') AND play_id IS NOT NULL AND audio_asset_id IS NOT NULL AND place_id IS NOT NULL AND language_code IS NOT NULL AND duration_seconds>0 AND duration_seconds<=600 AND play_started_at IS NOT NULL) OR (event_type NOT IN ('narration_started','narration_completed') AND play_id IS NULL AND audio_asset_id IS NULL AND duration_seconds IS NULL AND play_started_at IS NULL AND played_seconds IS NULL AND trigger IS NULL)"
                    );
                    table.CheckConstraint(
                        "ck_event_start",
                        "event_type<>'narration_started' OR (play_started_at=received_at AND played_seconds IS NULL AND trigger IN ('manual','auto'))"
                    );
                    table.CheckConstraint(
                        "ck_event_type",
                        "event_type IN ('page_view','place_view','location_permission_granted','location_permission_denied','geofence_entered','narration_started','narration_completed','language_selected') AND schema_version=1 AND octet_length(payload_hash)=32"
                    );
                    table.ForeignKey(
                        name: "FK_analytics_events_analytics_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "analytics_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_analytics_events_audio_assets_audio_asset_id_place_id_langu~",
                        columns: x => new
                        {
                            x.audio_asset_id,
                            x.place_id,
                            x.language_code,
                        },
                        principalTable: "audio_assets",
                        principalColumns: new[] { "id", "place_id", "language_code" },
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_analytics_events_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_analytics_events_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_analytics_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "narrations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    place_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(
                        type: "character varying(2)",
                        nullable: false
                    ),
                    transcript = table.Column<string>(type: "text", nullable: true),
                    audio_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sources_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    updated_at = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_narrations", x => x.id);
                    table.CheckConstraint(
                        "ck_transcript",
                        "transcript IS NULL OR length(transcript)<=20000"
                    );
                    table.ForeignKey(
                        name: "FK_narrations_audio_assets_audio_asset_id_place_id_language_co~",
                        columns: x => new
                        {
                            x.audio_asset_id,
                            x.place_id,
                            x.language_code,
                        },
                        principalTable: "audio_assets",
                        principalColumns: new[] { "id", "place_id", "language_code" },
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_narrations_languages_language_code",
                        column: x => x.language_code,
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_narrations_places_place_id",
                        column: x => x.place_id,
                        principalTable: "places",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict
                    );
                }
            );

            migrationBuilder.InsertData(
                table: "languages",
                columns: new[] { "code", "display_name" },
                values: new object[,]
                {
                    { "en", "English" },
                    { "vi", "Tiếng Việt" },
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_actor_user_id",
                table: "admin_audit_logs",
                column: "actor_user_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_entity_type_entity_id_occurred_at",
                table: "admin_audit_logs",
                columns: new[] { "entity_type", "entity_id", "occurred_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_occurred_at",
                table: "admin_audit_logs",
                column: "occurred_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_audio_asset_id_place_id_language_code",
                table: "analytics_events",
                columns: new[] { "audio_asset_id", "place_id", "language_code" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_language_code_event_type_received_at",
                table: "analytics_events",
                columns: new[] { "language_code", "event_type", "received_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_place_id_event_type_received_at",
                table: "analytics_events",
                columns: new[] { "place_id", "event_type", "received_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_received_at_event_type",
                table: "analytics_events",
                columns: new[] { "received_at", "event_type" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_session_id_play_id_event_type",
                table: "analytics_events",
                columns: new[] { "session_id", "play_id", "event_type" },
                unique: true,
                filter: "play_id IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_session_id_received_at",
                table: "analytics_events",
                columns: new[] { "session_id", "received_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_events_user_id_received_at",
                table: "analytics_events",
                columns: new[] { "user_id", "received_at" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_sessions_started_at",
                table: "analytics_sessions",
                column: "started_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_analytics_sessions_visitor_id",
                table: "analytics_sessions",
                column: "visitor_id",
                unique: true,
                filter: "closed_at IS NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_audio_assets_language_code",
                table: "audio_assets",
                column: "language_code"
            );

            migrationBuilder.CreateIndex(
                name: "IX_audio_assets_place_id",
                table: "audio_assets",
                column: "place_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_audio_assets_storage_key",
                table: "audio_assets",
                column: "storage_key",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_audio_assets_uploaded_by",
                table: "audio_assets",
                column: "uploaded_by"
            );

            migrationBuilder.CreateIndex(
                name: "IX_narrations_audio_asset_id",
                table: "narrations",
                column: "audio_asset_id",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_narrations_audio_asset_id_place_id_language_code",
                table: "narrations",
                columns: new[] { "audio_asset_id", "place_id", "language_code" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_narrations_language_code",
                table: "narrations",
                column: "language_code"
            );

            migrationBuilder.CreateIndex(
                name: "IX_narrations_place_id_language_code",
                table: "narrations",
                columns: new[] { "place_id", "language_code" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_place_translations_language_code",
                table: "place_translations",
                column: "language_code"
            );

            migrationBuilder.CreateIndex(
                name: "IX_place_translations_place_id_language_code",
                table: "place_translations",
                columns: new[] { "place_id", "language_code" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_places_code",
                table: "places",
                column: "code",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_places_created_by",
                table: "places",
                column: "created_by"
            );

            migrationBuilder.CreateIndex(
                name: "IX_places_status_code",
                table: "places",
                columns: new[] { "status", "code" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_places_updated_by",
                table: "places",
                column: "updated_by"
            );

            migrationBuilder.CreateIndex(
                name: "IX_places_verified_by",
                table: "places",
                column: "verified_by"
            );

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_absolute_expires_at",
                table: "user_sessions",
                column: "absolute_expires_at"
            );

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_handle_hash",
                table: "user_sessions",
                column: "handle_hash",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_user_id",
                table: "user_sessions",
                column: "user_id"
            );

            migrationBuilder.CreateIndex(
                name: "IX_users_normalized_email",
                table: "users",
                column: "normalized_email",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_users_preferred_locale",
                table: "users",
                column: "preferred_locale"
            );

            migrationBuilder.CreateIndex(
                name: "IX_users_role_status_created_at_id",
                table: "users",
                columns: new[] { "role", "status", "created_at", "id" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "admin_audit_logs");

            migrationBuilder.DropTable(name: "analytics_events");

            migrationBuilder.DropTable(name: "narrations");

            migrationBuilder.DropTable(name: "place_translations");

            migrationBuilder.DropTable(name: "user_sessions");

            migrationBuilder.DropTable(name: "analytics_sessions");

            migrationBuilder.DropTable(name: "audio_assets");

            migrationBuilder.DropTable(name: "places");

            migrationBuilder.DropTable(name: "users");

            migrationBuilder.DropTable(name: "languages");
        }
    }
}
