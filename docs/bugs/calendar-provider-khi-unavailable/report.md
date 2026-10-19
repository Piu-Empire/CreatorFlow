# Bug report — Calendar.CardProvider trả list rỗng khi Lịch đang "unavailable"

## Triệu chứng

CI PR #40 (nhánh `SCRUM-52-tinh-engagement-va-tổng-hợp-metrics`), job
**Build and test (Windows)** đỏ với 1 test fail:

```text
Failed IntegratedCalendar_IsUnavailableAndDisposedOnProjectSwitch [76 ms]
Assert.IsNull(calendar.CardProvider!())
Expected value to be null.
actual: []
tests/CreatorFlow.IntegrationTests/ApiClients/ProjectShellTests.cs:line 454
Failed: 1, Passed: 419, Total: 420
```

Test chỉ fail ở dòng 454; các assert còn lại của test (Lịch unavailable và bị dispose khi
đổi project) không được chạy tới sau khi assert fail.

## Tái hiện

1. `UxFixture` mở `BoardForm` ở chế độ server (`_projects != null`), `_boardBackend == null`.
2. `CompleteSelection(shell, 81, true)` → `CurrentSession.CurrentProjectId = 81` (HasProjectContext = true).
3. Gọi `BoardForm.OpenCalendar()` → `calendar.UnavailableReason` được gán (assert 453 pass).
4. `calendar.CardProvider()` trả `_allCards` (list rỗng) thay vì `null`.

Chạy riêng test:

```powershell
dotnet test tests/CreatorFlow.IntegrationTests/CreatorFlow.IntegrationTests.csproj `
  --filter "FullyQualifiedName~IntegratedCalendar_IsUnavailableAndDisposedOnProjectSwitch"
```

## Root cause

Commit `a7eedc0` (SCRUM-49 + SCRUM-53) đổi `BoardForm.OpenCalendar`:

```diff
- calendar.CardProvider = () => HasProjectContext && _projects is null ? _allCards : null;
+ calendar.CardProvider = () => HasProjectContext ? _allCards : null;
- if (_projects is not null) calendar.UnavailableReason = "...";
+ if (_projects is not null && _boardBackend is null) calendar.UnavailableReason = "...";
```

Ý định của thay đổi là cho phép Calendar chạy ở chế độ server khi có `_boardBackend`
(SCRUM-49/48 mở Calendar cho dữ liệu server). Nhưng điều kiện `CardProvider` bị nới quá tay:
nó trả `_allCards` kể cả khi Lịch **không khả dụng** (`_projects != null && _boardBackend == null`).

- `CalendarControl.RefreshData()` kiểm tra `UnavailableReason` trước và return sớm, nên lỗi này
  hiện **không** gây sai dữ liệu hiển thị.
- Tuy nhiên nó vi phạm contract mà test (và chú thích property) đang mô tả: `CardProvider`
  `null` = chưa có nguồn dữ liệu / chưa chọn project. Vì vậy test cũ fail.
- Test này không được sửa trong `a7eedc0`, nên đây là regression của nhánh, không phải test hỏng.
- `develop` vẫn dùng điều kiện cũ nên CI `develop` không bị ảnh hưởng.

## Hướng sửa đề xuất

Sửa phía product code, giữ hành vi mới của SCRUM-49 (server có backend thì Calendar chạy),
chỉ trả `null` khi Lịch thực sự unavailable:

```csharp
calendar.CardProvider = () =>
    HasProjectContext && (_projects is null || _boardBackend is not null) ? _allCards : null;
```

Hoặc tính `unavailable` một lần rồi dùng cho cả `UnavailableReason` và `CardProvider`
để hai điều kiện không lệch nhau về sau.

Không đề xuất sửa test thành `Assert.AreEqual(0, calendar.CardProvider!().Count)` vì như vậy
sẽ hợp thức hóa việc cấp dữ liệu cho một màn hình đang báo "chưa khả dụng".

## Phạm vi và rủi ro

- Chỉ đụng `1 dòng` trong `BoardForm.OpenCalendar` (và có thể tách biến `unavailable`).
- Không đổi `CalendarControl`, `DashboardControl`, API hay DB.
- Rủi ro thấp: local mode (`_projects == null`) vẫn trả `_allCards`; server mode có
  `_boardBackend` vẫn trả `_allCards`; chỉ trường hợp unavailable đổi từ `[]` về `null`,
  đúng như trước `a7eedc0`.
- Lưu ý: `DashboardControl` dùng cùng pattern `HasProjectContext ? _allCards : null` nhưng
  chưa có test tương ứng; ngoài phạm vi bug này, chỉ ghi nhận.

## Trạng thái: ĐÃ SỬA

Đã áp dụng hướng sửa phía product code trong `BoardForm.OpenCalendar` (giữ điều kiện
`_projects is null || _boardBackend is not null` cho `CardProvider`).

## Kiểm chứng

1. `IntegratedCalendar_IsUnavailableAndDisposedOnProjectSwitch` — **Passed**.
2. `ProjectShellTests` (39 test) — **Passed**.
3. Toàn bộ client suite với filter CI
   (`FullyQualifiedName!~CreatorFlow.IntegrationTests.Database&TestCategory!=ApiLive`)
   — **Passed: 416, Failed: 0** (local không chạy 4 test ApiLive/Database như CI runner).
