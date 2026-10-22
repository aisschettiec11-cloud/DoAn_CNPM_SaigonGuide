# Luồng nghiệp vụ và thiết kế tương tác

Cơ sở: PRD1.1, ERD, API Specification. Sơ đồ là thiết kế phản ánh implementation, không là bằng chứng deploy public hoặc GPS thực địa.

## 1. Use cases theo actor

```mermaid
flowchart LR
  G[Guest] --> P[Xem map, danh sách và chi tiết VI EN]
  G --> L[Bật tắt GPS cục bộ]
  G --> A[Nghe hoặc đọc thuyết minh]
  G --> C[Chọn hoặc thu hồi Analytics]
  U[User] --> P
  U --> A
  U --> F[Hồ sơ và đổi mật khẩu của mình]
  M[Admin] --> S[Đăng nhập quản trị]
  S --> Q[Quản trị nội dung và âm thanh]
  S --> D[Dashboard aggregate]
  S --> K[Quản lý và khóa User]
```

User/Admin chỉ có quyền server đã xác minh; đường nối actor không thay backend authorization. Guest không có bản ghi tài khoản.

## 2. GPS và âm thanh

```mermaid
sequenceDiagram
  actor Guest
  participant UI as Browser React
  participant GPS as Geolocation
  participant Geo as Geofence state machine
  participant API as API Services
  participant Audio as HTMLAudioElement
  Guest->>UI: Bật GPS
  UI->>GPS: watchPosition sau thao tác
  alt Bị từ chối hoặc không khả dụng
    GPS-->>UI: denied hoặc lỗi
    UI-->>Guest: Giữ map list đọc và nghe thủ công
  else Có mẫu GPS
    GPS-->>Geo: Tọa độ trong bộ nhớ browser
    Geo->>Geo: Accuracy, tuổi mẫu, hai mẫu, latch và cooldown
    alt Đủ điều kiện và player rảnh
      Geo-->>UI: Một đề nghị nghe
      opt Đã bật tự phát
        UI->>API: Lấy nội dung và audio ID
        API-->>UI: Nội dung published đúng locale
        UI->>Audio: play
        alt Browser chặn
          Audio-->>UI: NotAllowedError
          UI-->>Guest: Nút Nghe thủ công và transcript
        else Đã playing
          Audio-->>UI: playing
          opt Consent Analytics
            UI->>API: narration_started, play ID
            API-->>UI: accepted hoặc duplicate
          end
        end
      end
    end
  end
  Guest->>UI: Tắt GPS hoặc rời màn hình
  UI->>GPS: clearWatch
```

Không có tọa độ khách truyền đến API. Pause/resume giữ play ID; replay có ID mới. Completion chỉ gửi sau start được xác nhận và ended; server xác minh matching, elapsed và thời lượng. Đổi transcript phải cập nhật audio phù hợp trước khi công khai lại.

## 3. Xác thực và thu hồi

```mermaid
sequenceDiagram
  actor User
  participant Browser
  participant Controller
  participant Auth as AuthService
  participant DB as PostgreSQL
  User->>Browser: Nhập email và mật khẩu
  Browser->>Controller: GET csrf
  Controller-->>Browser: Token CSRF và cookie bảo vệ
  Browser->>Controller: POST login kèm CSRF
  Controller->>Auth: Validate và rate-limit
  Auth->>DB: Tìm tài khoản
  Auth->>Auth: Verify PasswordHasher
  Auth->>DB: Khóa User, kiểm status và version
  Auth->>DB: Thu hồi phiên cũ, lưu hash handle mới
  Auth-->>Browser: Cookie opaque HttpOnly
  Browser->>Controller: Protected request
  Controller->>DB: Kiểm session expiry, role, active và revocation
  Controller-->>Browser: DTO được phép hoặc401/403
  User->>Browser: Đăng xuất hoặc đổi mật khẩu
  Browser->>Auth: POST có CSRF
  Auth->>DB: Thu hồi session
  Auth-->>Browser: Xóa cookie
```

## 4. Ghi event và chống trùng

```mermaid
sequenceDiagram
  participant Browser
  participant API as AnalyticsController
  participant Service as AnalyticsService
  participant DB as PostgreSQL
  Browser->>API: Chủ động consent và xin session
  API->>Service: Context hợp lệ
  Service->>DB: Serialize theo visitor, đóng idle session nếu cần
  Service-->>Browser: Session ID và visitor cookie được bảo vệ
  Browser->>API: Batch event allowlist, UUID và CSRF
  API->>Service: Kiểm schema, size và rate
  Service->>DB: Kiểm context và ID đã tiếp nhận
  alt Cùng ID, cùng payload
    Service-->>Browser: duplicate, không ghi hoặc kéo dài phiên
  else ID bị dùng khác dữ liệu
    Service-->>Browser: conflict
  else Event mới hợp lệ
    Service->>DB: INSERT event và last_event_at nguyên tử
    Service-->>Browser: accepted
  end
```

Dữ liệu client không được coi là bằng chứng danh tính, hiện diện hoặc nghe thật. Không có quyền sửa KPI trực tiếp. Từ chối/thu hồi dừng đo và không cản du lịch.

## 5. Dashboard

```mermaid
sequenceDiagram
  actor Admin
  participant UI as Admin React
  participant Controller
  participant Service as DashboardService
  participant DB as PostgreSQL
  Admin->>UI: Chọn range và bucket
  UI->>Controller: GET dashboard
  Controller->>Controller: Authentication và Admin authorization
  Controller->>Service: Validate90ngày và giờ Việt Nam
  Service->>DB: REPEATABLE READ snapshot
  Service->>DB: SQL count distinct, cohort và date_trunc
  DB-->>Service: Aggregate, không raw event cá nhân
  Service->>Service: Thêm điểm và bucket rỗng, N/A đúng mẫu số
  Service-->>UI: Một response nhất quán
  UI-->>Admin: KPI, chart và bảng dữ liệu
```
