import { useState } from "react";
import { ShieldCheck, Navigation, Headphones } from "lucide-react";
import { consentChoice, setAnalytics } from "../lib/analytics";
import { useApp } from "../lib/AppContext";
export default function Privacy() {
  const a = useApp();
  const [choice, setChoice] = useState(consentChoice()),
    [message, setMessage] = useState("");
  const vi = a.locale === "vi";
  async function choose(on: boolean) {
    await setAnalytics(on);
    setChoice(on);
    setMessage(
      vi
        ? on
          ? "Đã bật thống kê sử dụng."
          : "Đã dừng thống kê và yêu cầu xóa mã trình duyệt hiện tại."
        : on
          ? "Usage analytics enabled."
          : "Analytics stopped; deletion of the current visitor cookie requested.",
    );
  }
  return (
    <main className="narrow page-space privacy-page">
      <span className="eyebrow">YOUR CITY. YOUR CHOICE.</span>
      <h1>{vi ? "Khám phá với sự an tâm." : "Explore with peace of mind."}</h1>
      <p className="detail-intro">
        {vi
          ? "Bạn quyết định khi nào chia sẻ vị trí và khi nào cho phép thống kê. Không lựa chọn nào là điều kiện để khám phá Sài Gòn."
          : "You decide whether to use location or allow analytics. Neither is required to explore Saigon."}
      </p>
      <section className="panel">
        <Navigation className="accent" />
        <h2>
          {vi
            ? "GPS ở lại trên thiết bị"
            : "Your location stays on your device"}
        </h2>
        <p>
          {vi
            ? "Chỉ xin vị trí sau khi bạn bấm Bật định vị. Tọa độ chỉ dùng trong trình duyệt để tính khoảng cách và nhận biết đến gần, không gửi hoặc lưu trên máy chủ. Tắt GPS, rời trang du lịch hoặc ẩn tab sẽ dừng theo dõi."
            : "Location is requested only when you enable it. Coordinates are processed in your browser for distance and nearby suggestions; they are never sent to or stored on our server. Tracking stops when disabled, when leaving the tour or when the tab is hidden."}
        </p>
        <button className="button outline" onClick={() => a.setGps(false)}>
          {vi ? "Dừng định vị" : "Stop location tracking"}
        </button>
      </section>
      <section className="panel">
        <ShieldCheck className="accent" />
        <h2>{vi ? "Thống kê có lựa chọn" : "Optional analytics"}</h2>
        <p>
          {vi
            ? "Khi bạn đồng ý, chúng tôi ghi loại trang/địa điểm đã xem, lựa chọn ngôn ngữ, tín hiệu đến gần và lượt bắt đầu/hoàn tất phát bằng mã trình duyệt ngẫu nhiên. Nếu bạn đăng nhập tài khoản khách, hoạt động mới có thể gắn với tài khoản; không gắn lại lịch sử Guest. Đây là thống kê ước tính, không phải số người thực."
            : "With your consent, we record views, language choices, nearby signals and playback events using a random visitor identifier. New activity may be linked to a signed-in guest account; earlier anonymous history is not linked retroactively. These are estimates, not a count of real people."}
        </p>
        <p>
          {vi
            ? "Sự kiện được giữ 90 ngày lịch Việt Nam gồm hôm nay. Phiên đo lường hết hạn sau 30 phút không hoạt động. Mã cookie được giữ tối đa 90 ngày, không dùng fingerprint. Thu hồi dừng đo và xóa mã hiện tại; không tự xóa toàn bộ dữ liệu quá khứ."
            : "Events are retained for 90 Vietnamese calendar days including today. Analytics sessions expire after 30 minutes of inactivity. The visitor cookie lasts at most 90 days; we do not fingerprint devices. Withdrawal stops collection and removes the current identifier; it does not automatically erase past data."}
        </p>
        <strong>
          {vi ? "Hiện tại: " : "Current choice: "}
          {choice === null
            ? vi
              ? "chưa chọn"
              : "not selected"
            : choice
              ? vi
                ? "đồng ý"
                : "allowed"
              : vi
                ? "từ chối"
                : "declined"}
        </strong>
        <div className="button-row">
          <button className="button" onClick={() => void choose(true)}>
            {vi ? "Cho phép thống kê" : "Allow analytics"}
          </button>
          <button className="button outline" onClick={() => void choose(false)}>
            {vi ? "Từ chối / thu hồi" : "Decline / withdraw"}
          </button>
        </div>
        {message && (
          <p role="status" className="success">
            {message}
          </p>
        )}
      </section>
      <section className="panel">
        <Headphones className="accent" />
        <h2>{vi ? "Âm thanh và nguồn nội dung" : "Audio and sources"}</h2>
        <p>
          {vi
            ? "Âm thanh ban đầu là tệp MP3 giọng tổng hợp từ bài viết được biên soạn kèm nguồn. Ảnh là minh họa nguyên bản, không phải ảnh chụp địa điểm. Tọa độ đã đối chiếu nguồn tra cứu nhưng chưa kiểm thử ngoài thực địa; GPS và tự phát phụ thuộc thiết bị/trình duyệt. Bản đồ dùng OpenStreetMap và việc mở chỉ đường đưa bạn sang Google Maps."
            : "Initial MP3 narrations use synthetic speech from original scripts with source references. Images are original illustrations rather than photographs. Coordinates are reference-checked but not field-tested; GPS and autoplay depend on your browser. Maps use OpenStreetMap, and directions open Google Maps."}
        </p>
        <p>
          {vi
            ? "Tài khoản lưu email, tên, ngôn ngữ ưu tiên và mật khẩu đã băm. Nhật ký quản trị giữ tối đa 180 ngày. Nếu cần xử lý dữ liệu cá nhân hoặc xóa tài khoản, liên hệ người vận hành triển khai website; bản triển khai công khai phải bổ sung thông tin liên hệ trước khi phát hành."
            : "Accounts store email, name, preferred language and a password hash. Administrative audit records are retained for up to 180 days. For account or personal-data requests, contact the site operator; public deployment must provide operator contact information before launch."}
        </p>
      </section>
    </main>
  );
}
