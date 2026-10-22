# Cài đặt và chạy Saigon Guide trên máy cá nhân

## 1. Công cụ và phạm vi đã xác minh

Cài Git, **Python 3.11+**, **Node.js 24**, **.NET SDK 10.0.402** (theo `global.json`), **Docker Desktop/Engine với Compose v2**, **FFmpeg và ffprobe** trên PATH. Windows dùng Linux containers; bật Docker Desktop trước khi setup. macOS dùng Docker Desktop; Linux dùng Docker Engine. Không cần cài PostgreSQL trực tiếp khi dùng hướng dẫn tự động.

Dùng VS Code với extension C# (C# Dev Kit tùy chọn), Python; hoặc Visual Studio có workload ASP.NET/web, phiên bản hỗ trợ .NET 10 và solution `.slnx`. Visual Studio cũ không hỗ trợ SDK này cần nâng cấp hoặc dùng VS Code/CLI. Không đổi target framework để chữa lỗi IDE.

Đã chạy script desktop và kiểm IDE configuration bằng CLI trên Linux cloud. **Chưa mở giao diện Visual Studio/VS Code trên Windows/macOS**, nên các hệ điều hành đó cần người nhận chạy smoke test theo mục 6. Không coi kiểm tra JSON là kiểm thử debugger thật.

Kiểm công cụ trong terminal mới:

```text
git --version
python --version
node --version
npm --version
dotnet --version
docker version
docker compose version
ffmpeg -version
ffprobe -version
```

Trên Linux/macOS, thay `python` bằng `python3` nếu cần. Nếu vừa thêm PATH, đóng/mở lại IDE và terminal. SDK phải đọc được `global.json` từ thư mục repository.

## 2. Lấy đúng source code

```bash
git clone --branch delivery/saigon-guide https://github.com/aisschettiec11-cloud/DoAn_CNPM_SaigonGuide.git
cd DoAn_CNPM_SaigonGuide
```

Branch bàn giao là `delivery/saigon-guide`. Khi nhóm đã merge vào main, có thể clone main. Với checkout đang ở branch bàn giao: kiểm `git status`, giữ thay đổi riêng trước khi `git pull --ff-only origin delivery/saigon-guide`. Không dùng reset hard hoặc force push.

Mở **thư mục gốc** bằng `code .` trong VS Code; hoặc mở **`SaigonGuide.slnx`** trong Visual Studio. Frontend nằm trong `apps/web`, backend là `apps/api/SaigonGuide.Api.csproj`, tests nằm trong `tests/SaigonGuide.Tests`. Hai file `KhongCoGi` và `SaigonGuide` ở gốc là file lịch sử của repository, không phải thư mục chứa ứng dụng; được giữ nguyên.

## 3. Setup tự động, giữ dữ liệu có sẵn

Chạy từ gốc repository:

```bash
python scripts/local.py setup
```

Script kiểm toolchain, tạo/start container `saigonguide-db` ở loopback55432 với volume `saigonguide-db`, chạy `npm ci`, locked `dotnet restore`, build React/.NET, copy React vào backend `wwwroot`, apply migration, seed sáu điểm/12MP3, tạo Admin **chỉ khi chưa có**. Không tải SDK desktop tự động. Cần Internet cho dependency/container lần đầu; không cần Maps API key.

Các file được tạo riêng trong `.local/`, đã ignore khỏi Git:

| File | Công dụng |
|---|---|
| `runtime.env` | DB connection, Development, URL5080; password ngẫu nhiên |
| `bootstrap.env` | Email Admin `admin@saigon.guide` và password ngẫu nhiên; đọc riêng trên máy |
| `ide-settings.json` | DB/media/keys/content dùng khi F5 IDE; chỉ nạp trong Development |
| `media/`, `keys/` | MP3 và data-protection keys bền vững |

Không có mật khẩu demo dùng chung. Không gửi `.local`, backup, cookie, hoặc log chứa thông tin đăng nhập lên GitHub. Trên Windows hạn chế quyền thư mục bằng ACL của tài khoản; chmod không thay thế Windows ACL. Đổi mật khẩu Admin sau lần đăng nhập đầu. Chạy setup lại giữ credential và nội dung đã sửa; không xóa volume để chạy lại.

Cấu hình desktop chỉ tự động hỗ trợ DB identity do script tạo. Nếu đã có container cùng tên nhưng password khác, kiểm file cấu hình/volume đang dùng; không reset DB. Thực hiện backup trước khi thay cấu hình.

## 4. Chạy và debug

Chạy frontend build + API cùng origin:

```bash
python scripts/local.py start
```

Ứng dụng lắng nghe loopback **5080**. Dùng browser trên **chính máy đó**, nhập `http://127.0.0.1:5080`. Health ở `/health/ready`, API public ở `/api/v1/places?language=vi`. Đây là cổng lấy từ script và `launchSettings.json`, đã kiểm bằng request thật. **Không có Swagger UI/OpenAPI endpoint** được cài trong ứng dụng; contract là [API Specification](API-Specification.md), không hướng dẫn mở `/swagger`.

VS Code: sau setup, chọn cấu hình **Saigon Guide API + built React** và F5. Task build dùng `dotnet` từ PATH. Visual Studio: chọn `SaigonGuide.Api` làm startup project, profile `http`, F5/Ctrl+F5. Chỉ chạy một instance trên5080; dừng CLI trước khi F5. `ide-settings.json` được backend nạp khi Development; environment và command-line vẫn có ưu tiên cao hơn file này. Không nạp file đó trong Production.

Để sửa React với HMR, giữ backend5080, mở terminal thứ hai:

```bash
python scripts/local.py dev
```

Vite dùng5173 (có thể chọn cổng kế tiếp khi bị chiếm; đọc terminal), proxy `/api` và `/health` đến5080. Source frontend thay đổi cần build lại trước khi chạy chế độ cùng origin5080. Không đưa file MP3/secret vào `public/`.

Các lệnh .NET trực tiếp sau setup (Development dùng profile http để đọc cấu hình IDE):

```bash
dotnet restore SaigonGuide.slnx --locked-mode
dotnet build SaigonGuide.slnx --no-restore
dotnet run --project apps/api --launch-profile http
```

`dotnet test` cần DB riêng như mục 5; chỉ build không khởi tạo DB. Khi code hoặc dependency thay đổi, chạy lại setup/build, không giả định `--no-build` tự compile.

## 5. Chạy kiểm thử

```bash
python scripts/local.py test
```

Script tạo DB riêng `saigonguide_test` nếu chưa có, đặt `SG_TEST_DB`, chạy `dotnet test SaigonGuide.slnx --logger trx`, rồi `npm --prefix apps/web test`. Integration tests reset dữ liệu DB test; tuyệt đối không đặt DB thật vào `SG_TEST_DB`. Cần FFmpeg/ffprobe cho test media. Kết quả kỳ vọng ở bản bàn giao:30backend +7GPS, không skip.

Browser suite hiện dùng harness Linux/cloud, không tự động chạy trong lệnh trên: `bash scripts/e2e.sh` tạo instance5081/DB `saigonguide_e2e_test`, chạy8Playwright cases rồi dừng instance. Harness dùng `scripts/env.py` và toolchain cloud `/workspace`; trên desktop ưu tiên kiểm thủ công hoặc chạy harness trong cloud đã setup. Không gọi harness này là tương thích Windows. Chromium cloud ở `/usr/bin/chromium`; CI dùng Playwright managed browser. Xem [báo cáo](TEST_REPORT.md).

## 6. Smoke test người nhận

1. Mở trang5080: có sáu điểm, đổi EN, tìm `dinh doc lap` ra Dinh Độc Lập.
2. Mở chi tiết, phát/tạm dừng/tiếp tục MP3, đọc transcript; từ chối GPS vẫn dùng được.
3. Đăng nhập bằng file bootstrap riêng, vào Admin/Dashboard, thử sửa điểm nháp; User thường bị chặn Admin.
4. Chọn đồng ý Analytics, duyệt/nghe; Dashboard phản ánh event được nhận. Từ chối/rút đồng ý dừng ghi mới. Không kỳ vọng event trước consent xuất hiện.
5. Kiểm `/health/ready`; kiểm browser Console/Network khi có lỗi. Không chia sẻ request có cookie/password.

GPS trên loopback được browser cho phép nếu người dùng cấp quyền; khi mở qua IP LAN trên điện thoại cần HTTPS hợp lệ. Chưa có URL staging/production, GPS thực địa hoặc kiểm Safari/iPhone thật.

## 7. Cấu hình DB riêng và lỗi thường gặp

DB tùy chọn: tạo PostgreSQL17/database và user có quyền migration, sửa `.local/runtime.env` và `.local/ide-settings.json` cho cùng connection. Trong terminal riêng, đặt `ASPNETCORE_ENVIRONMENT=Development`, `ConnectionStrings__Database`, `Media__Path`, `Keys__Path`, `Content__Path` bằng cơ chế biến môi trường của IDE/OS; dùng đường dẫn tuyệt đối. Sau restore/build, chạy `dotnet run --project apps/api --no-build --no-launch-profile` với `SG_COMMAND=migrate`, rồi `SG_COMMAND=seed`, rồi `SG_COMMAND=admin` cùng `Bootstrap__AdminEmail`/`Bootstrap__AdminPassword` lấy từ cấu hình riêng. Xóa `SG_COMMAND` và bootstrap variables trước khi chạy web. Lệnh `admin` chỉ dùng provision, không reset password. Script setup/test tự động không hỗ trợ DB tùy chọn; `dotnet test` cần `SG_TEST_DB` riêng kết thúc `_test`.

| Hiện tượng | Xử lý |
|---|---|
| Docker daemon unavailable | Bật Docker Desktop/Engine, chọn Linux containers, kiểm `docker version` |
| SDK not found / NETSDK1045 | Cài SDK10.0.402, kiểm PATH và hỗ trợ .NET10 của IDE |
| Python không tồn tại | Windows dùng `py -3` hoặc cài Python/add PATH; Linux/macOS dùng python3 |
| ffprobe/ffmpeg not found | Thêm thư mục binary vào PATH rồi mở lại terminal/IDE |
| Address already in use | Dừng đúng instance ứng dụng mình đã chạy; không kill tất cả dotnet |
| DB authentication failed | Kiểm runtime/IDE config cùng password của volume đã tạo; không xóa volume |
| Trang404/chưa có frontend | Chạy setup/build React và copy dist vào apps/api/wwwroot |
| Login HTTP local không giữ cookie | Chỉ dùng Development cho local; Production yêu cầu HTTPS và cookie Secure |
| Admin không đăng nhập sau đổi password | Bootstrap là credential provision ban đầu, không tự cập nhật sau đổi password |
| Map trắng/503 | Kiểm outbound HTTPS tới a.tile.openstreetmap.org; dùng list; không tắt TLS |
| Analytics trống | Kiểm consent, khoảng ngày VN, event hợp lệ; không seed lượt thật giả |
| npm permission/cache error | Script desktop dùng cache trong .local; không cần sudo, không đổi integrity lockfile |
| Hết disk | Dọn cache build có thể tái tạo sau kiểm tra; không prune volumes/backup |

Cloud onboarding riêng: `bash scripts/setup.sh` và `bash scripts/start.sh`; đó là quy trình Linux cloud có SDK download SHA512, không phải trình cài Windows.
