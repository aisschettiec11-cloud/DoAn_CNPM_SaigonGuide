# Saigon Guide — API Specification 1.0

Cơ sở: PRD 1.0 và ERD. API `/api/v1`, JSON UTF-8, UTC ISO 8601; cùng origin. Cookie phiên HttpOnly + antiforgery header `X-CSRF-TOKEN` cho POST/PUT/PATCH/DELETE, kể cả login. `GET /csrf` trả `{token}` và thiết lập cookie CSRF; gọi lại sau login/logout. Lỗi Problem Details `{title,status,detail,traceId,errors?}`. 400 malformed JSON; 401 thiếu/hết phiên; 403 sai quyền/CSRF; 404 không tìm thấy/điểm hidden; 409 conflict/version; 422 validation; 429 giới hạn; 500 lỗi nội bộ đã redaction.

## Endpoint

| Method/path sau /api/v1 | Quyền | Input | Output và kiểm tra |
|---|---|---|---|
| GET /csrf | Public | — | 200 `{token}`; không cache |
| GET /languages | Public | — | 200 VI/EN |
| GET /places | Public | `language=vi|en`, `q?`≤100 | 200 PlaceSummary[] published; keyword bỏ dấu ở client không làm lộ draft |
| GET /places/{id} | Public | UUID, language | 200 PlaceDetail đúng locale; hidden/draft 404 |
| GET /tiles/{z}/{x}/{y}.png | Public | z11–19, ô thuộc vùng TP.HCM | 200 image/png cache7ngày; server fixed-host OSM, TLS chuẩn, không proxy URL tùy ý; 502/504 khi upstream lỗi |
| GET /media/{id} | Public hoặc Admin | Range header | 200/206 MP3; public chỉ current narration của published; byte ranges |
| POST /auth/register | Public | email,displayName,password | 201 UserDto, role User; email trùng 409 thông báo chung; mật khẩu 12–128 |
| POST /auth/login | Public | email,password | 200 UserDto + cookie; sai/locked 401 lỗi chung, rotate session; rate limit |
| POST /auth/logout | Public + CSRF | — | 204, thu hồi session và xóa cookie |
| GET /auth/me | Auth | — | 200 UserDto hoặc 401 |
| PUT /auth/profile | Auth | displayName,preferredLocale | 200 UserDto; chỉ bản thân; email/role không sửa |
| POST /auth/password | Auth | currentPassword,newPassword | 204, thu hồi mọi phiên, xóa cookie, buộc login lại |
| GET /admin/places | Admin | — | 200 PlaceEditor[] tất cả trạng thái |
| POST /admin/places | Admin | PlaceEditor, version bỏ qua khi tạo | 201 draft + version |
| PUT /admin/places/{id} | Admin | PlaceEditor đầy đủ, version | 200 updated; optimistic concurrency 409; save published phải đạt gate |
| POST /admin/places/{id}/status | Admin | status,version | 200 updated; draft/published/hidden; publish gate giao dịch |
| POST /admin/places/{id}/audio/{language} | Admin | multipart file,version,credit,license | 201 asset + Place.version; MP3 decode valid ≤20.000.000 byte, duration≤600; gán narration trong transaction |
| GET /admin/users | Admin | q?,status?,page=1,size=20,from?,to? | 200 `{items,total,page,size}`; size≤100, ổn định createdAt/id; ngày đăng ký1900..hôm nay, không áp retention90ngày Analytics |
| POST /admin/users/{id}/status | Admin | status,reason?,version | 200 UserDto, chỉ User; khóa thu hồi ngay các phiên, audit; stale409 |
| POST /analytics/session | Consent chủ động + CSRF | `{consent:true}` | 200 `{sessionId}`, visitor cookie được bảo vệ; idle30m; consent:false422 |
| DELETE /analytics/session | Public + CSRF | — | 204, đóng session và xóa visitor cookie; không tự xóa lịch sử |
| POST /analytics/events | Consent cookie + CSRF | `{events: EventInput[]}` | 200 `{results:[{eventId,status,error?}]}`; ≤20/batch,32KB; accepted/duplicate/rejected/conflict từng item |
| GET /admin/dashboard | Admin | from/to ngày inclusive VN, bucket=day/week/month | 200 DashboardDto, default7ngày; range≤90ngày, không tương lai; aggregate một snapshot |
| GET /health/live | Public ngoài prefix | — | 200 tiến trình sống |
| GET /health/ready | Public ngoài prefix | — | 200 DB dùng được hoặc503 |

## DTO

`UserDto`: id,email,displayName,role,status,preferredLocale,version,createdAt. Không passwordHash/session/token. `PlaceSummary`: id,code,name,address,shortDescription,latitude,longitude,triggerRadiusM,imageUrl,imageAlt,isDemo,verificationStatus. `PlaceDetail` thêm languageCode,transcript,audioAssetId?,durationSeconds?,sources. Media URL lấy từ ID, không lộ storage key.

`PlaceEditor`: id?,code,latitude?,longitude?,triggerRadiusM,status,verificationStatus,isDemo,imageUrl?,imageCredit?,imageLicense?,sources[],translations[{languageCode,name,address,shortDescription,imageAlt}],narrations[{languageCode,transcript,sources[],audioAssetId?}],version. Chỉ VI/EN, unique locale; mã slug không đổi sau tạo; tọa độ finite đúng khoảng/radius25–500; text theo giới hạn ERD. Mọi child mutation khóa Place và tăng version. Public publish yêu cầu hai translation/narration/audio, ảnh/quyền/nguồn và verified; production từ chối isDemo. Không có delete cứng.

`EventInput`: eventId UUID,type enum,clientTimestamp UTC,sessionId,placeId?,languageCode?,pageKey?,playId?,durationSeconds?,playedSeconds?,trigger?. Reject unknown fields (GPS/email/userId/role). Backend gán receivedAt/user từ auth, duration và audio từ narration/start đã accepted. Page keys explore/place_detail/privacy. Audio start chỉ playing, pause/resume không start mới, complete chỉ ended và ≥90% duration + ≥90% server elapsed. Event ID trùng cùng payload/context trả duplicate; trùng khác payload conflict. `(session,play,type)` unique. Start/completion phải cùng point/locale. Không ghép event stale sang session mới. Client timestamp tối đa cũ5phút/tương lai1phút. Thời điểm KPI dùng server receivedAt. Session đóng/idle từ chối event mới; retry đã accepted kiểm context rồi trả duplicate.

`DashboardDto`: range{from,to,timeZone,bucket},kpis{pageViews,sessions,visitors,placeViews,narrationStarts,narrationCompletions,registeredUsers,activeUsers,completionRate?},traffic[{date,pageViews,sessions}],places[{id,name,status,views,sessions,entries,starts,completions,startRate?}],languages[{code,selections,starts}]. Distinct toàn range, cohort completion theo start, startRate là distinct session có view và start sau view / session có view. Mẫu số0=null. Có 6 điểm kể cả hidden/0. Không raw visitor/user IDs.

## Ví dụ

```json
{"email":"user@example.com","password":"a-long-private-password"}
```

Login sai:
```json
{"title":"Đăng nhập thất bại","status":401,"detail":"Email hoặc mật khẩu không hợp lệ.","traceId":"request-id"}
```

Conflict:
```json
{"title":"Xung đột dữ liệu","status":409,"detail":"Nội dung đã được cập nhật. Vui lòng tải lại.","traceId":"request-id"}
```

Validation:
```json
{"title":"Dữ liệu không hợp lệ","status":422,"detail":"Bán kính phải từ 25 đến 500 m.","traceId":"request-id"}
```

Event:
```json
{"events":[{"eventId":"93a9470f-a581-4be2-bcce-648391b00ad9","type":"place_view","clientTimestamp":"2026-10-22T10:00:00Z","sessionId":"914409a5-81bf-401d-b4b8-48bb1d466a68","placeId":"fd28c6af-1ba6-4e95-9438-044c59a008c3","languageCode":"vi"}]}
```

Không có API pause/GPS/Haversine; browser xử lý. Authentication idle30m/absolute8h, cookie Secure trên HTTPS và SameSite Lax; login/password có rate limit. Những URL/UUID minh họa không phải tài khoản/traffic thật. OpenAPI máy đọc được sinh từ controller sau implementation; kiểm endpoint và test thực tế là điều kiện bàn giao, không mặc định tài liệu đã chứng minh API hoạt động.
