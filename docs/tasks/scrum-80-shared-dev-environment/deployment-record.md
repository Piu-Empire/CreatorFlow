# SCRUM-80 — Deployment record

Trạng thái: NOT DEPLOYED. Điền sau approval, không ghi connection/password/key/token/OTP hoặc screenshot secrets.

| Trường | Giá trị |
|---|---|
| Baseline đọc | fdb184136e6e94f52e03e17d243c72202cff4b09 (không phải release SCRUM-80) |
| Branch | feature/SCRUM-80-shared-dev-environment |
| Approved release SHA | Chưa có commit release; source ở working tree |
| Operator/account owner | Chưa xác nhận |
| Render service/account/region | Chưa provision |
| URL HTTPS onrender.com | Chưa cấp |
| Render deployment ID/commit | Chưa deploy |
| Startup Version | NOT RUN |
| OCI revision/digest nếu exposed | NOT RUN; ghi limitation nếu provider không exposed |
| Neon project/endpoint/DB/version | Chưa provision; tối thiểu DEV cần thiết |
| Brevo sender/mail configured | Chưa cấu hình; Free 2525 có điều kiện |
| Schema/migration/checksum/applied time | NOT RUN; xem schema manifest |
| Backup/restore drill | NOT RUN |
| Máy A/B/user alias | Chưa xác nhận |
| Quyền provision/deploy/DB/test/mail | Chưa cấp |

## Quota check từng đợt lớn

Copy bảng cho mỗi đợt; ghi giờ/timezone, không invent usage. Gần quota (>=80% hoặc thiếu cho workload/dự phòng) dừng test không cần thiết, chờ/reset. Storage không reset cần review miễn phí, không upgrade.

| Mục | Trước đợt | Kết luận |
|---|---|---|
| Thời điểm/người đọc | NOT CHECKED | |
| Free plan/không payment method mọi provider | NOT CHECKED | |
| Render bandwidth/build/instance-hours dùng/còn/reset | NOT CHECKED | |
| Neon compute/transfer dùng/còn/reset | NOT CHECKED | |
| Neon storage dùng/còn | NOT CHECKED | |
| Brevo quota/sender nếu mail | NOT CHECKED | |
| Workload dự kiến + dự phòng | NOT ESTIMATED | |
| Proceed/stop/BLOCKED + approval | STOP: chưa có quyền/thông tin | |

## Migration/release log

Mỗi script ghi: target DEV xác nhận, SHA256/precondition/catalog state, backup checksum/location ngoài repo, giờ/exit status, verify summary, API SHA tương ứng. Không data rows/secret. Không coi script applied chỉ vì tạo file/hướng dẫn.

## Local wave2 evidence — không phải shared DEV deploy

Đợt2 ngày07/10/2026 đã duyệt: runtime image local revision, Linux51/51 và DB23/23 pass;
schema01/04/05/06, verify/upgrade/dump/restore/restart/health/readiness trên DB disposable local.
Chi tiết resource IDs/digest/backup hashes tại [wave2 results](test-results-wave2.md).
3container disposable đã dừng, volume/dumps giữ lại; cloud rows bên trên vẫn NOT DEPLOYED/NOT RUN.
