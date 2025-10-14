# API Documentation cho Frontend

## Base URL
```
https://your-api-domain.com/api
```

## Authentication
Tất cả API (trừ đăng ký, đăng nhập, quên mật khẩu) cần JWT token trong header:
```
Authorization: Bearer <your-jwt-token>
```

---

## 🔐 AUTHENTICATION APIs (Ưu tiên cao)

### 1. Đăng ký tài khoản
**POST** `/auth/register`

**Request Body:**
```json
{
  "fullName": "Nguyễn Văn A",
  "email": "user@gmail.com",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "phoneNumber": "0123456789",
  "dateOfBirth": "1990-01-01",
  "gender": "MALE",
  "address": "123 Đường ABC, TP.HCM"
}
```

**Validation Rules:**
- `fullName`: Bắt buộc, 2-100 ký tự
- `email`: Bắt buộc, phải có đuôi @gmail.com, tối đa 100 ký tự
- `password`: Bắt buộc, ít nhất 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt
- `confirmPassword`: Phải khớp với password
- `phoneNumber`: Bắt buộc, bắt đầu bằng 0 và có đúng 10 số
- `dateOfBirth`: Bắt buộc, phải đủ 16 tuổi
- `gender`: Bắt buộc, chỉ "MALE" hoặc "FEMALE"
- `address`: Tùy chọn, tối đa 255 ký tự

**Response Success (200):**
```json
{
  "success": true,
  "message": "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.",
  "data": {
    "email": "user@gmail.com",
    "fullName": "Nguyễn Văn A",
    "registeredAt": "2024-01-01T00:00:00Z"
  }
}
```

**Response Error (400):**
```json
{
  "success": false,
  "message": "Dữ liệu đầu vào không hợp lệ",
  "errors": ["Email này đã được sử dụng"]
}
```

### 2. Đăng nhập
**POST** `/auth/login`

**Request Body:**
```json
{
  "emailOrPhone": "user@gmail.com",
  "password": "Password123!"
}
```

**Validation Rules:**
- `emailOrPhone`: Bắt buộc (có thể là email hoặc số điện thoại)
- `password`: Bắt buộc, không được để trống

**Response Success (200):**
```json
{
  "success": true,
  "message": "Đăng nhập thành công",
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "refresh_token_here",
    "user": {
      "id": 1,
      "fullName": "Nguyễn Văn A",
      "email": "user@gmail.com",
      "role": "CUSTOMER",
      "emailVerified": true
    }
  }
}
```

### 3. Xác thực email
**POST** `/auth/verify-email`

**Request Body:**
```json
{
  "userId": 1,
  "otpCode": "123456"
}
```

**Validation Rules:**
- `userId`: Bắt buộc
- `otpCode`: Bắt buộc, đúng 6 chữ số

### 4. Gửi lại mã xác thực
**POST** `/auth/resend-verification`

**Request Body:**
```json
{
  "email": "user@gmail.com"
}
```

### 5. Yêu cầu đặt lại mật khẩu
**POST** `/auth/reset-password/request`

**Request Body:**
```json
{
  "email": "user@gmail.com"
}
```

**Validation Rules:**
- `email`: Bắt buộc, đúng định dạng, phải có đuôi @gmail.com

### 6. Xác nhận đặt lại mật khẩu
**POST** `/auth/reset-password/confirm`

**Request Body:**
```json
{
  "email": "user@gmail.com",
  "otpCode": "123456",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

**Validation Rules:**
- `email`: Bắt buộc, đúng định dạng @gmail.com
- `otpCode`: Bắt buộc, đúng 6 chữ số
- `newPassword`: Bắt buộc, ít nhất 8 ký tự với chữ hoa, chữ thường, số và ký tự đặc biệt
- `confirmPassword`: Phải khớp với newPassword

### 7. Đăng xuất
**POST** `/auth/logout`
**Authorization Required**

### 8. Lấy thông tin profile
**GET** `/auth/profile`
**Authorization Required**

### 9. Cập nhật profile
**PUT** `/auth/profile`
**Authorization Required**

**Request Body:**
```json
{
  "fullName": "Nguyễn Văn A Updated",
  "dateOfBirth": "1990-01-01",
  "gender": "MALE",
  "address": "456 Đường XYZ, TP.HCM"
}
```

### 10. Đổi mật khẩu
**POST** `/auth/change-password`
**Authorization Required**

**Request Body:**
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmNewPassword": "NewPassword123!"
}
```

### 11. Upload avatar
**POST** `/auth/upload-avatar`
**Authorization Required**
**Content-Type:** `multipart/form-data`

**Form Data:**
- `file`: File ảnh (JPG, PNG, GIF)

### 12. Đăng nhập Google
**POST** `/auth/login-google`

**Request Body:**
```json
{
  "token": "google_oauth_token_here"
}
```

---

## 📅 BOOKING APIs

### 1. Kiểm tra khả dụng
**GET** `/booking/availability?centerId=1&date=2024-01-01&serviceIds=1,2,3`
**Authorization Required**

**Query Parameters:**
- `centerId`: ID trung tâm (bắt buộc)
- `date`: Ngày đặt lịch YYYY-MM-DD (bắt buộc)
- `serviceIds`: Danh sách ID dịch vụ, cách nhau bởi dấu phẩy (tùy chọn)

### 2. Tạo booking mới
**POST** `/booking`
**Authorization Required**

### 3. Lấy danh sách booking
**GET** `/booking?pageNumber=1&pageSize=10`
**Authorization Required**

### 4. Lấy chi tiết booking
**GET** `/booking/{id}`
**Authorization Required**

### 5. Cập nhật trạng thái booking
**PUT** `/booking/{id}/status`
**Authorization Required**

### 6. Hủy booking
**DELETE** `/booking/{id}`
**Authorization Required**

---

## [object Object]ENTER APIs

### 1. Lấy danh sách trung tâm
**GET** `/center?pageNumber=1&pageSize=10&searchTerm=&city=`
**Authorization Required**

### 2. Lấy chi tiết trung tâm
**GET** `/center/{id}`
**Authorization Required**

### 3. Tạo trung tâm mới (Admin only)
**POST** `/center`
**Authorization Required (Admin)**

### 4. Cập nhật trung tâm (Admin only)
**PUT** `/center/{id}`
**Authorization Required (Admin)**

---

## 🔧 SERVICE APIs

### 1. Lấy danh sách dịch vụ
**GET** `/service?pageNumber=1&pageSize=10&searchTerm=&categoryId=`
**Authorization Required**

### 2. Lấy chi tiết dịch vụ
**GET** `/service/{id}`
**Authorization Required**

---

## 🚗 VEHICLE APIs

### 1. Lấy danh sách xe
**GET** `/vehicle?pageNumber=1&pageSize=10&customerId=&searchTerm=`
**Authorization Required**

### 2. Tạo xe mới
**POST** `/vehicle`
**Authorization Required**

### 3. Cập nhật thông tin xe
**PUT** `/vehicle/{id}`
**Authorization Required**

### 4. Xóa xe
**DELETE** `/vehicle/{id}`
**Authorization Required**

---

## 👥 USER APIs (Admin only)

### 1. Lấy danh sách người dùng
**GET** `/user?pageNumber=1&pageSize=10&searchTerm=&role=`
**Authorization Required (Admin)**

### 2. Tạo người dùng mới
**POST** `/user`
**Authorization Required (Admin)**

### 3. Cập nhật người dùng
**PUT** `/user/{id}`
**Authorization Required (Admin)**

### 4. Xóa người dùng
**DELETE** `/user/{id}`
**Authorization Required (Admin)**

---

## 📋 Common Response Format

### Success Response
```json
{
  "success": true,
  "message": "Thông báo thành công",
  "data": { /* dữ liệu trả về */ }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Thông báo lỗi",
  "errors": ["Chi tiết lỗi 1", "Chi tiết lỗi 2"]
}
```

## 🔒 Authorization Policies

- **AuthenticatedUser**: Tất cả user đã đăng nhập
- **AdminOnly**: Chỉ ADMIN
- **StaffOrAdmin**: STAFF hoặc ADMIN
- **CustomerOnly**: Chỉ CUSTOMER

## 📝 Notes cho Frontend

1. **Token Management**: Lưu JWT token và refresh token an toàn
2. **Error Handling**: Xử lý các mã lỗi HTTP (400, 401, 403, 500)
3. **Validation**: Frontend nên validate dữ liệu trước khi gửi API
4. **Loading States**: Hiển thị loading khi call API
5. **Retry Logic**: Implement retry cho các API quan trọng
