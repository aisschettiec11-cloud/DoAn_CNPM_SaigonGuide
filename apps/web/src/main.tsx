import { useEffect, lazy, Suspense } from "react";
import type { ReactNode } from "react";
import { createRoot } from "react-dom/client";
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useLocation,
  Link,
} from "react-router-dom";
import { AppProvider, useApp } from "./lib/AppContext";
import {
  Header,
  Footer,
  Player,
  ConsentBanner,
  Nearby,
} from "./components/Shell";
import Explore from "./pages/Explore";
import Detail from "./pages/Detail";
import Auth, { Account } from "./pages/Auth";
import Privacy from "./pages/Privacy";
const AdminLayout = lazy(() =>
  import("./pages/Admin").then((m) => ({ default: m.AdminLayout })),
);
const AdminDashboard = lazy(() =>
  import("./pages/Admin").then((m) => ({ default: m.AdminDashboard })),
);
const AdminPlaces = lazy(() =>
  import("./pages/Admin").then((m) => ({ default: m.AdminPlaces })),
);
const AdminUsers = lazy(() =>
  import("./pages/Admin").then((m) => ({ default: m.AdminUsers })),
);
const Editor = lazy(() => import("./pages/Editor"));
import "./styles.css";
function Protected({
  children,
  admin = false,
}: {
  children: ReactNode;
  admin?: boolean;
}) {
  const a = useApp();
  if (!a.authReady)
    return (
      <p className="page-space narrow" role="status">
        Đang kiểm tra phiên đăng nhập…
      </p>
    );
  if (!a.user) return <Navigate to="/login" replace />;
  if (admin && a.user.role !== "Admin")
    return (
      <main className="page-space narrow">
        <h1>Bạn không có quyền truy cập.</h1>
        <Link to="/">Về trang khám phá</Link>
      </main>
    );
  return children;
}
function Scroll() {
  const loc = useLocation();
  useEffect(() => {
    if (!loc.hash) window.scrollTo(0, 0);
  }, [loc.pathname]);
  return null;
}
function App() {
  return (
    <BrowserRouter>
      <AppProvider>
        <a className="skip-link" href="#main-content">
          Bỏ qua điều hướng
        </a>
        <Header />
        <Scroll />
        <div id="main-content">
          <Suspense
            fallback={
              <p className="narrow page-space" role="status">
                Đang tải…
              </p>
            }
          >
            <Routes>
              <Route path="/" element={<Explore />} />
              <Route path="/places/:id" element={<Detail />} />
              <Route path="/login" element={<Auth />} />
              <Route path="/register" element={<Auth register />} />
              <Route path="/privacy" element={<Privacy />} />
              <Route
                path="/account"
                element={
                  <Protected>
                    <Account />
                  </Protected>
                }
              />
              <Route
                path="/admin"
                element={
                  <Protected admin>
                    <AdminLayout />
                  </Protected>
                }
              >
                <Route index element={<AdminDashboard />} />
                <Route path="places" element={<AdminPlaces />} />
                <Route path="places/:id" element={<Editor />} />
                <Route path="users" element={<AdminUsers />} />
              </Route>
              <Route
                path="*"
                element={
                  <main className="narrow page-space">
                    <h1>Trang này chưa có câu chuyện.</h1>
                    <Link to="/">Trở về khám phá →</Link>
                  </main>
                }
              />
            </Routes>
          </Suspense>
        </div>
        <Footer />
        <ConsentBanner />
        <Nearby />
        <Player />
      </AppProvider>
    </BrowserRouter>
  );
}
createRoot(document.getElementById("root")!).render(<App />);
