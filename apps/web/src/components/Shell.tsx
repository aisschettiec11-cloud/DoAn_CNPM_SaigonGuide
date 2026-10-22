import { Link, NavLink, useNavigate } from "react-router-dom";
import {
  Compass,
  ArrowUpRight,
  Headphones,
  Play,
  Pause,
  Square,
  RotateCcw,
  X,
  MapPin,
  Navigation,
  ShieldCheck,
  Menu,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useApp } from "../lib/AppContext";
import { post } from "../lib/api";
import { consentChoice, setAnalytics } from "../lib/analytics";
export function Logo() {
  return (
    <Link to="/" className="logo" aria-label="Saigon Guide — Trang chủ">
      <span className="logo-mark">
        <Compass size={27} strokeWidth={1.6} />
      </span>
      <span>
        saigon<span className="logo-light">guide</span>
        <small>THE CITY, IN YOUR EARS</small>
      </span>
    </Link>
  );
}
export function Header() {
  const a = useApp(),
    nav = useNavigate();
  const [menu, setMenu] = useState(false);
  const vi = a.locale === "vi";
  async function logout() {
    await post("/auth/logout", {});
    await a.reloadUser();
    nav("/");
  }
  return (
    <header className="site-header">
      <div className="header-inner">
        <Logo />
        <button
          className="icon mobile-menu"
          onClick={() => setMenu(!menu)}
          aria-label="Mở menu"
        >
          <Menu />
        </button>
        <nav
          className={menu ? "header-nav open" : "header-nav"}
          aria-label="Điều hướng chính"
        >
          <NavLink to="/" end>
            {vi ? "Khám phá" : "Explore"}
          </NavLink>
          <Link to="/privacy">{vi ? "Quyền riêng tư" : "Privacy"}</Link>
          {a.user?.role === "Admin" && (
            <Link to="/admin">{vi ? "Quản trị" : "Admin"}</Link>
          )}
        </nav>
        <div className="header-actions">
          <div className="language-switch" aria-label="Ngôn ngữ">
            <button
              aria-pressed={vi}
              className={vi ? "active" : ""}
              onClick={() => a.setLocale("vi")}
            >
              VI
            </button>
            <span>/</span>
            <button
              aria-pressed={!vi}
              className={!vi ? "active" : ""}
              onClick={() => a.setLocale("en")}
            >
              EN
            </button>
          </div>
          {a.user ? (
            <>
              <Link className="account-name" to="/account">
                {a.user.displayName}
              </Link>
              <button className="text-button" onClick={() => void logout()}>
                {vi ? "Đăng xuất" : "Sign out"}
              </button>
            </>
          ) : (
            <Link to="/login" className="header-login">
              {vi ? "Đăng nhập" : "Sign in"}
              <ArrowUpRight size={15} />
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
export function Footer() {
  return (
    <footer className="site-footer">
      <Logo />
      <p>
        Đi để hiểu. Nghe để yêu.
        <br />
        <span>A little closer to the city.</span>
      </p>
      <div>
        <Link to="/privacy">Quyền riêng tư</Link>
        <span>© {new Date().getFullYear()} Saigon Guide</span>
      </div>
    </footer>
  );
}
const time = (s: number) =>
  `${Math.floor(s / 60)}:${Math.floor(s % 60)
    .toString()
    .padStart(2, "0")}`;
export function Player() {
  const a = useApp();
  if (!a.current) return null;
  const playing = a.audioState === "playing";
  return (
    <section className="global-player" aria-label="Bộ điều khiển thuyết minh">
      <img src={a.current.imageUrl} alt="" />
      <div className="player-title">
        <small>
          <Headphones size={12} />{" "}
          {a.current.languageCode === "vi" ? "ĐANG KHÁM PHÁ" : "NOW EXPLORING"}
        </small>
        <strong>{a.current.name}</strong>
        <span role="status">
          {a.audioError ||
            (a.audioState === "loading"
              ? "Đang tải âm thanh…"
              : a.audioState === "ended"
                ? "Đã phát xong · Giọng tổng hợp"
                : a.audioState === "paused"
                  ? "Đã tạm dừng"
                  : "Thuyết minh · Giọng tổng hợp")}
        </span>
      </div>
      <div className="player-buttons">
        <button
          className="icon"
          aria-label="Nghe lại"
          onClick={() => void a.play(a.current!, "manual", true)}
        >
          <RotateCcw size={18} />
        </button>
        <button
          className="play-circle"
          aria-label={playing ? "Tạm dừng" : "Tiếp tục phát"}
          onClick={() =>
            playing
              ? a.pause()
              : a.audioState === "paused"
                ? a.resume()
                : a.play(a.current!, "manual", true)
          }
        >
          {playing ? <Pause size={21} /> : <Play size={21} />}
        </button>
        <button className="icon" aria-label="Dừng thuyết minh" onClick={a.stop}>
          <Square size={17} />
        </button>
      </div>
      <div className="player-timeline">
        <progress
          value={a.elapsed}
          max={a.duration || 1}
          aria-label="Tiến độ thuyết minh"
        />
        <span>
          {time(a.elapsed)} / {time(a.duration)}
        </span>
      </div>
      <span className="player-lang">
        {a.current.languageCode.toUpperCase()}
      </span>
    </section>
  );
}
export function ConsentBanner() {
  const a = useApp();
  const [visible, setVisible] = useState(consentChoice() === null);
  useEffect(() => {
    const change = () => setVisible(false);
    window.addEventListener("sg:consent", change);
    return () => window.removeEventListener("sg:consent", change);
  }, []);
  if (!visible) return null;
  async function choose(value: boolean) {
    await setAnalytics(value);
    setVisible(false);
  }
  return (
    <aside className="consent-banner" aria-label="Lựa chọn Analytics">
      <ShieldCheck size={23} />
      <div>
        <strong>
          {a.locale === "vi"
            ? "Khám phá theo cách của bạn"
            : "Explore on your terms"}
        </strong>
        <p>
          {a.locale === "vi"
            ? "Cho phép thống kê ẩn danh ước tính để cải thiện hướng dẫn? Không lưu tọa độ GPS. Từ chối vẫn dùng đầy đủ."
            : "Allow anonymous usage estimates to improve this guide? We never store your GPS coordinates. All features work if you decline."}{" "}
          <Link to="/privacy">
            {a.locale === "vi" ? "Tìm hiểu" : "Learn more"}
          </Link>
        </p>
      </div>
      <button className="button subtle" onClick={() => void choose(false)}>
        {a.locale === "vi" ? "Từ chối" : "Decline"}
      </button>
      <button className="button small" onClick={() => void choose(true)}>
        {a.locale === "vi" ? "Đồng ý" : "Allow"}
      </button>
    </aside>
  );
}
export function Nearby() {
  const a = useApp(),
    nav = useNavigate();
  if (!a.nearby) return null;
  return (
    <aside className="nearby-toast" role="status">
      <MapPin />
      <div>
        <small>{a.locale === "vi" ? "BẠN ĐANG Ở GẦN" : "YOU ARE NEAR"}</small>
        <strong>{a.nearby.name}</strong>
        <button
          className="text-button"
          onClick={() => {
            nav(`/places/${a.nearby!.id}`);
            a.dismissNearby();
          }}
        >
          {a.locale === "vi" ? "Khám phá & nghe" : "Explore & listen"}{" "}
          <ArrowUpRight size={13} />
        </button>
      </div>
      <button className="icon" aria-label="Để sau" onClick={a.dismissNearby}>
        <X size={17} />
      </button>
    </aside>
  );
}
export function GpsControls() {
  const a = useApp(),
    vi = a.locale === "vi";
  const labels: Record<string, string> = {
    off: vi ? "GPS đang tắt" : "GPS is off",
    requesting: vi ? "Đang xin vị trí…" : "Locating…",
    tracking: vi ? "Đang định vị" : "Location on",
    degraded: vi ? "GPS yếu, chưa kích hoạt tự động" : "Low GPS accuracy",
    denied: vi ? "Bạn đã từ chối GPS" : "Location denied",
    unsupported: vi ? "Trình duyệt không hỗ trợ GPS" : "Location unavailable",
  };
  return (
    <div className="gps-controls">
      <button
        className={`button ${a.gps ? "gps-on" : "outline"}`}
        onClick={() => a.setGps(!a.gps)}
      >
        <Navigation size={16} />
        {a.gps
          ? vi
            ? "Tắt định vị"
            : "Stop location"
          : vi
            ? "Bật định vị"
            : "Use my location"}
      </button>
      <div className="gps-status">
        <span
          className={a.gpsStatus === "tracking" ? "live-dot" : "muted-dot"}
        />
        <span>
          {labels[a.gpsStatus]}
          {a.sample && a.gpsStatus === "tracking"
            ? ` · ±${Math.round(a.sample.accuracy)} m`
            : ""}
        </span>
      </div>
      <label className="auto-toggle">
        <input
          type="checkbox"
          checked={a.auto}
          onChange={(e) => a.setAuto(e.target.checked)}
        />
        <span>{vi ? "Tự phát khi đến gần" : "Auto-play nearby"}</span>
      </label>
    </div>
  );
}
