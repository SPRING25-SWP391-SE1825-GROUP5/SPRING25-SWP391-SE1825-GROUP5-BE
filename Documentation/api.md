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
