-- Script để thêm cột FulfillmentCenterID vào bảng Orders
-- FulfillmentCenterID lưu thông tin center nào đã fulfill order (trừ kho)

USE EVServiceCenter;
GO

-- 1. Thêm cột FulfillmentCenterID vào bảng Orders
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Orders')
    AND name = 'FulfillmentCenterID'
)
BEGIN
    ALTER TABLE dbo.Orders
    ADD FulfillmentCenterID INT NULL;

    PRINT 'Đã thêm cột FulfillmentCenterID vào bảng Orders';
END
ELSE
BEGIN
    PRINT 'Cột FulfillmentCenterID đã tồn tại trong bảng Orders';
END
GO

-- 2. Tạo foreign key constraint cho FulfillmentCenterID
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE object_id = OBJECT_ID('FK_Orders_FulfillmentCenter')
)
BEGIN
    ALTER TABLE dbo.Orders
    ADD CONSTRAINT FK_Orders_FulfillmentCenter
    FOREIGN KEY (FulfillmentCenterID)
    REFERENCES dbo.ServiceCenters(CenterID)
    ON DELETE SET NULL;

    PRINT 'Đã tạo foreign key constraint FK_Orders_FulfillmentCenter';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_Orders_FulfillmentCenter đã tồn tại';
END
GO

-- 3. Kiểm tra kết quả
SELECT
    'Orders' AS TableName,
    COUNT(*) AS TotalRows,
    COUNT(FulfillmentCenterID) AS RowsWithFulfillmentCenter,
    COUNT(*) - COUNT(FulfillmentCenterID) AS RowsWithoutFulfillmentCenter
FROM dbo.Orders;
GO

-- 4. Hiển thị các order đã có fulfillment center
SELECT TOP 10
    OrderID,
    CustomerID,
    Status,
    FulfillmentCenterID,
    CreatedAt
FROM dbo.Orders
WHERE FulfillmentCenterID IS NOT NULL
ORDER BY CreatedAt DESC;
GO

PRINT 'Hoàn thành! Đã thêm cột FulfillmentCenterID vào Orders với foreign key constraint.';
GO

