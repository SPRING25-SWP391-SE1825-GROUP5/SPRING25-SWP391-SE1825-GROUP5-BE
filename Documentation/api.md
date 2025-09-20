# EV Service Center API Documentation

## Tổng quan
Hệ thống EV Service Center cung cấp các API để quản lý dịch vụ sửa chữa và bảo dưỡng xe điện. Hệ thống hỗ trợ đăng ký, đăng nhập, quản lý khách hàng, phương tiện, đặt lịch dịch vụ và quản lý nhân viên.

## Base URL
- Development: `https://localhost:5001` hoặc `http://localhost:5000`
- Production: `https://your-domain.com`

## Authentication
Hệ thống sử dụng JWT Bearer Token để xác thực. Thêm header sau vào request:
```
Authorization: Bearer <your-jwt-token>
```

## Authorization Policies
- **AdminOnly**: Chỉ Admin mới có quyền truy cập
- **StaffOrAdmin**: Staff hoặc Admin có quyền truy cập
- **TechnicianOrAdmin**: Technician hoặc Admin có quyền truy cập
- **AuthenticatedUser**: Tất cả user đã đăng nhập

---

## 1. Authentication APIs (`/api/auth`)

### 1.1 Đăng ký tài khoản
- **Endpoint**: `POST /api/auth/register`
- **Mục đích**: Đăng ký tài khoản khách hàng mới
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "fullName": "Nguyễn Văn A",
  "email": "user@gmail.com",
  "passwordHash": "Password123!",
  "confirmPassword": "Password123!",
  "phoneNumber": "0123456789",
  "dateOfBirth": "1990-01-01",
  "gender": "MALE",
  "address": "123 Đường ABC, Quận 1, TP.HCM",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

### 1.2 Đăng nhập
- **Endpoint**: `POST /api/auth/login`
- **Mục đích**: Đăng nhập vào hệ thống
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "email": "user@gmail.com",
  "password": "Password123!"
}
```

### 1.3 Xác thực OTP
- **Endpoint**: `POST /api/auth/verify-otp`
- **Mục đích**: Xác thực mã OTP gửi qua email
- **Authorization**: Không cần
- **Query Parameters**: `email`, `otp`

### 1.4 Xác thực email
- **Endpoint**: `POST /api/auth/verify-email`
- **Mục đích**: Xác thực email với mã OTP
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "userId": 1,
  "otpCode": "123456"
}
```

### 1.5 Gửi lại mã xác thực
- **Endpoint**: `POST /api/auth/resend-verification`
- **Mục đích**: Gửi lại mã OTP xác thực email
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "email": "user@gmail.com"
}
```

### 1.6 Yêu cầu đặt lại mật khẩu
- **Endpoint**: `POST /api/auth/reset-password/request`
- **Mục đích**: Gửi OTP để đặt lại mật khẩu
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "email": "user@gmail.com"
}
```

### 1.7 Xác nhận đặt lại mật khẩu
- **Endpoint**: `POST /api/auth/reset-password/confirm`
- **Mục đích**: Đặt lại mật khẩu với OTP
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "email": "user@gmail.com",
  "otpCode": "123456",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

### 1.8 Đăng xuất
- **Endpoint**: `POST /api/auth/logout`
- **Mục đích**: Đăng xuất khỏi hệ thống
- **Authorization**: Required (AuthenticatedUser)

### 1.9 Lấy thông tin profile
- **Endpoint**: `GET /api/auth/profile`
- **Mục đích**: Lấy thông tin profile của user hiện tại
- **Authorization**: Required (AuthenticatedUser)

### 1.10 Cập nhật profile
- **Endpoint**: `PUT /api/auth/profile`
- **Mục đích**: Cập nhật thông tin profile (không cho đổi email/phone)
- **Authorization**: Required (AuthenticatedUser)
- **Request Body**:
```json
{
  "fullName": "Nguyễn Văn A",
  "dateOfBirth": "1990-01-01",
  "gender": "MALE",
  "address": "123 Đường ABC, Quận 1, TP.HCM"
}
```

### 1.11 Đổi mật khẩu
- **Endpoint**: `POST /api/auth/change-password`
- **Mục đích**: Đổi mật khẩu cho user hiện tại
- **Authorization**: Required (AuthenticatedUser)
- **Request Body**:
```json
{
  "currentPassword": "OldPassword123!",
  "newPassword": "NewPassword123!",
  "confirmNewPassword": "NewPassword123!"
}
```

### 1.12 Upload avatar
- **Endpoint**: `POST /api/auth/upload-avatar`
- **Mục đích**: Upload ảnh đại diện
- **Authorization**: Required (AuthenticatedUser)
- **Content-Type**: `multipart/form-data`
- **Body**: File upload

### 1.13 Đăng nhập bằng Google
- **Endpoint**: `POST /api/auth/login-google`
- **Mục đích**: Đăng nhập bằng tài khoản Google
- **Authorization**: Không cần
- **Request Body**:
```json
{
  "token": "google-id-token"
}
```

---

## 2. Booking APIs (`/api/booking`)

### 2.1 Lấy thông tin khả dụng
- **Endpoint**: `GET /api/booking/availability`
- **Mục đích**: Kiểm tra lịch trống của trung tâm theo ngày và dịch vụ
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `centerId`: ID trung tâm
  - `date`: Ngày (YYYY-MM-DD)
  - `serviceIds`: Danh sách ID dịch vụ (comma-separated)

### 2.2 Tạo đặt lịch mới
- **Endpoint**: `POST /api/booking`
- **Mục đích**: Tạo đặt lịch dịch vụ mới
- **Authorization**: Required (AuthenticatedUser)
- **Request Body**:
```json
{
  "customerId": 1,
  "centerId": 1,
  "preferredDate": "2024-01-15",
  "preferredTimeSlots": [1, 2, 3],
  "services": [1, 2],
  "notes": "Ghi chú đặc biệt"
}
```

### 2.3 Lấy thông tin đặt lịch
- **Endpoint**: `GET /api/booking/{id}`
- **Mục đích**: Lấy thông tin chi tiết đặt lịch
- **Authorization**: Required (AuthenticatedUser)

### 2.4 Cập nhật trạng thái đặt lịch
- **Endpoint**: `PUT /api/booking/{id}/status`
- **Mục đích**: Cập nhật trạng thái đặt lịch (Staff/Admin only)
- **Authorization**: Required (StaffOrAdmin)
- **Request Body**:
```json
{
  "status": "CONFIRMED",
  "notes": "Đã xác nhận lịch hẹn"
}
```

### 2.5 Gán dịch vụ cho đặt lịch
- **Endpoint**: `POST /api/booking/{id}/services`
- **Mục đích**: Gán danh sách dịch vụ cho đặt lịch (Staff/Admin only)
- **Authorization**: Required (StaffOrAdmin)
- **Request Body**:
```json
{
  "serviceIds": [1, 2, 3]
}
```

### 2.6 Gán time slots cho đặt lịch
- **Endpoint**: `POST /api/booking/{id}/assign-slots`
- **Mục đích**: Gán time slots cho đặt lịch (Staff/Admin only)
- **Authorization**: Required (StaffOrAdmin)
- **Request Body**:
```json
{
  "timeSlotIds": [1, 2, 3]
}
```

---

## 3. Center APIs (`/api/center`)

### 3.1 Lấy danh sách trung tâm
- **Endpoint**: `GET /api/center`
- **Mục đích**: Lấy danh sách tất cả trung tâm với phân trang
- **Authorization**: Required (StaffOrAdmin)
- **Query Parameters**: 
  - `pageNumber`: Số trang (default: 1)
  - `pageSize`: Kích thước trang (default: 10)
  - `searchTerm`: Từ khóa tìm kiếm
  - `city`: Lọc theo thành phố

### 3.2 Lấy trung tâm đang hoạt động
- **Endpoint**: `GET /api/center/active`
- **Mục đích**: Lấy danh sách trung tâm đang hoạt động
- **Authorization**: Required (StaffOrAdmin)

### 3.3 Lấy thông tin trung tâm
- **Endpoint**: `GET /api/center/{id}`
- **Mục đích**: Lấy thông tin chi tiết trung tâm
- **Authorization**: Required (StaffOrAdmin)

### 3.4 Tạo trung tâm mới
- **Endpoint**: `POST /api/center`
- **Mục đích**: Tạo trung tâm mới
- **Authorization**: Required (StaffOrAdmin)
- **Request Body**:
```json
{
  "centerName": "Trung tâm EV Hà Nội",
  "address": "123 Đường ABC, Quận 1, Hà Nội",
  "phoneNumber": "0123456789",
  "email": "hanoi@evservice.com",
  "city": "Hà Nội",
  "isActive": true
}
```

### 3.5 Cập nhật trung tâm
- **Endpoint**: `PUT /api/center/{id}`
- **Mục đích**: Cập nhật thông tin trung tâm
- **Authorization**: Required (StaffOrAdmin)

---

## 4. Customer APIs (`/api/customer`)

### 4.1 Lấy thông tin khách hàng hiện tại
- **Endpoint**: `GET /api/customer/me`
- **Mục đích**: Lấy thông tin khách hàng hiện tại
- **Authorization**: Required (AuthenticatedUser)

### 4.2 Tạo hồ sơ khách hàng
- **Endpoint**: `POST /api/customer`
- **Mục đích**: Tạo hồ sơ khách hàng mới
- **Authorization**: Required (AuthenticatedUser)

### 4.3 Lấy danh sách xe của khách hàng
- **Endpoint**: `GET /api/customer/{id}/vehicles`
- **Mục đích**: Lấy danh sách phương tiện của khách hàng
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm

### 4.4 Cập nhật thông tin khách hàng
- **Endpoint**: `PUT /api/customer/{id}`
- **Mục đích**: Cập nhật thông tin khách hàng
- **Authorization**: Required (AuthenticatedUser)

---

## 5. Inventory APIs (`/api/inventory`)

### 5.1 Lấy danh sách tồn kho
- **Endpoint**: `GET /api/inventory`
- **Mục đích**: Lấy danh sách tồn kho với phân trang
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `centerId`: Lọc theo trung tâm
  - `partId`: Lọc theo phụ tùng
  - `searchTerm`: Từ khóa tìm kiếm

### 5.2 Lấy thông tin tồn kho
- **Endpoint**: `GET /api/inventory/{id}`
- **Mục đích**: Lấy thông tin chi tiết tồn kho
- **Authorization**: Required (AuthenticatedUser)

### 5.3 Cập nhật tồn kho
- **Endpoint**: `PUT /api/inventory/{id}`
- **Mục đích**: Cập nhật số lượng tồn kho (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

---

## 6. Part APIs (`/api/part`)

### 6.1 Lấy danh sách phụ tùng
- **Endpoint**: `GET /api/part`
- **Mục đích**: Lấy danh sách tất cả phụ tùng
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `isActive`: Lọc theo trạng thái

### 6.2 Lấy thông tin phụ tùng
- **Endpoint**: `GET /api/part/{id}`
- **Mục đích**: Lấy thông tin chi tiết phụ tùng
- **Authorization**: Required (AuthenticatedUser)

### 6.3 Tạo phụ tùng mới
- **Endpoint**: `POST /api/part`
- **Mục đích**: Tạo phụ tùng mới (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Request Body**:
```json
{
  "partName": "Pin xe điện",
  "partCode": "BAT001",
  "brand": "Tesla",
  "description": "Pin lithium-ion 60kWh",
  "unitPrice": 5000000,
  "isActive": true
}
```

---

## 7. Promotion APIs (`/api/promotion`)

### 7.1 Lấy danh sách khuyến mãi
- **Endpoint**: `GET /api/promotion`
- **Mục đích**: Lấy danh sách tất cả khuyến mãi
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `status`: Lọc theo trạng thái (ACTIVE, INACTIVE, EXPIRED)
  - `promotionType`: Lọc theo loại (GENERAL, FIRST_TIME, BIRTHDAY, LOYALTY)

### 7.2 Lấy thông tin khuyến mãi
- **Endpoint**: `GET /api/promotion/{id}`
- **Mục đích**: Lấy thông tin chi tiết khuyến mãi
- **Authorization**: Required (AuthenticatedUser)

### 7.3 Lấy khuyến mãi theo mã
- **Endpoint**: `GET /api/promotion/code/{code}`
- **Mục đích**: Lấy thông tin khuyến mãi theo mã
- **Authorization**: Required (AuthenticatedUser)

### 7.4 Tạo khuyến mãi mới
- **Endpoint**: `POST /api/promotion`
- **Mục đích**: Tạo khuyến mãi mới (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 7.5 Cập nhật khuyến mãi
- **Endpoint**: `PUT /api/promotion/{id}`
- **Mục đích**: Cập nhật thông tin khuyến mãi (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 7.6 Xác thực mã khuyến mãi
- **Endpoint**: `POST /api/promotion/validate`
- **Mục đích**: Xác thực mã khuyến mãi có hợp lệ không
- **Authorization**: Required (AuthenticatedUser)
- **Request Body**:
```json
{
  "promotionCode": "SUMMER2024",
  "customerId": 1,
  "totalAmount": 1000000
}
```

### 7.7 Kích hoạt khuyến mãi
- **Endpoint**: `PUT /api/promotion/{id}/activate`
- **Mục đích**: Kích hoạt khuyến mãi (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 7.8 Vô hiệu hóa khuyến mãi
- **Endpoint**: `PUT /api/promotion/{id}/deactivate`
- **Mục đích**: Vô hiệu hóa khuyến mãi (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 7.9 Lấy khuyến mãi đang hoạt động
- **Endpoint**: `GET /api/promotion/active`
- **Mục đích**: Lấy danh sách khuyến mãi đang hoạt động
- **Authorization**: Required (AuthenticatedUser)

---

## 8. Service Category APIs (`/api/servicecategory`)

### 8.1 Lấy danh sách danh mục dịch vụ
- **Endpoint**: `GET /api/servicecategory`
- **Mục đích**: Lấy danh sách tất cả danh mục dịch vụ
- **Authorization**: Required (AuthenticatedUser)

### 8.2 Lấy danh mục đang hoạt động
- **Endpoint**: `GET /api/servicecategory/active`
- **Mục đích**: Lấy danh sách danh mục dịch vụ đang hoạt động
- **Authorization**: Required (AuthenticatedUser)

---

## 9. Service APIs (`/api/service`)

### 9.1 Lấy danh sách dịch vụ
- **Endpoint**: `GET /api/service`
- **Mục đích**: Lấy danh sách tất cả dịch vụ
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `categoryId`: Lọc theo danh mục

### 9.2 Lấy danh sách dịch vụ đang hoạt động
- **Endpoint**: `GET /api/service/active`
- **Mục đích**: Lấy danh sách các dịch vụ đang hoạt động (Services.IsActive = 1 AND ServiceCategories.IsActive = 1)
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang (default: 1)
  - `pageSize`: Kích thước trang (default: 10, max: 100)
  - `searchTerm`: Từ khóa tìm kiếm
  - `categoryId`: Lọc theo danh mục dịch vụ
- **Response**: Danh sách dịch vụ đang hoạt động với thông tin phân trang
- **Điều kiện**: Chỉ trả về dịch vụ có `IsActive = true` và danh mục dịch vụ có `IsActive = true`

### 9.3 Lấy thông tin dịch vụ
- **Endpoint**: `GET /api/service/{id}`
- **Mục đích**: Lấy thông tin chi tiết dịch vụ
- **Authorization**: Required (AuthenticatedUser)

---

## 10. Staff Management APIs (`/api/staffmanagement`)

### 10.1 Thêm nhân viên vào trung tâm
- **Endpoint**: `POST /api/staffmanagement/staff`
- **Mục đích**: Thêm nhân viên vào trung tâm (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Request Body**:
```json
{
  "userId": 1,
  "centerId": 1,
  "position": "Nhân viên tư vấn",
  "hireDate": "2024-01-01",
  "salary": 10000000,
  "isActive": true
}
```

### 10.2 Lấy thông tin nhân viên
- **Endpoint**: `GET /api/staffmanagement/staff/{id}`
- **Mục đích**: Lấy thông tin chi tiết nhân viên
- **Authorization**: Required (AdminOnly)

### 10.3 Lấy danh sách nhân viên
- **Endpoint**: `GET /api/staffmanagement/staff`
- **Mục đích**: Lấy danh sách nhân viên theo trung tâm hoặc tất cả
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `centerId`: ID trung tâm (optional)
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `position`: Lọc theo vị trí
  - `isActive`: Lọc theo trạng thái

### 10.4 Cập nhật nhân viên
- **Endpoint**: `PUT /api/staffmanagement/staff/{id}`
- **Mục đích**: Cập nhật thông tin nhân viên
- **Authorization**: Required (AdminOnly)

### 10.5 Xóa nhân viên
- **Endpoint**: `DELETE /api/staffmanagement/staff/{id}`
- **Mục đích**: Xóa nhân viên khỏi trung tâm
- **Authorization**: Required (AdminOnly)

### 10.6 Thêm kỹ thuật viên vào trung tâm
- **Endpoint**: `POST /api/staffmanagement/technician`
- **Mục đích**: Thêm kỹ thuật viên vào trung tâm (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Request Body**:
```json
{
  "userId": 1,
  "centerId": 1,
  "specialization": "Điện tử",
  "certification": "Chứng chỉ kỹ thuật viên",
  "hireDate": "2024-01-01",
  "salary": 15000000,
  "isActive": true
}
```

### 10.7 Lấy thông tin kỹ thuật viên
- **Endpoint**: `GET /api/staffmanagement/technician/{id}`
- **Mục đích**: Lấy thông tin chi tiết kỹ thuật viên
- **Authorization**: Required (AdminOnly)

### 10.8 Lấy danh sách kỹ thuật viên
- **Endpoint**: `GET /api/staffmanagement/technician`
- **Mục đích**: Lấy danh sách kỹ thuật viên theo trung tâm
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `centerId`: ID trung tâm
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `specialization`: Lọc theo chuyên môn
  - `isActive`: Lọc theo trạng thái

### 10.9 Cập nhật kỹ thuật viên
- **Endpoint**: `PUT /api/staffmanagement/technician/{id}`
- **Mục đích**: Cập nhật thông tin kỹ thuật viên
- **Authorization**: Required (AdminOnly)

### 10.10 Xóa kỹ thuật viên
- **Endpoint**: `DELETE /api/staffmanagement/technician/{id}`
- **Mục đích**: Xóa kỹ thuật viên khỏi trung tâm
- **Authorization**: Required (AdminOnly)

### 10.11 Kiểm tra mã nhân viên
- **Endpoint**: `GET /api/staffmanagement/validate/staff-code`
- **Mục đích**: Kiểm tra mã nhân viên có trùng không
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `staffCode`: Mã nhân viên
  - `excludeStaffId`: ID nhân viên loại trừ (khi cập nhật)

### 10.12 Kiểm tra mã kỹ thuật viên
- **Endpoint**: `GET /api/staffmanagement/validate/technician-code`
- **Mục đích**: Kiểm tra mã kỹ thuật viên có trùng không
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `technicianCode`: Mã kỹ thuật viên
  - `excludeTechnicianId`: ID kỹ thuật viên loại trừ (khi cập nhật)

### 10.13 Kiểm tra gán người dùng
- **Endpoint**: `GET /api/staffmanagement/validate/user-assignment`
- **Mục đích**: Kiểm tra người dùng có thể được gán vào trung tâm không
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `userId`: ID người dùng
  - `centerId`: ID trung tâm

---

## 11. Technician APIs (`/api/technician`)

### 11.1 Lấy danh sách kỹ thuật viên
- **Endpoint**: `GET /api/technician`
- **Mục đích**: Lấy danh sách tất cả kỹ thuật viên
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `centerId`: Lọc theo trung tâm

### 11.2 Lấy thông tin kỹ thuật viên
- **Endpoint**: `GET /api/technician/{id}`
- **Mục đích**: Lấy thông tin chi tiết kỹ thuật viên
- **Authorization**: Required (AuthenticatedUser)

### 11.3 Lấy lịch làm việc kỹ thuật viên
- **Endpoint**: `GET /api/technician/{id}/availability`
- **Mục đích**: Lấy lịch làm việc của kỹ thuật viên theo ngày
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `date`: Ngày (YYYY-MM-DD)

### 11.4 Lấy time slots của kỹ thuật viên
- **Endpoint**: `GET /api/technician/{id}/timeslots`
- **Mục đích**: Lấy danh sách time slots của kỹ thuật viên
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `active`: Lọc theo trạng thái active

### 11.5 Cập nhật lịch làm việc
- **Endpoint**: `PUT /api/technician/{id}/availability`
- **Mục đích**: Cập nhật lịch làm việc của kỹ thuật viên (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

---

## 12. Time Slot APIs (`/api/timeslot`)

### 12.1 Lấy danh sách time slots
- **Endpoint**: `GET /api/timeslot`
- **Mục đích**: Lấy danh sách tất cả time slots
- **Authorization**: Không cần
- **Query Parameters**: 
  - `active`: Lọc theo trạng thái active (true/false/null)

### 12.2 Tạo time slot mới
- **Endpoint**: `POST /api/timeslot`
- **Mục đích**: Tạo time slot mới (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Request Body**:
```json
{
  "startTime": "08:00:00",
  "endTime": "10:00:00",
  "isActive": true
}
```

---

## 13. User APIs (`/api/user`)

### 13.1 Lấy danh sách người dùng
- **Endpoint**: `GET /api/user`
- **Mục đích**: Lấy danh sách tất cả người dùng (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `searchTerm`: Từ khóa tìm kiếm
  - `role`: Lọc theo vai trò

### 13.2 Lấy thông tin người dùng
- **Endpoint**: `GET /api/user/{id}`
- **Mục đích**: Lấy thông tin chi tiết người dùng (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 13.3 Tạo người dùng mới
- **Endpoint**: `POST /api/user`
- **Mục đích**: Tạo người dùng mới (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)
- **Request Body**:
```json
{
  "fullName": "Nguyễn Văn A",
  "email": "user@gmail.com",
  "passwordHash": "Password123!",
  "phoneNumber": "0123456789",
  "dateOfBirth": "1990-01-01",
  "gender": "MALE",
  "address": "123 Đường ABC, Quận 1, TP.HCM",
  "role": "STAFF"
}
```

### 13.4 Kích hoạt người dùng
- **Endpoint**: `PATCH /api/user/{id}/activate`
- **Mục đích**: Kích hoạt người dùng (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

### 13.5 Vô hiệu hóa người dùng
- **Endpoint**: `PATCH /api/user/{id}/deactivate`
- **Mục đích**: Vô hiệu hóa người dùng (chỉ ADMIN)
- **Authorization**: Required (AdminOnly)

---

## 14. Vehicle APIs (`/api/vehicle`)

### 14.1 Lấy danh sách xe
- **Endpoint**: `GET /api/vehicle`
- **Mục đích**: Lấy danh sách xe với phân trang
- **Authorization**: Required (AuthenticatedUser)
- **Query Parameters**: 
  - `pageNumber`: Số trang
  - `pageSize`: Kích thước trang
  - `customerId`: Lọc theo khách hàng
  - `searchTerm`: Từ khóa tìm kiếm

### 14.2 Lấy thông tin xe
- **Endpoint**: `GET /api/vehicle/{id}`
- **Mục đích**: Lấy thông tin chi tiết xe
- **Authorization**: Required (AuthenticatedUser)

### 14.3 Tạo xe mới
- **Endpoint**: `POST /api/vehicle`
- **Mục đích**: Tạo xe mới
- **Authorization**: Required (AuthenticatedUser)
- **Request Body**:
```json
{
  "customerId": 1,
  "modelId": 1,
  "licensePlate": "29-T8 2843",
  "modelBrand": "Tesla",
  "modelName": "Model 3",
  "modelYear": 2023,
  "batteryCapacity": "60kWh",
  "range": "400km",
  "currentMileage": 10000,
  "lastServiceDate": "2024-01-01"
}
```

### 14.4 Cập nhật xe
- **Endpoint**: `PUT /api/vehicle/{id}`
- **Mục đích**: Cập nhật thông tin xe
- **Authorization**: Required (AuthenticatedUser)

---

## Response Format

Tất cả API đều trả về response theo format chuẩn:

### Success Response
```json
{
  "success": true,
  "message": "Thông báo thành công",
  "data": {
    // Dữ liệu trả về
  }
}
```

### Error Response
```json
{
  "success": false,
  "message": "Thông báo lỗi",
  "errors": [
    "Chi tiết lỗi 1",
    "Chi tiết lỗi 2"
  ]
}
```

## HTTP Status Codes

- **200 OK**: Thành công
- **201 Created**: Tạo mới thành công
- **400 Bad Request**: Dữ liệu đầu vào không hợp lệ
- **401 Unauthorized**: Chưa đăng nhập hoặc token không hợp lệ
- **403 Forbidden**: Không có quyền truy cập
- **404 Not Found**: Không tìm thấy tài nguyên
- **500 Internal Server Error**: Lỗi hệ thống

## Validation Rules

### Email
- Phải có đuôi @gmail.com
- Format hợp lệ

### Password
- Tối thiểu 8 ký tự
- Bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt

### Phone Number
- Bắt đầu bằng 0
- Có đúng 10 chữ số

### License Plate (Xe máy)
- Format: `XX-YZ ABCD`
- XX: 2 số đầu (mã tỉnh/thành)
- YZ: 1 chữ cái + 1 số
- ABCD: 4 chữ số

### Date Format
- YYYY-MM-DD (ISO 8601)

## Rate Limiting
- Không có giới hạn rate limiting hiện tại
- Có thể được thêm vào trong tương lai

## Versioning
- Hiện tại sử dụng version v1
- Có thể thêm versioning trong tương lai

---

*Tài liệu này được cập nhật lần cuối: 19/09/2025*
