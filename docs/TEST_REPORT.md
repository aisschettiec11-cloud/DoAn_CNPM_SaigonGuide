# Báo cáo kiểm thử bàn giao

Ngày23/10/2026 (UTC+7). Kết quả dưới đây được chạy lại trong workspace hiện hành; không chỉ sao chép kết luận cũ. Summary máy đọc được: [handover.json](test-evidence/handover.json). Kết quả benchmark cũ có ngày riêng, không được gọi là phép đo lần này.

## Môi trường và phiên bản

Linux x64 cloud, Node24.19.0/npm11.9.0, .NET SDK10.0.402/runtime10.0.13, PostgreSQL17 Docker, React19/Vite7/TypeScript5, Vitest5.0.5, Playwright1.56 và Chromium hệ thống, FFmpeg/ffprobe. Phiên bản dependency chính xác trong package-lock.json và packages.lock.json. Nguồn chạy tại `/workspace/DoAn_CNPM_SaigonGuide`; không có staging/production public.

## Lệnh và kết quả thực thi

| Lệnh/kiểm tra | Kết quả |
|---|---|
| `bash scripts/setup.sh` | PASS: frozen install/restore, build, migration, seed, Admin giữ nguyên |
| `python3 scripts/local.py setup` | PASS sau sửa cache npm; desktop helper chạy trên Linux, không claim Windows/macOS |
| `python3 scripts/local.py test` | PASS:30backend +7GPS,0failed/0skipped |
| `bash scripts/e2e.sh` | PASS:8Chromium cases trên DB e2e riêng,0failed |
| `npm --prefix apps/web run build` trong setup | PASS: TS strict + Vite production |
| `dotnet build SaigonGuide.slnx --no-restore` trong setup | PASS:0warnings/0errors |
| `python3 scripts/env.py dotnet publish apps/api -c Release --no-restore -o .local/handover-publish` | PASS |
| `npm --prefix apps/web audit --audit-level=high` | PASS:0vulnerabilities tại lần kiểm |
| `python3 scripts/env.py dotnet list apps/api/SaigonGuide.Api.csproj package --vulnerable --include-transitive` | PASS: nguồn advisory không báo vulnerable |
| Fresh clone GitHub source commit5ca8fc3: frozen npm/dotnet restore + frontend/backend build | PASS:12MP3/tài liệu đầy đủ, không có .local/secrets; dùng cache dependency đã xác minh |
| `python3 -m py_compile scripts/local.py` | PASS: syntax helper |
| Profile `http` với override cổng kiểm5090, không truyền DB env | PASS: Development đọc ide-settings.json, readiness/6điểm/EN/MP3Range206/search không dấu |
| Native5080 và Docker5088 | PASS: cùng smoke như trên; image Docker có sẵn chưa rebuild thay đổi desktop-helper lần này |
| `python3 scripts/backup.py` + `python3 scripts/verify-restore.py` | PASS: restore DB riêng6điểm/12audio assets/12MP3 |

Tổng **45passed,0failed,0skipped,0blocked trong suite tự động được chạy**. Các kiểm tra ngoài suite bị chặn/chưa chạy nằm ở mục giới hạn; không cộng chúng vào pass. Raw TRX, browser traces/logs được giữ private/ignored; chỉ summary không credential được commit.

## Danh sách case thực tế

### Backend unit và integration PostgreSQL

| Case | Kết quả |
|---|---|
| SaigonGuide.Tests.ApiTests.LockRevokesExistingUserSession | PASS |
| SaigonGuide.Tests.RulesTests.CalendarBucketsUseVietnameseTime | PASS |
| SaigonGuide.Tests.ApiTests.ConsentRequiredAndWithdrawalStopsEvents | PASS |
| SaigonGuide.Tests.RulesTests.LocaleRejectsUnsupported(s: "") | PASS |
| SaigonGuide.Tests.RulesTests.RejectsFutureAndReversedRanges | PASS |
| SaigonGuide.Tests.ApiTests.TranscriptChangesCannotKeepOutdatedPublishedAudio | PASS |
| SaigonGuide.Tests.ApiTests.MissingCsrfRejectedAndLogoutRevokesCopiedCookie | PASS |
| SaigonGuide.Tests.RulesTests.LocaleRejectsUnsupported(s: "fr") | PASS |
| SaigonGuide.Tests.RulesTests.TimezoneInclusiveRange | PASS |
| SaigonGuide.Tests.RulesTests.RetentionKeepsNinetyVietnameseCalendarDays | PASS |
| SaigonGuide.Tests.RulesTests.PasswordMinimum(s: "") | PASS |
| SaigonGuide.Tests.RulesTests.PasswordMinimum(s: "short") | PASS |
| SaigonGuide.Tests.RulesTests.InvalidEventShapeCannotInjectUnrelatedPlace | PASS |
| SaigonGuide.Tests.ApiTests.PublicSearchIgnoresVietnameseDiacritics | PASS |
| SaigonGuide.Tests.ApiTests.DashboardFixtureUsesDistinctVisitorsAndCohort | PASS |
| SaigonGuide.Tests.ApiTests.InvalidDashboardRangeRejected | PASS |
| SaigonGuide.Tests.ApiTests.AcceptedCompletionSurvivesExpiredStartCleanup | PASS |
| SaigonGuide.Tests.ApiTests.ConcurrentIdempotentEventsInsertOnlyOneRow | PASS |
| SaigonGuide.Tests.ApiTests.AccountDateFilterIsNotLimitedByAnalyticsRetention | PASS |
| SaigonGuide.Tests.ApiTests.MediaRejectsFakeMp3AndAcceptsDecodedFile | PASS |
| SaigonGuide.Tests.RulesTests.CompletionRequiresPlaybackDuration | PASS |
| SaigonGuide.Tests.RulesTests.SourcesRequireHttpsAndValidDate | PASS |
| SaigonGuide.Tests.ApiTests.EventDeduplicationAndNoGpsPayload | PASS |
| SaigonGuide.Tests.ApiTests.AnonymousAndUserCannotReadAdminAndCannotAssignRole | PASS |
| SaigonGuide.Tests.ApiTests.CrossLanguageAudioCannotBeAssignedAtDatabaseLevel | PASS |
| SaigonGuide.Tests.ApiTests.PublicContentHasSixPlacesCorrectLocaleAndPlayableAudio | PASS |
| SaigonGuide.Tests.ApiTests.PublishGateAndOptimisticConcurrencyAndHidden404 | PASS |
| SaigonGuide.Tests.RulesTests.PublishRejectsMissingTranslationAudioAndDemoProduction | PASS |
| SaigonGuide.Tests.RulesTests.LocaleRejectsUnsupported(s: "VI") | PASS |
| SaigonGuide.Tests.ApiTests.CompletedWithoutStartOrElapsedTimeRejected | PASS |

### GPS unit

| Case | Kết quả |
|---|---|
| calculates Haversine independently of language or GPS permission | PASS |
| rejects stale, imprecise, invalid and future samples | PASS |
| requires two valid fixes and does not repeat inside | PASS |
| resets candidates for stale fixes or intervals beyond 10 seconds | PASS |
| persists latch without storing coordinates | PASS |
| requires exit hysteresis and 120 seconds before rearm | PASS |
| selects one candidate deterministically and never interrupts a busy player | PASS |

### Browser Chromium

| Case | Kết quả |
|---|---|
| Guest explores six bilingual landmarks without an account | PASS |
| GPS denial preserves manual reading and real audio playback | PASS |
| Search empty state and tile failure keep the list usable | PASS |
| Registration, own profile, User403 and logout | PASS |
| Admin edits a landmark and reads real dashboard aggregates | PASS |
| Mobile layout fits 390px with working navigation | PASS |
| Desktop product screenshot | PASS |
| Consent records accepted views and playback, withdrawal stops collection | PASS |

## Lỗi phát hiện và kết quả sửa lại

- Script cũ chỉ dành cloud, pin đường dẫn `/workspace`; bổ sung `scripts/local.py`, private IDE config, tasks/launch và tài liệu desktop. Chạy setup/test/start thật bằng helper mới đạt.
- `launchSettings.json` còn cổng template5021; đổi5080 thống nhất hướng dẫn, kiểm profile thật ở cổng kiểm5090 để không chiếm instance đang chạy.
- Lần đầu desktop helper gặp npm cache mặc định không ghi được trong sandbox; đặt cache `.local/npm-cache`, giữ TLS/integrity, setup lại đạt. Một lần test chạy trong lúc npm ci chưa xong báo `vitest not found`; sau khi dependency ổn định, chạy lại toàn suite đạt. Không coi lượt thất bại ban đầu là pass.
- Trong soạn hướng dẫn đã phát hiện URL API nhầm `/api/places`; sửa thành `/api/v1/places?language=vi` theo controller và request thật200.
- Lỗi trước đó về SPA fallback Content-Type, ràng buộc media/ngôn ngữ, transcript/audio, event completion/retention, phiên logout/lock và account-date filter đã có regression tests; lần này30backend đều đạt. Lịch sử chi tiết tại [báo cáo trước](Ket-qua-kiem-thu.md).

## Kiểm giao diện và hiệu năng

Browser thật trong cloud đã kiểm6điểm/VI–EN, denyGPS+MP3, map tiles/list fallback, tìm rỗng, signup/profile/logout/User403, Admin edit/Dashboard, consent/withdraw và responsive. [Desktop](assets/desktop-home.png), [mobile](assets/mobile-home.png) là screenshot từ suite. Không phải ảnh mockup.

[API benchmark](test-evidence/performance.json) và [mobile giả lập](test-evidence/mobile-performance.json) là kết quả ngày22/10 của lượt trước, **không chạy lại** benchmark lần này. Dữ liệu100k event trong DB test, không phải traffic thật. Không có Lighthouse score/real-phone measurement.

## Hạng mục chưa kiểm và giới hạn

| Hạng mục | Trạng thái |
|---|---|
| GPS/geofence ngoài thực địa | BLOCKED: chưa có chuyến kiểm thực địa; unit/mocks không thay thế GPS thật |
| iOS/Android/Safari, screen reader thật | Chưa kiểm; viewport Chromium không chứng minh thiết bị thật |
| Windows/macOS và debugger GUI của VS/VS Code | Chưa kiểm; helper/profile/JSON chỉ xác minh trên Linux |
| Staging/production/HTTPS domain thật | BLOCKED: người dùng chưa có hosting/domain |
| Mọi TC-nn của baseline, mọi nhánh autoplay/lỗi audio/account/UI | Chưa tự động hóa đầy đủ; xem FR PARTIAL trong PRD |
| Nội dung và chất lượng12giọng đọc | Có nguồn/audio, chưa có nghiệm thu biên tập/nghe bởi nhóm |
| Backup scheduler/rotation và outbound tile trên host tương lai | Chưa cấu hình/kiểm ở production |
| GitHub Actions | Run39832146851: in_progress / chưa kết luận; xem PROJECT_STATUS. Local PASS không tương đương remote PASS |

Không có lỗi FAIL còn tái hiện trong45case hiện hành. Điều này không phải cam kết không còn bug hoặc đủ mọi acceptance criteria. Chưa nghiệm thu phát hành Internet.
