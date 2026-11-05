# ImportOps Alerts & Notifications Design

## Goal
Implement real in-app alerts for internal and client users, based on the existing ImportOps data model, with persisted lifecycle, per-user read state, deduplication, client isolation, and direct navigation to the affected record.

## Scope
V1 is in-app only. No email, WhatsApp, SMS, push, or external delivery channels.

## Audience
- CLIENT users only see alerts for their own `client_id` and only client-safe alert types/content.
- INTERNAL users see alerts for operations in their company according to responsibility first and permissions second.

## Persistence
- `ops.ALERT`: one persisted alert occurrence/lifecycle.
- `ops.ALERT_USER_STATE`: per-user read/dismiss state.
- `cfg.ALERT_RULE`: active thresholds and enablement so ETA/task/customs/settlement timing is not hard-coded.

## Lifecycle
- ACTIVE -> RESOLVED. Resolved alerts remain stored.
- `dedup_key` prevents multiple ACTIVE rows for the same business condition.
- A later recurrence after resolution creates a new alert cycle.

## Initial Rules
1. QUOTATION_PENDING_APPROVAL
2. TASK_DUE_SOON
3. TASK_OVERDUE
4. SHIPMENT_ETA_SOON
5. SHIPMENT_ETA_OVERDUE
6. SHIPMENT_ARRIVED
7. DOCUMENT_OBSERVED
8. DOCUMENT_REQUIRED_MISSING
9. CUSTOMS_OBSERVATION_OPEN
10. CUSTOMS_RELEASE_PENDING
11. CUSTOMS_RELEASED
12. SETTLEMENT_PENDING
13. OPERATION_READY_TO_CLOSE

## Evaluation
- `AlertEvaluationService` reconciles business conditions against persisted ACTIVE alerts.
- Reconciliation runs on demand from alert reads and periodically through a hosted background service.
- The periodic service evaluates every active company without relying on an HTTP user context.

## API
- `GET /api/alerts?status=&severity=&type=&page=&pageSize=`
- `GET /api/alerts/unread-count`
- `GET /api/alerts/summary`
- `POST /api/alerts/{id}/read`
- `POST /api/alerts/read-all`
- `POST /api/alerts/{id}/dismiss`
- `POST /api/alerts/refresh` internal only

## Frontend
- Header bell uses real unread count.
- Bell opens a compact dropdown with newest relevant alerts.
- `/alertas` provides full list, filters, severity, read/unread state, and direct action links.
- Client copy never exposes internal notes, staff tasks, finance-internal details, or customs-internal observations.
