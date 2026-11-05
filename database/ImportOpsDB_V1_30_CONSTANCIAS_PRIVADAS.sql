-- ImportOpsDB V1.30: constancias PDF privadas por gasto de tercero.
-- Ejecutar en QA después de V1.29. No modifica importes ni pagos financieros existentes.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'fin.OPERATION_EXPENSE', N'U') IS NULL THROW 51030, 'Falta fin.OPERATION_EXPENSE.', 1;
IF OBJECT_ID(N'fin.EXPENSE_PAYMENT_RECEIPT', N'U') IS NULL
BEGIN
    CREATE TABLE fin.EXPENSE_PAYMENT_RECEIPT (
        expense_payment_receipt_id BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        company_id BIGINT NOT NULL,
        operation_id BIGINT NOT NULL,
        operation_expense_id BIGINT NOT NULL,
        payment_date DATETIME2 NOT NULL,
        amount DECIMAL(19,2) NOT NULL,
        currency_code VARCHAR(3) NOT NULL,
        reference_no NVARCHAR(150) NULL,
        original_file_name NVARCHAR(255) NOT NULL,
        storage_key NVARCHAR(600) NOT NULL,
        storage_provider NVARCHAR(50) NOT NULL,
        sha256_hash VARCHAR(64) NOT NULL,
        file_size_bytes BIGINT NOT NULL,
        uploaded_at DATETIME2 NOT NULL,
        uploaded_by BIGINT NOT NULL,
        is_void BIT NOT NULL CONSTRAINT DF_EXPENSE_PAYMENT_RECEIPT_IS_VOID DEFAULT(0),
        voided_at DATETIME2 NULL,
        voided_by BIGINT NULL,
        void_reason NVARCHAR(500) NULL,
        CONSTRAINT CK_EXPENSE_PAYMENT_RECEIPT_AMOUNT CHECK(amount > 0),
        CONSTRAINT CK_EXPENSE_PAYMENT_RECEIPT_SIZE CHECK(file_size_bytes > 0),
        CONSTRAINT FK_EXPENSE_PAYMENT_RECEIPT_EXPENSE FOREIGN KEY(operation_expense_id)
            REFERENCES fin.OPERATION_EXPENSE(operation_expense_id)
    );
    CREATE INDEX IX_EXPENSE_PAYMENT_RECEIPT_EXPENSE
      ON fin.EXPENSE_PAYMENT_RECEIPT(company_id,operation_id,operation_expense_id,is_void);
END;
COMMIT TRANSACTION;
