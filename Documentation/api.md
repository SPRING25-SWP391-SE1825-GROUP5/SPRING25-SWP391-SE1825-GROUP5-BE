# API Documentation

## Authentication Controller (`api/auth`)

### Endpoints

1. **POST** `/api/auth/register`
   - Đăng ký tài khoản mới
   - Body: AccountRequest

2. **POST** `/api/auth/verify-otp`
   - Xác minh OTP
   - Query: email, otp

3. **POST** `/api/auth/login`
   - Đăng nhập
   - Body: LoginRequest

4. **POST** `/api/auth/set-password`
   - Đặt mật khẩu với token
   - Query: token
   - Body: ChangePasswordRequest

5. **POST** `/api/auth/verify-email`
   - Xác thực email
   - Body: VerifyEmailRequest

6. **POST** `/api/auth/resend-verification`
   - Gửi lại mã xác thực
   - Body: ResendVerificationRequest

7. **POST** `/api/auth/reset-password/request`
   - Yêu cầu đặt lại mật khẩu
   - Body: ResetPasswordRequest

8. **POST** `/api/auth/reset-password/confirm`
   - Xác nhận đặt lại mật khẩu
   - Body: ConfirmResetPasswordRequest

9. **POST** `/api/auth/logout`
   - Đăng xuất
   - Authorization: Required

10. **GET** `/api/auth/profile`
    - Lấy thông tin profile
    - Authorization: Required

11. **PUT** `/api/auth/profile`
    - Cập nhật thông tin profile
    - Authorization: Required
    - Body: UpdateProfileRequest

12. **POST** `/api/auth/change-password`
    - Đổi mật khẩu
    - Authorization: Required
    - Body: ChangePasswordRequest

13. **POST** `/api/auth/upload-avatar`
    - Upload avatar
    - Authorization: Required (Policy: AuthenticatedUser)
    - Form: file

14. **POST** `/api/auth/login-google`
    - Đăng nhập bằng Google
    - Body: GoogleLoginRequest

## User Controller (`api/User`)

### Endpoints

1. **GET** `/api/User`
   - Lấy danh sách tất cả người dùng với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, searchTerm, role
   - Authorization: Required

2. **GET** `/api/User/{id}`
   - Lấy thông tin người dùng theo ID
   - Authorization: Required

3. **POST** `/api/User`
   - Tạo người dùng mới (Staff/Admin) - Luôn tạo CUSTOMER, cần verify email
   - Body: CreateUserRequest (role phải là CUSTOMER, emailVerified phải là false)
   - Authorization: Required (Roles: ADMIN, STAFF, MANAGER)
   - Returns: Thông tin người dùng đã tạo + gửi OTP verification

4. **PATCH** `/api/User/{id}/activate`
   - Kích hoạt người dùng (Admin only)
   - Authorization: Required (Roles: ADMIN)

5. **PATCH** `/api/User/{id}/deactivate`
   - Vô hiệu hóa người dùng (Admin only)
   - Authorization: Required (Roles: ADMIN)

6. **PATCH** `/api/User/assign-role`
   - Gán vai trò cho người dùng (Admin only)
   - Body: AssignUserRoleRequest
   - Authorization: Required (Roles: ADMIN)
   - Validation: Không cho phép Admin thay đổi vai trò của chính mình

## Vehicle Controller (`api/Vehicle`)

### Endpoints

1. **GET** `/api/Vehicle`
   - Lấy danh sách tất cả xe
   - Query: pageNumber, pageSize, searchTerm, customerId

2. **GET** `/api/Vehicle/{id}`
   - Lấy thông tin xe theo ID

3. **POST** `/api/Vehicle`
   - Tạo xe mới
   - Body: CreateVehicleRequest
   - Validation: purchaseDate phải từ năm 1900 đến năm 2100

4. **PUT** `/api/Vehicle/{id}`
   - Cập nhật thông tin xe
   - Body: UpdateVehicleRequest
   - Validation: purchaseDate phải từ năm 1900 đến năm 2100

5. **DELETE** `/api/Vehicle/{id}`
   - Xóa xe

6. **GET** `/api/Vehicle/{id}/customer`
   - Lấy thông tin khách hàng theo ID xe
   - Authorization: Required

7. **GET** `/api/Vehicle/search/{vinOrLicensePlate}`
   - Tìm xe theo VIN hoặc biển số xe
   - Authorization: Required

## Center Controller (`api/Center`)

### Endpoints

1. **GET** `/api/Center`
   - Lấy danh sách tất cả trung tâm với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, searchTerm, city
   - Authorization: Required (Policy: StaffOrAdmin)

2. **GET** `/api/Center/active`
   - Lấy danh sách trung tâm đang hoạt động với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, searchTerm, city
   - Authorization: Required (Policy: StaffOrAdmin)

3. **GET** `/api/Center/{id}`
   - Lấy thông tin trung tâm theo ID
   - Authorization: Required (Policy: StaffOrAdmin)

4. **POST** `/api/Center`
   - Tạo trung tâm mới
   - Body: CreateCenterRequest
   - Authorization: Required (Policy: StaffOrAdmin)

5. **PUT** `/api/Center/{id}`
   - Cập nhật thông tin trung tâm
   - Body: UpdateCenterRequest
   - Authorization: Required (Policy: StaffOrAdmin)

6. **PATCH** `/api/Center/{id}/toggle-active`
   - Kích hoạt/Vô hiệu hóa trung tâm (Admin only)
   - Authorization: Required (Roles: ADMIN)
   - Chức năng: Toggle trạng thái IsActive của trung tâm

## Service Controller (`api/Service`)

### Endpoints

1. **GET** `/api/Service`
   - Lấy danh sách tất cả dịch vụ với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, searchTerm, categoryId
   - Authorization: Required (Policy: AuthenticatedUser)

2. **GET** `/api/Service/active`
   - Lấy danh sách dịch vụ đang hoạt động với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, searchTerm, categoryId
   - Authorization: Required (Policy: AuthenticatedUser)

3. **GET** `/api/Service/{id}`
   - Lấy thông tin dịch vụ theo ID
   - Authorization: Required (Policy: AuthenticatedUser)

4. **POST** `/api/Service`
   - Tạo dịch vụ mới
   - Body: CreateServiceRequest
   - Authorization: Required (Policy: StaffOrAdmin)

5. **PUT** `/api/Service/{id}`
   - Cập nhật thông tin dịch vụ
   - Body: UpdateServiceRequest
   - Authorization: Required (Policy: StaffOrAdmin)

6. **PATCH** `/api/Service/{id}/toggle-active`
   - Kích hoạt/Vô hiệu hóa dịch vụ
   - Authorization: Required (Policy: StaffOrAdmin)

### Service Parts Management

7. **GET** `/api/Service/{serviceId}/parts`
   - Lấy danh sách phụ tùng của dịch vụ
   - Authorization: Required (Policy: AuthenticatedUser)

8. **PUT** `/api/Service/{serviceId}/parts`
   - Thay thế toàn bộ phụ tùng của dịch vụ
   - Body: ServicePartsReplaceRequest
   - Authorization: Required (Policy: StaffOrAdmin)

9. **POST** `/api/Service/{serviceId}/parts`
   - Thêm phụ tùng vào dịch vụ
   - Body: ServicePartAddRequest
   - Authorization: Required (Policy: StaffOrAdmin)

10. **DELETE** `/api/Service/{serviceId}/parts/{partId}`
    - Xóa phụ tùng khỏi dịch vụ
    - Authorization: Required (Policy: StaffOrAdmin)

## Inventory Controller (`api/Inventory`)

### Endpoints

1. **GET** `/api/Inventory`
   - Lấy danh sách tồn kho với phân trang và tìm kiếm
   - Query: pageNumber, pageSize, centerId, partId, searchTerm
   - Authorization: Required (Policy: AuthenticatedUser)

2. **GET** `/api/Inventory/center/{centerId}`
   - Lấy danh sách tồn kho theo trung tâm
   - Query: pageNumber, pageSize, searchTerm
   - Authorization: Required (Policy: AuthenticatedUser)

3. **GET** `/api/Inventory/availability`
   - Lấy tồn kho theo center và danh sách partIds
   - Query: centerId, partIds (comma-separated)
   - Authorization: Required (Policy: AuthenticatedUser)

4. **GET** `/api/Inventory/{id}`
   - Lấy thông tin tồn kho theo ID
   - Authorization: Required (Policy: AuthenticatedUser)

5. **POST** `/api/Inventory`
   - Tạo tồn kho mới
   - Body: CreateInventoryRequest
   - Authorization: Required (Policy: AuthenticatedUser)
   - Validation: Mỗi cặp centerId/partId chỉ được tồn tại 1 lần duy nhất

6. **PUT** `/api/Inventory/{id}`
   - Cập nhật tồn kho (chỉ ADMIN)
   - Body: UpdateInventoryRequest
   - Authorization: Required (Policy: AdminOnly)

## Reminder Controller (`api/reminders`)

### Endpoints

1. **POST** `/api/reminders/vehicles/{vehicleId}/set`
   - Thiết lập nhắc nhở bảo dưỡng cho xe cụ thể
   - Body: SetVehicleRemindersRequest
   - Authorization: Required

3. **GET** `/api/reminders/vehicles/{vehicleId}/alerts`
   - Lấy danh sách cảnh báo nhắc nhở cho xe
   - Authorization: Required

4. **GET** `/api/reminders`
   - Lấy danh sách tất cả reminders với bộ lọc
   - Query: customerId, vehicleId, status, from, to
   - Authorization: Required

5. **POST** `/api/reminders`
   - Tạo reminder mới
   - Body: CreateReminderRequest
   - Authorization: Required

6. **GET** `/api/reminders/{id}`
   - Lấy thông tin reminder theo ID
   - Authorization: Required

7. **PUT** `/api/reminders/{id}`
   - Cập nhật thông tin reminder
   - Body: UpdateReminderRequest
   - Authorization: Required

8. **PATCH** `/api/reminders/{id}/complete`
   - Đánh dấu reminder đã hoàn thành
   - Authorization: Required

9. **PATCH** `/api/reminders/{id}/snooze`
   - Hoãn reminder
   - Body: SnoozeRequest
   - Authorization: Required

10. **GET** `/api/reminders/upcoming`
    - Lấy danh sách reminders sắp đến hạn
    - Query: customerId
    - Authorization: Required

11. **POST** `/api/reminders/appointments/dispatch`
    - Gửi nhắc nhở cuộc hẹn
    - Body: AppointmentDispatchRequest
    - Authorization: Required (Roles: ADMIN, STAFF)

12. **POST** `/api/reminders/{id}/send-test`
    - Gửi email test cho reminder
    - Authorization: Required

13. **POST** `/api/reminders/dispatch`
    - Gửi nhắc nhở theo danh sách hoặc tự động
    - Body: DispatchRequest
    - Authorization: Required (Roles: ADMIN, STAFF)

## Maintenance Reminder Controller (`api/maintenance-reminders`)

### Endpoints

1. **POST** `/api/maintenance-reminders/send-vehicle-maintenance-alerts`
   - Gửi thông báo nhắc nhở bảo dưỡng xe cho khách hàng
   - Body: SendVehicleMaintenanceAlertsRequest
   - Authorization: Required (Roles: ADMIN, STAFF)
   - Features: Gửi email với template HTML đẹp mắt, hỗ trợ SMS (placeholder)

2. **GET** `/api/maintenance-reminders/upcoming`
   - Lấy danh sách reminders sắp đến hạn
   - Query: customerId, vehicleId, upcomingDays
   - Authorization: Required

3. **POST** `/api/maintenance-reminders/{reminderId}/send-test-email`
   - Gửi email test cho một reminder cụ thể
   - Authorization: Required (Roles: ADMIN, STAFF)