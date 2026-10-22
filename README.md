# Saigon Guide

Website khám phá TP. Hồ Chí Minh với bản đồ, GPS và thuyết minh Việt–Anh; quản trị nội dung, tài khoản và Analytics có đồng ý.

## Lấy source và chạy trên máy cá nhân

```bash
git clone --branch delivery/saigon-guide https://github.com/aisschettiec11-cloud/DoAn_CNPM_SaigonGuide.git
cd DoAn_CNPM_SaigonGuide
python scripts/local.py setup
python scripts/local.py start
```

Cần Python3.11+, Node24, SDK.NET10.0.402, Docker và FFmpeg/ffprobe trên PATH. Linux/macOS dùng `python3` nếu cần. Script tạo password ngẫu nhiên trong `.local/bootstrap.env`; không có password demo dùng chung. API/web cùng origin ở loopback5080, Vite HMR5173. Không có Swagger UI; xem contract API Markdown. Hướng dẫn mở `SaigonGuide.slnx` bằng Visual Studio hoặc thư mục gốc bằng VS Code, F5, cấu hình và xử lý lỗi: [INSTALLATION](docs/INSTALLATION.md).

Kiểm thử desktop: `python scripts/local.py test` (30backend +7GPS, DB riêng). Browser harness8case hiện dành Linux cloud, xem hướng dẫn. Windows/macOS/IDE GUI chưa được kiểm thực tế; script desktop đã chạy trên Linux.

## Cấu trúc chính

```text
apps/api/       ASP.NET Core Controllers → Services → EF Core/Data/Migrations
apps/web/       React/TypeScript, public SVG, unit và Playwright tests
tests/          xUnit + PostgreSQL integration
content/        6 điểm,12MP3,12transcript và nguồn/license
scripts/        setup/start/test/backup/restore
deploy/         Dockerfile, Compose, Caddy
docs/           PRD, ERD, API, Backlog và bộ bàn giao
```

## Chạy trong môi trường cloud hiện tại

```bash
bash scripts/setup.sh
bash scripts/start.sh
```

Setup giữ cấu hình riêng trong `.local/`, cài phụ thuộc theo lockfile, build frontend/backend, chạy migration PostgreSQL, seed sáu điểm và provision Admin một lần. Không ghi đè nội dung hoặc tài khoản đã có. Thông tin Admin cục bộ nằm trong **`.local/bootstrap.env`**, quyền đọc giới hạn; xem file riêng trong workspace, không đưa vào chat/Git. Đổi mật khẩu qua Hồ sơ sau khi đăng nhập. Không có mật khẩu mặc định trong repository.

Tiến trình web/API dùng cổng **5080** trên loopback để kiểm tra trong máy; đây không phải URL public. Dev frontend HMR: chạy thêm `bash scripts/dev.sh` (cổng5173), proxy API đến5080. Không tạo worktree cho cloud task đã cách ly.

Prerequisites: Node24, SDK .NET10.0.402, Docker, Python3, curl, FFmpeg/ffprobe. `setup.sh` cài SDK vào `/workspace/.tools/dotnet` nếu cần bằng metadata chính thức + SHA512; công cụ khác phải có sẵn. Môi trường cloud này đã có Docker/Node/Python/FFmpeg. Restore script dành Linux x64; dùng Docker ở máy khác nếu chưa có toolchain.

## Chức năng

- Sáu điểm có nội dung VI/EN và nguồn; ảnh minh họa nguyên bản và 12 MP3 giọng tổng hợp, có transcript.
- Bản đồ Leaflet; ô nền do server lấy từ host OSM cố định bằng TLS chuẩn và cache7ngày. Attribution giữ nguyên, không bulk download; chỉ khu vực TP.HCM, zoom11–19. Tile lỗi không chặn danh sách.
- GPS chỉ bật sau thao tác, xử lý tọa độ tại browser; quality filter, hai mẫu, latch, hysteresis và cooldown; mặc định auto-play tắt, fallback nghe thủ công khi bị chặn.
- Một player cho play/pause/resume/replay/stop; đổi ngôn ngữ dừng bài cũ.
- Đăng ký/đăng nhập/hồ sơ/đổi mật khẩu; Admin có phân quyền backend, CRUD nội dung, upload/kiểm MP3, publish gate, version conflict và quản lý/khóa khách.
- Analytics opt-in, không GPS/email/IP trong event, session riêng auth, chống trùng; Dashboard distinct/cohort/timezone/zero/N/A theo PRD.

Tọa độ đã đối chiếu nguồn online, **chưa kiểm thử GPS ngoài thực địa**. Không khẳng định auto-play luôn thành công trên mọi browser. Đây là ứng dụng đã triển khai cục bộ; public hosting/domain/TLS và kiểm tra điện thoại thật là các điều kiện riêng trước phát hành công khai. Xem [hồ sơ nội dung](content/README.md).

## Kiểm thử

```bash
npm --prefix apps/web test
python3 scripts/env.py dotnet test SaigonGuide.slnx --logger trx
```

Backend integration dùng database riêng tên kết thúc `_test`, được reset để chạy fixture; tuyệt đối không trỏ `SG_TEST_DB` tới dữ liệu thật. Trong cloud `scripts/env.py` suy ra `saigonguide_test`; tạo một lần bằng `docker exec saigonguide-db createdb -U saigon saigonguide_test`. Test kiểm DB thật, auth/CSRF/revocation, publish/concurrency, event dedupe/completion/KPI/retention và media.

Browser tests: `SG_WEB_URL=http://127.0.0.1:5081 npm --prefix apps/web run e2e` sau khi dựng instance riêng `saigonguide_e2e_test`, có bootstrap file cục bộ. Chromium tại `/usr/bin/chromium` hoặc đặt `PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH=managed` sau `npx playwright install chromium`. Hướng dẫn đầy đủ và bằng chứng nằm trong [báo cáo kiểm thử](docs/Ket-qua-kiem-thu.md).

## Triển khai và vận hành

[Hướng dẫn Docker/HTTPS/backup/restore](docs/Trien-khai-va-van-hanh.md). Chưa có hosting/tên miền từ người dùng nên chưa có website public. Không có deploy tự động tới tài khoản bên ngoài hoặc secrets hardcode. CI GitHub Actions chạy cho push/PR main và push nhánh delivery; kết quả run thật được ghi tại [PROJECT_STATUS](docs/PROJECT_STATUS.md), không suy ra từ local tests.

## Bộ tài liệu bàn giao

- [PRD hiện trạng](docs/PRD.md)
- [Báo cáo kiểm thử hiện tại](docs/TEST_REPORT.md)
- [Cài đặt Visual Studio / VS Code](docs/INSTALLATION.md)
- [Triển khai local/staging/production](docs/DEPLOYMENT.md)
- [Trạng thái GitHub và công việc còn lại](docs/PROJECT_STATUS.md)

## Tài liệu thiết kế và bằng chứng trước đó

- [PRD](docs/PRD-Saigon-Guide.md)
- [ERD và schema](docs/ERD-va-Database-Schema.md)
- [API Specification](docs/API-Specification.md)
- [Cấu trúc thư mục](docs/Cau-truc-thu-muc.md)
- [Product Backlog và Sprint](docs/Product-Backlog.md)
- [Hướng dẫn khách và Admin](docs/Huong-dan-su-dung.md)
- [Báo cáo kiểm thử và giới hạn](docs/Ket-qua-kiem-thu.md)

Backend: Controllers → Services → EF Core/Data, PostgreSQL17. Frontend: React19/TypeScript/Vite7; các trang Admin tải tách khỏi bundle du lịch. Không microservices, không theo dõi hành trình GPS cá nhân.
