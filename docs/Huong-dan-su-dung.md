# Hướng dẫn khách và Admin

## Khách du lịch

1. Mở trang Khám phá; không cần đăng nhập. Chọn VI hoặc EN trên thanh đầu trang.
2. Tìm tên điểm đến hoặc chọn dấu ghim trên bản đồ. Nếu nền bản đồ lỗi, danh sách vẫn dùng được.
3. Mở chi tiết để đọc bài, xem nguồn, nghe MP3 hoặc mở Google Maps chỉ đường. Google Maps là dịch vụ ngoài; Saigon Guide không tự tính tuyến đường.
4. Bấm Nghe để bắt đầu; player cuối màn hình có tạm dừng/tiếp tục, nghe lại và dừng. Chỉ một bài phát tại một thời điểm. Âm thanh ban đầu là giọng tổng hợp; ảnh là minh họa.
5. Bật định vị khi muốn xem khoảng cách và nhận lời mời gần điểm. Nếu GPS yếu/từ chối, tiếp tục nghe thủ công. Tự phát mặc định tắt và có thể bị trình duyệt chặn dù đã bật.
6. Tắt GPS hoặc chuyển khỏi màn hình du lịch/ẩn tab để dừng theo dõi. Tọa độ không gửi về server.
7. Chọn đồng ý hoặc từ chối Analytics. Mở Quyền riêng tư để thay đổi lựa chọn; từ chối không làm mất chức năng. Thống kê chỉ là ước tính.
8. Nếu muốn tài khoản: Đăng ký bằng email/tên/mật khẩu12–128 ký tự; vào Hồ sơ để sửa tên/ngôn ngữ hoặc đổi mật khẩu. Đổi mật khẩu thu hồi tất cả phiên. Chưa có khôi phục mật khẩu qua email; cần người vận hành hỗ trợ khi quên.

Tọa độ online chưa thay thế kiểm tra thực địa. Chú ý giao thông, quy định địa điểm và kiểm tra giờ/giá/lối vào với đơn vị quản lý.

## Admin

1. Lấy thông tin provision riêng từ người vận hành; trong workspace local xem `.local/bootstrap.env` ngoài Git. Không dùng tài khoản khách để vào Admin; không có signup Admin.
2. Đăng nhập và mở Quản trị. Tổng quan có khoảng ngày, nhóm ngày/tuần/tháng, KPI, chart và bảng dữ liệu. Tất cả theo giờ Việt Nam; N/A khác số0 và lỗi tải khác chưa có dữ liệu.
3. Mở Địa điểm & nội dung; thêm nháp hoặc sửa một điểm. Mã điểm không đổi sau tạo. Nhập tọa độ/radius25–500m, ảnh được phép, attribution/license và nguồn.
4. Biên tập VI/EN: tên, địa chỉ, mô tả, alt ảnh, transcript và nguồn. Lưu nháp được khi chưa đủ publish. Bản công khai phải giữ đủ cả VI/EN hoặc cần chuyển ẩn trước khi sửa thiếu. Thay transcript sẽ bỏ liên kết âm thanh cũ để tránh lệch lời; ẩn điểm, lưu bài và upload âm thanh phù hợp trước khi công khai lại.
5. Lưu nháp trước upload MP3. Tệp≤20MB, duration≤10phút; server kiểm định dạng và giải mã. Upload yêu cầu ghi quyền sử dụng, tạo asset mới và gán đúng ngôn ngữ. File lỗi bị từ chối, không giả báo thành công.
6. Chỉ đánh dấu xác minh sau khi đối chiếu nguồn thực sự. Dữ liệu mẫu không được public production. Bấm Kiểm tra & công khai để áp publish gate; thiếu trường sẽ giữ trạng thái cũ và báo lỗi.
7. Nếu gặp409: người khác đã sửa. Tải lại và đối chiếu nội dung, không tiếp tục ghi đè bằng version cũ. Giữ bản nháp riêng nếu cần sao chép phần đang làm.
8. Dùng Ẩn để bỏ khỏi website; không xóa lịch sử thống kê. Điểm ẩn vẫn có thể xuất hiện trong báo cáo với nhãn trạng thái.
9. Tài khoản khách: tìm/lọc/phân trang, bấm tên xem thông tin tối thiểu, khóa có lý do và xác nhận. Khóa thu hồi phiên hiện có; mở khóa không khôi phục phiên, khách phải đăng nhập lại. API này không quản lý Admin.
10. Đăng xuất sau khi sử dụng; Admin không xem được mật khẩu/token hoặc hành trình GPS cá nhân. Dashboard không dùng để khẳng định số khách thực tế hay chứng minh đã nghe/đến địa điểm.
