# Nội dung Saigon Guide

Sáu điểm, 12 bài VI/EN, 12 MP3 và sáu minh họa nguyên bản.

- Nội dung là bản biên soạn riêng, không sao chép toàn bộ bài nguồn. Tham khảo cụ thể, ngày truy cập và phạm vi đối chiếu nằm trong `places.json` và được hiển thị tại từng địa điểm.
- Tọa độ đổi từ DMS công bố trên các trang nguồn Wikipedia sang decimal (6 chữ số). Đã đối chiếu nguồn ngày 22/10/2026; **chưa kiểm tra GPS thực địa**. Marker phố Nguyễn Huệ là một điểm tham chiếu, không toàn bộ tuyến phố.
- Không lưu giá vé/giờ mở cửa chưa xác minh hiện hành. Khách được hướng tới đơn vị quản lý để kiểm tra trước khi đi.
- MP3 là giọng tổng hợp eSpeak NG 1.52, đọc các script ở `text/`; encode bằng FFmpeg/libmp3lame 64kbps. Không phải giọng thu âm người thật. Thời lượng metadata lấy từ ffprobe; UI có transcript.
- Bài viết/script và âm thanh do dự án tạo: CC BY 4.0, ghi công “Saigon Guide”. Các sự kiện lịch sử được diễn đạt lại có dẫn nguồn, không khẳng định là bản thuyết minh chính thức của địa điểm. Tên eSpeak NG chỉ mô tả công cụ, không ngụ ý tác giả công cụ bảo trợ nội dung.
- SVG trong `apps/web/public/images` là minh họa hình học do dự án tạo, CC0-1.0; không mô tả chúng là ảnh chụp hoặc bản vẽ kiến trúc chính xác.
- Trước phát hành public: reviewer nội dung kiểm lại nguồn/biên tập và quyền, thử GPS ngoài thực địa, nghe lại chất lượng VI/EN trên điện thoại; có thể upload file thu âm tốt hơn bằng Admin. Việc này khác với kiểm thử phát MP3 trên Chromium trong cloud.

Không dùng fixture test làm traffic thật hoặc số liệu Dashboard demo. Seeder chỉ INSERT những ID chưa tồn tại, không ghi đè nội dung Admin đã sửa.
