# V11.4 - Formato monetario global

Base: Frontend V11.3.

- Los importes mostrados usan separador de miles y exactamente 2 decimales.
- Los inputs monetarios usan `MoneyInput`: al salir del campo muestran `2,000.00`; el estado enviado al API sigue siendo numérico sin separadores.
- Aplicado a cotización (precio/descuento), valor declarado de solicitud, tributos aduaneros y entradas financieras (fondos, gastos, pagos, cobros, aplicaciones, devoluciones, ajustes y refacturación).
- Los cálculos/resúmenes financieros, cotizaciones, rendición/cierre y aduanas usan el formateador monetario compartido.
- Tipos de cambio, porcentajes, cantidades, pesos, volúmenes y otros números no monetarios conservan su precisión original.
