# ImportOps Backend V10.18 — PDF comercial de cotización

- Agrega `GET /api/quotations/{id}/pdf` bajo el mismo permiso de consulta de cotizaciones.
- El aislamiento CLIENT se conserva: un cliente solo puede descargar cotizaciones propias que ya hayan sido enviadas.
- El PDF se genera desde los datos persistidos: cabecera, valor de aduana, derechos, conceptos agrupados, total gastos, IGV de servicios y total proforma.
- Los conceptos no gravados se marcan con `**` y se incluye la leyenda correspondiente.
- No requiere cambios de BD sobre V1.20 ni una librería PDF externa.
