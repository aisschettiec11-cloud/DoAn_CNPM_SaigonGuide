import { useEffect, useRef, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import {
  ArrowLeft,
  ArrowUpRight,
  BookOpen,
  Headphones,
  MapPin,
  Play,
  Clock,
  Info,
  ExternalLink,
} from "lucide-react";
import { api } from "../lib/api";
import { useApp } from "../lib/AppContext";
import type { Place } from "../lib/types";
import { track } from "../lib/analytics";
import { haversine } from "../lib/geofence";
import { GpsControls } from "../components/Shell";
export default function Detail() {
  const { id } = useParams(),
    a = useApp(),
    location = useLocation();
  const [p, setP] = useState<Place | null>(null),
    [error, setError] = useState(""),
    [retry, setRetry] = useState(0);
  const counted = useRef("");
  const vi = a.locale === "vi";
  useEffect(() => {
    let cancelled = false;
    setP(null);
    setError("");
    api<Place>(`/places/${id}?language=${a.locale}`)
      .then((p) => {
        if (cancelled) return;
        setP(p);
        if (counted.current !== location.key) {
          counted.current = location.key;
          void track("place_view", {
            placeId: p.id,
            languageCode: p.languageCode,
          });
        }
      })
      .catch((e) => {
        if (!cancelled) setError(e.message);
      });
    return () => {
      cancelled = true;
    };
  }, [id, a.locale, retry, location.key]);
  useEffect(() => {
    const consent = (event: Event) => {
      const e = event as CustomEvent<{ enabled: boolean; changed: boolean }>;
      if (e.detail.enabled && e.detail.changed && p)
        void track("place_view", {
          placeId: p.id,
          languageCode: p.languageCode,
        });
    };
    window.addEventListener("sg:consent", consent);
    return () => window.removeEventListener("sg:consent", consent);
  }, [p]);
  if (error)
    return (
      <main className="narrow page-space">
        <Link to="/">← Khám phá</Link>
        <div className="error-box">
          <h1>Chưa thể mở địa điểm</h1>
          <p>{error}</p>
          <button className="button" onClick={() => setRetry((n) => n + 1)}>
            Thử lại
          </button>
        </div>
      </main>
    );
  if (!p)
    return (
      <main className="narrow page-space" role="status">
        <div className="skeleton detail-skeleton" />
        Đang tải câu chuyện…
      </main>
    );
  return (
    <main className="detail-page page-space">
      <Link to="/" className="back-link">
        <ArrowLeft size={16} />
        {vi ? "Trở về khám phá" : "Back to exploring"}
      </Link>
      <div className="detail-grid">
        <div className="detail-story">
          <span className="eyebrow">SAIGON, ONE STORY AT A TIME</span>
          <h1>{p.name}</h1>
          <p className="detail-intro">{p.shortDescription}</p>
          <div className="detail-address">
            <MapPin size={17} />
            {p.address}
            {a.sample && (
              <b>
                {" "}
                · {Math.round(haversine(a.sample, p))} m{" "}
                {vi ? "đường chim bay" : "straight-line"}
              </b>
            )}
          </div>
          <img className="detail-image" src={p.imageUrl} alt={p.imageAlt} />
          <small className="image-credit">
            {p.imageCredit} · {p.imageLicense} ·{" "}
            {vi
              ? "Hình minh họa, không phải ảnh chụp"
              : "Illustration, not a photograph"}
          </small>
          <div className="story-heading">
            <BookOpen size={22} />
            <h2>
              {vi ? "Câu chuyện của điểm đến" : "The story of this place"}
            </h2>
          </div>
          <p className="transcript">
            {p.transcript ||
              (vi
                ? "Bản dịch này hiện chưa có. Hãy thử ngôn ngữ còn lại."
                : "This translation is unavailable. Please try the other language.")}
          </p>
          <section className="sources">
            <h3>
              {vi
                ? "Nguồn & thông tin tham quan"
                : "Sources & visitor information"}
            </h3>
            {p.sources.map((s) => (
              <a key={s.url} href={s.url} target="_blank" rel="noreferrer">
                <ExternalLink size={14} />
                {s.title}
                <small>{s.accessedOn}</small>
              </a>
            ))}
            <p>
              <Info size={14} />
              {vi
                ? "Tọa độ được đối chiếu từ nguồn tra cứu, chưa kiểm tra GPS thực địa. Giờ mở cửa, giá vé và lối vào có thể thay đổi; vui lòng kiểm tra với nơi quản lý."
                : "Coordinates are checked against reference sources, not field-tested. Please check current hours, tickets and access with the venue."}
            </p>
          </section>
        </div>
        <aside className="detail-aside">
          <div className="listen-card">
            <span className="listen-icon">
              <Headphones size={28} />
            </span>
            <span className="eyebrow">LISTEN TO THE CITY</span>
            <h2>
              {vi
                ? "Một câu chuyện, gần hơn một chút."
                : "A story brings you a little closer."}
            </h2>
            <p>
              {vi
                ? "Đeo tai nghe, chậm bước và để thành phố dẫn lời."
                : "Put on your headphones, slow down and let the city speak."}
            </p>
            <div className="listen-metadata">
              <span>
                <Clock size={14} />
                {Math.ceil((p.durationSeconds ?? 0) / 60)} {vi ? "phút" : "min"}
              </span>
              <span>{p.languageCode.toUpperCase()}</span>
              <span>{vi ? "Giọng tổng hợp" : "Synthetic voice"}</span>
            </div>
            <button
              className="button full"
              onClick={() => void a.play(p)}
              disabled={!p.audioAssetId}
            >
              <Play size={18} />
              {vi ? "Nghe thuyết minh" : "Listen to the story"}
            </button>
            {!p.audioAssetId && (
              <p role="status">
                {vi
                  ? "Chưa có âm thanh — nội dung văn bản vẫn có sẵn."
                  : "Audio unavailable — the transcript is available."}
              </p>
            )}
            <a
              className="button outline full"
              href={`https://www.google.com/maps/dir/?api=1&destination=${p.latitude},${p.longitude}`}
              target="_blank"
              rel="noreferrer"
            >
              {vi ? "Chỉ đường · Google Maps" : "Directions · Google Maps"}
              <ArrowUpRight size={17} />
            </a>
          </div>
          <div className="detail-gps">
            <h3>{vi ? "Để Sài Gòn tìm đến bạn" : "Let the city find you"}</h3>
            <p>
              {vi
                ? "Bật GPS để nhận lời mời nghe khi đến gần. Bạn luôn có thể nghe thủ công."
                : "Enable location for a nearby listening suggestion. Manual playback is always available."}
            </p>
            <GpsControls />
          </div>
        </aside>
      </div>
    </main>
  );
}
