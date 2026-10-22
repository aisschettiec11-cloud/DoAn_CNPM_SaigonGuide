> Tài liệu baseline/lượt trước được giữ để truy vết. Hiện trạng bàn giao mới nhất: [PRD](PRD.md), [TEST_REPORT](TEST_REPORT.md) và [PROJECT_STATUS](PROJECT_STATUS.md). Những câu “chưa push” trong nội dung dưới đây mô tả thời điểm lượt trước.

# Kết quả kiểm thử và bàn giao Saigon Guide

## 1. Kết luận theo bằng chứng

Ứng dụng đã được lập trình, build, chạy và kiểm thử **trong cloud workspace**, bao gồm một bản Docker có PostgreSQL/media/keys riêng. **Chưa triển khai public**: người dùng xác nhận chưa có hosting hoặc tên miền. Không có bằng chứng HTTPS public, CI run trên GitHub, iOS/Android thật hoặc GPS thực địa.

Mã nguồn, PRD/ERD/API/cấu trúc thư mục/Backlog, migration, nội dung và hướng dẫn đã có trong checkout. Code đang ở working tree để người dùng review; chưa push GitHub. Phân công A/B/C và6tuần là kế hoạch nhóm, không giả làm lịch sử làm việc thực tế.

## 2. Kết quả tự động cuối cùng

| Kiểm tra | Lệnh/nguồn | Kết quả |
|---|---|---|
| Frontend typecheck + production build | npm --prefix apps/web run build | Đạt; TypeScript strict, Vite build thành công |
| Backend compile/publish | dotnet build/test, dotnet publish apps/api -c Release | Đạt; bản hiện hành build không lỗi |
| Backend unit + integration DB thật | python3 scripts/env.py dotnet test SaigonGuide.slnx --logger trx | **30 passed, 0 failed, 0 skipped** |
| GPS/geofence unit | npm --prefix apps/web test | **7 passed, 0 failed**; Vitest5.0.5 |
| Browser end-to-end | bash scripts/e2e.sh | **8 passed, 0 failed** trên Chromium; DB riêng e2e |
| Dependency audit npm | npm audit --audit-level=high | **0 vulnerabilities** theo registry tại lần kiểm tra |
| Dependency audit NuGet | dotnet list apps/api/SaigonGuide.Api.csproj package --vulnerable --include-transitive | Không có package bị nguồn advisory báo vulnerable |
| Setup replay | bash scripts/setup.sh | Chạy lại thành công, frozen restore, migration/seed không ghi đè dữ liệu có sẵn |
| Docker build | deploy/Dockerfile; sau đó đóng gói publish artifact mới nhất trên runtime đã build/kiểm tra | Đạt; không bỏ TLS/checksum/signature; bản cuối không cần tải network khi đóng gói lại |
| Docker runtime smoke | health, public data, EN transcript, MP3 Range206, login/dashboard/logout | Đạt; app user UID1654, DB/app healthy |
| Backup/restore | scripts/backup.py, scripts/verify-restore.py | Đạt trên database riêng: **6 điểm published, 12 audio assets**, phục hồi đủ12MP3 |

Tổng số case tự động chính: **45 passed** =30backend +7GPS +8browser. Đây không phải lời khẳng định toàn bộ TC-nn/DB-nn trong tài liệu đã được tự động hóa. Test reports gốc nằm ở thư mục ignored `tests/SaigonGuide.Tests/TestResults` và `apps/web/playwright-report`; không commit trace chứa thông tin đăng nhập test. Summary không bí mật nằm trong `docs/test-evidence`.

### Các nghiệp vụ đã được test

- Public6điểm/VI–EN, detail, audio byte Range; Guest dùng không login; API search không phân biệt dấu tiếng Việt.
- Guest401/User403, không tự truyền roleAdmin; CSRF chặn request thiếu token; login/logout đúng.
- Replay **cookie đã sao chép**: dùng được trước logout và401 sau logout, không chỉ kiểm UI đã xóa cookie.
- Lọc ngày đăng ký tài khoản không bị áp nhầm retention90ngày của Analytics. Khóa User thu hồi phiên; mở khóa không hồi sinh phiên cũ. Ownership hồ sơ và role backend được kiểm qua hành trình/API liên quan.
- Publish gate thiếu VI/EN/audio/verified bị từ chối; hidden404; version cũ409; concurrent event ID chỉ một accepted.
- Upload text giả MP3 bị từ chối; tệp thật được probe và giải mã; composite FK chặn gán sai ngôn ngữ.
- Sửa transcript của điểm published không được giữ audio cũ khác lời; rollback giữ bản hợp lệ.
- Consent bắt buộc cho event, withdrawal dừng đo; unknown GPS field bị từ chối; start/completion matching và elapsed checks.
- Fixture Dashboard distinct session/visitor/cohort tính đúng; timezone/day/week/date range; completion còn hạn sống qua cleanup start hết hạn.
- GPS Haversine, accuracy/stale/future, hai mẫu, latch/reload, hysteresis/cooldown, nhiều điểm và player bận.
- Browser: khám phá song ngữ, deny GPS nhưng nghe MP3 thật, pause/resume/stop, tile lỗi vẫn dùng list, search empty/reset, đăng ký/hồ sơ/logout/User403, Admin sửa nội dung và Dashboard, consent → view/start accepted → withdrawal.
- Responsive kiểm các viewport360/390/768/1440px trong case mobile; đây là viewport Chromium, không phải bằng chứng browser điện thoại thật.

## 3. Đo hiệu năng

### 3.1 API với100.000 sự kiện

Script `scripts/performance.py`, DB **saigonguide_e2e_test**, dữ liệu giả lập100.000event/1.000session/1.000visitor, không trộn vào DB người dùng. Thời gian300giây,20client đồng thời,750request mỗi endpoint, tổng1.500request; **0lỗi**. Trong từng wave có10request đọc và10Dashboard; 4giây/wave. Chạy trong môi trường5logicalCPUs, CPU quota4core, memory limit32GiB, DB cùng máy qua Docker; kết quả không đại diện mọi hosting hoặc Internet.

| Endpoint | P50 | P95 | Max | Mục tiêu PRD |
|---|---|---|---|---|
| Public places | 26,61ms | **61,94ms** | 196,23ms | P95≤500ms: đạt |
| Admin Dashboard | 422,93ms | **655,64ms** | 1.207,51ms | P95≤1.000ms: đạt; max không phải percentile |

Dashboard tổng hợp bằng SQL, không tải raw100.000event về frontend hoặc cộng distinct từng ngày. Xem [kết quả benchmark](test-evidence/performance.json). Không suy ra traffic hoặc số khách thật từ fixture này.

### 3.2 Frontend trên mạng/CPU giả lập

Chromium390×844, cache lạnh, RTT150ms, down1,6Mbps/up750Kbps, CPU throttle4×;5lần. Đo từ navigation tới sáu card địa điểm sử dụng được, **không chờ tile ngoài**. Mẫu:1604/1624/1719/1763/2076ms; median **1.719ms**, đạt mục tiêu≤3giây theo cách đo này. Không gọi đây là Lighthouse score, phép đo điện thoại thật hoặc toàn bộ map tải xong.

Bản đồ và Admin được tách chunk, HTML/JSON/JS có nén Brotli/Gzip. Xem [kết quả frontend](test-evidence/mobile-performance.json).

## 4. Giao diện được kiểm tra

[Ảnh desktop có bản đồ thật](assets/desktop-home.png) · [Ảnh mobile](assets/mobile-home.png).

Ảnh là screenshot app chạy, không mockup. Các minh họa điểm đến trong app là SVG nguyên bản, không giả là ảnh chụp địa điểm. Nguồn du lịch/coordinate và license trong [content README](../content/README.md).

## 5. Các lỗi đã phát hiện và xử lý

- SPA fallback thiếu Content-Type khiến browser hiển thị mã HTML: đã sửa, E2E xác minh trang thật và deep links.
- Browser cloud không tin CA proxy khi tải tile trực tiếp: auto-review từ chối thay persistent Chromium trust store. App hiện lấy tile cùng origin qua fixed-host server HTTPS với kiểm TLS chuẩn/cache7ngày; request server image/png200 và screenshot bản đồ được kiểm chứng. Không tắt xác minh TLS.
- Ràng buộc/checks NULL, cặp audio/place/locale và play ID được củng cố bằng migration và validation; completion snapshot tránh mất dữ liệu còn hạn khi start bị cleanup.
- Transcript chỉnh sửa có thể lệch audio cũ: đã thêm invalidation và publish gate/test.
- Công cụ test cũ có advisory: nâng Vitest5.0.5 tương thích Vite7/Node24; npm audit trở về0; NuGet PKCS được nâng bản đã vá.
- Docker build trong proxy cloud cần giữ CA ở build secret/scoped process, không persistent trust store; tái sử dụng NuGet cache đã xác minh và lockfile, không bỏ verification.
- Cache nhiều lần build làm đầy32GBdisk, khiến PostgreSQL mới/testDB khởi tạo lỗi. Đã dọn cache build có thể tạo lại, giữ volumes; khôi phục DB/service và chạy lại suite thành công. Không coi kết quả fail lúc hết disk là pass.

## 6. Hạn chế và việc cần trước public release

| Hạng mục | Trạng thái và hành động |
|---|---|
| Hosting/domain/HTTPS thật | **Chưa có** theo xác nhận người dùng. Cần cấp hạ tầng/DNS và chạy checklist deploy; chưa có URL public |
| GitHub CI/CD | YAML đã có; chưa push code/run Actions hoặc deploy GitHub thật. Không ghi “CI GitHub xanh” |
| GPS thực địa | Chưa chạy; tọa độ chỉ đối chiếu nguồn online, unit/browser dùng mock có kiểm soát |
| Android/iOS/Safari và screen reader thật | Chưa kiểm chứng; responsive Chromium không thay thế kiểm tra thiết bị/khả năng truy cập đầy đủ |
| Review nội dung và giọng đọc | Có nguồn và12MP3 tổng hợp; cần người phụ trách nghe/biên tập, xác minh quyền/nguồn cập nhật trước public |
| Chính sách vận hành | Bổ sung liên hệ operator, backup scheduler/rotation production, kiểm outbound tile và volume trên host thật |
| Post-MVP | Chưa có email reset/favorites/liveTTS/offline/CD tự động; không hiển thị tính năng giả |
| Cloud publication | install_script và start_skill đã lưu draft; **chưa Publish**, chưa kiểm khôi phục trong task mới |

## 7. Cách chạy lại

```bash
bash scripts/setup.sh
npm --prefix apps/web test
python3 scripts/env.py dotnet test SaigonGuide.slnx --logger trx
bash scripts/e2e.sh
python3 scripts/backup.py
python3 scripts/verify-restore.py
```

Benchmark chỉ trên DB tên `_test`; giữ instance e2e đang chạy trước `python3 scripts/performance.py`. Test/benchmark thay đổi dữ liệu test, không chạy với DB thật. Failed browser traces có thể chứa password test: giữ private/ignored. Cấu hình .local bootstrap/runtime và keys không được đưa vào Git.

Đã lưu install/start vào cloud environment draft để người dùng review và lưu trong Environment Settings, rồi **Publish môi trường** nếu muốn tạo snapshot tái sử dụng. Bước đó khác việc công bố website trên Internet. Fresh-task restore chưa được xác minh.
