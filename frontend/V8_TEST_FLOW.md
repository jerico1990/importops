# Prueba funcional V8

## Shipment
1. Abrir una operación activa.
2. Ir a Embarques.
3. Crear un embarque.
4. Completar Booking + ETD + ETA.
5. Confirmar booking.
6. Para SEA: registrar MBL/HBL y al menos un contenedor activo.
7. Confirmar salida real.
8. Marcar IN_TRANSIT.
9. Confirmar arribo real.
10. Verificar que el workflow permita avanzar a las etapas que exigen Shipment.

## Documents
1. En la misma operación abrir Documentos.
2. Revisar requisitos de la etapa.
3. Cargar un documento requerido.
4. Confirmar que quede PENDING_REVIEW.
5. Usuario interno DOCUMENTS.MANAGE: validar u observar.
6. Si se observa, reemplazar por nueva versión.
7. Resolver observación cuando corresponda.
8. Confirmar que DocumentaryStatus se recalcula.
9. Intentar avanzar etapa: solo deben contar documentos VALID.
