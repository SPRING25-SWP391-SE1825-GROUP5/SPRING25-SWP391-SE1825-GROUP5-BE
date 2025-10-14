-- =====================================================
-- EV SERVICE CENTER - SAMPLE DATA SCRIPT (FIXED)
-- =====================================================
-- This script inserts realistic sample data for development and testing
-- Run this script in order to maintain referential integrity
-- FIXED: Removed explicit ID values for IDENTITY columns

USE [EVServiceCenterDB];
GO

-- Clear existing data (in reverse dependency order)
DELETE FROM [dbo].[Payments];
DELETE FROM [dbo].[Invoices];
DELETE FROM [dbo].[WorkOrderParts];
DELETE FROM [dbo].[WorkOrders];
DELETE FROM [dbo].[Bookings];
DELETE FROM [dbo].[Vehicles];
DELETE FROM [dbo].[Customers];
DELETE FROM [dbo].[Staff];
DELETE FROM [dbo].[Technicians];
DELETE FROM [dbo].[TechnicianTimeSlots];
DELETE FROM [dbo].[TimeSlots];
DELETE FROM [dbo].[ServiceCenters];
DELETE FROM [dbo].[Services];
DELETE FROM [dbo].[Parts];
DELETE FROM [dbo].[Skills];
DELETE FROM [dbo].[VehicleModel];
DELETE FROM [dbo].[Users];
DELETE FROM [dbo].[Promotions];
DELETE FROM [dbo].[Orders];
DELETE FROM [dbo].[OrderItems];
DELETE FROM [dbo].[Feedbacks];
DELETE FROM [dbo].[Conversations];
DELETE FROM [dbo].[Messages];
DELETE FROM [dbo].[MessageReads];
DELETE FROM [dbo].[VehicleModelParts];
DELETE FROM [dbo].[ServiceParts];
DELETE FROM [dbo].[ServiceRequiredSkills];
DELETE FROM [dbo].[TechnicianSkills];
DELETE FROM [dbo].[MaintenanceReminders];
DELETE FROM [dbo].[Notifications];
DELETE FROM [dbo].[OtpCodes];
DELETE FROM [dbo].[SystemSettings];
DELETE FROM [dbo].[Inventory];
DELETE FROM [dbo].[InventoryParts];
DELETE FROM [dbo].[MaintenanceChecklists];
DELETE FROM [dbo].[MaintenanceChecklistResults];
DELETE FROM [dbo].[MaintenancePolicies];
DELETE FROM [dbo].[ServiceCredits];
DELETE FROM [dbo].[UserPromotions];
GO

-- =====================================================
-- 1. USERS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [PhoneNumber], [Address], [Gender], [Role], [IsActive], [CreatedAt], [UpdatedAt])
VALUES 
(N'Nguyễn Văn Admin', 'admin@evservice.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234567', N'123 Nguyễn Huệ, Q1, TP.HCM', N'Nam', 'ADMIN', 1, GETDATE(), GETDATE()),
(N'Trần Thị Manager', 'manager@evservice.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234568', N'456 Lê Lợi, Q1, TP.HCM', N'Nữ', 'MANAGER', 1, GETDATE(), GETDATE()),
(N'Lê Văn Staff', 'staff@evservice.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234569', N'789 Đồng Khởi, Q1, TP.HCM', N'Nam', 'STAFF', 1, GETDATE(), GETDATE()),
(N'Phạm Thị Technician', 'tech1@evservice.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234570', N'321 Pasteur, Q3, TP.HCM', N'Nữ', 'TECHNICIAN', 1, GETDATE(), GETDATE()),
(N'Hoàng Văn Technician', 'tech2@evservice.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234571', N'654 Võ Văn Tần, Q3, TP.HCM', N'Nam', 'TECHNICIAN', 1, GETDATE(), GETDATE()),
(N'Nguyễn Thị Customer', 'customer1@email.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234572', N'987 Nguyễn Thị Minh Khai, Q3, TP.HCM', N'Nữ', 'CUSTOMER', 1, GETDATE(), GETDATE()),
(N'Trần Văn Customer', 'customer2@email.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234573', N'147 Điện Biên Phủ, Q.Bình Thạnh, TP.HCM', N'Nam', 'CUSTOMER', 1, GETDATE(), GETDATE()),
(N'Lê Thị Customer', 'customer3@email.com', '$2a$11$K8Y1x9Z2vN3mP4qR5sT6uV7wX8yZ9aB0cD1eF2gH3iJ4kL5mN6oP7qR8sT9uV', '0901234574', N'258 Cách Mạng Tháng 8, Q10, TP.HCM', N'Nữ', 'CUSTOMER', 1, GETDATE(), GETDATE());
GO

-- =====================================================
-- 2. SERVICE CENTERS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[ServiceCenters] ([CenterName], [Address], [PhoneNumber], [IsActive], [CreatedAt])
VALUES 
(N'Trung tâm dịch vụ EV Quận 1', N'123 Nguyễn Huệ, Quận 1, TP.HCM', '0981234567', 1, GETDATE()),
(N'Trung tâm dịch vụ EV Quận 3', N'456 Võ Văn Tần, Quận 3, TP.HCM', '0981234568', 1, GETDATE()),
(N'Trung tâm dịch vụ EV Bình Thạnh', N'789 Điện Biên Phủ, Q.Bình Thạnh, TP.HCM', '0981234569', 1, GETDATE()),
(N'EV Xô Viết Nghệ Tĩnh', N'369 Xô Viết Nghệ Tĩnh, Quận Bình Thạnh', '0987654321', 1, '2024-09-10'),
(N'EV Lê Văn Việt', N'147 Lê Văn Việt, TP Thủ Đức', '0123456789', 1, '2025-10-10'),
(N'EV Huỳnh Tấn Phát', N'117 Huỳnh Tấn Phát, Quận 7', '0879654321', 1, '2025-08-10'),
(N'EV Hoàng Văn Thụ', N'223 Hoàng Văn Thụ, Quận Tân Bình', '0789651321', 1, '2024-10-28'),
(N'EV Lê Đức Thọ', N'69 Lê Đức Thọ, Quận Gò Vấp', '0989776112', 1, '2024-12-10');

GO

-- =====================================================
-- 3. SKILLS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[Skills] ([Name], [Description])
VALUES 
(N'Kiểm tra và sửa chữa pin', N'Đo điện áp, kiểm tra cell pin, sửa mạch BMS, thay cell hoặc thay pin nguyên cụm'),
(N'Kỹ năng thay ắc qui', N'Thay các loại ắc qui'),
(N'Kỹ năng sửa chữa động cơ', N'Sửa chữa và thay thế động cơ điện, bạc đạn'),
(N'Kỹ năng sửa chữa bộ điều khiển', N'Sửa chữa, thay thế IC, bộ điều khiển tốc độ'),
(N'Kỹ năng bảo dưỡng hệ thống phanh', N'Sửa chữa, thay thế má phanh, dầu phanh, cân chỉnh phanh'),
(N'Kỹ năng sửa chữa hệ thống điện', N'Chẩn đoán và sửa chữa hệ thống dây điện, các giắc cắm'),
(N'Kỹ năng thay lốp và vành xe', N'Thay thế, vá lốp (vỏ) và cân bằng vành xe điện'),
(N'Kỹ năng sửa chữa hệ thống đèn, còi', N'Thay thế và sửa chữa đèn pha, đèn hậu, xi nhan, còi xe'),
(N'Kỹ năng bảo dưỡng tổng quát', N'Kiểm tra và bảo dưỡng định kỳ toàn bộ xe (khung sườn, tra dầu mỡ)'),
(N'Kỹ năng chẩn đoán lỗi bằng phần mềm', N'Sử dụng phần mềm/thiết bị chuyên dụng để đọc lỗi, chẩn đoán và xóa lỗi ECU'),
(N'Thay thế bộ sạc hoặc cổng sạc', N'Xác định lỗi sạc, kiểm tra chuẩn đầu vào/ra, thay thế sạc đúng thông số kỹ thuật.'),
(N'Kiểm tra và thay thế cảm biến tay ga', N'Kiểm tra tín hiệu từ tay ga, thay thế tay ga hỏng hoặc hoạt động không đều.'),
(N'Kiểm tra và sửa đồng hồ điện tử', N'Xử lý lỗi hiển thị tốc độ, pin, đèn cảnh báo trên màn hình trung tâm.'),
(N'Cài đặt và reset hệ thống điều khiển (ECU)', N'Dùng thiết bị chuyên dụng để cài đặt hoặc reset lại bộ điều khiển trung tâm.'),
(N'Bảo trì hệ thống truyền động', N'Kiểm tra, thay xích, nhông, dây curoa, bánh sau – tùy theo loại xe.'),
(N'Cân bằng và bảo trì pin định kỳ', N'Kiểm tra mức điện áp từng cell, thực hiện sạc cân bằng để tối ưu tuổi thọ pin.'),
(N'Phát hiện và xử lý lỗi nhiệt độ pin/motor cao', N'Kiểm tra nhiệt độ hoạt động, đề xuất thay thế hoặc nâng cấp hệ thống tản nhiệt.'),
(N'Thay thế tay phanh, chân chống điện', N'Kiểm tra cảm biến chân chống, công tắc phanh điện, thay thế đúng kỹ thuật.'),
(N'Sử dụng đồng hồ vạn năng thành thạo', N'Đo điện áp, dòng, trở kháng để kiểm tra các thành phần điện trên xe.'),
(N'Lắp ráp, thay thế phụ tùng chuẩn xác', N'Lắp đặt các bộ phận như chắn bùn, đèn, tay lái, kính chiếu hậu đúng kỹ thuật.');
GO

-- =====================================================
-- 4. SERVICES (No dependencies)
-- =====================================================
INSERT INTO [dbo].[Services] ([ServiceName], [Description], [BasePrice], [IsActive], [CreatedAt])
VALUES 
(N'Bảo dưỡng định kỳ', N'Bảo dưỡng định kỳ 6 tháng/lần cho xe điện', 500000.00, 1, GETDATE()),
(N'Thay pin', N'Thay thế pin xe điện khi cần thiết', 15000000.00, 1, GETDATE()),
(N'Sửa chữa động cơ', N'Sửa chữa và bảo dưỡng động cơ điện', 2000000.00, 1, GETDATE()),
(N'Kiểm tra an toàn', N'Kiểm tra tổng thể an toàn xe điện', 300000.00, 1, GETDATE()),
(N'Cập nhật phần mềm', N'Cập nhật phần mềm hệ thống xe điện', 200000.00, 1, GETDATE());
GO

-- =====================================================
-- 5. PARTS (No dependencies) - Phụ tùng xe máy điện
-- =====================================================
INSERT INTO [dbo].[Parts] ([PartName], [PartNumber], [Brand], [Price], [Rating], [IsActive], [CreatedAt])
VALUES 
-- Pin và ắc quy
(N'Pin Lithium-ion 48V 20Ah', 'BAT-48V20AH-001', 'CATL', 3500000.00, 4.5, 1, GETDATE()),
(N'Pin Lithium-ion 60V 30Ah', 'BAT-60V30AH-001', 'BYD', 4500000.00, 4.6, 1, GETDATE()),
(N'Pin Lithium-ion 72V 40Ah', 'BAT-72V40AH-001', 'LG Chem', 5500000.00, 4.7, 1, GETDATE()),
(N'Ắc quy chì 12V 7Ah', 'BAT-PB12V7AH-001', 'GS Battery', 450000.00, 4.2, 1, GETDATE()),

-- Động cơ và truyền động
(N'Động cơ điện 48V 1000W', 'MOT-48V1KW-001', 'Bosch', 2800000.00, 4.6, 1, GETDATE()),
(N'Động cơ điện 60V 1500W', 'MOT-60V1.5KW-001', 'Bafang', 3200000.00, 4.5, 1, GETDATE()),
(N'Động cơ điện 72V 2000W', 'MOT-72V2KW-001', 'QSMotor', 3800000.00, 4.4, 1, GETDATE()),
(N'Bộ truyền động xích', 'DRV-CHAIN-001', 'KMC', 180000.00, 4.3, 1, GETDATE()),

-- Bộ điều khiển và ECU
(N'Bộ điều khiển 48V 30A', 'CTRL-48V30A-001', 'Kelly', 1200000.00, 4.4, 1, GETDATE()),
(N'Bộ điều khiển 60V 40A', 'CTRL-60V40A-001', 'Sabvoton', 1500000.00, 4.5, 1, GETDATE()),
(N'Bộ điều khiển 72V 50A', 'CTRL-72V50A-001', 'Fardriver', 1800000.00, 4.6, 1, GETDATE()),
(N'ECU điều khiển trung tâm', 'ECU-MAIN-001', 'Bosch', 800000.00, 4.5, 1, GETDATE()),

-- Hệ thống sạc
(N'Bộ sạc 48V 5A', 'CHG-48V5A-001', 'Mean Well', 650000.00, 4.3, 1, GETDATE()),
(N'Bộ sạc 60V 6A', 'CHG-60V6A-001', 'Shenzhen', 750000.00, 4.4, 1, GETDATE()),
(N'Bộ sạc 72V 8A', 'CHG-72V8A-001', 'Dongguan', 850000.00, 4.2, 1, GETDATE()),
(N'Cổng sạc Type-C', 'PORT-TYPEC-001', 'USB-C', 120000.00, 4.1, 1, GETDATE()),

-- Phanh và an toàn
(N'Phanh đĩa trước 220mm', 'BRAKE-DISC220-001', 'Shimano', 350000.00, 4.4, 1, GETDATE()),
(N'Phanh đĩa sau 180mm', 'BRAKE-DISC180-001', 'Tektro', 280000.00, 4.3, 1, GETDATE()),
(N'Má phanh đĩa', 'BRAKE-PAD-001', 'EBC', 85000.00, 4.2, 1, GETDATE()),
(N'Dầu phanh DOT4', 'BRAKE-FLUID-001', 'Motul', 95000.00, 4.3, 1, GETDATE()),

-- Lốp và vành
(N'Lốp trước 3.00-10', 'TIRE-F3.00-10-001', 'Cheng Shin', 180000.00, 4.2, 1, GETDATE()),
(N'Lốp sau 3.50-10', 'TIRE-R3.50-10-001', 'Maxxis', 200000.00, 4.3, 1, GETDATE()),
(N'Vành nhôm 10 inch', 'RIM-AL10-001', 'Excel', 450000.00, 4.4, 1, GETDATE()),
(N'Ruột xe 10 inch', 'TUBE-10-001', 'Kenda', 65000.00, 4.1, 1, GETDATE()),

-- Đèn và điện tử
(N'Đèn pha LED 12V', 'LIGHT-HEAD-001', 'Osram', 120000.00, 4.3, 1, GETDATE()),
(N'Đèn hậu LED', 'LIGHT-TAIL-001', 'Philips', 85000.00, 4.2, 1, GETDATE()),
(N'Xi nhan LED', 'LIGHT-TURN-001', 'Hella', 65000.00, 4.1, 1, GETDATE()),
(N'Còi điện 12V', 'HORN-12V-001', 'Bosch', 45000.00, 4.0, 1, GETDATE()),

-- Cảm biến và đo lường
(N'Cảm biến tốc độ', 'SENS-SPEED-001', 'Hall Sensor', 85000.00, 4.2, 1, GETDATE()),
(N'Cảm biến nhiệt độ pin', 'SENS-TEMP-001', 'DS18B20', 65000.00, 4.1, 1, GETDATE()),
(N'Cảm biến dòng điện', 'SENS-CURRENT-001', 'ACS712', 95000.00, 4.3, 1, GETDATE()),
(N'Đồng hồ hiển thị', 'DISPLAY-LCD-001', 'LCD 12864', 180000.00, 4.2, 1, GETDATE()),

-- Phụ kiện và linh kiện
(N'Dây cáp điện 16AWG', 'CABLE-16AWG-001', 'Belden', 25000.00, 4.1, 1, GETDATE()),
(N'Công tắc chính', 'SWITCH-MAIN-001', 'Schneider', 45000.00, 4.2, 1, GETDATE()),
(N'Cầu chì 30A', 'FUSE-30A-001', 'Bussmann', 15000.00, 4.0, 1, GETDATE()),
(N'Relay 12V 30A', 'RELAY-12V30A-001', 'Omron', 35000.00, 4.1, 1, GETDATE());
GO

-- =====================================================
-- 6. VEHICLE MODELS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[VehicleModel] ([ModelName], [Brand], [IsActive], [CreatedAt])
VALUES 
(N'Klara S', N'VinFast', 1, GETDATE()),
(N'Feliz S', N'VinFast', 1, GETDATE()),
(N'Evo 200', N'VinFast', 1, GETDATE()),
(N'Ludo', N'VinFast', 1, GETDATE()),
(N'Theon S', N'VinFast', 1, GETDATE()),

(N'Weaver++', N'Dat Bike', 1, GETDATE()),
(N'Weaver 200', N'Dat Bike', 1, GETDATE()),
(N'Rider', N'Dat Bike', 1, GETDATE()),

(N'G5', N'Yadea', 1, GETDATE()),
(N'E3', N'Yadea', 1, GETDATE()),
(N'BuyE', N'Yadea', 1, GETDATE()),
(N'Xmen', N'Yadea', 1, GETDATE()),

(N'NewTech', N'Pega', 1, GETDATE()),
(N'Aura', N'Pega', 1, GETDATE()),
(N'eSH', N'Pega', 1, GETDATE()),

(N'Miku Max', N'DKBike', 1, GETDATE()),
(N'Roma SX', N'DKBike', 1, GETDATE()),
(N'Jeek One', N'DKBike', 1, GETDATE());
GO

-- =====================================================
-- 7. CUSTOMERS (Depends on Users)
-- =====================================================
INSERT INTO [dbo].[Customers] ([UserID], [IsGuest])
VALUES 
(6, 0),
(7, 0),
(8, 0);
GO

-- =====================================================
-- 8. STAFF (Depends on Users, ServiceCenters)
-- =====================================================
INSERT INTO [dbo].[Staff] ([CenterID], [UserID], [IsActive], [CreatedAt])
VALUES 
(1, 2, 1, GETDATE()),
(2, 3, 1, GETDATE());
GO

-- =====================================================
-- 9. TECHNICIANS (Depends on Users, ServiceCenters)
-- =====================================================
INSERT INTO [dbo].[Technicians] ([CenterID], [UserID], [Position], [Rating], [IsActive], [CreatedAt])
VALUES 
(1, 4, N'Kỹ thuật viên cao cấp', 4.8, 1, GETDATE()),
(2, 5, N'Kỹ thuật viên', 4.5, 1, GETDATE());
GO

-- =====================================================
-- 10. TIME SLOTS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[TimeSlots] ([SlotLabel], [SlotTime], [IsActive])
VALUES 
(N'8:00-10:00', '08:00:00', 1),
(N'10:00-12:00', '10:00:00', 1),
(N'14:00-16:00', '14:00:00', 1),
(N'16:00-18:00', '16:00:00', 1);
GO

-- =====================================================
-- 11. VEHICLES (Depends on Customers, VehicleModels)
-- =====================================================
INSERT INTO [dbo].[Vehicles] ([CustomerID], [ModelID], [LicensePlate], [VIN], [Color], [PurchaseDate], [CreatedAt])
VALUES 
(1, 1, N'30A-12345', '1HGBH41JXMN109186', N'Đen', '2023-01-15', GETDATE()),
(2, 2, N'30B-67890', '1HGBH41JXMN109187', N'Trắng', '2023-03-20', GETDATE()),
(3, 4, N'30C-11111', '1HGBH41JXMN109188', N'Xanh', '2023-05-10', GETDATE());
GO

-- =====================================================
-- 12. BOOKINGS (Depends on Customers, Centers, Slots, Services, Vehicles)
-- =====================================================
INSERT INTO [dbo].[Bookings] ([CustomerID], [CenterID], [SlotID], [ServiceID], [VehicleID], [BookingDate], [SpecialRequests], [Status], [TotalCost], [CreatedAt], [UpdatedAt])
VALUES 
(1, 1, 1, 1, 1, '2024-01-15', N'Kiểm tra kỹ hệ thống làm mát', 'CONFIRMED', 500000.00, GETDATE(), GETDATE()),
(2, 2, 2, 2, 2, '2024-01-16', N'Thay pin chính hãng', 'PENDING', 15000000.00, GETDATE(), GETDATE()),
(3, 1, 3, 4, 3, '2024-01-17', N'Kiểm tra an toàn tổng thể', 'CONFIRMED', 300000.00, GETDATE(), GETDATE());
GO

-- =====================================================
-- 13. WORK ORDERS (Depends on Bookings, Technicians, etc.)
-- =====================================================
INSERT INTO [dbo].[WorkOrders] ([BookingID], [TechnicianID], [CustomerID], [VehicleID], [CenterID], [ServiceID], [Status], [LicensePlate], [CreatedAt], [UpdatedAt])
VALUES 
(1, 1, 1, 1, 1, 1, 'IN_PROGRESS', N'30A-12345', GETDATE(), GETDATE()),
(2, 2, 2, 2, 2, 2, 'NOT_STARTED', N'30B-67890', GETDATE(), GETDATE()),
(3, 1, 3, 3, 1, 4, 'COMPLETED', N'30C-11111', GETDATE(), GETDATE());
GO

-- =====================================================
-- 14. PROMOTIONS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[Promotions] ([Code], [Description], [DiscountValue], [DiscountType], [MinOrderAmount], [MaxDiscount], [StartDate], [EndDate], [Status], [UsageLimit], [UsageCount], [CreatedAt], [UpdatedAt])
VALUES 
-- Promotions bắt đầu từ hôm nay (10/10/2025)
(N'FALL2025', N'Khuyến mãi mùa thu 2025 - Giảm giá dịch vụ bảo dưỡng xe điện', 15.00, N'PERCENT', 800000.00, 500000.00, '2025-10-10', '2025-11-30', N'ACTIVE', 1000, 0, GETDATE(), GETDATE()),
(N'NEWCUSTOMER', N'Ưu đãi đặc biệt cho khách hàng mới - Giảm giá lần đầu sử dụng dịch vụ', 20.00, N'PERCENT', 500000.00, 300000.00, '2025-10-10', '2025-12-31', N'ACTIVE', 500, 0, GETDATE(), GETDATE()),
(N'VIPMEMBER', N'Chương trình thành viên VIP - Ưu đãi cao cấp cho khách hàng thân thiết', 25.00, N'PERCENT', 2000000.00, 1000000.00, '2025-10-10', '2026-10-09', N'ACTIVE', 200, 0, GETDATE(), GETDATE()),
(N'ELECTRICCAR', N'Khuyến mãi chuyên biệt cho xe điện - Bảo dưỡng và sửa chữa', 18.00, N'PERCENT', 1200000.00, 600000.00, '2025-10-10', '2025-12-31', N'ACTIVE', 800, 0, GETDATE(), GETDATE()),
(N'MAINTENANCE', N'Gói bảo dưỡng định kỳ - Chăm sóc xe điện toàn diện', 12.00, N'PERCENT', 1000000.00, 400000.00, '2025-10-10', '2025-11-30', N'ACTIVE', 600, 0, GETDATE(), GETDATE()),

-- Promotions sắp bắt đầu (chuyển thành ACTIVE với ngày bắt đầu trong tương lai)
(N'WINTER2025', N'Chương trình mùa đông 2025 - Dịch vụ sửa chữa xe điện mùa lạnh', 22.00, N'PERCENT', 1500000.00, 800000.00, '2025-12-01', '2026-02-28', N'ACTIVE', 400, 0, GETDATE(), GETDATE()),
(N'NEWYEAR2026', N'Khuyến mãi năm mới 2026 - Đầu năm mới, xe mới', 30.00, N'PERCENT', 3000000.00, 1500000.00, '2026-01-01', '2026-01-31', N'ACTIVE', 100, 0, GETDATE(), GETDATE()),
(N'SPRING2026', N'Chương trình mùa xuân 2026 - Khởi động lại xe điện sau mùa đông', 16.00, N'PERCENT', 1000000.00, 500000.00, '2026-03-01', '2026-05-31', N'ACTIVE', 500, 0, GETDATE(), GETDATE()),

-- Promotions đã kết thúc (để test)
(N'SUMMER2025', N'Chương trình mùa hè 2025 - Bảo dưỡng xe điện mùa nóng', 14.00, N'PERCENT', 800000.00, 400000.00, '2025-06-01', '2025-09-30', N'EXPIRED', 800, 800, GETDATE(), GETDATE()),
(N'BLACKFRIDAY2024', N'Khuyến mãi Black Friday 2024 - Siêu giảm giá cuối năm', 40.00, N'PERCENT', 2000000.00, 1000000.00, '2024-11-24', '2024-11-30', N'EXPIRED', 300, 300, GETDATE(), GETDATE()),
(N'CHRISTMAS2024', N'Chương trình Giáng sinh 2024 - Quà tặng đặc biệt', 35.00, N'PERCENT', 1500000.00, 800000.00, '2024-12-20', '2024-12-25', N'EXPIRED', 200, 200, GETDATE(), GETDATE()),

-- Promotions đã hủy (để test)
(N'FLASH2024', N'Khuyến mãi flash sale 2024 - Giảm giá trong thời gian ngắn', 50.00, N'PERCENT', 500000.00, 300000.00, '2024-10-01', '2024-10-07', N'CANCELLED', 100, 45, GETDATE(), GETDATE()),
(N'TEST2024', N'Chương trình test 2024 - Khuyến mãi thử nghiệm (đã hủy)', 10.00, N'PERCENT', 100000.00, 50000.00, '2024-09-01', '2024-09-30', N'CANCELLED', 50, 12, GETDATE(), GETDATE()),

-- Promotions với giá trị cố định
(N'SAVE100K', N'Tiết kiệm 100,000 VNĐ - Giảm giá cố định cho dịch vụ bảo dưỡng', 100000.00, N'FIXED', 500000.00, 100000.00, '2025-10-10', '2025-12-31', N'ACTIVE', 1000, 0, GETDATE(), GETDATE()),
(N'SAVE200K', N'Tiết kiệm 200,000 VNĐ - Giảm giá cố định cho dịch vụ sửa chữa', 200000.00, N'FIXED', 1000000.00, 200000.00, '2025-10-10', '2025-11-30', N'ACTIVE', 500, 0, GETDATE(), GETDATE()),
(N'SAVE500K', N'Tiết kiệm 500,000 VNĐ - Giảm giá cố định cho dịch vụ cao cấp', 500000.00, N'FIXED', 3000000.00, 500000.00, '2025-10-10', '2026-03-31', N'ACTIVE', 100, 0, GETDATE(), GETDATE());
GO

-- =====================================================
-- 15. ORDERS (Depends on Customers)
-- =====================================================
INSERT INTO [dbo].[Orders] ([CustomerID], [Status], [CreatedAt], [UpdatedAt])
VALUES 
(1, N'COMPLETED', GETDATE(), GETDATE()),
(2, N'PENDING', GETDATE(), GETDATE());
GO

-- =====================================================
-- 16. INVOICES (Depends on Customers, WorkOrders, Orders)
-- =====================================================
INSERT INTO [dbo].[Invoices] ([CustomerID], [WorkOrderID], [OrderID], [Email], [Phone], [Status], [CreatedAt])
VALUES 
(1, 1, NULL, 'customer1@email.com', '0901234572', N'PAID', GETDATE()),
(2, 2, 2, 'customer2@email.com', '0901234573', N'PENDING', GETDATE()),
(3, 3, NULL, 'customer3@email.com', '0901234574', N'PAID', GETDATE());
GO

-- =====================================================
-- 17. PAYMENTS (Depends on Invoices)
-- =====================================================
INSERT INTO [dbo].[Payments] ([InvoiceID], [PaymentCode], [PaymentMethod], [Amount], [Status], [CreatedAt], [PaidAt])
VALUES 
(1, N'PAY20240115001', N'PAYOS', 500000, N'PAID', GETDATE(), GETDATE()),
(3, N'PAY20240117001', N'CASH', 300000, N'PAID', GETDATE(), GETDATE());
GO

-- =====================================================
-- 18. VEHICLE MODEL PARTS (Depends on VehicleModels, Parts)
-- =====================================================
INSERT INTO [dbo].[VehicleModelParts] ([ModelID], [PartID], [IsCompatible], [CreatedAt])
VALUES 
-- VinFast Klara S (ModelID=1) - Xe tay ga 48V
(1, 1, 1, GETDATE()),   -- Pin Lithium-ion 48V 20Ah
(1, 5, 1, GETDATE()),   -- Động cơ điện 48V 1000W
(1, 9, 1, GETDATE()),   -- Bộ điều khiển 48V 30A
(1, 13, 1, GETDATE()),  -- Bộ sạc 48V 5A
(1, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(1, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(1, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(1, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(1, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- VinFast Feliz S (ModelID=2) - Xe tay ga 60V
(2, 2, 1, GETDATE()),   -- Pin Lithium-ion 60V 30Ah
(2, 6, 1, GETDATE()),   -- Động cơ điện 60V 1500W
(2, 10, 1, GETDATE()),  -- Bộ điều khiển 60V 40A
(2, 14, 1, GETDATE()),  -- Bộ sạc 60V 6A
(2, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(2, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(2, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(2, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(2, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- VinFast Evo 200 (ModelID=3) - Xe số 72V
(3, 3, 1, GETDATE()),   -- Pin Lithium-ion 72V 40Ah
(3, 7, 1, GETDATE()),   -- Động cơ điện 72V 2000W
(3, 11, 1, GETDATE()),  -- Bộ điều khiển 72V 50A
(3, 15, 1, GETDATE()),  -- Bộ sạc 72V 8A
(3, 8, 1, GETDATE()),   -- Bộ truyền động xích
(3, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(3, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(3, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(3, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(3, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- VinFast Ludo (ModelID=4) - Xe tay ga 48V
(4, 1, 1, GETDATE()),   -- Pin Lithium-ion 48V 20Ah
(4, 5, 1, GETDATE()),   -- Động cơ điện 48V 1000W
(4, 9, 1, GETDATE()),   -- Bộ điều khiển 48V 30A
(4, 13, 1, GETDATE()),  -- Bộ sạc 48V 5A
(4, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(4, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(4, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(4, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(4, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- VinFast Theon S (ModelID=5) - Xe tay ga 60V cao cấp
(5, 2, 1, GETDATE()),   -- Pin Lithium-ion 60V 30Ah
(5, 6, 1, GETDATE()),   -- Động cơ điện 60V 1500W
(5, 10, 1, GETDATE()),  -- Bộ điều khiển 60V 40A
(5, 14, 1, GETDATE()),  -- Bộ sạc 60V 6A
(5, 12, 1, GETDATE()),  -- ECU điều khiển trung tâm
(5, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(5, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(5, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(5, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(5, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- Dat Bike Weaver++ (ModelID=6) - Xe số 72V
(6, 3, 1, GETDATE()),   -- Pin Lithium-ion 72V 40Ah
(6, 7, 1, GETDATE()),   -- Động cơ điện 72V 2000W
(6, 11, 1, GETDATE()),  -- Bộ điều khiển 72V 50A
(6, 15, 1, GETDATE()),  -- Bộ sạc 72V 8A
(6, 8, 1, GETDATE()),   -- Bộ truyền động xích
(6, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(6, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(6, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(6, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(6, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- Dat Bike Weaver 200 (ModelID=7) - Xe số 60V
(7, 2, 1, GETDATE()),   -- Pin Lithium-ion 60V 30Ah
(7, 6, 1, GETDATE()),   -- Động cơ điện 60V 1500W
(7, 10, 1, GETDATE()),  -- Bộ điều khiển 60V 40A
(7, 14, 1, GETDATE()),  -- Bộ sạc 60V 6A
(7, 8, 1, GETDATE()),   -- Bộ truyền động xích
(7, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(7, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(7, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(7, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(7, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- Dat Bike Rider (ModelID=8) - Xe tay ga 48V
(8, 1, 1, GETDATE()),   -- Pin Lithium-ion 48V 20Ah
(8, 5, 1, GETDATE()),   -- Động cơ điện 48V 1000W
(8, 9, 1, GETDATE()),   -- Bộ điều khiển 48V 30A
(8, 13, 1, GETDATE()),  -- Bộ sạc 48V 5A
(8, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(8, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(8, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(8, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(8, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- Yadea G5 (ModelID=9) - Xe tay ga 48V
(9, 1, 1, GETDATE()),   -- Pin Lithium-ion 48V 20Ah
(9, 5, 1, GETDATE()),   -- Động cơ điện 48V 1000W
(9, 9, 1, GETDATE()),   -- Bộ điều khiển 48V 30A
(9, 13, 1, GETDATE()),  -- Bộ sạc 48V 5A
(9, 17, 1, GETDATE()),  -- Lốp trước 3.00-10
(9, 18, 1, GETDATE()),  -- Lốp sau 3.50-10
(9, 21, 1, GETDATE()),  -- Đèn pha LED 12V
(9, 25, 1, GETDATE()),  -- Cảm biến tốc độ
(9, 29, 1, GETDATE()),  -- Dây cáp điện 16AWG

-- Yadea E3 (ModelID=10) - Xe tay ga 60V
(10, 2, 1, GETDATE()),  -- Pin Lithium-ion 60V 30Ah
(10, 6, 1, GETDATE()),  -- Động cơ điện 60V 1500W
(10, 10, 1, GETDATE()), -- Bộ điều khiển 60V 40A
(10, 14, 1, GETDATE()), -- Bộ sạc 60V 6A
(10, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(10, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(10, 21, 1, GETDATE()), -- Đèn pha LED 12V
(10, 25, 1, GETDATE()), -- Cảm biến tốc độ
(10, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- Yadea BuyE (ModelID=11) - Xe tay ga 48V
(11, 1, 1, GETDATE()),  -- Pin Lithium-ion 48V 20Ah
(11, 5, 1, GETDATE()),  -- Động cơ điện 48V 1000W
(11, 9, 1, GETDATE()),  -- Bộ điều khiển 48V 30A
(11, 13, 1, GETDATE()), -- Bộ sạc 48V 5A
(11, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(11, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(11, 21, 1, GETDATE()), -- Đèn pha LED 12V
(11, 25, 1, GETDATE()), -- Cảm biến tốc độ
(11, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- Yadea Xmen (ModelID=12) - Xe số 72V
(12, 3, 1, GETDATE()),  -- Pin Lithium-ion 72V 40Ah
(12, 7, 1, GETDATE()),  -- Động cơ điện 72V 2000W
(12, 11, 1, GETDATE()), -- Bộ điều khiển 72V 50A
(12, 15, 1, GETDATE()), -- Bộ sạc 72V 8A
(12, 8, 1, GETDATE()),  -- Bộ truyền động xích
(12, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(12, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(12, 21, 1, GETDATE()), -- Đèn pha LED 12V
(12, 25, 1, GETDATE()), -- Cảm biến tốc độ
(12, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- Pega NewTech (ModelID=13) - Xe tay ga 48V
(13, 1, 1, GETDATE()),  -- Pin Lithium-ion 48V 20Ah
(13, 5, 1, GETDATE()),  -- Động cơ điện 48V 1000W
(13, 9, 1, GETDATE()),  -- Bộ điều khiển 48V 30A
(13, 13, 1, GETDATE()), -- Bộ sạc 48V 5A
(13, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(13, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(13, 21, 1, GETDATE()), -- Đèn pha LED 12V
(13, 25, 1, GETDATE()), -- Cảm biến tốc độ
(13, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- Pega Aura (ModelID=14) - Xe tay ga 60V
(14, 2, 1, GETDATE()),  -- Pin Lithium-ion 60V 30Ah
(14, 6, 1, GETDATE()),  -- Động cơ điện 60V 1500W
(14, 10, 1, GETDATE()), -- Bộ điều khiển 60V 40A
(14, 14, 1, GETDATE()), -- Bộ sạc 60V 6A
(14, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(14, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(14, 21, 1, GETDATE()), -- Đèn pha LED 12V
(14, 25, 1, GETDATE()), -- Cảm biến tốc độ
(14, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- Pega eSH (ModelID=15) - Xe tay ga 48V
(15, 1, 1, GETDATE()),  -- Pin Lithium-ion 48V 20Ah
(15, 5, 1, GETDATE()),  -- Động cơ điện 48V 1000W
(15, 9, 1, GETDATE()),  -- Bộ điều khiển 48V 30A
(15, 13, 1, GETDATE()), -- Bộ sạc 48V 5A
(15, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(15, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(15, 21, 1, GETDATE()), -- Đèn pha LED 12V
(15, 25, 1, GETDATE()), -- Cảm biến tốc độ
(15, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- DKBike Miku Max (ModelID=16) - Xe tay ga 60V
(16, 2, 1, GETDATE()),  -- Pin Lithium-ion 60V 30Ah
(16, 6, 1, GETDATE()),  -- Động cơ điện 60V 1500W
(16, 10, 1, GETDATE()), -- Bộ điều khiển 60V 40A
(16, 14, 1, GETDATE()), -- Bộ sạc 60V 6A
(16, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(16, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(16, 21, 1, GETDATE()), -- Đèn pha LED 12V
(16, 25, 1, GETDATE()), -- Cảm biến tốc độ
(16, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- DKBike Roma SX (ModelID=17) - Xe số 72V
(17, 3, 1, GETDATE()),  -- Pin Lithium-ion 72V 40Ah
(17, 7, 1, GETDATE()),  -- Động cơ điện 72V 2000W
(17, 11, 1, GETDATE()), -- Bộ điều khiển 72V 50A
(17, 15, 1, GETDATE()), -- Bộ sạc 72V 8A
(17, 8, 1, GETDATE()),  -- Bộ truyền động xích
(17, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(17, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(17, 21, 1, GETDATE()), -- Đèn pha LED 12V
(17, 25, 1, GETDATE()), -- Cảm biến tốc độ
(17, 29, 1, GETDATE()), -- Dây cáp điện 16AWG

-- DKBike Jeek One (ModelID=18) - Xe tay ga 48V
(18, 1, 1, GETDATE()),  -- Pin Lithium-ion 48V 20Ah
(18, 5, 1, GETDATE()),  -- Động cơ điện 48V 1000W
(18, 9, 1, GETDATE()),  -- Bộ điều khiển 48V 30A
(18, 13, 1, GETDATE()), -- Bộ sạc 48V 5A
(18, 17, 1, GETDATE()), -- Lốp trước 3.00-10
(18, 18, 1, GETDATE()), -- Lốp sau 3.50-10
(18, 21, 1, GETDATE()), -- Đèn pha LED 12V
(18, 25, 1, GETDATE()), -- Cảm biến tốc độ
(18, 29, 1, GETDATE()); -- Dây cáp điện 16AWG
GO

-- 19. SERVICE PARTS removed (replaced by checklist templates)

-- =====================================================
-- 20. SERVICE REQUIRED SKILLS (Depends on Services, Skills)
-- =====================================================
INSERT INTO [dbo].[ServiceRequiredSkills] ([ServiceID], [SkillID])
VALUES 
(1, 1),
(1, 4),
(2, 2),
(3, 1),
(4, 4),
(5, 5);
GO

-- =====================================================
-- 21. TECHNICIAN SKILLS (Depends on Technicians, Skills)
-- =====================================================
INSERT INTO [dbo].[TechnicianSkills] ([TechnicianID], [SkillID])
VALUES 
-- Technician cao cấp (ID=1): Kỹ năng đầy đủ
(1, 1),  -- Kiểm tra và sửa chữa pin
(1, 2),  -- Kỹ năng thay ắc qui
(1, 3),  -- Kỹ năng sửa chữa động cơ
(1, 4),  -- Kỹ năng sửa chữa bộ điều khiển
(1, 9),  -- Kỹ năng bảo dưỡng tổng quát
(1, 10), -- Kỹ năng chẩn đoán lỗi bằng phần mềm
(1, 19), -- Sử dụng đồng hồ vạn năng thành thạo

-- Technician trung cấp (ID=2): Kỹ năng cơ bản
(2, 1),  -- Kiểm tra và sửa chữa pin
(2, 4),  -- Kỹ năng sửa chữa bộ điều khiển
(2, 5),  -- Kỹ năng bảo dưỡng hệ thống phanh
(2, 9),  -- Kỹ năng bảo dưỡng tổng quát
(2, 19); -- Sử dụng đồng hồ vạn năng thành thạo
GO

-- =====================================================
-- 22. TECHNICIAN TIME SLOTS (Depends on Technicians, TimeSlots)
-- =====================================================
INSERT INTO [dbo].[TechnicianTimeSlots] ([TechnicianID], [SlotID], [WorkDate], [IsAvailable], [BookingID], [Notes], [CreatedAt])
VALUES 
(1, 1, '2024-01-15', 0, 1, N'Đang thực hiện bảo dưỡng', GETDATE()),
(1, 2, '2024-01-15', 1, NULL, NULL, GETDATE()),
(2, 1, '2024-01-16', 0, 2, N'Chuẩn bị thay pin', GETDATE()),
(2, 2, '2024-01-16', 1, NULL, NULL, GETDATE());
GO

-- =====================================================
-- 23. WORK ORDER PARTS (Depends on WorkOrders, Parts)
-- =====================================================
INSERT INTO [dbo].[WorkOrderParts] ([WorkOrderID], [PartID], [VehicleModelPartID], [QuantityUsed], [UnitCost])
VALUES 
(1, 5, NULL, 2, 500000.00),
(1, 6, NULL, 1, 800000.00),
(2, 1, 1, 1, 15000000.00),
(3, 7, NULL, 1, 1200000.00);
GO

-- =====================================================
-- 24. INVENTORY (Depends on ServiceCenters)
-- =====================================================
INSERT INTO [dbo].[Inventory] ([CenterID], [LastUpdated])
VALUES 
(1, GETDATE()),  -- Trung tâm Quận 1
(2, GETDATE()),  -- Trung tâm Quận 3  
(4, GETDATE()),  -- EV Xô Viết Nghệ Tĩnh
(5, GETDATE()),  -- EV Lê Văn Việt
(6, GETDATE()),  -- EV Huỳnh Tấn Phát
(7, GETDATE()),  -- EV Hoàng Văn Thụ
GO

-- =====================================================
-- 25. INVENTORY PARTS (Depends on Inventory, Parts)
-- =====================================================
INSERT INTO [dbo].[InventoryParts] ([InventoryID], [PartID], [CurrentStock], [MinimumStock], [LastUpdated])
VALUES 
-- Inventory cho Center 1 (Quận 1) - Kho đầy đủ - Phụ tùng xe máy điện
(1, 1, 12, 3, GETDATE()),   -- Pin Lithium-ion 48V 20Ah
(1, 2, 8, 2, GETDATE()),    -- Pin Lithium-ion 60V 30Ah
(1, 3, 6, 2, GETDATE()),    -- Pin Lithium-ion 72V 40Ah
(1, 4, 15, 5, GETDATE()),   -- Ắc quy chì 12V 7Ah
(1, 5, 10, 3, GETDATE()),   -- Động cơ điện 48V 1000W
(1, 6, 8, 2, GETDATE()),    -- Động cơ điện 60V 1500W
(1, 7, 6, 2, GETDATE()),    -- Động cơ điện 72V 2000W
(1, 8, 12, 4, GETDATE()),   -- Bộ truyền động xích
(1, 9, 15, 5, GETDATE()),   -- Bộ điều khiển 48V 30A
(1, 10, 12, 4, GETDATE()),  -- Bộ điều khiển 60V 40A
(1, 11, 8, 3, GETDATE()),   -- Bộ điều khiển 72V 50A
(1, 12, 6, 2, GETDATE()),   -- ECU điều khiển trung tâm
(1, 13, 20, 6, GETDATE()),  -- Bộ sạc 48V 5A
(1, 14, 15, 5, GETDATE()),  -- Bộ sạc 60V 6A
(1, 15, 10, 3, GETDATE()),  -- Bộ sạc 72V 8A
(1, 16, 8, 2, GETDATE()),   -- Cổng sạc Type-C
(1, 17, 25, 8, GETDATE()),  -- Phanh đĩa trước 220mm
(1, 18, 25, 8, GETDATE()),  -- Phanh đĩa sau 180mm
(1, 19, 30, 10, GETDATE()), -- Má phanh đĩa
(1, 20, 20, 6, GETDATE()),  -- Dầu phanh DOT4
(1, 21, 25, 8, GETDATE()),  -- Lốp trước 3.00-10
(1, 22, 25, 8, GETDATE()),  -- Lốp sau 3.50-10
(1, 23, 15, 5, GETDATE()),  -- Vành nhôm 10 inch
(1, 24, 40, 15, GETDATE()), -- Ruột xe 10 inch
(1, 25, 30, 10, GETDATE()), -- Đèn pha LED 12V
(1, 26, 25, 8, GETDATE()),  -- Đèn hậu LED
(1, 27, 20, 6, GETDATE()),  -- Xi nhan LED
(1, 28, 15, 5, GETDATE()),  -- Còi điện 12V
(1, 29, 40, 15, GETDATE()), -- Cảm biến tốc độ
(1, 30, 35, 12, GETDATE()), -- Cảm biến nhiệt độ pin
(1, 31, 30, 10, GETDATE()), -- Cảm biến dòng điện
(1, 32, 20, 6, GETDATE()),  -- Đồng hồ hiển thị
(1, 33, 50, 20, GETDATE()), -- Dây cáp điện 16AWG
(1, 34, 25, 8, GETDATE()),  -- Công tắc chính
(1, 35, 40, 15, GETDATE()), -- Cầu chì 30A
(1, 36, 30, 10, GETDATE()), -- Relay 12V 30A

-- Inventory cho Center 2 (Quận 3) - Kho trung bình
(2, 1, 8, 3, GETDATE()),    -- Pin Lithium-ion 48V 20Ah
(2, 2, 5, 2, GETDATE()),    -- Pin Lithium-ion 60V 30Ah
(2, 3, 3, 2, GETDATE()),    -- Pin Lithium-ion 72V 40Ah
(2, 4, 10, 5, GETDATE()),   -- Ắc quy chì 12V 7Ah
(2, 5, 6, 3, GETDATE()),    -- Động cơ điện 48V 1000W
(2, 6, 4, 2, GETDATE()),    -- Động cơ điện 60V 1500W
(2, 7, 3, 2, GETDATE()),    -- Động cơ điện 72V 2000W
(2, 8, 8, 4, GETDATE()),    -- Bộ truyền động xích
(2, 9, 10, 5, GETDATE()),   -- Bộ điều khiển 48V 30A
(2, 10, 8, 4, GETDATE()),   -- Bộ điều khiển 60V 40A
(2, 11, 5, 3, GETDATE()),   -- Bộ điều khiển 72V 50A
(2, 12, 3, 2, GETDATE()),   -- ECU điều khiển trung tâm
(2, 13, 15, 6, GETDATE()),  -- Bộ sạc 48V 5A
(2, 14, 10, 5, GETDATE()),  -- Bộ sạc 60V 6A
(2, 15, 6, 3, GETDATE()),   -- Bộ sạc 72V 8A
(2, 16, 5, 2, GETDATE()),   -- Cổng sạc Type-C
(2, 17, 15, 8, GETDATE()),  -- Phanh đĩa trước 220mm
(2, 18, 15, 8, GETDATE()),  -- Phanh đĩa sau 180mm
(2, 19, 20, 10, GETDATE()), -- Má phanh đĩa
(2, 20, 12, 6, GETDATE()),  -- Dầu phanh DOT4
(2, 21, 15, 8, GETDATE()),  -- Lốp trước 3.00-10
(2, 22, 15, 8, GETDATE()),  -- Lốp sau 3.50-10
(2, 23, 8, 5, GETDATE()),   -- Vành nhôm 10 inch
(2, 24, 25, 15, GETDATE()), -- Ruột xe 10 inch
(2, 25, 20, 10, GETDATE()), -- Đèn pha LED 12V
(2, 26, 15, 8, GETDATE()),  -- Đèn hậu LED
(2, 27, 12, 6, GETDATE()),  -- Xi nhan LED
(2, 28, 10, 5, GETDATE()),  -- Còi điện 12V
(2, 29, 25, 15, GETDATE()), -- Cảm biến tốc độ
(2, 30, 20, 12, GETDATE()), -- Cảm biến nhiệt độ pin
(2, 31, 18, 10, GETDATE()), -- Cảm biến dòng điện
(2, 32, 12, 6, GETDATE()),  -- Đồng hồ hiển thị
(2, 33, 30, 20, GETDATE()), -- Dây cáp điện 16AWG
(2, 34, 15, 8, GETDATE()),  -- Công tắc chính
(2, 35, 25, 15, GETDATE()), -- Cầu chì 30A
(2, 36, 20, 10, GETDATE()), -- Relay 12V 30A

-- Inventory cho Center 4-7 (Các trung tâm khác) - Kho cơ bản
-- Center 4 (EV Xô Viết Nghệ Tĩnh) - Phụ tùng cơ bản
(4, 1, 5, 3, GETDATE()),    -- Pin Lithium-ion 48V 20Ah
(4, 4, 8, 5, GETDATE()),    -- Ắc quy chì 12V 7Ah
(4, 5, 4, 3, GETDATE()),    -- Động cơ điện 48V 1000W
(4, 9, 8, 5, GETDATE()),    -- Bộ điều khiển 48V 30A
(4, 13, 10, 6, GETDATE()),  -- Bộ sạc 48V 5A
(4, 17, 10, 8, GETDATE()),  -- Phanh đĩa trước 220mm
(4, 21, 15, 8, GETDATE()),  -- Lốp trước 3.00-10
(4, 25, 20, 10, GETDATE()), -- Đèn pha LED 12V
(4, 29, 25, 15, GETDATE()), -- Cảm biến tốc độ
(4, 33, 30, 20, GETDATE()), -- Dây cáp điện 16AWG

-- Center 5 (EV Lê Văn Việt) - Phụ tùng cơ bản
(5, 1, 5, 3, GETDATE()),    -- Pin Lithium-ion 48V 20Ah
(5, 4, 8, 5, GETDATE()),    -- Ắc quy chì 12V 7Ah
(5, 5, 4, 3, GETDATE()),    -- Động cơ điện 48V 1000W
(5, 9, 8, 5, GETDATE()),    -- Bộ điều khiển 48V 30A
(5, 13, 10, 6, GETDATE()),  -- Bộ sạc 48V 5A
(5, 17, 10, 8, GETDATE()),  -- Phanh đĩa trước 220mm
(5, 21, 15, 8, GETDATE()),  -- Lốp trước 3.00-10
(5, 25, 20, 10, GETDATE()), -- Đèn pha LED 12V
(5, 29, 25, 15, GETDATE()), -- Cảm biến tốc độ
(5, 33, 30, 20, GETDATE()), -- Dây cáp điện 16AWG

-- Center 6 (EV Huỳnh Tấn Phát) - Phụ tùng cơ bản
(6, 1, 5, 3, GETDATE()),    -- Pin Lithium-ion 48V 20Ah
(6, 4, 8, 5, GETDATE()),    -- Ắc quy chì 12V 7Ah
(6, 5, 4, 3, GETDATE()),    -- Động cơ điện 48V 1000W
(6, 9, 8, 5, GETDATE()),    -- Bộ điều khiển 48V 30A
(6, 13, 10, 6, GETDATE()),  -- Bộ sạc 48V 5A
(6, 17, 10, 8, GETDATE()),  -- Phanh đĩa trước 220mm
(6, 21, 15, 8, GETDATE()),  -- Lốp trước 3.00-10
(6, 25, 20, 10, GETDATE()), -- Đèn pha LED 12V
(6, 29, 25, 15, GETDATE()), -- Cảm biến tốc độ
(6, 33, 30, 20, GETDATE()); -- Dây cáp điện 16AWG
GO

-- =====================================================
-- 26. SYSTEM SETTINGS (No dependencies)
-- =====================================================
INSERT INTO [dbo].[SystemSettings] ([SettingKey], [Description], [SettingValue], [UpdatedAt])
VALUES 
(N'MAINTENANCE_REMINDER_DAYS', N'Số ngày nhắc nhở bảo dưỡng trước', N'30', GETDATE()),
(N'BOOKING_CANCELLATION_HOURS', N'Số giờ hủy booking trước', N'24', GETDATE()),
(N'MAX_BOOKING_PER_DAY', N'Số booking tối đa mỗi ngày', N'50', GETDATE()),
(N'EMAIL_SMTP_HOST', N'SMTP Host cho email', N'smtp.gmail.com', GETDATE()),
(N'EMAIL_SMTP_PORT', N'SMTP Port', N'587', GETDATE());
GO

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================
PRINT 'Sample data insertion completed successfully!';
PRINT 'Total Users: ' + CAST((SELECT COUNT(*) FROM [dbo].[Users]) AS VARCHAR(10));
PRINT 'Total Service Centers: ' + CAST((SELECT COUNT(*) FROM [dbo].[ServiceCenters]) AS VARCHAR(10));
PRINT 'Total Customers: ' + CAST((SELECT COUNT(*) FROM [dbo].[Customers]) AS VARCHAR(10));
PRINT 'Total Bookings: ' + CAST((SELECT COUNT(*) FROM [dbo].[Bookings]) AS VARCHAR(10));
PRINT 'Total Work Orders: ' + CAST((SELECT COUNT(*) FROM [dbo].[WorkOrders]) AS VARCHAR(10));
PRINT 'Total Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions]) AS VARCHAR(10));
PRINT 'Total Invoices: ' + CAST((SELECT COUNT(*) FROM [dbo].[Invoices]) AS VARCHAR(10));
PRINT 'Total Payments: ' + CAST((SELECT COUNT(*) FROM [dbo].[Payments]) AS VARCHAR(10));
PRINT '';
PRINT '=== PROMOTION STATISTICS ===';
PRINT 'Active Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'ACTIVE') AS VARCHAR(10));
PRINT 'Expired Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'EXPIRED') AS VARCHAR(10));
PRINT 'Cancelled Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'CANCELLED') AS VARCHAR(10));
PRINT 'Percent Discount Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [DiscountType] = 'PERCENT') AS VARCHAR(10));
PRINT 'Fixed Discount Promotions: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [DiscountType] = 'FIXED') AS VARCHAR(10));
PRINT '';
PRINT '=== CURRENT DATE: 2025-10-10 ===';
PRINT 'Promotions active today: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'ACTIVE' AND GETDATE() BETWEEN [StartDate] AND [EndDate]) AS VARCHAR(10));
PRINT 'Promotions starting soon: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'ACTIVE' AND [StartDate] > GETDATE()) AS VARCHAR(10));
PRINT 'Promotions ending soon: ' + CAST((SELECT COUNT(*) FROM [dbo].[Promotions] WHERE [Status] = 'ACTIVE' AND [EndDate] BETWEEN GETDATE() AND DATEADD(day, 30, GETDATE())) AS VARCHAR(10));
GO
