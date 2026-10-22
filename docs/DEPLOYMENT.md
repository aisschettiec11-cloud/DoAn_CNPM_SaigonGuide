# Triển khai Saigon Guide

| Môi trường | Trạng thái thực tế |
|---|---|
| Local CLI | Build React + ASP.NET Core ở5080, PostgreSQL Docker55432; đã kiểm |
| Local Docker | Compose DB/app riêng, loopback5088, persistent volumes; đã kiểm readiness |
| Staging | Chưa cấp host/domain; chưa triển khai và chưa kiểm HTTPS |
| Production | Chưa có hosting/domain/ngân sách; chưa triển khai |

Hướng dẫn build, migration, seed, provision Admin, Caddy HTTPS, backup/restore và rollback nằm trong [Triển khai và vận hành](Trien-khai-va-van-hanh.md). Đây là quy trình để người vận hành thực hiện, không phải bằng chứng đã deploy Internet. Không cần dịch vụ Analytics/Maps trả phí cho MVP; OSM không có SLA.

Thứ tự đưa lên Internet: chọn host hỗ trợ Docker và volume bền vững → cấp domain/DNS → đặt secrets trong secret store/file private → build image từ commit đã kiểm → backup/migrate/seed/provision lần đầu → chạy app/Caddy → xác minh HTTPS/cookie/auth/audio/GPS trên domain thật. Tách DB, media, keys và secret của staging khỏi production; không copy DB có thông tin người dùng sang staging để test.

Biến bắt buộc cho Compose: `POSTGRES_PASSWORD`; provision lần đầu thêm `ADMIN_EMAIL`, `ADMIN_PASSWORD`. Public thêm `DOMAIN` thật, giữ `ASPNETCORE_ENVIRONMENT=Production`. Native API dùng `ConnectionStrings__Database`, `Media__Path`, `Keys__Path`, `Content__Path`. Không commit giá trị thật hoặc nạp `.local/ide-settings.json` vào Production. Không đưa DB ra public. Backend Production phát cookie Secure; không thử login production qua HTTP rồi hạ bảo mật để chữa lỗi.

Trước phát hành: nhóm nghe/review12MP3 và nguồn nội dung, kiểm Android/iOS/GPS thực địa, bổ sung liên hệ người vận hành, đặt lịch backup/rotation và thử restore, kiểm outbound tile HTTPS, xem CI trên đúng commit. Chưa chạy các bước này thì ghi BLOCKED/PARTIAL trong báo cáo.
