# PRD Saigon Guide — phạm vi và hiện trạng bàn giao

Phiên bản1.2, ngày23/10/2026 (UTC+7). Tài liệu này ghi trạng thái sản phẩm hiện tại; [baseline đã chốt](PRD-Saigon-Guide.md) giữ nguyên các yêu cầu/US/BR/TC ban đầu để truy vết. Các đoạn “chưa triển khai” trong baseline là lịch sử tại thời điểm thiết kế, không thay thế bảng hiện trạng dưới đây. [TEST_REPORT](TEST_REPORT.md) là nguồn kết quả kiểm thử lần bàn giao; đề xuất tương lai không tự trở thành phạm vi đã phê duyệt.

## 1. Tổng quan, vấn đề và giá trị

Saigon Guide là website giúp khách tự khám phá sáu điểm TP.HCM bằng bản đồ, nội dung Việt–Anh và thuyết minh MP3. Giải quyết nội dung phân tán, khác biệt ngôn ngữ và nhu cầu nghe khi đến gần địa điểm. Giá trị: một luồng khám phá/đọc/nghe không cần tài khoản; Admin biên tập và xem mức quan tâm của những người đồng ý Analytics.

Không đặt mục tiêu số khách thật khi chưa có traffic. Dữ liệu benchmark và tài khoản test không là bằng chứng sử dụng sản phẩm thật.

## 2. Người dùng và personas

| Persona | Nhu cầu | Quyền |
|---|---|---|
| Khách Việt đi bộ | Tìm điểm, nghe VI, vẫn đọc khi GPS lỗi | Public, GPS/Analytics tự chọn |
| Khách quốc tế | Nội dung EN rõ ràng, nghe thủ công | Public VI/EN |
| User đã đăng ký | Quản lý tên/ngôn ngữ/mật khẩu | Hồ sơ của mình, không Admin |
| Admin nội dung | Sửa nội dung/âm thanh, xuất bản có kiểm chứng | Admin API, Dashboard, quản lý khóa User |
| Người vận hành | Setup, backup, restore, triển khai | Công cụ ngoài web; không có quyền production ngầm |

Guest không tạo bản ghi User. Chỉ hai role `Admin`/`User`, không public signup Admin.

## 3. Phạm vi MVP đã thống nhất

Must: sáu điểm/map/list/chi tiết VI–EN,12MP3+transcript, GPS chủ động và geofence chống lặp, player, Admin auth/content, Analytics consent/dedupe và Dashboard, PostgreSQL/migration/test/tài liệu, quy trình triển khai. Should đã xây: tìm kiếm không dấu, chỉ đường ngoài, đăng ký/hồ sơ User, quản lý/khóa User, browser tests.

Sáu điểm: Dinh Độc Lập, Nhà thờ Đức Bà, Bưu điện Trung tâm, Chợ Bến Thành, Bảo tàng Chứng tích Chiến tranh, Phố đi bộ Nguyễn Huệ. Tọa độ đối chiếu nguồn online; chưa xác nhận thực địa. MP3 là giọng tổng hợp eSpeak, hình là SVG minh họa; không giả là giọng người/ảnh chụp. Nguồn/license tại [content](../content/README.md).

Ngoài MVP/chưa xây: native app, offline đầy đủ, GPS nền/hành trình cá nhân, đặt vé/thanh toán, email reset password, yêu thích, social login, live TTS, route engine, CD tự động. Swagger/OpenAPI máy đọc được chưa có; API contract hiện là Markdown.

## 4. User stories và acceptance criteria

| Story | Acceptance criteria cốt lõi | Bằng chứng hiện tại |
|---|---|---|
| Guest khám phá | Không login vẫn xem6điểm, đổi EN và tìm kiếm | API integration + browser PASS |
| Guest nghe | MP3 phát/pause/resume, transcript vẫn dùng khi deny GPS | Browser phát âm thanh thật PASS; mọi lỗi mạng audio chưa bao phủ |
| Guest gần điểm | Hai mẫu GPS tốt xác nhận entry, latch/hysteresis/cooldown |7unit PASS; tích hợp cấp vị trí thật chưa nghiệm thu |
| User bảo vệ hồ sơ | Chỉ sửa mình; role phía server; logout thu hồi cookie cũ | API + browser PASS; mọi biến thể profile chưa bao phủ |
| Admin xuất bản | Đủ dịch/nguồn/audio, version cũ409, hidden không public | Integration PASS |
| Admin xem KPI | Fixture tính distinct/cohort đúng, không tạo số giả | Integration + browser PASS |
| Khách kiểm soát dữ liệu | Chưa consent không ingest; withdraw dừng thu thập | API + browser PASS |

US-01..US-51 và TC tương ứng được giữ tại baseline mục29. Mỗi case kế hoạch không tự được coi là đã chạy.

## 5. Functional requirements và hiện trạng từng FR

PASS = hành vi trong phạm vi bằng chứng đã chạy đạt. PARTIAL = có code nhưng acceptance/thiết bị hoặc nhánh lỗi chưa được kiểm hết; không đồng nghĩa bỏ chức năng. FAIL = kết quả hiện hành lỗi. NOT IMPLEMENTED = chưa xây. BLOCKED = thiếu hạ tầng/quyền để xác minh. Các số FR giữ nguyên baseline; bằng chứng chi tiết tại TEST_REPORT.

| FR | Yêu cầu | Trạng thái | Bằng chứng/giới hạn |
|---|---|---|---|
| FR-01 | Hiển thị bản đồ | PASS | Browser tile failure/list + screenshot có tiles |
| FR-02 | Marker địa điểm | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-02 của baseline |
| FR-03 | Chi tiết địa điểm | PASS | API content/locale/hidden + browser bilingual |
| FR-04 | Tìm kiếm/lọc | PASS | API unaccent search + browser empty/reset |
| FR-05 | Xin quyền GPS | PASS | Browser deny GPS, không chặn nghe |
| FR-06 | Tính khoảng cách | PASS | GPS Haversine unit |
| FR-07 | Mở chỉ đường ngoài | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-07 của baseline |
| FR-08 | Theo dõi vị trí | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-08 của baseline |
| FR-09 | Xác nhận vào vùng | PASS | GPS two-fixes unit |
| FR-10 | GPS kém chính xác | PASS | GPS stale/accuracy/future unit |
| FR-11 | Chống kích hoạt lặp | PASS | GPS repeated fixes/latch unit |
| FR-12 | Phát thủ công/tự động | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-12 của baseline |
| FR-13 | Tắt tự động | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-13 của baseline |
| FR-14 | Nhiều điểm gần nhau | PASS | GPS tie-break/busy-player unit |
| FR-15 | Kích hoạt lại | PASS | GPS exit/cooldown unit |
| FR-16 | Chọn ngôn ngữ | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-16 của baseline |
| FR-17 | Lấy đúng nội dung | PASS | API VI/EN content test |
| FR-18 | Điều khiển player | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-18 của baseline |
| FR-19 | Đọc thuyết minh | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-19 của baseline |
| FR-20 | Thiếu dịch/âm thanh | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-20 của baseline |
| FR-21 | Đổi ngôn ngữ đang nghe | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-21 của baseline |
| FR-22 | Đăng ký khách | PASS | API role injection + browser registration |
| FR-23 | Đăng nhập khách | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-23 của baseline |
| FR-24 | Logout và quản lý phiên | PASS | API copied-cookie logout revocation |
| FR-25 | Hồ sơ khách | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-25 của baseline |
| FR-26 | Đổi mật khẩu | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-26 của baseline |
| FR-27 | Trạng thái tài khoản khách | PASS | API lock revocation |
| FR-28 | Kiểm tra quyền | PASS | API Guest401/User403/role injection |
| FR-29 | Đăng nhập Admin | PASS | API/browser Admin login |
| FR-30 | Quản trị địa điểm | PASS | API publish/hide/concurrency + browser edit |
| FR-31 | Tọa độ và bán kính | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-31 của baseline |
| FR-32 | Bản dịch thuyết minh | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-32 của baseline |
| FR-33 | Tài nguyên âm thanh | PASS | API fake MP3 reject/decoded MP3 accept |
| FR-34 | Kiểm tra công khai | PASS | API publish missing-content gate |
| FR-35 | Danh sách tài khoản | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-35 của baseline |
| FR-36 | Tìm tài khoản | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-36 của baseline |
| FR-37 | Lọc tài khoản | PASS | API account date-filter test (không retention90ngày) |
| FR-38 | Phân trang tài khoản | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-38 của baseline |
| FR-39 | Chi tiết tài khoản | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-39 của baseline |
| FR-40 | Khóa/mở khóa khách | PASS | API lock/unlock không hồi sinh phiên |
| FR-41 | Chỉ số tài khoản | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-41 của baseline |
| FR-42 | Ghi sự kiện | PASS | API consent/dedupe/concurrent/noGPS + browser withdraw |
| FR-43 | KPI tổng quan | PASS | API Dashboard fixture distinct totals |
| FR-44 | Xu hướng truy cập | PASS | Timezone/calendar unit + Dashboard fixture |
| FR-45 | Xếp hạng địa điểm | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-45 của baseline |
| FR-46 | Thống kê xem điểm | PASS | API Dashboard fixture views/sessions |
| FR-47 | Thống kê thuyết minh | PASS | API completion/start/cohort validation |
| FR-48 | Thống kê ngôn ngữ | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-48 của baseline |
| FR-49 | Bộ lọc thời gian | PASS | API range rejection + calendar unit |
| FR-50 | Chưa có dữ liệu | PARTIAL | Đã có code; chưa chạy độc lập đủ acceptance case TC-50 của baseline |
| FR-51 | Quyền xem thống kê | PASS | API Guest401/User403 Dashboard |

Không có FAIL còn tái hiện trong45case tự động hiện hành. Các FR PARTIAL phải được nghiệm thu bổ sung trước khi nói “100% acceptance criteria”. GPS thực địa và đa browser là giới hạn xuyên suốt, kể cả khi logic unit PASS.

## 6. Non-functional requirements và UX

Mobile-first cream/sage, điều hướng map/list/detail, thông báo lỗi/empty/loading, transcript và player một bài, trang Admin tải riêng. Chromium viewport360/390/768/1440 đã kiểm; chưa audit accessibility/screen reader hoàn chỉnh.

Mục tiêu public API P95≤500ms, Dashboard≤1s với100k events, sáu card usable≤3s trên cấu hình mạng giả lập đã định nghĩa. Benchmark **lần trước** đạt theo [bằng chứng có ngày](test-evidence/performance.json); không chạy lại benchmark trong lần bàn giao này và không suy ra hiệu năng hosting tương lai. Mục tiêu uptime/Internet/điện thoại thật chưa đo. Build dùng lockfiles, TS strict, migration có ràng buộc và database test riêng.

## 7. Kiến trúc và stack thực tế

React19/TypeScript5/Vite7/Leaflet/Recharts → REST cùng origin → ASP.NET Core10 Controllers → Services → EF Core10/Npgsql → PostgreSQL17. Node24, SDK10.0.402, xUnit/Vitest5/Playwright. FFmpeg/ffprobe kiểm MP3. Đơn khối, không microservices. React build được backend phục vụ; development Vite5173 proxy API5080. Docker dùng volume DB/media/keys, Caddy chuẩn bị HTTPS. Không có dịch vụ cloud tính phí tự tạo.

## 8. Luồng nghiệp vụ

Khám phá → chọn ngôn ngữ → xem chi tiết → nghe/đọc; GPS và Analytics là hai lựa chọn riêng. GPS bật → browser nhận mẫu → lọc chất lượng → hai mẫu xác nhận → đề nghị nghe → tự phát chỉ khi người dùng chọn/browser cho phép. Admin đăng nhập → sửa draft → kiểm nội dung/âm thanh → publish → public API nhận bản hiện hành. Consent → session Analytics → batch allowlist events → validate/dedupe → SQL aggregate Dashboard; withdrawal dừng gửi mới. Sơ đồ chi tiết tại [luồng nghiệp vụ](Thiet-ke-luong-nghiep-vu.md).

## 9. API và mô hình dữ liệu

[API Specification](API-Specification.md) mô tả public places/media, auth/profile, Analytics consent/session/events và Admin content/users/dashboard. Health `/health/live` và `/health/ready`. JSON errors,422validation,401/403auth,409version conflict,429rate limit. Không có Swagger UI runtime.

10entity: Places, Languages, PlaceTranslations, Narrations, AudioAssets, Users, UserSessions, AnalyticsSessions, AnalyticsEvents, AdminAuditLogs. [ERD/schema](ERD-va-Database-Schema.md) và migrations là chi tiết triển khai. UUID, FK/checks/unique; composite audio-place-locale; hidden thay xóa điểm có lịch sử. Events không chứa vị trí khách. Media file riêng DB, metadata/hash/duration trong DB.

## 10. Quy tắc GPS và âm thanh

Chỉ xin vị trí sau click; không gửi tọa độ khách lên server. Accuracy≤40m, tuổi mẫu≤15s/future≤5s; lấy mẫu tối đa mỗi3s. Hai mẫu3–10s với `distance+accuracy≤R`; R75m mặc định, Admin25–500m. Latch qua sessionStorage không tọa độ; exit hai mẫu `distance-accuracy≥R+30m`, cooldown120s. Một ứng viên gần nhất/tieID, không ngắt player bận. Tắt/rời trang/ẩn tab dừng watcher; cần kiểm thực địa.

Một HTMLAudio, pause/resume giữ playID, replay tạo ID mới, đổi ngôn ngữ dừng bài cũ. Start chỉ khi playing; complete khi ended/played≥90% và server kiểm elapsed theo snapshot start. Không coi click nút là lượt nghe thành công. MP3 upload≤20MB/600s, probe và giải mã thật, không fetch URL tùy ý. Transcript thay đổi phải gắn audio mới trước publish.

## 11. Tài khoản, bảo mật và quyền riêng tư

Mật khẩu hash ASP.NET Identity PasswordHasher; session cookie opaque, DB chỉ hash handle, HttpOnly/SameSite, Secure ở Production. Idle30phút/max8giờ, DB kiểm trạng thái/role/revocation mỗi request. Logout/password/lock thu hồi; CSRF cho unsafe methods, rate limit login/API. Không cho public chọn role. File credential trong `.local`, không commit; production qua secret store. API lỗi không trả stack trace/password; không ghi GPS/email/IP vào event. Browser/OS TLS verification giữ nguyên.

## 12. Analytics và KPI

Opt-in độc lập GPS, không chặn Guest. Visitor ngẫu nhiên bảo vệ cookie90ngày, session Analytics idle30phút riêng auth. Event schema allowlist, batch≤20/32KB, queue≤100/5phút, UUID retry và semantic play dedupe. Không retrofit lịch sử Guest vào User.

Dashboard: pageviews, distinct sessions/visitors, place views/sessions, audio starts, completed cùng cohort và tỷ lệ, ngôn ngữ, tài khoản riêng, rank và trend. Nhiều pageviews của một session không tăng session. 0khi thật sự không có event; mẫu số0→N/A. Ngày giờ Asia/Ho_Chi_Minh, tuần thứ Hai, event retention90ngày lịch; tài khoản không bị áp retention90ngày khi lọc ngày đăng ký. Không có heatmap/tọa độ khách; Dashboard quan tâm **địa điểm**, không hành trình người dùng.

## 13. Nghiệm thu, giới hạn và quyết định còn lại

Nghiệm thu local: setup/restore/build/publish thành công;30backend+7GPS+8browser đạt; API/DB/audio/auth/Admin/consent kết nối thật, tài liệu và source được đồng bộ GitHub theo PROJECT_STATUS. Điều đó chưa nghiệm thu đầy đủ mọi TC gốc hoặc release Internet.

BLOCKED: host/domain/TLS public, staging/production smoke, GPS thực địa và kiểm Android/iOS/Safari thật. PARTIAL: nhận xét giọng đọc/nội dung bởi người phụ trách, kiểm IDE trên máy nhận, accessibility, backup scheduler/rotation production. NOT IMPLEMENTED: tính năng ngoài MVP kể trên.

Nhóm đã chốt MVP/stack/VI–EN/Admin–User/Analytics tự quản; không cần chốt lại. Nhóm còn cần chọn hosting/ngân sách/domain, người chịu trách nhiệm nội dung/quyền tài nguyên và liên hệ privacy, lịch backup/retention vận hành, thiết bị/địa điểm nghiệm thu GPS. Không tự mua dịch vụ hoặc công khai repository/dữ liệu. Rủi ro: OSM không SLA, browser chặn autoplay/GPS, độ chính xác đô thị, synthetic voice chất lượng hạn chế và filesystem host tạm làm mất media nếu không có volume.
