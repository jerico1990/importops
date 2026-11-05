# V10.16.2 — Conceptos de proforma consistentes
- Agrega `quotation-concepts` al mantenimiento genérico de catálogos, por empresa.
- Exige exactamente una categoría (servicio o gasto), grupo válido y configuración tributaria.
- Protege eliminación cuando el concepto ya fue usado en ítems de cotización.
- Al usar un `QuotationConceptId`, el backend deriva tipo, categoría, impuesto y rebillable desde el maestro; no confía en clasificación enviada por el cliente.
- Conceptos legacy sin categoría deben configurarse antes de volver a utilizarse.
