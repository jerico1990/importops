/* ImportOps - Aduanas PEN: migración aditiva, no ejecutar sin respaldo.
   No modifica tablas existentes ni suma automáticamente estos importes a liquidaciones.
   Vinculación lógica a operaciones mediante company_id/operation_id; FK pendiente de verificar
   contra esquema de operaciones de la instalación destino. */
SET XACT_ABORT ON;
GO
IF SCHEMA_ID(N'fin') IS NULL EXEC(N'CREATE SCHEMA fin');
GO
IF OBJECT_ID(N'fin.CUSTOMS_TAX_ITEM',N'U') IS NULL
BEGIN
 CREATE TABLE fin.CUSTOMS_TAX_ITEM (
  customs_tax_item_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CUSTOMS_TAX_ITEM PRIMARY KEY,
  company_id bigint NOT NULL,
  operation_id bigint NOT NULL,
  tax_code varchar(30) NOT NULL,
  description nvarchar(250) NOT NULL,
  document_reference nvarchar(100) NULL,
  assessed_at datetime2(0) NULL,
  amount_pen decimal(19,2) NOT NULL,
  notes nvarchar(1000) NULL,
  is_void bit NOT NULL CONSTRAINT DF_CUSTOMS_TAX_ITEM_VOID DEFAULT(0),
  void_reason nvarchar(500) NULL,
  created_at datetime2(0) NOT NULL CONSTRAINT DF_CUSTOMS_TAX_ITEM_CREATED DEFAULT(SYSUTCDATETIME()),
  created_by bigint NOT NULL,
  modified_at datetime2(0) NULL,
  modified_by bigint NULL,
  row_ver rowversion NOT NULL,
  CONSTRAINT CK_CUSTOMS_TAX_ITEM_AMOUNT CHECK(amount_pen >= 0),
  CONSTRAINT CK_CUSTOMS_TAX_ITEM_CODE CHECK(tax_code IN ('ADVALOREM','IGV','IPM','PERCEPCION','OTRO')),
  CONSTRAINT CK_CUSTOMS_TAX_ITEM_VOID_REASON CHECK(is_void=0 OR NULLIF(LTRIM(RTRIM(void_reason)),N'') IS NOT NULL)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.CUSTOMS_TAX_ITEM') AND name=N'IX_CUSTOMS_TAX_ITEM_OPERATION')
 CREATE INDEX IX_CUSTOMS_TAX_ITEM_OPERATION ON fin.CUSTOMS_TAX_ITEM(company_id,operation_id,is_void);
GO
IF OBJECT_ID(N'fin.CUSTOMS_TAX_PAYMENT',N'U') IS NULL
BEGIN
 CREATE TABLE fin.CUSTOMS_TAX_PAYMENT (
  customs_tax_payment_id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_CUSTOMS_TAX_PAYMENT PRIMARY KEY,
  company_id bigint NOT NULL,
  operation_id bigint NOT NULL,
  customs_tax_item_id bigint NOT NULL,
  payment_date datetime2(0) NOT NULL,
  amount_pen decimal(19,2) NOT NULL,
  payer_type varchar(20) NOT NULL,
  payment_reference nvarchar(100) NULL,
  notes nvarchar(1000) NULL,
  is_void bit NOT NULL CONSTRAINT DF_CUSTOMS_TAX_PAYMENT_VOID DEFAULT(0),
  void_reason nvarchar(500) NULL,
  created_at datetime2(0) NOT NULL CONSTRAINT DF_CUSTOMS_TAX_PAYMENT_CREATED DEFAULT(SYSUTCDATETIME()),
  created_by bigint NOT NULL,
  row_ver rowversion NOT NULL,
  CONSTRAINT FK_CUSTOMS_TAX_PAYMENT_ITEM FOREIGN KEY(customs_tax_item_id) REFERENCES fin.CUSTOMS_TAX_ITEM(customs_tax_item_id),
  CONSTRAINT CK_CUSTOMS_TAX_PAYMENT_AMOUNT CHECK(amount_pen > 0),
  CONSTRAINT CK_CUSTOMS_TAX_PAYMENT_PAYER CHECK(payer_type IN ('COMPANY','CLIENT')),
  CONSTRAINT CK_CUSTOMS_TAX_PAYMENT_VOID_REASON CHECK(is_void=0 OR NULLIF(LTRIM(RTRIM(void_reason)),N'') IS NOT NULL)
 );
END;
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'fin.CUSTOMS_TAX_PAYMENT') AND name=N'IX_CUSTOMS_TAX_PAYMENT_OPERATION')
 CREATE INDEX IX_CUSTOMS_TAX_PAYMENT_OPERATION ON fin.CUSTOMS_TAX_PAYMENT(company_id,operation_id,is_void);
GO
/* Consulta de saldos por tributo: no confundir con la conciliación de anticipos y devoluciones. */
CREATE OR ALTER VIEW fin.V_CUSTOMS_TAX_BALANCE AS
 SELECT t.company_id,t.operation_id,t.customs_tax_item_id,t.tax_code,t.description,
        t.amount_pen,
        COALESCE(SUM(CASE WHEN p.is_void=0 THEN p.amount_pen ELSE 0 END),0) AS paid_pen,
        t.amount_pen-COALESCE(SUM(CASE WHEN p.is_void=0 THEN p.amount_pen ELSE 0 END),0) AS outstanding_pen
 FROM fin.CUSTOMS_TAX_ITEM t
 LEFT JOIN fin.CUSTOMS_TAX_PAYMENT p ON p.customs_tax_item_id=t.customs_tax_item_id
 WHERE t.is_void=0
 GROUP BY t.company_id,t.operation_id,t.customs_tax_item_id,t.tax_code,t.description,t.amount_pen;
GO
