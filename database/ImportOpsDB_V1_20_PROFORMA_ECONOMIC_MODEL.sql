/* ImportOps DB V1.20 - Proforma economic model
   Requires V1.19.
   Standard customs duties:
   - ADVALOREM: manual amount when applicable
   - IGV: 16% of customs value
   - IPM: 2% of customs value
   - PERCEPCION: 3.5% of customs value + ADVALOREM + IGV + IPM
   Historical quotations are NOT recalculated by this migration.
*/
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRAN;

 IF OBJECT_ID('cfg.CUSTOMS_TAX_CONCEPT','U') IS NULL OR OBJECT_ID('sales.QUOTATION_CUSTOMS_TAX','U') IS NULL
   THROW 51001,'V1.20 requiere V1.19.',1;

 IF COL_LENGTH('cfg.CUSTOMS_TAX_CONCEPT','calculation_mode') IS NULL
   ALTER TABLE cfg.CUSTOMS_TAX_CONCEPT ADD calculation_mode varchar(40) NOT NULL CONSTRAINT DF_CUSTOMS_TAX_CONCEPT_CALC_MODE DEFAULT('MANUAL');
 IF COL_LENGTH('cfg.CUSTOMS_TAX_CONCEPT','default_rate') IS NULL
   ALTER TABLE cfg.CUSTOMS_TAX_CONCEPT ADD default_rate decimal(9,4) NULL;

 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_CUSTOMS_TAX_CONCEPT_CALC_MODE' AND parent_object_id=OBJECT_ID('cfg.CUSTOMS_TAX_CONCEPT'))
   ALTER TABLE cfg.CUSTOMS_TAX_CONCEPT ADD CONSTRAINT CK_CUSTOMS_TAX_CONCEPT_CALC_MODE CHECK(calculation_mode IN ('MANUAL','CUSTOMS_VALUE_PERCENT','PERCEPTION_BASE_PERCENT'));
 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_CUSTOMS_TAX_CONCEPT_DEFAULT_RATE' AND parent_object_id=OBJECT_ID('cfg.CUSTOMS_TAX_CONCEPT'))
   ALTER TABLE cfg.CUSTOMS_TAX_CONCEPT ADD CONSTRAINT CK_CUSTOMS_TAX_CONCEPT_DEFAULT_RATE CHECK(default_rate IS NULL OR (default_rate >= 0 AND default_rate <= 100));

 UPDATE cfg.CUSTOMS_TAX_CONCEPT SET calculation_mode='MANUAL', default_rate=NULL, modified_at=sysutcdatetime() WHERE code='ADVALOREM';
 UPDATE cfg.CUSTOMS_TAX_CONCEPT SET calculation_mode='CUSTOMS_VALUE_PERCENT', default_rate=16.0000, modified_at=sysutcdatetime() WHERE code='IGV';
 UPDATE cfg.CUSTOMS_TAX_CONCEPT SET calculation_mode='CUSTOMS_VALUE_PERCENT', default_rate=2.0000, modified_at=sysutcdatetime() WHERE code='IPM';
 UPDATE cfg.CUSTOMS_TAX_CONCEPT SET calculation_mode='PERCEPTION_BASE_PERCENT', default_rate=3.5000, modified_at=sysutcdatetime() WHERE code='PERCEPCION';
 UPDATE cfg.CUSTOMS_TAX_CONCEPT SET calculation_mode='MANUAL', default_rate=NULL, modified_at=sysutcdatetime() WHERE code IN ('ISC','ANTIDUMPING','OTHER');

 IF COL_LENGTH('sales.QUOTATION_CUSTOMS_TAX','rate_applied') IS NULL
   ALTER TABLE sales.QUOTATION_CUSTOMS_TAX ADD rate_applied decimal(9,4) NULL;
 IF COL_LENGTH('sales.QUOTATION_CUSTOMS_TAX','calculation_base') IS NULL
   ALTER TABLE sales.QUOTATION_CUSTOMS_TAX ADD calculation_base decimal(19,4) NULL;
 IF COL_LENGTH('sales.QUOTATION_CUSTOMS_TAX','is_calculated') IS NULL
   ALTER TABLE sales.QUOTATION_CUSTOMS_TAX ADD is_calculated bit NOT NULL CONSTRAINT DF_QUOTATION_CUSTOMS_TAX_CALCULATED DEFAULT(0);

 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK;
 THROW;
END CATCH;
GO

IF EXISTS(SELECT 1 FROM cfg.CUSTOMS_TAX_CONCEPT WHERE code='IGV' AND calculation_mode='CUSTOMS_VALUE_PERCENT' AND default_rate=16.0000)
AND EXISTS(SELECT 1 FROM cfg.CUSTOMS_TAX_CONCEPT WHERE code='IPM' AND calculation_mode='CUSTOMS_VALUE_PERCENT' AND default_rate=2.0000)
AND EXISTS(SELECT 1 FROM cfg.CUSTOMS_TAX_CONCEPT WHERE code='PERCEPCION' AND calculation_mode='PERCEPTION_BASE_PERCENT' AND default_rate=3.5000)
AND COL_LENGTH('sales.QUOTATION_CUSTOMS_TAX','is_calculated') IS NOT NULL
 PRINT 'ImportOps DB V1.20 OK';
ELSE
 THROW 51001,'V1.20 incompleta.',1;
GO
