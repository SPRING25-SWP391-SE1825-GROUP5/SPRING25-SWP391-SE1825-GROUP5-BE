-- Script để xóa cột WorkOrderID khỏi bảng Invoices
-- WorkOrderID không còn được sử dụng trong hệ thống mới

USE EVServiceCenter;
GO

-- Kiểm tra xem cột WorkOrderID có tồn tại trong bảng Invoices không
IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('dbo.Invoices')
    AND name = 'WorkOrderID'
)
BEGIN
    -- Kiểm tra xem có foreign key constraint nào liên quan đến cột này không
    DECLARE @fkName NVARCHAR(128);
    SELECT TOP 1 @fkName = fk.name
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON fkc.parent_column_id = c.column_id AND fkc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('dbo.Invoices')
    AND c.name = 'WorkOrderID';

    -- Nếu có foreign key, xóa foreign key trước
    IF @fkName IS NOT NULL
    BEGIN
        DECLARE @sqlDropFK NVARCHAR(MAX) = 'ALTER TABLE dbo.Invoices DROP CONSTRAINT ' + QUOTENAME(@fkName);
        EXEC sp_executesql @sqlDropFK;
        PRINT 'Đã xóa foreign key constraint: ' + @fkName;
    END

    -- Kiểm tra xem có index nào trên cột này không
    DECLARE @indexName NVARCHAR(128);
    SELECT TOP 1 @indexName = i.name
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
    WHERE c.object_id = OBJECT_ID('dbo.Invoices')
    AND c.name = 'WorkOrderID'
    AND i.name IS NOT NULL;

    -- Nếu có index, xóa index trước
    IF @indexName IS NOT NULL
    BEGIN
        DECLARE @sqlDropIndex NVARCHAR(MAX) = 'DROP INDEX ' + QUOTENAME(@indexName) + ' ON dbo.Invoices';
        EXEC sp_executesql @sqlDropIndex;
        PRINT 'Đã xóa index: ' + @indexName;
    END

    -- Xóa cột WorkOrderID
    ALTER TABLE dbo.Invoices
    DROP COLUMN WorkOrderID;

    PRINT 'Đã xóa cột WorkOrderID khỏi bảng Invoices.';
END
ELSE
BEGIN
    PRINT 'Cột WorkOrderID không tồn tại trong bảng Invoices.';
END
GO

-- Xác nhận lại cấu trúc bảng Invoices sau khi xóa
PRINT 'Cấu trúc bảng Invoices sau khi xóa WorkOrderID:';
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    c.column_id AS ColumnOrder
FROM sys.columns c
INNER JOIN sys.types t ON c.system_type_id = t.system_type_id
WHERE c.object_id = OBJECT_ID('dbo.Invoices')
ORDER BY c.column_id;
GO

PRINT 'Hoàn thành! Đã xóa cột WorkOrderID khỏi bảng Invoices.';
GO

