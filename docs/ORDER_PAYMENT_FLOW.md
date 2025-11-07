# 📋 LUỒNG THANH TOÁN ORDER - TỔNG QUAN

## 🔄 Flow tổng thể

```
1. Customer thêm parts vào Cart
   ↓
2. Tạo Order từ Cart hoặc Quick Order
   ↓
3. Tạo Payment Link (PayOS)
   ↓
4. Customer thanh toán trên PayOS
   ↓
5. PayOS callback về /api/payment/result
   ↓
6. Confirm Payment → Trừ kho + Update Invoice
```

---

## 📍 CÁC API ENDPOINTS

### 🛒 **1. CART MANAGEMENT** (`/api/cart`)

#### Thêm item vào cart
- **POST** `/api/cart/customer/{customerId}/items`
- **Mô tả**: Thêm part vào giỏ hàng
- **Body**: `{ partId, quantity }`
- **Response**: Cart với items

#### Lấy cart
- **GET** `/api/cart/customer/{customerId}`
- **Mô tả**: Lấy giỏ hàng của customer
- **Response**: Cart với danh sách items

#### Cập nhật quantity
- **PUT** `/api/cart/customer/{customerId}/items/{partId}`
- **Mô tả**: Cập nhật số lượng part trong cart
- **Body**: `{ quantity }`

#### Xóa item khỏi cart
- **DELETE** `/api/cart/customer/{customerId}/items/{partId}`
- **Mô tả**: Xóa part khỏi giỏ hàng

#### Xóa toàn bộ cart
- **DELETE** `/api/cart/customer/{customerId}`
- **Mô tả**: Xóa toàn bộ giỏ hàng

---

### 📦 **2. ORDER MANAGEMENT** (`/api/order`)

#### Tạo Order từ Cart
- **POST** `/api/order/customer/{customerId}/create`
- **Mô tả**: Tạo order từ giỏ hàng
- **Body**: `CreateOrderRequest { Latitude?, Longitude?, Notes? }`
- **Response**: Order với `SuggestedFulfillmentCenterId` (nếu có lat/lng)
- **Logic**:
  - Tạo Order với status = "PENDING"
  - Tạo OrderItems từ Cart
  - Suggest fulfillment center (nếu có lat/lng) - **CHỈ LÀ SUGGESTION**
  - Xóa Cart sau khi tạo Order

#### Tạo Quick Order (Mua ngay)
- **POST** `/api/order/customers/{customerId}/orders/quick`
- **Mô tả**: Tạo order trực tiếp từ danh sách parts (không qua cart)
- **Body**: `QuickOrderRequest { Items: [{ partId, quantity }], Latitude?, Longitude? }`
- **Response**: Order với `SuggestedFulfillmentCenterId` (nếu có lat/lng)
- **Logic**: Tương tự CreateOrder nhưng không cần cart

#### Lấy danh sách Orders của Customer
- **GET** `/api/order/customer/{customerId}`
- **Mô tả**: Lấy tất cả orders của customer
- **Response**: List<OrderResponse>

#### Lấy chi tiết Order
- **GET** `/api/order/{orderId}`
- **Mô tả**: Lấy thông tin chi tiết order
- **Response**: OrderResponse với `FulfillmentCenterId` (nếu đã thanh toán)

#### Lấy OrderItems
- **GET** `/api/order/{orderId}/items`
- **Mô tả**: Lấy danh sách items trong order
- **Response**: List<OrderItemResponse>

#### Lấy tất cả Orders (Admin)
- **GET** `/api/order/admin` hoặc `/api/order`
- **Mô tả**: Lấy tất cả orders (Admin only)
- **Response**: List<OrderResponse>

#### Export Orders (Admin)
- **GET** `/api/order/export`
- **Mô tả**: Export orders ra file Excel
- **Response**: Excel file

#### Cập nhật Order Status
- **PUT** `/api/order/{orderId}/status`
- **Mô tả**: Cập nhật trạng thái order (Admin/Staff)
- **Body**: `{ status: "PENDING" | "PAID" | "COMPLETED" | "CANCELLED" }`

#### Xóa Order
- **DELETE** `/api/order/{orderId}`
- **Mô tả**: Xóa order (chỉ khi status = "PENDING")

---

### 💳 **3. PAYMENT MANAGEMENT** (`/api/payment`)

#### Tạo Payment Link cho Order
- **POST** `/api/order/{orderId}/checkout/online`
- **Mô tả**: Tạo PayOS payment link cho order
- **Response**: `{ checkoutUrl, orderId }`
- **Logic**:
  - Validate order (status = "PENDING", có items, tổng tiền > 0)
  - Generate unique `PayOSOrderCode` (nếu chưa có)
  - Lưu `PayOSOrderCode` vào Order
  - Tạo payment link trên PayOS
  - Return checkoutUrl

#### Lấy Payment Link hiện có
- **GET** `/api/order/{orderId}/payment/link`
- **Mô tả**: Lấy payment link đã tạo trước đó
- **Response**: `{ checkoutUrl }` hoặc 404 nếu chưa có

#### Payment Result Callback (PayOS)
- **GET** `/api/payment/result?orderCode={payOSOrderCode}&status={status}&code={code}`
- **Mô tả**: Callback từ PayOS sau khi thanh toán
- **Logic**:
  1. Parse `orderCode` từ PayOS
  2. Tìm Order bằng `PayOSOrderCode`
  3. Nếu `status == "PAID" && code == "00"`:
     - Gọi `ConfirmOrderPaymentAsync(orderId)`
     - **Trong ConfirmOrderPaymentAsync**:
       - Update Order status = "PAID"
       - Tạo/Update Invoice
       - **Tính PartsAmount** từ OrderItems
       - **Xác định FulfillmentCenter** (center có đủ stock)
       - **Lưu FulfillmentCenterId** vào Order
       - **Trừ kho** từ FulfillmentCenter
       - **Update Invoice.PartsAmount**
       - Tạo Payment record
  4. Redirect về frontend (success/error/failed)

#### Cancel Payment (Order)
- **GET** `/api/payment/order/{orderId}/cancel`
- **Mô tả**: Redirect khi customer hủy thanh toán
- **Logic**: Redirect về frontend với orderId

---

## 🔧 **LOGIC CHI TIẾT**

### **ConfirmOrderPaymentAsync** Flow:

```
1. Validate Order
   ├─ Order tồn tại?
   ├─ Status != "PAID" && != "COMPLETED"?
   └─ Có OrderItems?

2. Update Order
   ├─ Status = "PAID"
   └─ UpdatedAt = DateTime.UtcNow

3. Tạo/Update Invoice
   ├─ Tạo Invoice mới (nếu chưa có)
   ├─ Status = "PAID"
   └─ CustomerId, Email, Phone từ Order.Customer

4. Tính PartsAmount
   └─ Sum(OrderItems.UnitPrice × OrderItems.Quantity)

5. Xác định FulfillmentCenter
   ├─ Loop qua tất cả active centers
   ├─ Check center có đủ stock cho TẤT CẢ OrderItems?
   └─ Chọn center đầu tiên có đủ stock

6. Lưu FulfillmentCenterId
   └─ Order.FulfillmentCenterId = fulfillmentCenterId

7. Trừ kho
   ├─ Loop qua từng OrderItem
   ├─ Tìm InventoryPart trong FulfillmentCenter
   ├─ Validate: CurrentStock >= Quantity
   └─ CurrentStock -= Quantity

8. Update Invoice.PartsAmount
   └─ Invoice.PartsAmount = partsAmount

9. Tạo Payment Record
   ├─ PaymentCode = "PAY{method}{timestamp}{orderId}"
   ├─ InvoiceId, PaymentMethod, Amount
   ├─ Status = "PAID", PaidAt = DateTime.UtcNow
   └─ PaidByUserID = Order.Customer.User.UserId

10. Send Notification
    └─ Thông báo thanh toán thành công cho customer
```

---

## 📊 **DATABASE CHANGES**

### **Order Table**
- ✅ `PayOSOrderCode` (INT NULL) - Unique random number cho PayOS
- ✅ `FulfillmentCenterId` (INT NULL) - Center nào đã fulfill order
- ✅ Foreign key: `FK_Orders_FulfillmentCenter` → `ServiceCenters(CenterID)`

### **Invoice Table**
- ✅ `PartsAmount` (DECIMAL) - Tổng tiền parts (được update khi thanh toán)
- ❌ `WorkOrderID` - **Đã xóa** (không còn dùng)

---

## 🔍 **CÁCH TRUY VẤN**

### Query Order với FulfillmentCenter:
```sql
SELECT
    o.OrderID,
    o.CustomerID,
    o.Status,
    o.FulfillmentCenterID,
    sc.CenterName,
    sc.Address,
    i.PartsAmount
FROM Orders o
LEFT JOIN ServiceCenters sc ON o.FulfillmentCenterID = sc.CenterID
LEFT JOIN Invoices i ON i.OrderID = o.OrderID
WHERE o.OrderID = @OrderId;
```

### Query tất cả orders được fulfill từ một center:
```sql
SELECT *
FROM Orders
WHERE FulfillmentCenterID = @CenterId
AND Status = 'PAID';
```

### Query inventory đã trừ cho order:
```sql
SELECT
    ip.PartID,
    p.PartName,
    oi.Quantity AS OrderQuantity,
    ip.CurrentStock AS RemainingStock,
    sc.CenterName AS FulfillmentCenter
FROM Orders o
INNER JOIN OrderItems oi ON o.OrderID = oi.OrderID
INNER JOIN Parts p ON oi.PartID = p.PartID
INNER JOIN ServiceCenters sc ON o.FulfillmentCenterID = sc.CenterID
INNER JOIN Inventories inv ON sc.CenterID = inv.CenterID
INNER JOIN InventoryParts ip ON inv.InventoryID = ip.InventoryID AND ip.PartID = oi.PartID
WHERE o.OrderID = @OrderId;
```

---

## ⚠️ **LƯU Ý QUAN TRỌNG**

1. **FulfillmentCenter chỉ được xác định khi thanh toán**, không phải khi tạo order
2. **SuggestCenterAsync** chỉ là suggestion, không được lưu vào Order
3. **PayOSOrderCode** được generate unique, không còn dùng offset
4. **Tất cả operations trong ConfirmOrderPaymentAsync** được thực hiện trong **TransactionScope** để đảm bảo atomicity
5. Nếu không tìm thấy fulfillment center có đủ stock → **throw exception**, không thanh toán được

---

## 🎯 **TÓM TẮT CÁC API CHÍNH**

| API | Method | Endpoint | Mô tả |
|-----|--------|----------|-------|
| Create Order | POST | `/api/order/customer/{customerId}/create` | Tạo order từ cart |
| Quick Order | POST | `/api/order/customers/{customerId}/orders/quick` | Tạo order trực tiếp |
| Get Order | GET | `/api/order/{orderId}` | Lấy chi tiết order |
| Create Payment Link | POST | `/api/order/{orderId}/checkout/online` | Tạo PayOS link |
| Payment Callback | GET | `/api/payment/result` | PayOS callback |
| Cancel Payment | GET | `/api/payment/order/{orderId}/cancel` | Hủy thanh toán |

---

## 📝 **CHECKLIST KHI TEST**

- [ ] Tạo order từ cart
- [ ] Tạo quick order
- [ ] Tạo payment link
- [ ] Thanh toán thành công → Check:
  - [ ] Order.Status = "PAID"
  - [ ] Order.FulfillmentCenterId được lưu
  - [ ] Invoice.PartsAmount được update
  - [ ] Inventory.CurrentStock được trừ đúng
  - [ ] Payment record được tạo
- [ ] Query Order với FulfillmentCenter
- [ ] Test trường hợp không đủ stock → Exception

