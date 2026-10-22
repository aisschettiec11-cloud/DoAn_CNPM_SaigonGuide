import { useEffect, useState, useRef } from "react";
import type { ReactNode } from "react";
import {
  Link,
  NavLink,
  Outlet,
  useNavigate,
  useSearchParams,
} from "react-router-dom";
import {
  LayoutDashboard,
  MapPin,
  Users,
  ArrowUpRight,
  Headphones,
  Eye,
  CalendarDays,
  Plus,
  Search,
  RefreshCcw,
  Pencil,
  ShieldCheck,
} from "lucide-react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { api, post } from "../lib/api";
import type { Dashboard, Editor, User } from "../lib/types";
import { useApp } from "../lib/AppContext";
export function AdminLayout() {
  return (
    <div className="admin-shell">
      <aside className="admin-sidebar">
        <span className="eyebrow">WORKSPACE</span>
        <h2>
          Quản trị
          <br />
          <em>Saigon Guide.</em>
        </h2>
        <nav aria-label="Quản trị">
          <NavLink to="/admin" end>
            <LayoutDashboard size={18} />
            Tổng quan
          </NavLink>
          <NavLink to="/admin/places">
            <MapPin size={18} />
            Địa điểm & nội dung
          </NavLink>
          <NavLink to="/admin/users">
            <Users size={18} />
            Tài khoản khách
          </NavLink>
        </nav>
        <div className="admin-sidebar-note">
          <ShieldCheck size={21} />
          <p>
            Dữ liệu có ý nghĩa.
            <br />
            <span>Quyền riêng tư luôn đi trước.</span>
          </p>
          <Link to="/">
            Xem website <ArrowUpRight size={14} />
          </Link>
        </div>
      </aside>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  );
}
export function AdminTitle({
  kicker,
  title,
  children,
}: {
  kicker: string;
  title: string;
  children?: ReactNode;
}) {
  return (
    <div className="admin-title">
      <div>
        <span className="eyebrow">{kicker}</span>
        <h1>{title}</h1>
      </div>
      {children}
    </div>
  );
}
export function AdminDashboard() {
  const [d, setD] = useState<Dashboard | null>(null),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(true),
    [retry, setRetry] = useState(0);
  const [params, setParams] = useSearchParams();
  const from = params.get("from") ?? "",
    to = params.get("to") ?? "",
    bucket = params.get("bucket") ?? "day";
  const [filter, setFilter] = useState({ from, to, bucket });
  useEffect(() => {
    let cancel = false;
    setError("");
    setLoading(true);
    api<Dashboard>(`/admin/dashboard?${params.toString()}`)
      .then((r) => {
        if (!cancel) {
          setD(r);
          setFilter(r.range);
        }
      })
      .catch((e) => {
        if (!cancel) setError(e.message);
      })
      .finally(() => {
        if (!cancel) setLoading(false);
      });
    return () => {
      cancel = true;
    };
  }, [params, retry]);
  return (
    <>
      <AdminTitle
        kicker="INSIGHTS & STORIES"
        title="Nhìn lại những hành trình."
      >
        <span className="admin-badge">
          <span className="live-dot" />
          Dữ liệu trực tiếp từ API
        </span>
      </AdminTitle>
      <p className="admin-intro">
        Hiểu cách mọi người khám phá Sài Gòn, từng điểm dừng một.
      </p>
      <form
        className="dashboard-filter"
        onSubmit={(e) => {
          e.preventDefault();
          setParams(filter);
        }}
      >
        <CalendarDays size={18} />
        <label>
          Từ ngày
          <input
            type="date"
            required
            value={filter.from}
            onChange={(e) => setFilter({ ...filter, from: e.target.value })}
          />
        </label>
        <label>
          Đến ngày
          <input
            type="date"
            required
            value={filter.to}
            onChange={(e) => setFilter({ ...filter, to: e.target.value })}
          />
        </label>
        <label>
          Nhóm
          <select
            value={filter.bucket}
            onChange={(e) => setFilter({ ...filter, bucket: e.target.value })}
          >
            <option value="day">Ngày</option>
            <option value="week">Tuần</option>
            <option value="month">Tháng</option>
          </select>
        </label>
        <button className="button small">Áp dụng</button>
        <button
          className="icon"
          type="button"
          aria-label="Tải lại thống kê"
          onClick={() => setRetry((n) => n + 1)}
        >
          <RefreshCcw size={17} />
        </button>
      </form>
      {error ? (
        <div className="error-box" role="alert">
          <h3>Không thể tải thống kê</h3>
          <p>{error}</p>
          <button className="button" onClick={() => setRetry((n) => n + 1)}>
            Thử lại
          </button>
        </div>
      ) : loading ? (
        <div className="skeleton" role="status">
          Đang tổng hợp dữ liệu…
        </div>
      ) : (
        d && (
          <>
            <div className="kpi-grid">
              {[
                { name: "Lượt xem trang", value: d.kpis.pageViews, icon: Eye },
                { name: "Phiên truy cập", value: d.kpis.sessions, icon: Users },
                {
                  name: "Lượt bắt đầu nghe",
                  value: d.kpis.narrationStarts,
                  icon: Headphones,
                },
                {
                  name: "Khách ước tính",
                  value: d.kpis.visitors,
                  icon: MapPin,
                },
              ].map((k) => (
                <div className="kpi" key={k.name}>
                  <div>
                    <span>{k.name}</span>
                    <k.icon size={18} />
                  </div>
                  <strong>{k.value.toLocaleString("vi")}</strong>
                  <small>Trong khoảng thời gian đã chọn</small>
                </div>
              ))}
            </div>
            <section className="panel chart-panel">
              <div className="panel-title">
                <div>
                  <h2>Nhịp điệu khám phá</h2>
                  <p>Lượt xem trang và số phiên có tương tác theo thời gian.</p>
                </div>
                <span className="muted tiny">Asia/Ho_Chi_Minh</span>
              </div>
              {d.kpis.pageViews === 0 && (
                <p className="empty-chart">
                  Chưa có lượt xem được ghi nhận trong khoảng này.
                </p>
              )}
              <div className="chart">
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={d.traffic}>
                    <defs>
                      <linearGradient id="traffic" x1="0" y1="0" x2="0" y2="1">
                        <stop
                          offset="0"
                          stopColor="#c76a43"
                          stopOpacity={0.2}
                        />
                        <stop offset="1" stopColor="#c76a43" stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid vertical={false} stroke="#ece7df" />
                    <XAxis
                      dataKey="date"
                      tick={{ fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <YAxis
                      allowDecimals={false}
                      tick={{ fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip />
                    <Area
                      type="monotone"
                      name="Lượt xem trang"
                      dataKey="pageViews"
                      stroke="#bc5d37"
                      fill="url(#traffic)"
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      name="Phiên"
                      dataKey="sessions"
                      stroke="#65836e"
                      fill="transparent"
                      strokeWidth={2}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
              <details>
                <summary>Bảng dữ liệu biểu đồ</summary>
                <div className="table-scroll">
                  <table>
                    <thead>
                      <tr>
                        <th>Ngày/nhóm</th>
                        <th>Lượt xem</th>
                        <th>Phiên</th>
                      </tr>
                    </thead>
                    <tbody>
                      {d.traffic.map((x) => (
                        <tr key={x.date}>
                          <td>{x.date}</td>
                          <td>{x.pageViews}</td>
                          <td>{x.sessions}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </details>
            </section>
            <section className="panel">
              <div className="panel-title">
                <div>
                  <h2>Những điểm đến được quan tâm</h2>
                  <p>Địa điểm ẩn được giữ trong lịch sử thống kê.</p>
                </div>
                <MapPin size={20} />
              </div>
              <div className="table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th>Địa điểm</th>
                      <th>Lượt xem</th>
                      <th>Phiên xem</th>
                      <th>Đến gần</th>
                      <th>Bắt đầu</th>
                      <th>Hoàn tất</th>
                      <th>Tỷ lệ bắt đầu</th>
                    </tr>
                  </thead>
                  <tbody>
                    {d.places.map((p, i) => (
                      <tr key={p.id}>
                        <td>
                          <span className="rank">
                            {String(i + 1).padStart(2, "0")}
                          </span>
                          <strong>{p.name}</strong>
                          {p.status !== "published" && (
                            <small className="status-pill">{p.status}</small>
                          )}
                        </td>
                        <td>{p.views}</td>
                        <td>{p.sessions}</td>
                        <td>{p.entries}</td>
                        <td>{p.starts}</td>
                        <td>{p.completions}</td>
                        <td>
                          {p.startRate === null
                            ? "N/A"
                            : p.startRate.toFixed(1) + "%"}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
            <div className="admin-two">
              <section className="panel">
                <h2>Ngôn ngữ của hành trình</h2>
                {d.languages.map((l) => (
                  <div className="language-stat" key={l.code}>
                    <strong>
                      {l.code === "vi" ? "Tiếng Việt" : "English"}
                    </strong>
                    <span>
                      {l.selections} lựa chọn · {l.starts} lượt phát
                    </span>
                    <progress
                      value={l.starts}
                      max={Math.max(1, d.kpis.narrationStarts)}
                    />
                  </div>
                ))}
              </section>
              <section className="panel">
                <h2>Âm thanh & tài khoản</h2>
                <dl className="stat-list">
                  <div>
                    <dt>Nghe hoàn tất</dt>
                    <dd>{d.kpis.narrationCompletions}</dd>
                  </div>
                  <div>
                    <dt>Tỷ lệ hoàn tất theo lượt bắt đầu</dt>
                    <dd>
                      {d.kpis.completionRate === null
                        ? "N/A"
                        : d.kpis.completionRate.toFixed(1) + "%"}
                    </dd>
                  </div>
                  <div>
                    <dt>Tài khoản đã đăng ký đến cuối kỳ</dt>
                    <dd>{d.kpis.registeredUsers}</dd>
                  </div>
                  <div>
                    <dt>Tài khoản hoạt động đo được</dt>
                    <dd>{d.kpis.activeUsers}</dd>
                  </div>
                </dl>
              </section>
            </div>
            <p className="privacy-caption">
              <ShieldCheck size={15} />
              Chỉ ghi nhận trình duyệt đồng ý Analytics. Không phải số khách du
              lịch thực tế. Lượt hoàn tất là tín hiệu phát, không chứng minh
              người nghe.
            </p>
          </>
        )
      )}
    </>
  );
}
export function AdminPlaces() {
  const a = useApp(),
    nav = useNavigate();
  const [rows, setRows] = useState<Editor[]>([]),
    [q, setQ] = useState(""),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(true);
  async function load() {
    setLoading(true);
    setError("");
    try {
      setRows(await api<Editor[]>("/admin/places"));
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setLoading(false);
    }
  }
  useEffect(() => {
    void load();
  }, []);
  async function status(p: Editor) {
    const s = p.status === "published" ? "hidden" : "published";
    if (!confirm(`${s === "hidden" ? "Ẩn" : "Công khai"} địa điểm này?`))
      return;
    try {
      await post(`/admin/places/${p.id}/status`, {
        status: s,
        version: p.version,
      });
      await load();
      a.reloadPlaces();
    } catch (e) {
      setError((e as Error).message);
    }
  }
  return (
    <>
      <AdminTitle kicker="PLACES & THEIR STORIES" title="Quản lý điểm đến.">
        <Link className="button" to="/admin/places/new">
          <Plus size={17} />
          Thêm địa điểm
        </Link>
      </AdminTitle>
      <p className="admin-intro">
        Biên tập nội dung, kiểm chứng nguồn và đưa những câu chuyện đến người
        nghe.
      </p>
      <label className="search-box admin-search">
        <Search size={18} />
        <input
          aria-label="Tìm địa điểm quản trị"
          placeholder="Tìm theo tên hoặc mã…"
          value={q}
          onChange={(e) => setQ(e.target.value)}
        />
      </label>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {loading ? (
        <p role="status">Đang tải nội dung…</p>
      ) : (
        <section className="panel">
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Địa điểm</th>
                  <th>Trạng thái</th>
                  <th>Xác minh</th>
                  <th>Âm thanh VI/EN</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {rows
                  .filter((p) =>
                    (p.code + " " + p.translations.map((t) => t.name).join(" "))
                      .toLowerCase()
                      .includes(q.toLowerCase()),
                  )
                  .map((p) => (
                    <tr key={p.id}>
                      <td>
                        <div className="admin-place-cell">
                          <img src={p.imageUrl ?? ""} alt="" />
                          <div>
                            <strong>
                              {p.translations.find(
                                (t) => t.languageCode === "vi",
                              )?.name ?? p.code}
                            </strong>
                            <small>
                              {p.code} · v{p.version}
                            </small>
                          </div>
                        </div>
                      </td>
                      <td>
                        <span className={`status-pill ${p.status}`}>
                          {p.status}
                        </span>
                      </td>
                      <td>
                        {p.verificationStatus === "verified"
                          ? "Đã đối chiếu"
                          : "Chưa xác minh"}
                        {p.isDemo && <small> · Dữ liệu mẫu</small>}
                      </td>
                      <td>
                        {["vi", "en"].map((l) => (
                          <span className="locale-chip" key={l}>
                            {l.toUpperCase()}{" "}
                            {p.narrations.find((n) => n.languageCode === l)
                              ?.audioAssetId
                              ? "✓"
                              : "—"}
                          </span>
                        ))}
                      </td>
                      <td>
                        <div className="row-actions">
                          <button
                            className="icon"
                            aria-label={`Sửa ${p.code}`}
                            onClick={() => nav(`/admin/places/${p.id}`)}
                          >
                            <Pencil size={16} />
                          </button>
                          <button
                            className="text-button"
                            onClick={() => void status(p)}
                          >
                            {p.status === "published" ? "Ẩn" : "Công khai"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
          {!rows.length && (
            <p className="empty-state">
              Chưa có địa điểm. Hãy thêm điểm đầu tiên.
            </p>
          )}
        </section>
      )}
    </>
  );
}
export function AdminUsers() {
  const [rows, setRows] = useState<User[]>([]),
    [total, setTotal] = useState(0),
    [q, setQ] = useState(""),
    [status, setStatus] = useState(""),
    [page, setPage] = useState(1),
    [error, setError] = useState(""),
    [loading, setLoading] = useState(true),
    [revision, setRevision] = useState(0),
    [from, setFrom] = useState(""),
    [to, setTo] = useState(""),
    [selected, setSelected] = useState<User | null>(null);
  const dialog = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    if (selected) dialog.current?.showModal();
  }, [selected]);
  useEffect(() => {
    let cancel = false;
    setLoading(true);
    setError("");
    const params = new URLSearchParams({ page: String(page), size: "10", q });
    if (status) params.set("status", status);
    if (from) params.set("from", from);
    if (to) params.set("to", to);
    api<{ items: User[]; total: number }>(`/admin/users?${params}`)
      .then((d) => {
        if (!cancel) {
          setRows(d.items);
          setTotal(d.total);
        }
      })
      .catch((e) => {
        if (!cancel) setError(e.message);
      })
      .finally(() => {
        if (!cancel) setLoading(false);
      });
    return () => {
      cancel = true;
    };
  }, [q, status, page, revision, from, to]);
  async function change(u: User) {
    const target = u.status === "active" ? "locked" : "active";
    const reason =
      target === "locked"
        ? prompt(`Lý do khóa tài khoản ${u.email}:`)
        : undefined;
    if (target === "locked" && !reason) return;
    if (!confirm(`${target === "locked" ? "Khóa" : "Mở khóa"} ${u.email}?`))
      return;
    try {
      await post(`/admin/users/${u.id}/status`, {
        status: target,
        version: u.version,
        reason,
      });
      dialog.current?.close();
      setSelected(null);
      setRevision((n) => n + 1);
    } catch (e) {
      setError((e as Error).message);
    }
  }
  return (
    <>
      <AdminTitle kicker="PEOPLE & PERMISSIONS" title="Tài khoản khách." />
      <p className="admin-intro">
        Chỉ thông tin cần thiết để hỗ trợ và quản lý trạng thái truy cập.
      </p>
      <div className="dashboard-filter">
        <label className="search-box">
          <Search size={17} />
          <input
            placeholder="Tìm tên hoặc email…"
            aria-label="Tìm tài khoản"
            value={q}
            onChange={(e) => {
              setQ(e.target.value);
              setPage(1);
            }}
          />
        </label>
        <select
          aria-label="Trạng thái tài khoản"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
            setPage(1);
          }}
        >
          <option value="">Tất cả trạng thái</option>
          <option value="active">Đang hoạt động</option>
          <option value="locked">Bị khóa</option>
        </select>
        <label>
          Đăng ký từ
          <input
            type="date"
            value={from}
            onChange={(e) => {
              setFrom(e.target.value);
              setPage(1);
            }}
          />
        </label>
        <label>
          Đăng ký đến
          <input
            type="date"
            value={to}
            onChange={(e) => {
              setTo(e.target.value);
              setPage(1);
            }}
          />
        </label>
      </div>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {loading ? (
        <p role="status">Đang tải tài khoản…</p>
      ) : (
        <section className="panel">
          <div className="table-scroll">
            <table>
              <thead>
                <tr>
                  <th>Khách</th>
                  <th>Email</th>
                  <th>Ngôn ngữ</th>
                  <th>Ngày đăng ký</th>
                  <th>Trạng thái</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <button
                        className="text-button"
                        onClick={() => setSelected(u)}
                      >
                        {u.displayName}
                      </button>
                    </td>
                    <td>{u.email}</td>
                    <td>{u.preferredLocale.toUpperCase()}</td>
                    <td>{new Date(u.createdAt).toLocaleDateString("vi")}</td>
                    <td>
                      <span className={`status-pill ${u.status}`}>
                        {u.status}
                      </span>
                    </td>
                    <td>
                      <button
                        className="text-button"
                        onClick={() => void change(u)}
                      >
                        {u.status === "active" ? "Khóa" : "Mở khóa"}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {rows.length === 0 && (
            <p className="empty-state">Không có tài khoản phù hợp.</p>
          )}
          <div className="pagination">
            <span>
              {total} tài khoản · Trang {page}
            </span>
            <button
              className="button outline small"
              disabled={page === 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Trước
            </button>
            <button
              className="button outline small"
              disabled={page * 10 >= total}
              onClick={() => setPage((p) => p + 1)}
            >
              Sau
            </button>
          </div>
        </section>
      )}
      <dialog
        ref={dialog}
        className="account-dialog"
        onClose={() => setSelected(null)}
        aria-labelledby="user-detail-title"
      >
        {selected && (
          <>
            <span className="eyebrow">GUEST ACCOUNT</span>
            <h2 id="user-detail-title">{selected.displayName}</h2>
            <dl className="stat-list">
              <div>
                <dt>Email</dt>
                <dd>{selected.email}</dd>
              </div>
              <div>
                <dt>Ngôn ngữ</dt>
                <dd>{selected.preferredLocale.toUpperCase()}</dd>
              </div>
              <div>
                <dt>Trạng thái</dt>
                <dd>{selected.status}</dd>
              </div>
              <div>
                <dt>Đăng ký</dt>
                <dd>{new Date(selected.createdAt).toLocaleString("vi")}</dd>
              </div>
            </dl>
            <p className="muted tiny">
              Không thu thập lịch sử GPS. Mật khẩu và phiên đăng nhập không được
              hiển thị.
            </p>
            <div className="button-row">
              <button
                className="button outline"
                onClick={() => dialog.current?.close()}
              >
                Đóng
              </button>
              <button className="button" onClick={() => void change(selected)}>
                {selected.status === "active" ? "Khóa tài khoản" : "Mở khóa"}
              </button>
            </div>
          </>
        )}
      </dialog>
    </>
  );
}
