# Đánh giá và xác thực các api cho luồng Bão dưỡng
_Exported on 11/5/2025 at 22:04:37 GMT+7 from Cursor (2.0.34)_

---

**User**

Tôi chuẩn bị sang luồng Bão dưỡng.

Tôi có liệt kê vài api bạn hãy cho tôi đánh giá và ghi ra hinhg dung luồng mà bạn hiểu và các validation cho nó

/api/Booking/{id}/status - staff chuyển sang confirm

/api/Booking/{id}/status - techinician chuyển sang progressing

/api/maintenance-checklist/{bookingId} - techinician lấy lên danh sách

/api/maintenance-checklist/{bookingId}/results/{resultId} - technician update tình trạng - > gửi qua cho khách hàng

/api/Booking/{bookingId}/parts/{workOrderPartId}/customer-approve - cho phép thay thế - nằm ở Booking history detail của customer

/api/Promotion/bookings/{bookingId}/apply - apply nếu có

/api/Booking/{id}/status - chuyển sang completed

---

**Cursor**

[Mode：Intelligent]
Phân tích sơ bộ cho thấy yêu cầu phù hợp với giai đoạn Intelligent. Tôi sẽ tóm lược luồng bảo dưỡng, nêu validation chi tiết cho từng API, chỉ ra trạng thái/role, và đề xuất cải tiến ngắn gọn.

### Hình dung luồng (state + actor)
- Trạng thái chính của `Booking`: Pending → Confirmed → In_Process (aka Progressing) → Completed.
- Diễn viên:
  - Staff: xác nhận booking.
  - Technician: thực thi bảo dưỡng, cập nhật checklist/results, đề xuất parts.
  - Customer: duyệt thay thế parts, nhận kết quả.
  - System: áp mã khuyến mãi.
- Trình tự điển hình:
  1) Staff PATCH `/api/Booking/{id}/status` → Confirmed
  2) Technician PATCH `/api/Booking/{id}/status` → In_Process
  3) Technician GET `/api/maintenance-checklist/{bookingId}`
  4) Technician PUT `/api/maintenance-checklist/{bookingId}/results/{resultId}` → gửi cho khách
  5) Customer POST `/api/Booking/{bookingId}/parts/{workOrderPartId}/customer-approve`
  6) (Tùy chọn) POST `/api/Promotion/bookings/{bookingId}/apply`
  7) PATCH `/api/Booking/{id}/status` → Completed

### Đánh giá nhanh các API
- Thiết kế trạng thái tập trung 1 endpoint `/api/Booking/{id}/status` là gọn, nhưng cần ràng buộc role + guard theo state machine.
- Checklist/results và customer-approve tách rời hợp lý; cần liên kết rõ ràng với cùng `bookingId` và versioning để tránh ghi đè.
- Promotion apply nên kiểm tra đủ điều kiện theo thời điểm (trước Completed) và tính idempotent.

### Validation chi tiết theo API

1) PATCH `/api/Booking/{id}/status` — Staff chuyển sang Confirmed
- Role/Quyền:
  - Staff hoặc Role có quyền xác nhận booking.
- Preconditions:
  - `Booking` tồn tại, thuộc quyền truy cập (multi-tenant/branch).
  - Trạng thái hiện tại ∈ {Pending} (không cho Confirmed nếu đã Cancelled/Confirmed/Progressing/Completed).
  - Không có xung đột lịch (nếu xác nhận gắn timeslot/kỹ thuật viên).
- Request:
  - Body gợi ý:
    ```
    { "status": "CONFIRMED", "note": "optional", "version": <int> }
    ```
- Validation:
  - `status` ∈ whitelist.
  - Kiểm tra optimistic lock `version`.
  - Kiểm tra giờ hẹn hợp lệ, tài nguyên sẵn có.
- Side effects:
  - Ghi lịch sử, gửi thông báo cho technician/customer nếu cần.
- Lỗi:
  - 400 (invalid transition), 403 (forbidden), 404 (not found), 409 (version/conflict).

2) PATCH `/api/Booking/{id}/status` — Technician chuyển sang Progressing
- Role:
  - Technician được assign vào booking.
- Preconditions:
  - Trạng thái hiện tại ∈ {Confirmed}.
  - Technician phải active/available.
- Request/Validation:
  - Giống (1) với `status = "PROGRESSING"`.
- Lỗi:
  - 400/403/404/409 như trên.

2a) PATCH `/api/Booking/{id}/status` — Technician đổi sang In_Process (tiến hành bảo dưỡng)
- Role:
  - Technician được assign vào booking.
- Preconditions:
  - Trạng thái hiện tại ∈ {Confirmed}.
  - Technician phải active/available.
- Request:
  - Body gợi ý:
    ```
    { "status": "IN_PROCESS", "note": "optional", "version": <int> }
    ```
- Validation:
  - `status` phải đúng giá trị `IN_PROCESS` (được ánh xạ nội bộ sang Progressing nếu hệ thống dùng tên Progressing).
  - Kiểm tra optimistic lock `version`.
  - Bảo đảm quyền sở hữu/phân quyền chi nhánh và technician đã được gán.
- Side effects:
  - Ghi lịch sử, phát sự kiện bắt đầu bảo dưỡng, thông báo cho khách (nếu policy yêu cầu).
- Lỗi:
  - 400 (invalid transition), 403 (forbidden), 404 (not found), 409 (version/conflict).

3) GET `/api/maintenance-checklist/{bookingId}` — Technician lấy danh sách
- Role:
  - Technician được gán hoặc Staff giám sát.
- Preconditions:
  - `bookingId` hợp lệ; trạng thái ∈ {Confirmed, Progressing} (trước hoặc trong quá trình).
- Validation:
  - `bookingId` UUID/int hợp lệ.
  - Phân quyền theo cơ sở/đơn vị.
- Response:
  - Danh sách mục kiểm tra kèm `resultId` (nếu đã có), `required`, `spec`, `lastUpdatedAt`, `version` mỗi item.
- Lỗi:
  - 403/404.

4) PUT `/api/maintenance-checklist/{bookingId}/results/{resultId}` — Technician cập nhật tình trạng, gửi khách
- Role:
  - Technician được gán.
- Preconditions:
  - Trạng thái booking ∈ {Progressing}.
  - `resultId` thuộc checklist của `bookingId`.
- Request:
  ```
  {
    "status": "OK|ISSUE|NA",
    "note": "string?",
    "photos": ["url"...]?,
    "recommendations": ["replace_part_x"...]?,
    "version": <int>,
    "notifyCustomer": true
  }
  ```
- Validation:
  - `status` hợp lệ; chiều dài `note`; số lượng ảnh; loại file ảnh; `version` cho optimistic lock.
  - Nếu `notifyCustomer=true`, đảm bảo thông báo/đính kèm tạo bản xem cho khách.
- Side effects:
  - Lưu lịch sử; phát sự kiện “ChecklistItemUpdated”; nếu có đề xuất parts, tạo đề nghị duyệt.
- Lỗi:
  - 400 (payload), 403, 404, 409 (version).

5) POST `/api/Booking/{bookingId}/parts/{workOrderPartId}/customer-approve` — Customer cho phép thay thế
- Role:
  - Customer chủ booking (xác thực qua session/token + quyền sở hữu).
- Preconditions:
  - `workOrderPartId` thuộc booking, trạng thái phê duyệt ∈ {PENDING}.
  - Booking ∈ {Progressing} (đang thực hiện) hoặc theo quy trình cho phép “pending approval”.
- Request:
  ```
  { "approve": true, "note": "optional", "version": <int> }
  ```
- Validation:
  - `approve` boolean; nếu `false` xử lý từ chối.
  - Giá/khuyến mãi áp lên part không vượt quyền; kiểm tra tồn kho/sẵn có.
  - Idempotency key (header) để tránh double-click.
- Side effects:
  - Cập nhật chi phí dự kiến của booking, khóa báo giá mục part sau khi duyệt.
  - Gửi thông báo lại cho technician/staff.
- Lỗi:
  - 400/403/404/409/410 (nếu đã hết hạn phê duyệt).

6) POST `/api/Promotion/bookings/{bookingId}/apply` — Áp mã nếu có
- Role:
  - Customer/Staff theo chính sách (thường Customer).
- Preconditions:
  - Booking chưa Completed/Cancelled.
  - Mã khuyến mãi hợp lệ (thời gian, số lần, điều kiện dịch vụ, khách hàng mới/cũ, vùng, stackable hay không).
- Request:
  ```
  { "code": "STRING", "version": <int> }
  ```
- Validation:
  - Không vượt giá trị tối đa; không âm tổng; chống stacking sai quy định; điều kiện tối thiểu đơn hàng.
  - Idempotent: cùng code nhiều lần → 200 với trạng thái đã áp.
- Side effects:
  - Ghi lịch sử; giữ lại snapshot giá trước/sau; lock code theo user nếu cần.
- Lỗi:
  - 400 (invalid/ineligible), 403, 404, 409.

7) PATCH `/api/Booking/{id}/status` — Chuyển sang Completed
- Role:
  - Technician hoặc Staff theo policy (thường Supervisor/Staff).
- Preconditions:
  - Trạng thái hiện tại ∈ {Progressing}.
  - Tất cả checklist required đã có kết quả; không còn parts chờ duyệt; thanh toán/billing sẵn sàng (nếu bắt buộc trước completion).
- Request/Validation:
  - `status = "COMPLETED"`, `version` bắt buộc; bảo đảm không thiếu việc.
- Side effects:
  - Đóng công việc, tạo hóa đơn/biên bản bàn giao, gửi báo cáo tổng.
- Lỗi:
  - 400/403/404/409.

### Cross-cutting validations/best practices
- Trạng thái theo state machine (whitelist transition): Pending→Confirmed→In_Process(=Progressing)→Completed; cấm lùi trạng thái.
- Optimistic locking: mọi update có `version`; trả 409 khi xung đột.
- Idempotency: cho các hành động “nhấp lặp” (approve, apply promotion) qua header `Idempotency-Key`.
- Audit trail: lưu `who/when/what/old→new` cho các thay đổi.
- Authorization: RBAC rõ ràng cho Staff/Technician/Customer; kiểm tra ownership + scoping chi nhánh.
- Validation định danh: `{id}`, `{bookingId}`, `{workOrderPartId}`, `{resultId}` thuộc cùng booking và còn hiệu lực.
- Rate limit nhẹ cho endpoint nhạy cảm (approve/apply).
- Notification: gửi sự kiện ra message bus để thông báo khách/nhân viên.
- Compliance: lọc nội dung `note` (PII), kiểm tra MIME ảnh.

### Hai hướng tiếp cận (orthogonal) cho quản lý trạng thái
- Phương án A: Single endpoint status (như hiện tại)
  - Ưu: đơn giản, ít API.
  - Nhược: cần logic ràng buộc role + state ở backend rất chặt; khó tracking theo hành động cụ thể.
- Phương án B: Endpoint theo hành động cụ thể
  - Ví dụ: `/confirm`, `/start-progress`, `/complete`.
  - Ưu: quyền/validation tường minh từng hành động; logs dễ đọc.
  - Nhược: tăng số API, cần tiêu chuẩn đặt tên.
- Khuyến nghị: Giữ A nhưng bắt buộc trường `statusTransition` + kiểm tra state machine tập trung; hoặc chuyển dần các bước quan trọng (complete) sang B để siết validation tốt hơn.

### Tình huống lỗi và phản hồi chuẩn
- 400: dữ liệu không hợp lệ/transition sai.
- 401/403: chưa đăng nhập/không đủ quyền.
- 404: không tìm thấy hoặc không thuộc quyền sở hữu.
- 409: xung đột version/idempotency.
- 410: yêu cầu đã hết hạn (approval timeout).
- 422: điều kiện khuyến mãi không đạt.

— 
- Tôi sẽ bổ sung nếu bạn cung cấp thêm rule nghiệp vụ: timeout approval, stacking khuyến mãi, yêu cầu ảnh minh chứng, ràng buộc thanh toán trước/ sau Completed.

```plaintext
    { "status": "CONFIRMED", "note": "optional", "version": <int> }
    ```
- Validation:
  - `status` ∈ whitelist.
  - Kiểm tra optimistic lock `version`.
  - Kiểm tra giờ hẹn hợp lệ, tài nguyên sẵn có.
- Side effects:
  - Ghi lịch sử, gửi thông báo cho technician/customer nếu cần.
- Lỗi:
  - 400 (invalid transition), 403 (forbidden), 404 (not found), 409 (version/conflict).

2) PATCH `/api/Booking/{id}/status` — Technician chuyển sang Progressing
- Role:
  - Technician được assign vào booking.
- Preconditions:
  - Trạng thái hiện tại ∈ {Confirmed}.
  - Technician phải active/available.
- Request/Validation:
  - Giống (1) với `status = "PROGRESSING"`.
- Lỗi:
  - 400/403/404/409 như trên.

3) GET `/api/maintenance-checklist/{bookingId}` — Technician lấy danh sách
- Role:
  - Technician được gán hoặc Staff giám sát.
- Preconditions:
  - `bookingId` hợp lệ; trạng thái ∈ {Confirmed, Progressing} (trước hoặc trong quá trình).
- Validation:
  - `bookingId` UUID/int hợp lệ.
  - Phân quyền theo cơ sở/đơn vị.
- Response:
  - Danh sách mục kiểm tra kèm `resultId` (nếu đã có), `required`, `spec`, `lastUpdatedAt`, `version` mỗi item.
- Lỗi:
  - 403/404.

4) PUT `/api/maintenance-checklist/{bookingId}/results/{resultId}` — Technician cập nhật tình trạng, gửi khách
- Role:
  - Technician được gán.
- Preconditions:
  - Trạng thái booking ∈ {Progressing}.
  - `resultId` thuộc checklist của `bookingId`.
- Request:
  ```
  {
    "status": "OK|ISSUE|NA",
    "note": "string?",
    "photos": ["url"...]?,
    "recommendations": ["replace_part_x"...]?,
    "version": <int>,
    "notifyCustomer": true
  }
  ```
- Validation:
  - `status` hợp lệ; chiều dài `note`; số lượng ảnh; loại file ảnh; `version` cho optimistic lock.
  - Nếu `notifyCustomer=true`, đảm bảo thông báo/đính kèm tạo bản xem cho khách.
- Side effects:
  - Lưu lịch sử; phát sự kiện “ChecklistItemUpdated”; nếu có đề xuất parts, tạo đề nghị duyệt.
- Lỗi:
  - 400 (payload), 403, 404, 409 (version).

5) POST `/api/Booking/{bookingId}/parts/{workOrderPartId}/customer-approve` — Customer cho phép thay thế
- Role:
  - Customer chủ booking (xác thực qua session/token + quyền sở hữu).
- Preconditions:
  - `workOrderPartId` thuộc booking, trạng thái phê duyệt ∈ {PENDING}.
  - Booking ∈ {Progressing} (đang thực hiện) hoặc theo quy trình cho phép “pending approval”.
- Request:
  ```
  { "approve": true, "note": "optional", "version": <int> }
  ```
- Validation:
  - `approve` boolean; nếu `false` xử lý từ chối.
  - Giá/khuyến mãi áp lên part không vượt quyền; kiểm tra tồn kho/sẵn có.
  - Idempotency key (header) để tránh double-click.
- Side effects:
  - Cập nhật chi phí dự kiến của booking, khóa báo giá mục part sau khi duyệt.
  - Gửi thông báo lại cho technician/staff.
- Lỗi:
  - 400/403/404/409/410 (nếu đã hết hạn phê duyệt).

6) POST `/api/Promotion/bookings/{bookingId}/apply` — Áp mã nếu có
- Role:
  - Customer/Staff theo chính sách (thường Customer).
- Preconditions:
  - Booking chưa Completed/Cancelled.
  - Mã khuyến mãi hợp lệ (thời gian, số lần, điều kiện dịch vụ, khách hàng mới/cũ, vùng, stackable hay không).
- Request:
  ```
  { "code": "STRING", "version": <int> }
  ```
- Validation:
  - Không vượt giá trị tối đa; không âm tổng; chống stacking sai quy định; điều kiện tối thiểu đơn hàng.
  - Idempotent: cùng code nhiều lần → 200 với trạng thái đã áp.
- Side effects:
  - Ghi lịch sử; giữ lại snapshot giá trước/sau; lock code theo user nếu cần.
- Lỗi:
  - 400 (invalid/ineligible), 403, 404, 409.

7) PATCH `/api/Booking/{id}/status` — Chuyển sang Completed
- Role:
  - Technician hoặc Staff theo policy (thường Supervisor/Staff).
- Preconditions:
  - Trạng thái hiện tại ∈ {Progressing}.
  - Tất cả checklist required đã có kết quả; không còn parts chờ duyệt; thanh toán/billing sẵn sàng (nếu bắt buộc trước completion).
- Request/Validation:
  - `status = "COMPLETED"`, `version` bắt buộc; bảo đảm không thiếu việc.
- Side effects:
  - Đóng công việc, tạo hóa đơn/biên bản bàn giao, gửi báo cáo tổng.
- Lỗi:
  - 400/403/404/409.

### Cross-cutting validations/best practices
- Trạng thái theo state machine (whitelist transition): Pending→Confirmed→Progressing→Completed; cấm lùi trạng thái.
- Optimistic locking: mọi update có `version`; trả 409 khi xung đột.
- Idempotency: cho các hành động “nhấp lặp” (approve, apply promotion) qua header `Idempotency-Key`.
- Audit trail: lưu `who/when/what/old→new` cho các thay đổi.
- Authorization: RBAC rõ ràng cho Staff/Technician/Customer; kiểm tra ownership + scoping chi nhánh.
- Validation định danh: `{id}`, `{bookingId}`, `{workOrderPartId}`, `{resultId}` thuộc cùng booking và còn hiệu lực.
- Rate limit nhẹ cho endpoint nhạy cảm (approve/apply).
- Notification: gửi sự kiện ra message bus để thông báo khách/nhân viên.
- Compliance: lọc nội dung `note` (PII), kiểm tra MIME ảnh.

### Hai hướng tiếp cận (orthogonal) cho quản lý trạng thái
- Phương án A: Single endpoint status (như hiện tại)
  - Ưu: đơn giản, ít API.
  - Nhược: cần logic ràng buộc role + state ở backend rất chặt; khó tracking theo hành động cụ thể.
- Phương án B: Endpoint theo hành động cụ thể
  - Ví dụ: `/confirm`, `/start-progress`, `/complete`.
  - Ưu: quyền/validation tường minh từng hành động; logs dễ đọc.
  - Nhược: tăng số API, cần tiêu chuẩn đặt tên.
- Khuyến nghị: Giữ A nhưng bắt buộc trường `statusTransition` + kiểm tra state machine tập trung; hoặc chuyển dần các bước quan trọng (complete) sang B để siết validation tốt hơn.

### Tình huống lỗi và phản hồi chuẩn
- 400: dữ liệu không hợp lệ/transition sai.
- 401/403: chưa đăng nhập/không đủ quyền.
- 404: không tìm thấy hoặc không thuộc quyền sở hữu.
- 409: xung đột version/idempotency.
- 410: yêu cầu đã hết hạn (approval timeout).
- 422: điều kiện khuyến mãi không đạt.

— 
- Tôi sẽ bổ sung nếu bạn cung cấp thêm rule nghiệp vụ: timeout approval, stacking khuyến mãi, yêu cầu ảnh minh chứng, ràng buộc thanh toán trước/ sau Completed.
```

```plaintext
  {
    "status": "OK|ISSUE|NA",
    "note": "string?",
    "photos": ["url"...]?,
    "recommendations": ["replace_part_x"...]?,
    "version": <int>,
    "notifyCustomer": true
  }
```

```plaintext
  { "approve": true, "note": "optional", "version": <int> }
```

```plaintext
  { "code": "STRING", "version": <int> }
```

---

**User**

Đây là api payment

---

**Cursor**

[Mode：Intelligent]
Phân tích sơ bộ cho thấy yêu cầu phù hợp với giai đoạn Intelligent. Tôi sẽ mô tả luồng thanh toán phù hợp với quy trình bảo dưỡng, giải thích vai trò từng API, điều kiện/validation, và lỗi/bao quát bảo mật.

### Hình dung luồng thanh toán trong quy trình bảo dưỡng
- Trạng thái Booking: Pending → Confirmed → Progressing → AwaitingPayment → Completed.
- Điểm chèn Payment:
  - Có thể yêu cầu đặt cọc trước khi chuyển sang Progressing (tùy chính sách).
  - Hoặc thu toàn bộ sau khi technician hoàn tất checklist và khách duyệt parts → chuyển sang AwaitingPayment → thanh toán thành công → Completed.

### Ý nghĩa từng API và điều kiện/validation

- POST `/api/Payment/booking/{bookingId}/link`
  - **Mục đích**: Tạo “payment session/link” tổng quát (cổng mặc định theo config hoặc theo tham số).
  - **Preconditions**: `bookingId` tồn tại, số tiền phải thu > 0, trạng thái cho phép thu (Progressing/AwaitingPayment), chưa “paid in full”.
  - **Validation**:
    - Kiểm tra quyền sở hữu (Customer) hoặc Staff.
    - Số tiền tính từ: công + parts − khuyến mãi (không âm), kiểm tra “version” chống race khi thay đổi parts/promo.
    - Tạo `idempotency-key` để không sinh nhiều phiên.
  - **Kết quả**: Trả URL thanh toán/qr hoặc meta để client điều hướng.

- POST `/api/Payment/booking/{bookingId}/sepay-qr`
  - **Mục đích**: Phát hành QR cho Sepay.
  - **Preconditions/Validation**: như trên + giới hạn kênh Sepay bật.
  - **Kết quả**: Payload QR, thời hạn, amount, orderCode.

- POST `/api/Payment/booking/{bookingId}/vnpay-link`
  - **Mục đích**: Phát hành link VNPAY.
  - **Preconditions/Validation**: như trên + cấu hình VNPAY hợp lệ (merchantCode, secret, returnUrl).
  - **Kết quả**: `vnp_Url` đã ký.

- GET `/api/payment/result`
  - **Mục đích**: Client truy vấn trạng thái payment theo `paymentId`/`orderCode`.
  - **Validation**: bắt buộc token chủ sở hữu; chống lộ thông tin; rate limit.
  - **Trạng thái**: PENDING/SUCCEEDED/FAILED/CANCELED, số tiền, phương thức, reference.

- GET `/api/payment/cancel`
  - **Mục đích**: Client hủy luồng (chưa thanh toán).
  - **Validation**: chỉ hủy session PENDING; audit lý do; không xóa transaction log.

- POST `/api/Payment/booking/{bookingId}/payments/offline`
  - **Mục đích**: Ghi nhận thanh toán tiền mặt/pos tại quầy.
  - **Preconditions**: Role Staff/Thu ngân; số tiền hợp lệ; ca làm việc/shift mở.
  - **Validation**:
    - Đính kèm chứng từ (receiptNo), kỳ sổ quỹ, kiểm tra dư thừa (không vượt amount còn thiếu).
    - Idempotency theo `receiptNo` + `bookingId`.
  - **Side-effects**: Cập nhật công nợ, in hóa đơn.

- GET `/api/payment/vnpay-return`
  - **Mục đích**: Return URL cho trình duyệt (front-channel).
  - **Validation**:
    - Xác thực chữ ký VNPAY (`vnp_SecureHash`), khớp amount/orderInfo.
    - Không tự quyết toán: chỉ hiển thị kết quả/redirect, việc ghi nhận thành công nên dựa vào IPN.
  - **Kết quả**: Redirect UI + hiển thị pending/success/fail.

- POST `/api/payment/vnpay-ipn`
  - **Mục đích**: Server-side xác nhận thanh toán từ VNPAY (nguồn chân lý).
  - **Validation quan trọng**:
    - Verify chữ ký, `vnp_TmnCode`, `vnp_Amount` (đơn vị x100), `vnp_ResponseCode/TransactionStatus`.
    - Kiểm tra `orderId` chưa xử lý (idempotent), số tiền khớp số phải thu (hoặc <= phần còn thiếu cho partial payment nếu hỗ trợ).
    - Trạng thái giao dịch của chính mình (PENDING → SUCCEEDED/FAILED).
  - **Side-effects**:
    - Cộng tiền đã thu cho booking; nếu đủ → chuyển trạng thái Booking từ AwaitingPayment → Completed; phát hóa đơn.
    - Lưu audit, phát sự kiện “PaymentSucceeded”.

- POST `/api/payment/sepay-webhook`
  - **Mục đích**: Webhook Sepay.
  - **Validation**:
    - Verify chữ ký HMAC/secret, địa chỉ IP whitelist (nếu có), khớp amount/orderCode, idempotent.
  - **Side-effects**: tương tự IPN VNPAY.

### Luồng tổng hợp theo kênh
- Online (VNPAY/Sepay):
  - Create link/QR → khách thanh toán → Webhook(IPN) nhận kết quả → cập nhật transaction → nếu đủ tiền thì set Booking=Completed → client có thể check bằng GET `/api/payment/result`.
- Offline:
  - Staff POST offline payment → cập nhật số đã thu → nếu đủ thì Completed.

### Ràng buộc/validation nghiệp vụ then chốt
- **Số tiền**: amount phải = tổng công + parts đã duyệt − Promotion; nếu cho phép trả nhiều lần, ghi nợ còn lại; nếu không, yêu cầu “paid in full” trước Completed.
- **Idempotency**: mọi ghi nhận thanh toán (IPN/webhook/offline) phải dùng khóa idempotent theo `providerTxId` hoặc `receiptNo`.
- **Optimistic Lock**: khi tạo link thanh toán cần gửi `bookingVersion`; nếu sau đó parts/promo thay đổi → link cũ hết hạn.
- **State machine**:
  - Confirmed/Progressing → có thể tạo payment.
  - Khi đã “paid in full” → cho phép Completed; chặn thêm parts/promo sau khi chốt (hoặc bắt buộc tạo phụ lục tăng tiền).
- **Security**:
  - Verified signatures bắt buộc; không tin `return` (chỉ UI), chỉ quyết toán theo `ipn/webhook`.
  - Rate limit `result`, `cancel`, webhook xác thực nguồn.
  - Không lộ số tiền/chi tiết booking cho user không sở hữu.
- **Error mapping**:
  - 400: amount/điều kiện không hợp lệ; 401/403: không đúng chủ booking/role; 404: booking hoặc session không tồn tại; 409: version hoặc transaction đã xử lý; 422: khuyến mãi/điều kiện không đạt.
- **Thông báo**:
  - Khi thanh toán thành công: gửi biên lai, cập nhật lịch sử booking, sự kiện cho kế toán.

### Gợi ý cải tiến nhỏ
- Thêm trạng thái `AwaitingPayment` rõ ràng trước `Completed`.
- Chuẩn hóa “payment-intent” resource: `POST /api/payment-intents` → trả `clientSecret/link`, các kênh thành thuộc tính provider; hạn chế rải rác quá nhiều endpoint tạo link.
- Bắt buộc `Idempotency-Key` header cho: create link/qr, offline payment.
- Lưu `pricingSnapshotId` trong transaction để truy vết số tiền đã tính ở thời điểm tạo link.

### Điểm cần xác nhận thêm (nếu có)
- Có hỗ trợ partial payment hay bắt buộc full?
- Promotion áp trước hay sau parts approve, và có lock giá sau khi tạo payment intent?
- Chính sách “hoàn tiền/void” và endpoint reverse/refund? (chưa thấy trong danh sách)

