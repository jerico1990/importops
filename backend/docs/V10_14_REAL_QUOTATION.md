# ImportOps Backend V10.14 - Proforma Real

Base estricta: V10.13. Requiere DB V1.17.

## Alcance
- EF mapping completo de V1.17.
- Proforma: referencia, contenido, régimen, aduana, ETA, TC, EXW, flete, seguro, valor aduanero, peso/bultos/CBM.
- N proveedores y N mercancías por cotización.
- Derechos aduaneros estimados.
- Conceptos de cotización configurables con tratamiento IGV.
- Cuentas bancarias configuradas visibles en proforma.
- Totales calculados por backend: valor aduanero, derechos, gastos, IGV gastos y total proforma.
- CreateFromRequest precarga proveedor, mercancía, peso/volumen/valor/origen disponibles en IMPORT_REQUEST.
- Versionado copia cabecera proforma, proveedores, carga, tributos e items.
- No crea aún presupuesto ni CUSTOMS_TAX real al aprobar; esa integración queda separada del modelo estimado.

## Endpoints nuevos
GET /api/quotations/{id}/proforma
PUT /api/quotations/{id}/proforma
GET /api/quotations/concepts
POST /api/quotations/{id}/suppliers
DELETE /api/quotations/{id}/suppliers/{sid}
POST /api/quotations/{id}/cargo
DELETE /api/quotations/{id}/cargo/{cid}
POST /api/quotations/{id}/customs-taxes
DELETE /api/quotations/{id}/customs-taxes/{tid}
