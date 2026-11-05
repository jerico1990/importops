# Prueba end-to-end

1. Solicitud -> revisión.
2. Cotización -> envío -> aprobación del CLIENT_ADMIN.
3. Crear operación.
4. Completar workflow/tareas/documentos.
5. Crear Shipment, booking, salida, tránsito y arribo.
6. En etapa CUSTOMS: abrir gestión aduanera.
7. Registrar régimen, DAM, fecha y oficina.
8. Iniciar revisión.
9. Asignar canal.
10. Registrar/resolver observaciones si aplica.
11. Registrar tributos y marcarlos PAID/EXEMPT/VOID según corresponda.
12. Registrar levante.
13. Volver al workflow y avanzar a RELEASED/PENDING_DELIVERY/DELIVERED.
14. Avanzar a SETTLEMENT.
15. Finanzas: generar presupuesto, registrar fondos/gastos/cobros/devoluciones.
16. Aplicar cobros a servicios hasta PendingServiceCollection = 0.
17. Pagar gastos pendientes y dejar ManagedFundBalance = 0.
18. Preparar rendición.
19. Aprobar rendición -> FinancialStatus = CLOSED.
20. Completar tareas/documentos de SETTLEMENT.
21. Cerrar operación -> etapa CLOSED y OperationalStatus = CLOSED.
