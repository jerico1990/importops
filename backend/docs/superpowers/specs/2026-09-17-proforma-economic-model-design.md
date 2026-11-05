# Proforma economic model design

## Goal
Align ImportOps quotation/proforma with PROFORMA 0054-2026.

## Rules
- Customs value = EXW + international freight + customs-value insurance. It is reference information and is not added directly to Total Proforma.
- Customs duties are a separate block.
- Ad Valorem is manually entered when applicable.
- Import IGV = 16% of customs value.
- IPM = 2% of customs value.
- Perception = 3.5% of customs value + Ad Valorem + import IGV + IPM.
- Rates belong to the database catalog, not React.
- Other customs-tax concepts remain manual unless configured otherwise.
- Quotation concepts keep their own taxable/non-taxable configuration. Expense/service IGV is calculated per taxable concept and aggregated separately.
- Total Proforma = customs duties + expense/service base after discount + expense/service IGV.
- Sent/approved historical quotations are not recalculated by the migration.

## UX
- Rename customs-value insurance label to "Seguro internacional".
- Replace generic "Agregar derecho" flow for standard duties with an always-visible customs-duty summary.
- Ad Valorem is editable; IGV importación, IPM and Percepción are read-only calculated amounts and show their rates/bases.
- Concepts show base amount and tax condition; IGV is summarized at block level instead of emphasizing tax-inclusive line totals.
- Standard automatic customs duties are rounded to whole USD amounts before the next dependent calculation, matching the reference proforma (IGV 9,029; IPM 1,129; perception 2,331).
