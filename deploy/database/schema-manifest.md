# SCRUM-80 — Schema, migration, seed và verification manifest

Source snapshot: baseline fdb184136e6e94f52e03e17d243c72202cff4b09 và verification mới của working tree SCRUM-80. Chưa có commit release, chưa apply DB. Khi chốt release, đối chiếu lại danh sách/checksums với đúng commit và cập nhật deployment record; không coi snapshot này là migration-history database.

Checksum dưới đây là SHA256 UTF-8 không BOM của nội dung sau chuẩn hóa CRLF → LF (không trim/chỉnh nội dung khác), để checkout Windows và build Git/Linux cùng giá trị. Operator dùng cùng thuật toán trên file của commit release; khác checksum phải dừng review. Dump backup dùng checksum byte nguyên bản riêng.

| File | Phân loại | Tiền điều kiện/cách dùng | Canonical SHA256 |
|---|---|---|---|
| database/01_schema.sql | Schema baseline | DB mới/rỗng, tạo 22 bảng | 8976f0f006d27831a621cef03f46c335c1affb9c884b70029b1d4f3fc6029ce6 |
| database/04_password_reset_requests.sql | Migration | Sau 01; reset requests | 1a265572dbdae6be2084aa3eff2c2142402b1110c5f0ab4b0699b67fd2b38d25 |
| database/05_auth_email_avatar.sql | Migration | Sau 01/04; OTP v2, verification/avatar; invalidates pending v1 | 950463c62b0d0260c852c9a315087715afc4b95e35237a8ac5cf76a75f18241d |
| database/06_auth_token_version.sql | Migration | Sau 01/04/05; token_version và security trigger | f33c6ad3ab217e52b5a797b8a6173a32097eeb07c56c93756d5e1cfdf7c9624d |
| database/02_seed.sql | Optional DEV seed | Không tự apply; DEV_HASH identities không dùng nghiệm thu Auth | 071d8e84dd8e8c85aa4cf1f045cabd07681f75d1976b74f8e5b250a5ccd2f79b |
| database/03_verify.sql | Verification | Baseline; không chạy negative write samples comment | 89984d66dea04f667ad63bfc7d71848585a4e78f2b74b32d07c45650388af3d8 |
| database/verify_auth_schema.sql | Verification | Read-only sau 01/04/05/06; không seed/migrate | a5fcfb9274e5d80debca8ae30251080167fbf812e4185107113aa25a44c9d133 |

DB mới: 01 → 04 → 05 → 06. DB existing: catalog inventory và evidence trước, chỉ scripts thiếu đúng dependency; không chạy lại CREATE/che drift bằng IF NOT EXISTS. Release có migration mới phải bổ sung manifest theo dependency, không tự bỏ qua.

Verification chỉ kiểm tra catalog/cột/type/object cần Auth, không chứng minh định nghĩa mọi business object hoặc dữ liệu. Đối chiếu schema-only dump với scripts khi nghi drift. Không API auto-migrate, không SQL qua WinForms, không ghi password/OTP/user data ra evidence. Apply/verify/backup/restore cần approval đúng target theo runbook.
