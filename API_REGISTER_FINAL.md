# 🎯 API Register - Final Version

## **Endpoint**: `POST /api/Auth/register`

### **Request Body:**
```json
{
  "fullName": "Nguyễn Văn A",
  "email": "nguyenvana@example.com",
  "passwordHash": "P@sswOrd123!",
  "confirmPassword": "P@sswOrd123!",
  "phoneNumber": "0901234567",
  "dateOfBirth": "1990-01-01",
  "gender": "Male",
  "address": "123 Đường ABC, Quận XYZ, TP.HCM",
  "avatarUrl": "https://example.com/avatar.jpg"
}
```

### **Trường BẮT BUỘC:**
- `fullName` - Họ tên (2-100 ký tự)
- `email` - Email (format chuẩn + không trùng)  
- `passwordHash` - Mật khẩu (8+ ký tự: hoa/thường/số/ký tự đặc biệt)
- `confirmPassword` - Xác nhận mật khẩu (phải khớp)
- `phoneNumber` - SĐT (bắt đầu 0, đúng 10 số + không trùng)
- `dateOfBirth` - Ngày sinh (đủ 16 tuổi)
- `gender` - Giới tính (Male/Female/Other)

### **Trường TÙY CHỌN:**
- `address` - Địa chỉ (tối đa 255 ký tự, có thể để trống)
- `avatarUrl` - Link avatar (có thể để trống, không validate format)

### **Role:**
- ✅ **Mặc định là "Customer"** 
- ❌ **Không cho phép user chọn role**

### **Success Response:**
```json
{
  "success": true,
  "message": "Đăng ký tài khoản thành công! Vui lòng kiểm tra email để nhận thông tin chào mừng.",
  "data": {
    "email": "nguyenvana@example.com",
    "fullName": "Nguyễn Văn A",
    "registeredAt": "2024-01-01T00:00:00Z"
  }
}
```

### **Error Response:**
```json
{
  "success": false,
  "message": "Dữ liệu đầu vào không hợp lệ",
  "errors": [
    "Email không đúng định dạng",
    "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt"
  ]
}
```

### **Features:**
- ✅ Validation đa tầng
- ✅ Email chào mừng tự động  
- ✅ Password hashing (BCrypt)
- ✅ Error messages tiếng Việt
- ✅ Clean Architecture
- ✅ Flexible fields (address + avatar optional)
