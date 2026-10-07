# SCRUM-80 — Điều tra cảnh báo Linux runtime sau đợt 2

Trạng thái: ĐÃ ĐIỀU TRA, CHƯA SỬA, CHỜ REVIEW nếu muốn thay cấu hình. Các kiểm tra chức năng
đợt2 pass; đây là runtime warnings, không failure kết nối hoặc migration.

## 1. Thiếu GSS/Kerberos library

Hiện tượng khi API Linux mở PostgreSQL connection: `Cannot load library libgssapi_krb5.so.2`
và thông báo library không tìm thấy. Readiness sau migrated schema vẫn200, DB tests23/23 pass.

Nguyên nhân: source pin Npgsql10.0.3; connection test không đặt GSS Encryption Mode. Theo
[Npgsql security docs](https://www.npgsql.org/doc/security), mặc định Prefer trong10 thử GSSAPI;
Linux thiếu Kerberos phát cảnh báo, Npgsql xử lý và fallback kết nối không GSSAPI. Log và kết quả
runtime khớp hành vi được tài liệu mô tả; không phải thiếu SkiaSharp/native avatar hoặc credential sai.

Đề xuất optional để log gọn: thêm `GSS Encryption Mode=Disable` vào Npgsql configuration **của
môi trường DEV dùng password/TLS**, ghi environment reference/runbook tương ứng; không thêm
package Kerberos/Docker image hoặc đổi Auth. Giữ `SSL Mode=VerifyFull` cho Neon. Disable GSS
không phải disable TLS; trước khi áp dụng phải review nếu có môi trường thực dùng Kerberos.

Phạm vi nếu duyệt: `deploy/render/environment.example`, `docs/shared-dev-test-runbook.md`;
configuration server mới khi được phép, không toàn bộ client/local legacy. Chưa áp dụng thay đổi
trong test hoặc sửa connection để che log. Retest connection/readiness + check stderr sau config
được duyệt, không dùng cloud/SMTP nếu chưa có permission.

## 2. Data Protection key warning

Log ASP.NET FileSystemXmlRepository[60]: keys tại `/home/app/.aspnet/DataProtection-Keys`
không persisted bên ngoài container; XmlKeyManager[35]: không XML encryptor, key có thể lưu
không mã hóa. Không xuất XML/key contents trong điều tra.

Root cause ở mức đã chứng minh: runtime dùng default filesystem key repository không mount
volume/không cấu hình XML encryption. Dockerfile non-root không persistent disk; code API không
có AddDataProtection/custom repository. [Microsoft default settings](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings)
giải thích key management khi host container.

Source `JwtTokenIssuer` dùng SigningCredentials/HmacSha256 với JwtConfiguration signing key;
`EmailOtpCodeProtector` dùng HMACSHA256 riêng. Không thấy explicit IDataProtectionProvider,
cookie/session consumer trong source API. **Suy luận giới hạn:** chưa có evidence JWT/OTP hiện
tại phụ thuộc Data Protection key ring; restart health/DB pass không chứng minh mọi framework
feature không dùng nó. Không gọi warning harmless cho module tương lai dùng cookies/antiforgery.

Đề xuất hiện tại: ghi technical limitation, không mua persistent disk/Key Vault/storage để dọn
warning. Không tự disable toàn bộ logging hoặc thay đổi key management. Khi cần Data Protection
consumer thật, review riêng giải pháp miễn phí/quản lý keys và tác động redeploy; không mở rộng
SCRUM-80 sang hệ thống lưu key mới khi chưa có yêu cầu.

## Phạm vi/rủi ro và kết luận

- Không leak JWT/HMAC/DB passwords được quan sát trong warning trích; GUID key ID không là key material.
- Chỉ xác nhận functional local tests pass; secret/log audit cloud và TLS thật vẫn chưa kiểm chứng.
- GSS cleanup là optional config follow-up, không lý do mua dịch vụ/package mới. Data Protection
  cần ghi rõ giới hạn nếu sau này bảo vệ cookies/data qua redeploy.
- Không sửa code/config trong lúc test. Muốn áp dụng đề xuất thì duyệt hướng sửa trước theo
  bug-investigation-workflow/AGENTS.md; ngân sách và các gate cloud/DB/test vẫn giữ nguyên.
