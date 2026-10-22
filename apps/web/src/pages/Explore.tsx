import { useState, lazy, Suspense } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  ArrowUpRight,
  Search,
  Headphones,
  MapPin,
  Globe2,
  Compass,
  ArrowDown,
  Layers,
  SlidersHorizontal,
} from "lucide-react";
import { useApp } from "../lib/AppContext";
const MapView = lazy(() =>
  import("../components/MapView").then((m) => ({ default: m.MapView })),
);
import { GpsControls } from "../components/Shell";
import { haversine } from "../lib/geofence";
export default function Explore() {
  const a = useApp(),
    nav = useNavigate();
  const [q, setQ] = useState(""),
    [mode, setMode] = useState<"split" | "list">("split");
  const vi = a.locale === "vi";
  const normalize = (s: string) =>
    s
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .replace(/đ/g, "d")
      .toLowerCase();
  const places = a.places.filter((p) =>
    normalize(p.name).includes(normalize(q)),
  );
  return (
    <main>
      <section className="hero">
        <div className="hero-copy">
          <span className="eyebrow">
            <span />{" "}
            {vi
              ? "MỘT SÀI GÒN, VÔ VÀN CÂU CHUYỆN"
              : "ONE CITY. A THOUSAND STORIES."}
          </span>
          <h1>
            {vi ? (
              <>
                Sài Gòn,
                <br />
                nghe một chút,
                <br />
                <em>yêu nhiều hơn.</em>
              </>
            ) : (
              <>
                Saigon,
                <br />a little listening,
                <br />
                <em>a lot to love.</em>
              </>
            )}
          </h1>
          <p>
            {vi
              ? "Chậm lại một nhịp. Để những góc phố, công trình và câu chuyện đưa bạn đến gần hơn với thành phố này."
              : "Slow down a little. Let the streets, landmarks and their stories bring you closer to this extraordinary city."}
          </p>
          <a href="#explore" className="button">
            {vi ? "Bắt đầu khám phá" : "Start exploring"}
            <ArrowUpRight size={19} />
          </a>
          <div className="hero-facts">
            <span>
              <MapPin size={16} />
              <b>06</b> {vi ? "địa điểm" : "landmarks"}
            </span>
            <i />
            <span>
              <Globe2 size={16} />
              <b>02</b> {vi ? "ngôn ngữ" : "languages"}
            </span>
            <i />
            <span>
              <Headphones size={16} />
              {vi ? "Nghe & trải nghiệm" : "Listen & discover"}
            </span>
          </div>
        </div>
        <div className="hero-art" aria-label="Minh họa các công trình Sài Gòn">
          <div className="art-orbit" />
          <div className="art-circle" />
          <span className="art-coordinate">
            10°46′ N<br />
            106°41′ E
          </span>
          <div className="art-card back">
            <img
              src="/images/nha-tho-duc-ba.svg"
              alt="Minh họa Nhà thờ Đức Bà"
            />
            <span>
              Nhà thờ Đức Bà <ArrowUpRight size={15} />
            </span>
          </div>
          <div className="art-card front">
            <img src="/images/dinh-doc-lap.svg" alt="Minh họa Dinh Độc Lập" />
            <span>
              Dinh Độc Lập <ArrowUpRight size={15} />
            </span>
          </div>
          <div className="floating-note">
            <span>
              <Headphones size={22} />
            </span>
            <div>
              <b>
                {vi ? "Thành phố có điều muốn kể." : "The city has a story."}
              </b>
              <small>
                {vi ? "BẠN CHỈ CẦN LẮNG NGHE" : "ALL YOU NEED TO DO IS LISTEN"}
              </small>
            </div>
          </div>
          <span className="art-stamp">
            SAIGON
            <br />
            <Compass size={34} />
            <br />
            EST. IN YOUR HEART
          </span>
        </div>
      </section>
      <div className="section-divider">
        <span>HO CHI MINH CITY, VIETNAM</span>
        <span>
          {vi
            ? "MỖI ĐIỂM DỪNG, MỘT GÓC NHÌN MỚI"
            : "EVERY STOP, A NEW PERSPECTIVE"}{" "}
          <ArrowDown size={13} />
        </span>
      </div>
      <section id="explore" className="explore-section">
        <div className="section-heading">
          <div>
            <span className="eyebrow">THE CITY IS YOURS</span>
            <h2>{vi ? "Đi đâu hôm nay?" : "Where to today?"}</h2>
            <p>
              {vi
                ? "Chọn một điểm đến. Phần còn lại, để Sài Gòn kể."
                : "Choose a landmark. Let Saigon tell you the rest."}
            </p>
          </div>
          <GpsControls />
        </div>
        <div className="explore-toolbar">
          <label className="search-box">
            <Search size={18} />
            <input
              value={q}
              onChange={(e) => setQ(e.target.value)}
              placeholder={vi ? "Tìm một điểm đến…" : "Find a landmark…"}
              aria-label={vi ? "Tìm địa điểm" : "Search places"}
            />
            {q && (
              <button className="text-button" onClick={() => setQ("")}>
                {vi ? "Xóa" : "Clear"}
              </button>
            )}
          </label>
          <div className="filter-label">
            <SlidersHorizontal size={15} />
            {vi ? "Tất cả địa điểm" : "All landmarks"}{" "}
            <span>{places.length.toString().padStart(2, "0")}</span>
          </div>
          <button
            className="view-toggle"
            aria-pressed={mode === "split"}
            onClick={() => setMode(mode === "split" ? "list" : "split")}
          >
            <Layers size={17} />
            {mode === "split"
              ? vi
                ? "Bản đồ"
                : "Map"
              : vi
                ? "Danh sách"
                : "List"}
          </button>
        </div>
        {a.placesLoading ? (
          <div className="loading-grid" role="status">
            {[1, 2, 3].map((i) => (
              <div className="skeleton" key={i} />
            ))}
            <span className="sr-only">Đang tải địa điểm…</span>
          </div>
        ) : a.placesError ? (
          <div className="error-box" role="alert">
            <h3>{vi ? "Chưa thể tải địa điểm" : "Unable to load landmarks"}</h3>
            <p>{a.placesError}</p>
            <button className="button" onClick={a.reloadPlaces}>
              Thử lại
            </button>
          </div>
        ) : (
          <div
            className={`explore-layout ${mode === "list" ? "list-only" : ""}`}
          >
            <div className="place-grid">
              {places.map((p, i) => (
                <Link key={p.id} to={`/places/${p.id}`} className="place-card">
                  <div className="place-image">
                    <img src={p.imageUrl} alt={p.imageAlt} />
                    <span className="place-number">
                      {String(i + 1).padStart(2, "0")}
                    </span>
                    <span className="audio-badge">
                      <Headphones size={12} /> VI / EN
                    </span>
                  </div>
                  <div className="place-card-body">
                    <div className="place-meta">
                      <span>
                        {vi ? "VĂN HÓA & THÀNH PHỐ" : "CULTURE & CITY"}
                      </span>
                      {a.sample && (
                        <span>{Math.round(haversine(a.sample, p))} m</span>
                      )}
                    </div>
                    <h3>{p.name}</h3>
                    <p>{p.shortDescription}</p>
                    <div className="place-card-footer">
                      <span>
                        <MapPin size={13} />
                        {vi ? "TP. Hồ Chí Minh" : "Ho Chi Minh City"}
                      </span>
                      <span className="card-arrow">
                        <ArrowUpRight size={19} />
                      </span>
                    </div>
                  </div>
                </Link>
              ))}
              {!places.length && (
                <div className="empty-state">
                  <Search size={30} />
                  <h3>
                    {vi ? "Chưa tìm thấy điểm đến" : "No landmarks found"}
                  </h3>
                  <p>
                    {vi
                      ? "Thử một tên khác hoặc xóa từ khóa."
                      : "Try another name or clear your search."}
                  </p>
                  <button className="button outline" onClick={() => setQ("")}>
                    {vi ? "Xem tất cả" : "Show all"}
                  </button>
                </div>
              )}
            </div>
            {mode === "split" && (
              <aside className="map-panel">
                <Suspense
                  fallback={
                    <div className="skeleton" role="status">
                      Đang mở bản đồ…
                    </div>
                  }
                >
                  <MapView
                    places={places}
                    sample={a.sample}
                    onSelect={(id) => nav(`/places/${id}`)}
                  />
                </Suspense>
                <div className="map-note">
                  <Compass size={20} />
                  <p>
                    {vi
                      ? "Đi theo cảm hứng, không cần tài khoản."
                      : "Follow your curiosity. No account needed."}
                    <span>
                      {vi
                        ? "Bấm một dấu ghim để khám phá câu chuyện."
                        : "Tap a pin to discover its story."}
                    </span>
                  </p>
                </div>
              </aside>
            )}
          </div>
        )}
        <div className="discovery-note">
          <span>✳</span>
          <p>
            {vi
              ? "Không cần lên một kế hoạch hoàn hảo. Đôi khi, một câu chuyện hay là đủ để bắt đầu."
              : "You don’t need a perfect plan. Sometimes, a good story is all it takes to begin."}
          </p>
        </div>
      </section>
    </main>
  );
}
