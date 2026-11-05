# Backend V10.17 - Proforma economic model
- Rates for standard customs duties come from cfg.CUSTOMS_TAX_CONCEPT.
- ADVALOREM remains manual.
- IGV importación and IPM are calculated from customs value.
- Percepción is calculated from customs value + Ad Valorem + import IGV + IPM.
- Standard automatic customs duties use whole-USD rounding to reproduce the approved reference proforma.
- Automatic duty lines persist rate/base/is_calculated for traceability.
- PUT /api/quotations/{id}/customs-taxes/ad-valorem updates Ad Valorem and recalculates dependent duties.
- Manual API insertion rejects concepts configured as automatic.
- Requires DB V1.20.
