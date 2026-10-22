import { useState } from "react";
import type { FormEvent } from "react";
import { Link, useNavigate } from "react-router-dom";
import { ArrowRight, Compass, Eye, EyeOff } from "lucide-react";
import { useApp } from "../lib/AppContext";
import { api, post, refreshCsrf } from "../lib/api";
export default function Auth({ register = false }: { register?: boolean }) {
  const a = useApp(),
    nav = useNavigate(),
    vi = a.locale === "vi";
  const [email, setEmail] = useState(""),
    [password, setPassword] = useState(""),
    [name, setName] = useState(""),
    [show, setShow] = useState(false),
    [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      if (register)
        await post("/auth/register", { email, password, displayName: name });
      await post("/auth/login", { email, password });
      await refreshCsrf();
      await a.reloadUser();
      const u = await api<{ role: string }>("/auth/me");
      nav(u.role === "Admin" ? "/admin" : "/");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="auth-page">
      <aside className="auth-art">
        <Compass size={52} strokeWidth={1} />
        <span className="eyebrow">A LITTLE CLOSER TO SAIGON</span>
        <h1>
          {vi ? (
            <>
              Có một Sài Gòn
              <br />
              đang chờ bạn
              <br />
              <em>lắng nghe.</em>
            </>
          ) : (
            <>
              A whole city.
              <br />A thousand stories.
              <br />
              <em>All yours.</em>
            </>
          )}
        </h1>
        <img src="/images/nha-tho-duc-ba.svg" alt="Minh họa thành phố" />
        <p>
          {vi
            ? "Không cần tài khoản để bắt đầu hành trình."
            : "No account needed to begin exploring."}{" "}
          <Link to="/">{vi ? "Khám phá ngay" : "Explore now"} →</Link>
        </p>
      </aside>
      <section className="auth-form">
        <span className="eyebrow">SAIGON GUIDE</span>
        <h2>
          {register
            ? vi
              ? "Một hành trình mới."
              : "A new beginning."
            : vi
              ? "Rất vui gặp lại bạn."
              : "Welcome back."}
        </h2>
        <p>
          {register
            ? vi
              ? "Tạo tài khoản để quản lý hồ sơ của bạn."
              : "Create an account to manage your profile."
            : vi
              ? "Đăng nhập để tiếp tục với Saigon Guide."
              : "Sign in to continue with Saigon Guide."}
        </p>
        <form onSubmit={(e) => void submit(e)}>
          {register && (
            <label>
              {vi ? "Tên hiển thị" : "Display name"}
              <input
                required
                maxLength={100}
                autoComplete="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
              />
            </label>
          )}
          <label>
            Email
            <input
              type="email"
              autoComplete="email"
              maxLength={254}
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
          </label>
          <label>
            {vi ? "Mật khẩu" : "Password"}
            <div className="password-field">
              <input
                type={show ? "text" : "password"}
                autoComplete={register ? "new-password" : "current-password"}
                minLength={register ? 12 : undefined}
                maxLength={128}
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <button
                type="button"
                className="icon"
                aria-label={show ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                onClick={() => setShow(!show)}
              >
                {show ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
            {register && (
              <small>
                {vi
                  ? "12–128 ký tự. Không dùng lại mật khẩu của dịch vụ khác."
                  : "12–128 characters. Use a unique password."}
              </small>
            )}
          </label>
          {error && (
            <p role="alert" className="form-error">
              {error}
            </p>
          )}
          <button className="button full" disabled={busy}>
            {busy
              ? vi
                ? "Đang xử lý…"
                : "Please wait…"
              : register
                ? vi
                  ? "Tạo tài khoản"
                  : "Create account"
                : vi
                  ? "Đăng nhập"
                  : "Sign in"}
            <ArrowRight size={18} />
          </button>
        </form>
        <p className="auth-switch">
          {register
            ? vi
              ? "Đã có tài khoản?"
              : "Already registered?"
            : vi
              ? "Chưa có tài khoản?"
              : "New here?"}{" "}
          <Link to={register ? "/login" : "/register"}>
            {register
              ? vi
                ? "Đăng nhập"
                : "Sign in"
              : vi
                ? "Đăng ký"
                : "Create an account"}
          </Link>
        </p>
        <p className="muted tiny">
          {vi
            ? "Tài khoản Admin được cấp riêng. Không có đăng ký Admin công khai."
            : "Administrator accounts are provisioned separately."}
        </p>
      </section>
    </main>
  );
}
export function Account() {
  const a = useApp(),
    nav = useNavigate();
  const [name, setName] = useState(a.user?.displayName ?? ""),
    [locale, setLocale] = useState(a.user?.preferredLocale ?? "vi"),
    [old, setOld] = useState(""),
    [password, setPassword] = useState(""),
    [error, setError] = useState(""),
    [message, setMessage] = useState(""),
    [busy, setBusy] = useState(false);
  async function profile(e: FormEvent) {
    e.preventDefault();
    setError("");
    setMessage("");
    setBusy(true);
    try {
      await api("/auth/profile", {
        method: "PUT",
        body: JSON.stringify({ displayName: name, preferredLocale: locale }),
      });
      await a.reloadUser();
      setMessage("Đã lưu hồ sơ.");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function change(e: FormEvent) {
    e.preventDefault();
    setError("");
    setBusy(true);
    try {
      await post("/auth/password", {
        currentPassword: old,
        newPassword: password,
      });
      await a.reloadUser();
      await refreshCsrf();
      nav("/login");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="narrow page-space">
      <span className="eyebrow">YOUR ACCOUNT</span>
      <h1>Hồ sơ của bạn</h1>
      <p className="muted">
        Tài khoản {a.user?.status === "active" ? "đang hoạt động" : "bị khóa"}.
        Chức năng khám phá luôn sử dụng được không cần đăng nhập.
      </p>
      {error && (
        <p className="form-error" role="alert">
          {error}
        </p>
      )}
      {message && (
        <p className="success" role="status">
          {message}
        </p>
      )}
      <form className="panel" onSubmit={(e) => void profile(e)}>
        <h2>Thông tin cơ bản</h2>
        <label>
          Email
          <input value={a.user?.email ?? ""} readOnly />
        </label>
        <label>
          Tên hiển thị
          <input
            required
            maxLength={100}
            value={name}
            onChange={(e) => setName(e.target.value)}
          />
        </label>
        <label>
          Ngôn ngữ ưu tiên
          <select
            value={locale}
            onChange={(e) => setLocale(e.target.value as "vi" | "en")}
          >
            <option value="vi">Tiếng Việt</option>
            <option value="en">English</option>
          </select>
        </label>
        <button className="button" disabled={busy}>
          Lưu hồ sơ
        </button>
      </form>
      <form className="panel" onSubmit={(e) => void change(e)}>
        <h2>Đổi mật khẩu</h2>
        <p className="muted">
          Sau khi đổi, tất cả phiên đăng nhập sẽ được thu hồi.
        </p>
        <label>
          Mật khẩu hiện tại
          <input
            type="password"
            autoComplete="current-password"
            required
            maxLength={128}
            value={old}
            onChange={(e) => setOld(e.target.value)}
          />
        </label>
        <label>
          Mật khẩu mới
          <input
            type="password"
            autoComplete="new-password"
            required
            minLength={12}
            maxLength={128}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </label>
        <button className="button" disabled={busy}>
          Đổi mật khẩu & đăng nhập lại
        </button>
      </form>
    </main>
  );
}
