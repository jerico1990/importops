-- ImportOpsDB V1.31 - Ejecutar despues de V1.30, primero en QA.
-- No reescribe ni elimina pagos históricos. Su origen se revisa antes de emitir.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'fin.CUSTOMS_TAX_ITEM',N'U') IS NULL THROW 51031, 'Falta fin.CUSTOMS_TAX_ITEM; aplique las migraciones previas.', 1;
IF COL_LENGTH('fin.CUSTOMS_TAX_ITEM','payer_type') IS NULL
BEGIN
 ALTER TABLE fin.CUSTOMS_TAX_ITEM ADD payer_type varchar(20) NOT NULL CONSTRAINT DF_CUSTOMS_TAX_ITEM_PAYER_TYPE DEFAULT ('UNREVIEWED');
END;
IF COL_LENGTH('fin.CUSTOMS_TAX_ITEM','payment_date') IS NULL
BEGIN
 ALTER TABLE fin.CUSTOMS_TAX_ITEM ADD payment_date date NULL;
END;
-- Inferir solo cuando todos los pagos históricos activos del tributo tienen el mismo pagador.
UPDATE t SET payer_type = CASE WHEN p.company_count > 0 AND p.client_count = 0 THEN 'COMPANY'
                               WHEN p.client_count > 0 AND p.company_count = 0 THEN 'CLIENT'
                               ELSE 'UNREVIEWED' END,
             payment_date = p.last_payment
FROM fin.CUSTOMS_TAX_ITEM t
CROSS APPLY (SELECT SUM(CASE WHEN UPPER(payer_type)='COMPANY' THEN 1 ELSE 0 END) company_count,
                    SUM(CASE WHEN UPPER(payer_type)='CLIENT' THEN 1 ELSE 0 END) client_count,
                    MAX(payment_date) last_payment, COUNT(*) total_count
             FROM fin.CUSTOMS_TAX_PAYMENT p WHERE p.customs_tax_item_id=t.customs_tax_item_id AND p.is_void=0) p
WHERE t.payer_type='UNREVIEWED' AND p.total_count>0;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name='CK_CUSTOMS_TAX_ITEM_PAYER_TYPE')
 ALTER TABLE fin.CUSTOMS_TAX_ITEM WITH CHECK ADD CONSTRAINT CK_CUSTOMS_TAX_ITEM_PAYER_TYPE CHECK (payer_type IN ('COMPANY','CLIENT','UNREVIEWED'));
-- No inferir el pagador de gastos sin evidencia; los nuevos se registran con paid_by explícito.
COMMIT TRANSACTION;
-- Revision requerida de tributos antiguos sin pagador claro:
SELECT customs_tax_item_id, operation_id, tax_code, amount_pen, payer_type FROM fin.CUSTOMS_TAX_ITEM WHERE payer_type='UNREVIEWED' AND is_void=0;
