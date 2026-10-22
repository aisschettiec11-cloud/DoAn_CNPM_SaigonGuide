import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Save,
  Plus,
  Trash2,
  Upload,
  Headphones,
  Info,
} from "lucide-react";
import { api, post } from "../lib/api";
import { useApp } from "../lib/AppContext";
import type { Editor as EditorType, Locale, Source } from "../lib/types";
import { AdminTitle } from "./Admin";
const empty = (): EditorType => ({
  code: "",
  latitude: null,
  longitude: null,
  triggerRadiusM: 75,
  status: "draft",
  verificationStatus: "unverified",
  isDemo: true,
  imageUrl: "/images/dinh-doc-lap.svg",
  imageCredit: "Saigon Guide — minh họa nguyên bản",
  imageLicense: "CC0-1.0",
  sources: [],
  version: 1,
  translations: ["vi", "en"].map((l) => ({
    languageCode: l as Locale,
    name: "",
    address: "",
    shortDescription: "",
    imageAlt: "",
  })),
  narrations: ["vi", "en"].map((l) => ({
    languageCode: l as Locale,
    transcript: "",
    sources: [],
    audioAssetId: null,
  })),
});
const normalize = (p: EditorType): EditorType => ({
  ...p,
  translations: (["vi", "en"] as Locale[]).map(
    (l) =>
      p.translations.find((t) => t.languageCode === l) ??
      empty().translations.find((t) => t.languageCode === l)!,
  ),
  narrations: (["vi", "en"] as Locale[]).map(
    (l) =>
      p.narrations.find((n) => n.languageCode === l) ??
      empty().narrations.find((n) => n.languageCode === l)!,
  ),
});
const source = (): Source => ({
  url: "",
  title: "",
  accessedOn: new Date().toISOString().slice(0, 10),
  scope: "",
});
function Sources({
  value,
  onChange,
}: {
  value: Source[];
  onChange: (s: Source[]) => void;
}) {
  function set(i: number, key: keyof Source, text: string) {
    onChange(
      value.map((s, index) => (index === i ? { ...s, [key]: text } : s)),
    );
  }
  return (
    <div className="source-editor">
      {value.map((s, i) => (
        <div className="source-fields" key={i}>
          <label>
            URL nguồn HTTPS
            <input
              type="url"
              required
              value={s.url}
              onChange={(e) => set(i, "url", e.target.value)}
              maxLength={2048}
            />
          </label>
          <label>
            Tên nguồn
            <input
              required
              value={s.title}
              onChange={(e) => set(i, "title", e.target.value)}
              maxLength={200}
            />
          </label>
          <label>
            Ngày đối chiếu
            <input
              type="date"
              required
              value={s.accessedOn}
              onChange={(e) => set(i, "accessedOn", e.target.value)}
            />
          </label>
          <label>
            Phạm vi xác minh
            <input
              required
              value={s.scope}
              onChange={(e) => set(i, "scope", e.target.value)}
              maxLength={500}
            />
          </label>
          <button
            type="button"
            className="icon"
            aria-label={`Xóa nguồn ${i + 1}`}
            onClick={() => onChange(value.filter((_, index) => index !== i))}
          >
            <Trash2 size={17} />
          </button>
        </div>
      ))}
      <button
        type="button"
        className="button outline small"
        disabled={value.length >= 10}
        onClick={() => onChange([...value, source()])}
      >
        <Plus size={15} />
        Thêm nguồn
      </button>
    </div>
  );
}
export default function Editor() {
  const { id } = useParams();
  const a = useApp();
  const [p, setP] = useState<EditorType | null>(id === "new" ? empty() : null),
    [error, setError] = useState(""),
    [message, setMessage] = useState(""),
    [busy, setBusy] = useState(false),
    [dirty, setDirty] = useState(false),
    [lang, setLang] = useState<Locale>("vi");
  useEffect(() => {
    if (id === "new") return;
    let cancelled = false;
    api<EditorType[]>("/admin/places")
      .then((all) => {
        if (cancelled) return;
        const p = all.find((x) => x.id === id);
        if (p) {
          for (const l of ["vi", "en"] as Locale[]) {
            if (!p.translations.some((x) => x.languageCode === l))
              p.translations.push({
                ...empty().translations.find((x) => x.languageCode === l)!,
              });
            if (!p.narrations.some((x) => x.languageCode === l))
              p.narrations.push({
                ...empty().narrations.find((x) => x.languageCode === l)!,
              });
          }
          setP(p);
        } else setError("Không tìm thấy địa điểm.");
      })
      .catch((e) => setError(e.message));
    return () => {
      cancelled = true;
    };
  }, [id]);
  useEffect(() => {
    const warn = (e: BeforeUnloadEvent) => {
      if (dirty) e.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);
  function set<K extends keyof EditorType>(key: K, value: EditorType[K]) {
    setP((p) => (p ? { ...p, [key]: value } : p));
    setDirty(true);
  }
  async function save(e: FormEvent) {
    e.preventDefault();
    if (!p) return;
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const endpoint = p.id ? `/admin/places/${p.id}` : "/admin/places";
      const payload = {
        code: p.code,
        latitude: p.latitude,
        longitude: p.longitude,
        triggerRadiusM: p.triggerRadiusM,
        status: p.status,
        verificationStatus: p.verificationStatus,
        isDemo: p.isDemo,
        imageUrl: p.imageUrl,
        imageCredit: p.imageCredit,
        imageLicense: p.imageLicense,
        sources: p.sources,
        version: p.version,
        translations: p.translations.filter((t) => t.name.trim()),
        narrations: p.narrations.filter(
          (n) => n.transcript?.trim() || n.audioAssetId || n.sources.length,
        ),
      };
      const saved = await api<EditorType>(endpoint, {
        method: p.id ? "PUT" : "POST",
        body: JSON.stringify(payload),
      });
      setP(normalize(saved));
      setDirty(false);
      setMessage("Đã lưu nội dung.");
      a.reloadPlaces();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function upload(file: File) {
    if (!p?.id) {
      setError("Hãy lưu nháp trước khi tải âm thanh.");
      return;
    }
    if (dirty) {
      setError("Hãy lưu thay đổi nội dung trước khi tải âm thanh.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      const form = new FormData();
      form.append("file", file);
      form.append("version", String(p.version));
      form.append("credit", "Saigon Guide — tệp do quản trị viên cung cấp");
      form.append(
        "license",
        prompt("Quyền sử dụng tệp âm thanh (không để trống):") ?? "",
      );
      const result = await api<{ id: string; version: number }>(
        `/admin/places/${p.id}/audio/${lang}`,
        { method: "POST", body: form },
      );
      setP({
        ...p,
        version: result.version,
        narrations: p.narrations.map((n) =>
          n.languageCode === lang ? { ...n, audioAssetId: result.id } : n,
        ),
      });
      setMessage("Đã kiểm tra và gán âm thanh.");
      a.reloadPlaces();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function publish() {
    if (!p?.id || dirty) {
      setError("Cần lưu nội dung trước khi công khai.");
      return;
    }
    setBusy(true);
    setError("");
    try {
      setP(
        await post<EditorType>(`/admin/places/${p.id}/status`, {
          status: "published",
          version: p.version,
        }),
      );
      setMessage("Đã công khai địa điểm.");
      a.reloadPlaces();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  if (!p)
    return (
      <>
        <Link to="/admin/places">← Danh sách</Link>
        <p role={error ? "alert" : "status"}>{error || "Đang tải nội dung…"}</p>
      </>
    );
  const t = p.translations.find((x) => x.languageCode === lang)!,
    n = p.narrations.find((x) => x.languageCode === lang)!;
  function translation(
    key: "name" | "address" | "shortDescription" | "imageAlt",
    v: string,
  ) {
    set(
      "translations",
      p!.translations.map((x) =>
        x.languageCode === lang ? { ...x, [key]: v } : x,
      ),
    );
  }
  return (
    <>
      <Link
        className="back-link"
        to="/admin/places"
        onClick={(e) => {
          if (dirty && !confirm("Có thay đổi chưa lưu. Rời trang?"))
            e.preventDefault();
        }}
      >
        <ArrowLeft size={15} />
        Danh sách địa điểm
      </Link>
      <AdminTitle
        kicker="EDIT THE STORY"
        title={p.id ? "Biên tập câu chuyện." : "Thêm một điểm đến."}
      >
        <span className="status-pill">
          {p.status} · v{p.version}
        </span>
      </AdminTitle>
      {error && (
        <p role="alert" className="form-error">
          {error}
        </p>
      )}
      {message && (
        <p role="status" className="success">
          {message}
        </p>
      )}
      <form onSubmit={(e) => void save(e)}>
        <section className="panel">
          <h2>Thông tin chung</h2>
          <div className="form-grid">
            <label>
              Mã địa điểm
              <input
                required
                pattern="[a-z0-9]+(-[a-z0-9]+)*"
                maxLength={80}
                readOnly={!!p.id}
                value={p.code}
                onChange={(e) => set("code", e.target.value)}
              />
            </label>
            <label>
              Bán kính kích hoạt (m)
              <input
                type="number"
                required
                min={25}
                max={500}
                value={p.triggerRadiusM}
                onChange={(e) => set("triggerRadiusM", Number(e.target.value))}
              />
            </label>
            <label>
              Vĩ độ
              <input
                type="number"
                step="any"
                min={-90}
                max={90}
                value={p.latitude ?? ""}
                onChange={(e) =>
                  set(
                    "latitude",
                    e.target.value === "" ? null : Number(e.target.value),
                  )
                }
              />
            </label>
            <label>
              Kinh độ
              <input
                type="number"
                step="any"
                min={-180}
                max={180}
                value={p.longitude ?? ""}
                onChange={(e) =>
                  set(
                    "longitude",
                    e.target.value === "" ? null : Number(e.target.value),
                  )
                }
              />
            </label>
            <label>
              Ảnh minh họa / URL cho phép
              <input
                value={p.imageUrl ?? ""}
                onChange={(e) => set("imageUrl", e.target.value)}
              />
            </label>
            <label>
              Tác giả ảnh
              <input
                value={p.imageCredit ?? ""}
                maxLength={500}
                onChange={(e) => set("imageCredit", e.target.value)}
              />
            </label>
            <label>
              Quyền sử dụng ảnh
              <input
                value={p.imageLicense ?? ""}
                maxLength={500}
                onChange={(e) => set("imageLicense", e.target.value)}
              />
            </label>
            <label>
              Trạng thái khi lưu
              <select
                value={p.status}
                onChange={(e) => set("status", e.target.value)}
              >
                <option value="draft">Nháp</option>
                <option value="hidden">Ẩn</option>
                {p.id && (
                  <option value="published">Công khai (kiểm đầy đủ)</option>
                )}
              </select>
            </label>
          </div>
          <label className="check-label">
            <input
              type="checkbox"
              checked={p.verificationStatus === "verified"}
              onChange={(e) =>
                set(
                  "verificationStatus",
                  e.target.checked ? "verified" : "unverified",
                )
              }
            />
            Đã đối chiếu tọa độ và nội dung với nguồn tham khảo
          </label>
          <label className="check-label">
            <input
              type="checkbox"
              checked={p.isDemo}
              onChange={(e) => set("isDemo", e.target.checked)}
            />
            Dữ liệu mẫu (không công khai production)
          </label>
          <p className="muted tiny">
            <Info size={13} /> Đối chiếu nguồn không thay thế kiểm tra GPS thực
            địa. Không ghi dữ liệu chưa xác minh là dữ liệu thật.
          </p>
          <h3>Nguồn địa điểm</h3>
          <Sources value={p.sources} onChange={(v) => set("sources", v)} />
        </section>
        <section className="panel">
          <div className="panel-title">
            <h2>Nội dung song ngữ</h2>
            <div className="language-switch">
              <button
                type="button"
                className={lang === "vi" ? "active" : ""}
                onClick={() => setLang("vi")}
              >
                VI
              </button>
              <span>/</span>
              <button
                type="button"
                className={lang === "en" ? "active" : ""}
                onClick={() => setLang("en")}
              >
                EN
              </button>
            </div>
          </div>
          <label>
            Tên địa điểm ({lang.toUpperCase()})
            <input
              required
              maxLength={200}
              value={t.name}
              onChange={(e) => translation("name", e.target.value)}
            />
          </label>
          <label>
            Địa chỉ
            <input
              maxLength={500}
              value={t.address ?? ""}
              onChange={(e) => translation("address", e.target.value)}
            />
          </label>
          <label>
            Mô tả ngắn
            <textarea
              rows={3}
              maxLength={1000}
              value={t.shortDescription ?? ""}
              onChange={(e) => translation("shortDescription", e.target.value)}
            />
          </label>
          <label>
            Văn bản thay thế của ảnh
            <input
              maxLength={500}
              value={t.imageAlt ?? ""}
              onChange={(e) => translation("imageAlt", e.target.value)}
            />
          </label>
          <label>
            Bài thuyết minh
            <textarea
              rows={12}
              maxLength={20000}
              value={n.transcript ?? ""}
              onChange={(e) =>
                set(
                  "narrations",
                  p.narrations.map((x) =>
                    x.languageCode === lang
                      ? { ...x, transcript: e.target.value, audioAssetId: null }
                      : x,
                  ),
                )
              }
            />
            <small>
              {n.transcript?.length ?? 0} / 20.000 ký tự. Văn bản thuần. Sửa bài
              cần cập nhật âm thanh tương ứng; hãy ẩn điểm trước nếu đang công
              khai.
            </small>
          </label>
          <h3>Nguồn bài thuyết minh ({lang.toUpperCase()})</h3>
          <Sources
            value={n.sources}
            onChange={(v) =>
              set(
                "narrations",
                p.narrations.map((x) =>
                  x.languageCode === lang ? { ...x, sources: v } : x,
                ),
              )
            }
          />
          <div className="audio-editor">
            <Headphones size={23} />
            <div>
              <strong>Âm thanh {lang.toUpperCase()}</strong>
              <p>
                {n.audioAssetId
                  ? "Đã có tệp MP3. Thay tệp sẽ tạo tài nguyên mới."
                  : "Chưa có âm thanh. Lưu nháp trước khi upload."}
              </p>
              {n.audioAssetId && (
                <audio
                  controls
                  preload="none"
                  src={`/api/v1/media/${n.audioAssetId}`}
                />
              )}
            </div>
            <label className="button outline small upload-label">
              <Upload size={15} />
              Tải MP3
              <input
                type="file"
                accept="audio/mpeg,.mp3"
                disabled={busy || !p.id}
                onChange={(e) => {
                  if (e.target.files?.[0]) void upload(e.target.files[0]);
                  e.target.value = "";
                }}
              />
            </label>
          </div>
        </section>
        <div className="editor-actions">
          <button className="button" disabled={busy}>
            <Save size={17} />
            {busy ? "Đang xử lý…" : "Lưu nội dung"}
          </button>
          <button
            type="button"
            className="button outline"
            disabled={busy || !p.id}
            onClick={() => void publish()}
          >
            Kiểm tra & công khai
          </button>
          {dirty && <span className="muted">Có thay đổi chưa lưu</span>}
        </div>
      </form>
    </>
  );
}
