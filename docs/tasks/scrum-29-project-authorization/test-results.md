# SCRUM-29 — Kết quả kiểm chứng

Người dùng duyệt chạy test bằng yêu cầu “test”. Thực hiện ngày 19/10/2026 trên branch `SCRUM-29-Kiểm-tra-quyền-Owner-Manager-Creator`. Không sửa code trong giai đoạn test, không chạy API triển khai hoặc thao tác database thật.

## Kết quả

| Nhóm | Kết quả | Bằng chứng |
| --- | --- | --- |
| Build solution | Thành công, 0 lỗi, 3 cảnh báo | `dotnet build CreatorFlow.slnx --no-restore -m:1 -v:normal`, exit 0 |
| Backend/API theo filter đã duyệt | 243 passed, 0 failed, 0 skipped | `test-results/scrum29-api.trx` |
| WinForms/client theo filter đã duyệt | 73 passed, 0 failed, 0 skipped | `test-results/scrum29-client.trx` |
| Whitespace | Không phát hiện lỗi | `git diff --check`, exit 0 |

Đã đối chiếu TRX: các test mới về JWT actor và giả mạo role, Manager dùng chức năng Owner, truy cập chéo Project, role đổi/membership mất, self-assignment đổi deadline, quyền Workflow, chuyển menu theo Project/logout và refresh quyền sau 403 đều Passed.

## Điều chỉnh môi trường khi chạy

- Lệnh build/test mặc định trong sandbox ban đầu dừng hoặc chưa chạy được test host. Log compiler ghi `UnauthorizedAccessException`/timeout khi kết nối named pipe.
- Build tuần tự `--no-restore -m:1` thành công; test projects cũng được biên dịch với `--no-restore -m:1 -p:UseSharedCompilation=false`.
- Hai lần chạy test trong sandbox chưa trả kết quả đã được dừng. Sau đó chạy lại ngoài sandbox bằng `--no-build --no-restore`, giữ nguyên filter đã duyệt, thêm TRX logger và `--blame-hang-timeout 60s`. Cả hai hoàn thành thành công; không phát sinh hang dump, collector báo tất cả test đã hoàn thành.
- Không sửa project/package/config để vượt hạn chế môi trường.

## Cảnh báo và giới hạn

- Build báo NU1900: không tải được dữ liệu vulnerability từ NuGet.
- IntegrationTests báo NU1902/NU1903 cho dependency hiện có `System.Security.Cryptography.Pkcs` 10.0.0. Không nâng package trong SCRUM-29.
- API test sử dụng JWT middleware thật với repository fake; client/UI dùng HTTP stub và fixture WinForms. Kết quả này không xác nhận persistence/transaction trên PostgreSQL thật hoặc end-to-end với API triển khai.
- Chưa chạy kiểm chứng thủ công với tài khoản và môi trường thật; phần đó cần môi trường/quyền thao tác được xác nhận riêng theo test-plan.
- Chưa commit/push/tạo PR/cập nhật Jira.
