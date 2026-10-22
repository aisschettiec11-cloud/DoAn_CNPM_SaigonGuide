# Product Backlog và Sprint Backlog 1.0

Baseline PRD đã chốt; 3 Sprint/6 tuần là giả định thời gian nhóm, không là hạn chế số lượt AI thực hiện. A backend, B du lịch, C quản trị/tích hợp; một người review chéo. Story Points tương đối, không quy đổi giờ. Trạng thái ban đầu Planned; cập nhật bằng bằng chứng cuối mỗi Sprint.

DoD chung: business AC đúng, backend kiểm quyền/validation, review logic, test liên quan pass, build, tích hợp frontend/backend thật, tài liệu cập nhật; không secret/PII log. Dependencies phải hoàn thành trước task. Không tính mock/zero-test run là done.

| ID | Story / FR | Mức | SP | Phụ thuộc | Owner | Sprint | Nghiệm thu |
|---|---|---|---|---|---|---|---|
| PB01 | Nền tảng .NET/React/PostgreSQL/CI | M | 5 | thiết kế | A/C | 1 | lock restore/build/test và health DB thật |
| PB02 | Guest xem map/list/chi tiết VI/EN, FR01–03/16–17/19 | M | 5 | PB01 | B/A | 1 | 6 điểm từ API, hidden404, đổi locale đúng |
| PB03 | Admin login/logout/password/authorization FR24/26/28/29 | M | 5 | PB01 | A | 1 | cookie session/CSRF, expiry/revoke401, Guest401/User403 |
| PB04 | Player một điểm thật FR12/18/20/21 | M | 5 | PB02 | B | 1 | play/pause/resume/replay/stop, fallback transcript, không phát chồng |
| PB05 | GPS/geofence FR05–15 | M | 8 | PB02/PB04 | B | 2 | quality/two samples/latch/hysteresis/cooldown và denied/manual |
| PB06 | Content CRUD/publish/audio FR30–34 | M | 8 | PB03/PB02 | A/C | 2 | full VI/EN gate, decode upload, version409, audit nguyên tử |
| PB07 | Consent/session/event ingestion FR42 | M | 8 | PB03/PB04 | A/B | 2 | opt-in, no GPS, event/play dedupe, client không đặt user/KPI |
| PB08 | Dữ liệu/âm thanh 6 điểm có nguồn | M | 5 | PB04/PB06 | C/B | 2 | source/license labels và verified/demo phân biệt |
| PB09 | Dashboard/metrics/timezone FR43–51 | M | 8 | PB07 | A/C | 3 | fixture exact, distinct/cohort đúng, hidden/empty/N/A/errors |
| PB10 | Signup/profile/password và quản lý khách FR22/23/25/27/35–41 | S | 8 | PB03/PB06 | A/C | 3 | User không admin, lock revoke, ownership, search/filter/page |
| PB11 | E2E/security/accessibility/mobile/performance | M | 8 | PB05–09 | C/A/B | 3 | test reports thật, không lỗ auth/CSRF, fallback hoạt động |
| PB12 | Docker/HTTPS/backup/restore và hướng dẫn | M | 5 | PB11 | C/A | 3 | local Docker smoke/restore; public deploy cần hosting/domain |
| PB13 | Search/link directions FR04/07 | S | 2 | PB02 | B | 2 | search có empty/reset; directions không gửi GPS khách |
| PB14 | CI triển khai tự động | S | 3 | PB12 + hosting | C | sau MVP | không claim deploy khi chưa có credentials/host |
| PB15 | TTS live/email reset/favorites/export/offline | C | chưa ước lượng | MVP | nhóm | sau MVP | không nằm cổng nghiệm thu hiện tại |

## Increment từng Sprint

1. Sprint 1: PB01–04; demo một điểm DB→API→React→player, Admin auth; lưu build/test evidence.
2. Sprint 2: PB05–08 và PB13 nếu khả thi; GPS/audio/consent/CRUD đầu-cuối; unit và integration có lỗi được sửa trước tiếp tục.
3. Sprint 3: PB09/11/12; PB10 khi Must ổn định; Dashboard fixture, security/E2E/restore; phân biệt bản local sẵn dùng với public hosting chưa cấp.

Sprint Review ghi trạng thái trong `docs/Ket-qua-kiem-thu.md`, không đánh dấu mọi việc done chỉ vì đã tạo file. Public release còn cần kiểm nội dung thực địa/quyền tài nguyên và hosting; dữ liệu mẫu không đủ để claim GPS chính xác ngoài thực tế.

## Kết quả thực hiện theo Sprint

Các Sprint dưới đây là cách tổ chức phạm vi trong lần triển khai này; không khẳng định đã trải qua6tuần hoặc có ba sinh viên thực hiện/review. Phân công A/B/C vẫn là kế hoạch cho nhóm, review con người và capacity lịch thực cần nhóm bổ sung.

| Sprint | Increment thực tế | Bằng chứng | Phần còn ngoài workspace |
|---|---|---|---|
| 1 — PB01–04 | .NET/React/PostgreSQL, migration, sáu điểm API VI/EN, map/list/detail, player, cookie auth/CSRF, CI YAML | build, public-content/audio Range và authorization tests; browser Guest/Admin | CI GitHub run chưa có; code chưa push |
| 2 — PB05–08/PB13 | GPS/geofence, fallback, consent/event/session/dedupe, Admin CRUD/publish/version/audio, nguồn và12MP3, search/directions | GPS unit, DB integration, upload/decoding, concurrent event dedupe, E2E deny GPS/audio/Admin | GPS thực địa và chất lượng thuyết minh trên thiết bị thật cần kiểm tiếp |
| 3 — PB09–12 | SQL aggregate Dashboard, guest accounts/profile/password/lock, responsive, benchmark, Docker non-root, backup/restore và tài liệu | test report, 100.000-event benchmark5phút, Docker smoke và restore database riêng | HTTPS public/domain/hosting, contact vận hành, Android/iOS/screen reader thật |

PB14 chưa thực hiện vì chưa có hosting/domain; PB15 không nằm MVP. PB12 đã có Docker chạy và hướng dẫn HTTPS, không đánh dấu public release done. Không trộn database benchmark/E2E vào dữ liệu người dùng. Báo cáo cuối và status từng hạn chế nằm trong `Ket-qua-kiem-thu.md`.
