# Trạng thái bàn giao Saigon Guide

Ngày23/10/2026 (UTC+7). Source gốc: `/workspace/DoAn_CNPM_SaigonGuide`. React/TypeScript + ASP.NET Core10 + PostgreSQL17, đầy đủ code/tests/migrations/content và tài liệu. Chưa có hosting/domain public.

## Chức năng và kết quả

| Nhóm | Trạng thái | Bằng chứng/giới hạn |
|---|---|---|
|6điểm/map/list/search/VI–EN | PASS | API + Chromium thật, map loaded và tile-failure fallback |
|12MP3/transcript/player thủ công | PASS | API media decode/Range206 + browser playback |
| GPS/geofence | PARTIAL |7unit đạt, browser deny đạt; chưa thực địa/mọi nhánh watcher/autoplay |
| Signup/login/profile/Admin role | PASS trong luồng đã kiểm | API + browser; một số acceptance nhánh account còn PARTIAL trong PRD |
| Admin content/publish/upload/version | PASS trong luồng đã kiểm | Regression/API/browser; không có xóa cứng điểm lịch sử |
| Dashboard/Analytics consent | PASS trong luồng đã kiểm | Fixture distinct/cohort/dedupe, withdrawal thật; không thu GPS |
| DB/migrations/backup/restore | PASS local | PostgreSQL thật, restore DB riêng6điểm12assets12MP3 |
| Desktop setup/IDE | PARTIAL | Script và profile Linux đạt; chưa kiểm Windows/macOS/IDE GUI |
| Deploy Internet | BLOCKED | Thiếu host/domain; không có URL public |
| Ngoài MVP | NOT IMPLEMENTED | Offline/native/email reset/favorites/liveTTS/CD tự động |

Bảng51FR và các nhánh chưa nghiệm thu tại [PRD](PRD.md); không tuyên bố mọi TC gốc đã chạy.

## Thay đổi lần bàn giao

- Giữ hai commit và hai file gốc `KhongCoGi`, `SaigonGuide` của main, phát triển trên branch riêng; không force/reset/xóa lịch sử.
- Chuẩn hóa bộ tài liệu tên PRD/TEST_REPORT/INSTALLATION/DEPLOYMENT/PROJECT_STATUS.
- Bổ sung Python desktop setup/start/dev/test, private config chỉ đọc trong Development; môi trường/command-line vẫn ưu tiên.
- Thống nhất launch profile5080, thêm VS Code tasks/launch/extensions; ignore cache/IDE/private output.
- Chạy lại setup/build/release publish,45tests, dependency audits, profile smoke và backup/restore.
- Bổ sung CI chạy trên branch delivery; kết quả CI remote cần kiểm độc lập.

## Tài liệu bàn giao

[README](../README.md), [PRD](PRD.md), [TEST_REPORT](TEST_REPORT.md), [INSTALLATION](INSTALLATION.md), [DEPLOYMENT](DEPLOYMENT.md), tài liệu này; kèm [ERD](ERD-va-Database-Schema.md), [API](API-Specification.md), [cấu trúc](Cau-truc-thu-muc.md), [Backlog](Product-Backlog.md), [luồng](Thiet-ke-luong-nghiep-vu.md), [hướng dẫn sử dụng](Huong-dan-su-dung.md), [vận hành](Trien-khai-va-van-hanh.md).

## Kiểm thử và Git

45/45case tự động PASS (30backend +7GPS +8browser),0fail/skip; build/publish PASS. Evidence không bí mật tại [handover.json](test-evidence/handover.json). Runtime và từng case ghi trong TEST_REPORT. Các benchmark trước được giữ nguyên ngày và không gọi là chạy lại.

Repository đích: https://github.com/aisschettiec11-cloud/DoAn_CNPM_SaigonGuide

Branch bàn giao: `delivery/saigon-guide`, dựa trên main `aaf51ac9bc082c0c5c0eea6d7c4c1200644b573b`. Commit bàn giao và kết quả push được cập nhật sau xác minh remote. Hiện tại: đang chuẩn bị commit; chưa tuyên bố đã push. Main được giữ nguyên để nhóm review/merge branch. Không đổi visibility repository.

## Bước tiếp theo và tồn đọng

Người nhận clone branch theo INSTALLATION, chạy setup/start rồi smoke và F5 trên máy cá nhân. Credential ngẫu nhiên chỉ trong `.local/bootstrap.env`; không có password dùng chung trong repository. Dữ liệu cá nhân/secrets/raw browser traces/build output không thuộc bàn giao Git.

Nhóm còn cần nghiệm thu các FR PARTIAL, nghe/review nội dung/giọng đọc, kiểm GPS tại6điểm và Safari/Android/iOS, kiểm IDE Windows/macOS, chọn ngân sách/host/domain và người vận hành/privacy, đặt backup scheduler/rotation và thử phục hồi trên host thật. Sau review branch, merge an toàn vào main rồi triển khai staging/production theo DEPLOYMENT; kiểm CI và health/auth/audio/GPS qua HTTPS trước công bố. Không tự mua dịch vụ.

Cloud install_script/start_skill đã lưu draft ở lượt trước; việc lưu/publish môi trường khác với deploy website. Task mới đã chạy lại setup thành công; không giả định process cũ luôn tồn tại.
