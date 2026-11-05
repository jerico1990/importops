USE ImportOpsDB;
GO
SET XACT_ABORT ON;
GO

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.CLIENT_FUND') AND name=N'IX_CLIENT_FUND_OPERATION_STATUS')
 CREATE INDEX IX_CLIENT_FUND_OPERATION_STATUS ON fin.CLIENT_FUND(company_id,operation_id,fund_status) INCLUDE(base_amount,received_at,is_deleted);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.OPERATION_EXPENSE') AND name=N'IX_EXPENSE_OPERATION_STATUS')
 CREATE INDEX IX_EXPENSE_OPERATION_STATUS ON fin.OPERATION_EXPENSE(company_id,operation_id,expense_status) INCLUDE(base_amount,expense_date,is_rebillable,is_deleted);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.OPERATION_SERVICE') AND name=N'IX_SERVICE_OPERATION_STATUS')
 CREATE INDEX IX_SERVICE_OPERATION_STATUS ON fin.OPERATION_SERVICE(company_id,operation_id,service_status) INCLUDE(base_amount,is_deleted);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.CLIENT_COLLECTION') AND name=N'IX_COLLECTION_OPERATION_STATUS')
 CREATE INDEX IX_COLLECTION_OPERATION_STATUS ON fin.CLIENT_COLLECTION(company_id,operation_id,collection_status) INCLUDE(base_amount,collection_type,is_void);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.CLIENT_REFUND') AND name=N'IX_REFUND_OPERATION_STATUS')
 CREATE INDEX IX_REFUND_OPERATION_STATUS ON fin.CLIENT_REFUND(company_id,operation_id,refund_status) INCLUDE(base_amount,refund_date);
GO
