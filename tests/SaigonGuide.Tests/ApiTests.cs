using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaigonGuide.Api.Data;
using SaigonGuide.Api.Domain;
using SaigonGuide.Api.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SaigonGuide.Tests;

public class ApiFixture : IAsyncLifetime
{
    public WebApplicationFactory<Program> Factory = null!;
    public const string AdminEmail = "admin-test@example.com",
        Password = "Test-only-password-12345";

    public async Task InitializeAsync()
    {
        var cs =
            Environment.GetEnvironmentVariable("SG_TEST_DB")
            ?? throw new InvalidOperationException(
                "SG_TEST_DB must point at a dedicated *_test database."
            );
        var connection = new Npgsql.NpgsqlConnectionStringBuilder(cs);
        if (!connection.Database!.EndsWith("_test"))
            throw new InvalidOperationException("Refusing to reset a non-test database.");
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Testing");
            b.ConfigureAppConfiguration(
                (_, c) =>
                    c.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Database"] = cs,
                            ["Keys:Path"] = Path.GetFullPath(
                                "../../../../../.local/test-keys",
                                AppContext.BaseDirectory
                            ),
                            ["Media:Path"] = Path.GetFullPath(
                                "../../../../../.local/test-media",
                                AppContext.BaseDirectory
                            ),
                            ["Content:Path"] = Path.GetFullPath(
                                "../../../../../content",
                                AppContext.BaseDirectory
                            ),
                        }
                    )
            );
        });
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await Seeder.Seed(
            db,
            scope.ServiceProvider.GetRequiredService<ContentService>(),
            config,
            scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>()
        );
        var admin = new User
        {
            Email = AdminEmail,
            NormalizedEmail = AdminEmail.ToUpperInvariant(),
            DisplayName = "Test Admin",
            Role = "Admin",
        };
        admin.PasswordHash = scope
            .ServiceProvider.GetRequiredService<IPasswordHasher<User>>()
            .HashPassword(admin, Password);
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    public HttpClient Client() =>
        Factory.CreateClient(new() { AllowAutoRedirect = false, HandleCookies = true });

    public async Task<HttpResponseMessage> Send(
        HttpClient c,
        HttpMethod method,
        string path,
        object body
    )
    {
        var csrf = await c.GetFromJsonAsync<JsonElement>("/api/v1/csrf");
        using var r = new HttpRequestMessage(method, "/api/v1" + path)
        {
            Content = JsonContent.Create(body),
        };
        r.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
        return await c.SendAsync(r);
    }

    public Task<HttpResponseMessage> Post(HttpClient c, string path, object body) =>
        Send(c, HttpMethod.Post, path, body);

    public async Task Login(HttpClient c, string email = AdminEmail, string password = Password)
    {
        var r = await Post(c, "/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    public async Task<Guid> Place()
    {
        using var c = Client();
        return (await c.GetFromJsonAsync<JsonElement>("/api/v1/places"))[0]
            .GetProperty("id")
            .GetGuid();
    }

    public async Task<(HttpClient Client, Guid Session)> Consent()
    {
        var c = Client();
        var r = await Post(c, "/analytics/session", new { consent = true });
        r.EnsureSuccessStatusCode();
        return (
            c,
            (await r.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("sessionId").GetGuid()
        );
    }
}

public class ApiTests(ApiFixture f) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task PublicContentHasSixPlacesCorrectLocaleAndPlayableAudio()
    {
        using var c = f.Client();
        var vi = await c.GetFromJsonAsync<JsonElement>("/api/v1/places?language=vi");
        var en = await c.GetFromJsonAsync<JsonElement>("/api/v1/places?language=en");
        Assert.Equal(6, vi.GetArrayLength());
        Assert.Equal(6, en.GetArrayLength());
        var id = vi[0].GetProperty("id").GetGuid();
        var detail = await c.GetFromJsonAsync<JsonElement>($"/api/v1/places/{id}?language=en");
        Assert.Equal("en", detail.GetProperty("languageCode").GetString());
        Assert.True(detail.GetProperty("transcript").GetString()!.Length > 100);
        var aid = detail.GetProperty("audioAssetId").GetGuid();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/media/{aid}");
        req.Headers.Range = new(0, 99);
        var audio = await c.SendAsync(req);
        Assert.Equal(HttpStatusCode.PartialContent, audio.StatusCode);
        Assert.Equal(100, (await audio.Content.ReadAsByteArrayAsync()).Length);
    }

    [Fact]
    public async Task AnonymousAndUserCannotReadAdminAndCannotAssignRole()
    {
        using var c = f.Client();
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await c.GetAsync("/api/v1/admin/dashboard")).StatusCode
        );
        var email = $"{Guid.NewGuid():N}@example.com";
        var malicious = await f.Post(
            c,
            "/auth/register",
            new
            {
                email,
                password = ApiFixture.Password,
                displayName = "Guest",
                role = "Admin",
            }
        );
        Assert.Equal(HttpStatusCode.UnprocessableEntity, malicious.StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            (
                await f.Post(
                    c,
                    "/auth/register",
                    new
                    {
                        email,
                        password = ApiFixture.Password,
                        displayName = "Guest",
                    }
                )
            ).StatusCode
        );
        await f.Login(c, email);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await c.GetAsync("/api/v1/admin/dashboard")).StatusCode
        );
    }

    [Fact]
    public async Task MissingCsrfRejectedAndLogoutRevokesCopiedCookie()
    {
        using var client = f.Client();
        var credentials = new { email = ApiFixture.AdminEmail, password = ApiFixture.Password };
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await client.PostAsJsonAsync("/api/v1/auth/login", credentials)).StatusCode
        );
        var login = await f.Post(client, "/auth/login", credentials);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var copiedCookie = login
            .Headers.GetValues("Set-Cookie")
            .Last(x => x.StartsWith("sg_session=") && !x.StartsWith("sg_session=;"))
            .Split(';')[0];
        using var replay = f.Factory.CreateClient(
            new() { HandleCookies = false, AllowAutoRedirect = false }
        );
        replay.DefaultRequestHeaders.Add("Cookie", copiedCookie);
        Assert.Equal(HttpStatusCode.OK, (await replay.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await f.Post(client, "/auth/logout", new { })).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await replay.GetAsync("/api/v1/auth/me")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode
        );
    }

    [Fact]
    public async Task EventDeduplicationAndNoGpsPayload()
    {
        var (c, session) = await f.Consent();
        using (c)
        {
            var evt = new
            {
                eventId = Guid.NewGuid(),
                type = "page_view",
                clientTimestamp = DateTimeOffset.UtcNow,
                sessionId = session,
                pageKey = "explore",
            };
            var first = await f.Post(c, "/analytics/events", new { events = new[] { evt } });
            Assert.Contains("accepted", await first.Content.ReadAsStringAsync());
            var again = await f.Post(c, "/analytics/events", new { events = new[] { evt } });
            Assert.Contains("duplicate", await again.Content.ReadAsStringAsync());
            var bad = await f.Post(
                c,
                "/analytics/events",
                new
                {
                    events = new[]
                    {
                        new
                        {
                            eventId = Guid.NewGuid(),
                            type = "page_view",
                            clientTimestamp = DateTimeOffset.UtcNow,
                            sessionId = session,
                            pageKey = "explore",
                            latitude = 10.77,
                        },
                    },
                }
            );
            Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
        }
    }

    [Fact]
    public async Task ConsentRequiredAndWithdrawalStopsEvents()
    {
        using var c = f.Client();
        var denied = await f.Post(
            c,
            "/analytics/events",
            new
            {
                events = new[]
                {
                    new
                    {
                        eventId = Guid.NewGuid(),
                        type = "page_view",
                        clientTimestamp = DateTimeOffset.UtcNow,
                        sessionId = Guid.NewGuid(),
                        pageKey = "explore",
                    },
                },
            }
        );
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        var handshake = await f.Post(c, "/analytics/session", new { consent = true });
        Assert.Equal(HttpStatusCode.OK, handshake.StatusCode);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await f.Send(c, HttpMethod.Delete, "/analytics/session", new { })).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (
                await f.Post(
                    c,
                    "/analytics/events",
                    new
                    {
                        events = new[]
                        {
                            new
                            {
                                eventId = Guid.NewGuid(),
                                type = "page_view",
                                clientTimestamp = DateTimeOffset.UtcNow,
                                sessionId = Guid.NewGuid(),
                                pageKey = "explore",
                            },
                        },
                    }
                )
            ).StatusCode
        );
    }

    [Fact]
    public async Task CompletedWithoutStartOrElapsedTimeRejected()
    {
        var (c, s) = await f.Consent();
        using (c)
        {
            var place = await f.Place();
            var play = Guid.NewGuid();
            var invalid = await f.Post(
                c,
                "/analytics/events",
                new
                {
                    events = new[]
                    {
                        new
                        {
                            eventId = Guid.NewGuid(),
                            type = "narration_completed",
                            clientTimestamp = DateTimeOffset.UtcNow,
                            sessionId = s,
                            placeId = place,
                            languageCode = "vi",
                            playId = play,
                            playedSeconds = 500,
                        },
                    },
                }
            );
            Assert.Contains("rejected", await invalid.Content.ReadAsStringAsync());
            var start = await f.Post(
                c,
                "/analytics/events",
                new
                {
                    events = new[]
                    {
                        new
                        {
                            eventId = Guid.NewGuid(),
                            type = "narration_started",
                            clientTimestamp = DateTimeOffset.UtcNow,
                            sessionId = s,
                            placeId = place,
                            languageCode = "vi",
                            playId = play,
                            trigger = "manual",
                        },
                    },
                }
            );
            Assert.Contains("accepted", await start.Content.ReadAsStringAsync());
            var early = await f.Post(
                c,
                "/analytics/events",
                new
                {
                    events = new[]
                    {
                        new
                        {
                            eventId = Guid.NewGuid(),
                            type = "narration_completed",
                            clientTimestamp = DateTimeOffset.UtcNow,
                            sessionId = s,
                            placeId = place,
                            languageCode = "vi",
                            playId = play,
                            playedSeconds = 1,
                        },
                    },
                }
            );
            Assert.Contains("rejected", await early.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task PublishGateAndOptimisticConcurrencyAndHidden404()
    {
        using var c = f.Client();
        await f.Login(c);
        var payload = new
        {
            code = "test-" + Guid.NewGuid().ToString("N"),
            latitude = (decimal?)null,
            longitude = (decimal?)null,
            triggerRadiusM = 75,
            status = "draft",
            verificationStatus = "unverified",
            isDemo = true,
            imageUrl = (string?)null,
            imageCredit = (string?)null,
            imageLicense = (string?)null,
            sources = Array.Empty<object>(),
            translations = new[]
            {
                new
                {
                    languageCode = "vi",
                    name = "Test point",
                    address = "",
                    shortDescription = "",
                    imageAlt = "",
                },
            },
            narrations = Array.Empty<object>(),
            version = 1,
        };
        var created = await f.Post(c, "/admin/places", payload);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var p = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = p.GetProperty("id").GetGuid();
        var v = p.GetProperty("version").GetInt64();
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await c.GetAsync($"/api/v1/places/{id}")).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (
                await f.Post(
                    c,
                    $"/admin/places/{id}/status",
                    new { status = "published", version = v }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await f.Post(
                    c,
                    $"/admin/places/{id}/status",
                    new { status = "hidden", version = v }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Conflict,
            (
                await f.Post(c, $"/admin/places/{id}/status", new { status = "draft", version = v })
            ).StatusCode
        );
    }

    [Fact]
    public async Task LockRevokesExistingUserSession()
    {
        var email = $"lock-{Guid.NewGuid():N}@example.com";
        using var user = f.Client();
        var reg = await f.Post(
            user,
            "/auth/register",
            new
            {
                email,
                password = ApiFixture.Password,
                displayName = "Lock test",
            }
        );
        var u = await reg.Content.ReadFromJsonAsync<JsonElement>();
        await f.Login(user, email);
        using var admin = f.Client();
        await f.Login(admin);
        var locked = await f.Post(
            admin,
            $"/admin/users/{u.GetProperty("id").GetGuid()}/status",
            new
            {
                status = "locked",
                version = u.GetProperty("version").GetInt64(),
                reason = "Integration test",
            }
        );
        Assert.Equal(HttpStatusCode.OK, locked.StatusCode);
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await user.GetAsync("/api/v1/auth/me")).StatusCode
        );
        var updated = await locked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            HttpStatusCode.OK,
            (
                await f.Post(
                    admin,
                    $"/admin/users/{u.GetProperty("id").GetGuid()}/status",
                    new { status = "active", version = updated.GetProperty("version").GetInt64() }
                )
            ).StatusCode
        );
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            (await user.GetAsync("/api/v1/auth/me")).StatusCode
        );
    }

    [Fact]
    public async Task DashboardFixtureUsesDistinctVisitorsAndCohort()
    {
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
        await db.AnalyticsEvents.ExecuteDeleteAsync();
        await db.AnalyticsSessions.ExecuteDeleteAsync();
        var places = await db
            .Places.Where(x => x.Status == "published")
            .OrderBy(x => x.Id)
            .Take(2)
            .ToListAsync();
        var visitor = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow.AddMinutes(-10);
        var ss = new[]
        {
            new AnalyticsSession { VisitorId = visitor, StartedAt = now },
            new AnalyticsSession { VisitorId = Guid.NewGuid(), StartedAt = now },
            new AnalyticsSession
            {
                VisitorId = visitor,
                StartedAt = now,
                ClosedAt = now.AddMinutes(1),
                CloseReason = "support",
            },
        };
        db.AnalyticsSessions.AddRange(ss);
        await db.SaveChangesAsync();
        AnalyticsEvent E(int session, string type, Guid? p = null) =>
            new()
            {
                Id = Guid.NewGuid(),
                SessionId = ss[session].Id,
                EventType = type,
                PlaceId = p,
                LanguageCode = p.HasValue ? "vi" : null,
                PageKey = type == "page_view" ? "explore" : null,
                ClientTimestamp = now,
                ReceivedAt = now,
                PayloadHash = new byte[32],
            };
        db.AnalyticsEvents.AddRange(
            E(0, "page_view"),
            E(0, "page_view"),
            E(1, "page_view"),
            E(2, "page_view"),
            E(0, "place_view", places[0].Id),
            E(0, "place_view", places[0].Id),
            E(1, "place_view", places[0].Id),
            E(2, "place_view", places[1].Id)
        );
        var n = await db.Narrations.FirstAsync(x =>
            x.PlaceId == places[0].Id && x.LanguageCode == "vi"
        );
        var asset = await db.AudioAssets.SingleAsync(x => x.Id == n.AudioAssetId);
        var plays = new[] { Guid.NewGuid(), Guid.NewGuid() };
        for (int i = 0; i < 2; i++)
        {
            var start = E(i, "narration_started", places[0].Id);
            start.PlayId = plays[i];
            start.AudioAssetId = asset.Id;
            start.DurationSeconds = asset.DurationSeconds;
            start.PlayStartedAt = start.ReceivedAt;
            start.Trigger = "manual";
            db.AnalyticsEvents.Add(start);
        }
        var end = E(0, "narration_completed", places[0].Id);
        end.PlayId = plays[0];
        end.AudioAssetId = asset.Id;
        end.DurationSeconds = asset.DurationSeconds;
        end.PlayedSeconds = asset.DurationSeconds;
        end.PlayStartedAt = now;
        end.ReceivedAt = now.AddSeconds((double)asset.DurationSeconds);
        db.AnalyticsEvents.Add(end);
        await db.SaveChangesAsync();
        using var admin = f.Client();
        await f.Login(admin);
        var result = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/dashboard");
        var k = result.GetProperty("kpis");
        Assert.Equal(4, k.GetProperty("pageViews").GetInt32());
        Assert.Equal(3, k.GetProperty("sessions").GetInt32());
        Assert.Equal(2, k.GetProperty("visitors").GetInt32());
        Assert.Equal(2, k.GetProperty("narrationStarts").GetInt32());
        Assert.Equal(1, k.GetProperty("narrationCompletions").GetInt32());
        Assert.Equal(50, k.GetProperty("completionRate").GetDouble());
    }

    [Fact]
    public async Task InvalidDashboardRangeRejected()
    {
        using var c = f.Client();
        await f.Login(c);
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await c.GetAsync("/api/v1/admin/dashboard?from=2099-01-02&to=2020-01-01")).StatusCode
        );
    }

    [Fact]
    public async Task MediaRejectsFakeMp3AndAcceptsDecodedFile()
    {
        using var c = f.Client();
        await f.Login(c);
        var all = await c.GetFromJsonAsync<JsonElement>("/api/v1/admin/places");
        var place = all.EnumerateArray()
            .First(x => x.GetProperty("status").GetString() == "published");
        var id = place.GetProperty("id").GetGuid();
        var version = place.GetProperty("version").GetInt64();
        var csrf = await c.GetFromJsonAsync<JsonElement>("/api/v1/csrf");
        async Task<HttpResponseMessage> Upload(byte[] bytes)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/v1/admin/places/{id}/audio/vi"
            );
            request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
            var form = new MultipartFormDataContent();
            form.Add(new ByteArrayContent(bytes), "file", "test.mp3");
            form.Add(new StringContent(version.ToString()), "version");
            form.Add(new StringContent("Test fixture"), "credit");
            form.Add(new StringContent("CC BY 4.0"), "license");
            request.Content = form;
            return await c.SendAsync(request);
        }
        Assert.Equal(
            HttpStatusCode.UnprocessableEntity,
            (await Upload(System.Text.Encoding.UTF8.GetBytes("not an mp3"))).StatusCode
        );
        var file = Directory.GetFiles(
            Path.GetFullPath("../../../../../content/audio", AppContext.BaseDirectory),
            "*.mp3"
        )[0];
        Assert.Equal(
            HttpStatusCode.Created,
            (await Upload(await File.ReadAllBytesAsync(file))).StatusCode
        );
    }

    [Fact]
    public async Task CrossLanguageAudioCannotBeAssignedAtDatabaseLevel()
    {
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
        var narration = await db.Narrations.FirstAsync(x => x.LanguageCode == "vi");
        var wrong = await db.AudioAssets.FirstAsync(x =>
            x.PlaceId == narration.PlaceId && x.LanguageCode == "en"
        );
        narration.AudioAssetId = wrong.Id;
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task ConcurrentIdempotentEventsInsertOnlyOneRow()
    {
        var (c, session) = await f.Consent();
        using (c)
        {
            var csrf = await c.GetFromJsonAsync<JsonElement>("/api/v1/csrf");
            var id = Guid.NewGuid();
            var payload = new
            {
                events = new[]
                {
                    new
                    {
                        eventId = id,
                        type = "page_view",
                        clientTimestamp = DateTimeOffset.UtcNow,
                        sessionId = session,
                        pageKey = "explore",
                    },
                },
            };
            async Task<string> Send()
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "/api/v1/analytics/events"
                )
                {
                    Content = JsonContent.Create(payload),
                };
                request.Headers.Add("X-CSRF-TOKEN", csrf.GetProperty("token").GetString());
                return await (await c.SendAsync(request)).Content.ReadAsStringAsync();
            }
            var results = await Task.WhenAll(Send(), Send(), Send());
            Assert.Single(results, x => x.Contains("accepted"));
            Assert.Equal(2, results.Count(x => x.Contains("duplicate")));
        }
    }

    [Fact]
    public async Task AcceptedCompletionSurvivesExpiredStartCleanup()
    {
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
        var place = await db.Places.FirstAsync(x => x.Status == "published");
        var asset = await db.AudioAssets.FirstAsync(x =>
            x.PlaceId == place.Id && x.LanguageCode == "vi"
        );
        var cutoff = RetentionWorker.Cutoff(DateTimeOffset.UtcNow).ToUniversalTime();
        var s = new AnalyticsSession
        {
            VisitorId = Guid.NewGuid(),
            StartedAt = cutoff.AddMinutes(-5),
        };
        db.AnalyticsSessions.Add(s);
        await db.SaveChangesAsync();
        var play = Guid.NewGuid();
        var old = cutoff.AddSeconds(-1);
        var start = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            SessionId = s.Id,
            EventType = "narration_started",
            PlaceId = place.Id,
            LanguageCode = "vi",
            PlayId = play,
            AudioAssetId = asset.Id,
            PlayStartedAt = old,
            DurationSeconds = asset.DurationSeconds,
            ClientTimestamp = old,
            ReceivedAt = old,
            Trigger = "manual",
            PayloadHash = new byte[32],
        };
        var end = new AnalyticsEvent
        {
            Id = Guid.NewGuid(),
            SessionId = s.Id,
            EventType = "narration_completed",
            PlaceId = place.Id,
            LanguageCode = "vi",
            PlayId = play,
            AudioAssetId = asset.Id,
            PlayStartedAt = old,
            DurationSeconds = asset.DurationSeconds,
            PlayedSeconds = asset.DurationSeconds,
            ClientTimestamp = cutoff.AddMinutes(3),
            ReceivedAt = cutoff.AddMinutes(3),
            PayloadHash = new byte[32],
        };
        db.AnalyticsEvents.AddRange(start, end);
        await db.SaveChangesAsync();
        await db.AnalyticsEvents.Where(x => x.ReceivedAt < cutoff).ExecuteDeleteAsync();
        Assert.True(await db.AnalyticsEvents.AnyAsync(x => x.Id == end.Id));
        Assert.False(await db.AnalyticsEvents.AnyAsync(x => x.Id == start.Id));
    }

    [Fact]
    public async Task TranscriptChangesCannotKeepOutdatedPublishedAudio()
    {
        using var c = f.Client();
        await f.Login(c);
        var all = await c.GetFromJsonAsync<JsonElement>("/api/v1/admin/places");
        var record = all.EnumerateArray()
            .First(x => x.GetProperty("status").GetString() == "published");
        var id = record.GetProperty("id").GetGuid();
        var original = record.Deserialize<SaigonGuide.Api.Contracts.PlaceInput>(
            ContentService.Json
        )!;
        var changed = original with
        {
            Narrations = original
                .Narrations.Select(n =>
                    n.LanguageCode == "vi"
                        ? n with
                        {
                            Transcript = n.Transcript + " Nội dung đã sửa.",
                        }
                        : n
                )
                .ToList(),
        };
        var result = await f.Send(c, HttpMethod.Put, $"/admin/places/{id}", changed);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        using var scope = f.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
        var stored = await db
            .Narrations.AsNoTracking()
            .SingleAsync(n => n.PlaceId == id && n.LanguageCode == "vi");
        Assert.Equal(
            original.Narrations.Single(n => n.LanguageCode == "vi").Transcript,
            stored.Transcript
        );
        Assert.NotNull(stored.AudioAssetId);
    }

    [Fact]
    public async Task PublicSearchIgnoresVietnameseDiacritics()
    {
        using var c = f.Client();
        var list = await c.GetFromJsonAsync<JsonElement>("/api/v1/places?q=dinh%20doc%20lap");
        Assert.Single(list.EnumerateArray());
        Assert.Equal("dinh-doc-lap", list[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task AccountDateFilterIsNotLimitedByAnalyticsRetention()
    {
        using var c = f.Client();
        var email = $"older-{Guid.NewGuid():N}@example.com";
        var registered = await f.Post(
            c,
            "/auth/register",
            new
            {
                email,
                password = ApiFixture.Password,
                displayName = "Older account",
            }
        );
        var user = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var id = user.GetProperty("id").GetGuid();
        using (var scope = f.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GuideDb>();
            await db
                .Users.Where(x => x.Id == id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(x => x.CreatedAt, DateTimeOffset.UtcNow.AddDays(-200))
                );
        }
        await f.Login(c);
        var date = DateTime.UtcNow.AddHours(7);
        var result = await c.GetFromJsonAsync<JsonElement>(
            $"/api/v1/admin/users?from={date.AddDays(-201):yyyy-MM-dd}&to={date.AddDays(-199):yyyy-MM-dd}"
        );
        Assert.Contains(
            result.GetProperty("items").EnumerateArray(),
            u => u.GetProperty("id").GetGuid() == id
        );
    }
}
