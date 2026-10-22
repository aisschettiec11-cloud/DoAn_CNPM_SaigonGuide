# Cấu trúc thư mục đã chọn

```text
apps/
  api/
    Controllers/       # Presentation, DTO binding và status code
    Contracts/         # Request/response DTO
    Domain/            # Entities và enums
    Data/              # DbContext, migrations, seeder và truy vấn
    Services/          # Auth, content, analytics và media nghiệp vụ
    Security/          # Authentication handler, CSRF và session
    wwwroot/           # Frontend production build, generated
  web/
    src/
      components/      # Player, Map và UI dùng chung
      pages/           # Explore, Auth, Admin
      lib/             # API client, analytics, geofence thuần
      styles.css
    tests/             # Unit và E2E
 tests/
  SaigonGuide.Tests/   # Backend unit/integration
 scripts/              # Setup, start, test, backup, restore
 deploy/               # Dockerfile, Compose, TLS proxy
 content/              # Nội dung song ngữ và hồ sơ nguồn, không secret
 docs/                 # PRD, ERD, API, backlog, hướng dẫn/kết quả
 .github/workflows/    # CI
```

Controllers → Services → EF DbContext (Data Access). Không đặt business rule vào React hoặc controller chỉ để vượt qua gate. Auth kiểm tại backend; frontend UI guards chỉ hỗ trợ UX. Không tạo worktree cho cloud task đã cách ly. Runtime local (DB password, Admin bootstrap password, media, logs) nằm `.local/` bị ignore. Dependencies/build outputs không commit. Deployment DB/media dùng volume bền vững.
