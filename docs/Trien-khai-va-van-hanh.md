# Triển khai và vận hành Saigon Guide

## 1. Trạng thái bàn giao

Ứng dụng được xây dựng và kiểm thử trong cloud workspace. Người dùng xác nhận **chưa có hosting hoặc tên miền**, nên chưa có địa chỉ public, TLS public hoặc triển khai GitHub CD thật. Không thay localhost bằng một link public giả. Cấu hình bên dưới là đường triển khai cụ thể, không phải bằng chứng đã thuê máy chủ.

Các bản DB tách biệt: `saigonguide` cho chạy cục bộ; `saigonguide_test` cho integration; `saigonguide_e2e_test` cho trình duyệt/benchmark; `saigonguide_restore_test` cho khôi phục. Không dùng fixture 100.000 event làm số liệu người dùng thật.

## 2. Local cloud

`bash scripts/setup.sh` rồi `bash scripts/start.sh`. Setup idempotent, giữ cấu hình/credentials riêng ở `.local/`, dùng lockfiles, apply EF migrations và INSERT seed ID chưa có. Không ghi đè nội dung Admin đã sửa. SDK pin trong global.json, bản tải dùng TLS và checksum SHA512 từ metadata chính thức. Node24 và FFmpeg/ffprobe phải có; môi trường hiện tại đã có.

Process không được giả định sống sau khi khôi phục snapshot: cần khởi động lại DB container và web theo startup instructions. Khi cần restart, chỉ dừng đúng tiến trình SaigonGuide.Api đang chạy từ checkout này. Không kill mọi dotnet hoặc xóa volume để chữa lỗi.

## 3. Docker trên máy chủ Linux

Prerequisites: Docker/Compose, dung lượng trống đủ build image (khuyến nghị ít nhất10GB trống), RAM phù hợp build .NET/Node; một VPS nhỏ cho demo có thể bắt đầu với2GB RAM và đo lại. Không tự mua dịch vụ hoặc cam kết giá chưa kiểm tra.

Tạo private env file bên ngoài Git, quyền600, với `POSTGRES_PASSWORD`, `ADMIN_EMAIL`, `ADMIN_PASSWORD`, `DOMAIN`, `ASPNETCORE_ENVIRONMENT=Production`. Mật khẩu đủ mạnh, không dùng ví dụ CI làm secret production. Không đưa file vào repo/image/log.

```bash
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml build app
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml up -d db
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml run --rm -e SG_COMMAND=migrate app
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml run --rm -e SG_COMMAND=seed app
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml run --rm bootstrap
docker compose --env-file /path/private-deploy.env -f deploy/compose.yml up -d app
```

`/path/private-deploy.env` là vị trí do người vận hành chọn, không phải file đã tồn tại. Provision Admin chỉ một lần; lệnh `admin` không ghi đè credential nếu email đã có. Mỗi lần phát hành sau: backup → migrate → app, không provision lại Admin hoặc reset DB. Image chạy user `app`, không root; database không mở cổng public; app bind cổng host loopback5088, frontend/API/media cùng origin. Volume database/media/keys bền vững; không `down -v` khi có dữ liệu cần giữ.

Để thử local không HTTPS, dùng private env `ASPNETCORE_ENVIRONMENT=Development`. Production cookie luôn Secure, yêu cầu HTTPS thật cho login. Không hạ Secure cookie để chạy public HTTP.

Trong môi trường có proxy doanh nghiệp, Docker build có thể dùng `--build-arg HTTP_PROXY --build-arg HTTPS_PROXY --build-arg NO_PROXY=localhost,127.0.0.1` và BuildKit secret `platform-ca` do môi trường cung cấp. Secret chỉ được dùng trong bước tải gói; không cài CA vào kho tin cậy browser/hệ thống, không tắt TLS. Build hỗ trợ optional named context `warm-nuget` để tái sử dụng package cache đã tải/xác minh; restore vẫn frozen lockfile. Máy Internet thông thường không cần cấu hình proxy/CA này.

## 4. HTTPS public

1. Cấp máy chủ và domain, trỏ DNS A/AAAA đúng host, mở80/443 theo nhà cung cấp.
2. Cấu hình private env DOMAIN là hostname thật; dùng Production, bổ sung thông tin liên hệ người vận hành trong chính sách quyền riêng tư trước phát hành.
3. Chạy thêm profile `--profile public up -d caddy`. Caddy cấp/chăm sóc TLS tự động khi DNS/port hợp lệ, reverse proxy đến app8080. Đây là điều kiện, chưa được kiểm chứng với domain thật trong workspace.
4. Kiểm `/health/ready`, sáu điểm, detail/audio Range, login/logout/CSRF, quyền Admin và Dashboard qua HTTPS/domain thật.
5. Kiểm cookie Secure/HttpOnly/SameSite, không public DB, không mount `.local` vào web root, không trả config/connection string.

Không phải mọi free hosting đều có persistent disk hoặc hỗ trợ Docker Compose. Nếu đổi hosting, cần bảo đảm PostgreSQL17, volume media/keys, HTTPS cùng origin và secret store; không tự đổi sang SQLite hoặc lưu audio trên filesystem tạm.

## 5. Network, bản đồ và quyền riêng tư

Runtime gọi cố định `a.tile.openstreetmap.org` bằng HttpClient kiểm TLS chuẩn; không nhận URL proxy tùy ý, giới hạn ô TP.HCM/zoom11–19, cache7ngày, giữ attribution. User-Agent nêu tên dự án. Không bulk download/prefetch toàn thành phố. OSM public tiles không có SLA; nếu traffic lớn, chọn nhà cung cấp tile phù hợp và cập nhật chính sách/chi phí.

Đã gặp CA của proxy cloud không được Chromium tin cậy. Auto-review từ chối thao tác thêm CA vào persistent browser trust store vì nguy cơ chặn bắt lưu lượng tương lai. Giải pháp hiện tại giữ nguyên browser trust, browser lấy tile cùng origin, server TLS đã được xác minh thực tế. Không dùng ignoreHTTPSErrors, rejectUnauthorized=false hoặc bỏ kiểm chữ ký gói để chữa lỗi.

Production ở host khác phải kiểm outbound HTTPS thật. Nếu mạng cần proxy/VPN, dùng cấu hình được nhà cung cấp cho phép, không cài VPN trong container ứng dụng hoặc tắt TLS.

## 6. Backup, khôi phục và rollback

Local: `python3 scripts/backup.py` tạo pg_dump custom + archive media/keys trong `.local/backups/<UTC>`, permissions600. `python3 scripts/verify-restore.py` phục hồi vào `saigonguide_restore_test` và thư mục tạm, kiểm số điểm/audio, không ghi đè DB chính. Backup chứa thông tin nhạy cảm, không upload/Git. Giữ tối đa7ngày theo chính sách vận hành; scheduler/rotation production phải được thiết lập ở host.

Production: dùng pg_dump từ service db, sao lưu cả media và data-protection keys, mã hóa/kho riêng và hạn chế quyền. Phục hồi ở môi trường riêng trước; sau restore chạy lại retention và áp dụng yêu cầu xóa trước khi mở phục vụ. Rollback image về commit/tag trước chỉ khi schema tương thích. Migration phá vỡ phải khôi phục backup phù hợp và đánh giá dữ liệu phát sinh; không giả định Down migration an toàn.

## 7. Log, health và vận hành

- `/health/live`: tiến trình; `/health/ready`: kiểm DB. Docker healthcheck dùng readiness. Không trả mật khẩu/internal exceptions.
- Log mặc định redacted; EF không bật sensitive data logging. Không dump env/request body/password/token/GPS.
- Retention job chạy khởi động rồi mỗi giờ: event90ngày lịch VN, auth session7ngày sau hết hiệu lực, audit180ngày, asset không tham chiếu quá24giờ. Log lần hoàn thành/số event xóa; lỗi không được xem như đã dọn thành công.
- Nếu hết dung lượng, kiểm DB/log/build cache. Chỉ dọn cache build có thể tạo lại; không prune volumes hoặc xóa dữ liệu thật. Workspace này đã gặp hết dung lượng do cache nhiều lần build, được xử lý bằng dọn cache rồi kiểm lại.
- Cấu hình bắt buộc: ConnectionStrings__Database. Production cần Keys__Path/Media__Path trên volume. Bootstrap secret chỉ dùng cho lệnh provision, nên gỡ khỏi môi trường web lâu dài sau tạo Admin.

## 8. Điều kiện trước public release

Hosting/domain/TLS thật; thông tin liên hệ vận hành; review nội dung/quyền tài nguyên; kiểm Android/iOS và GPS thực địa; backup scheduler/rotation; run CI thật trên GitHub; health/smoke sau deploy. Các kiểm tra chưa chạy phải ghi đúng là chưa chạy, không thay bằng kết quả mô phỏng.
