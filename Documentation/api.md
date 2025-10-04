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
