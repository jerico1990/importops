# Alerts & Notifications Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add persisted, deduplicated, role-aware in-app alerts to ImportOps for client and internal users.

**Architecture:** Add `cfg.ALERT_RULE`, `ops.ALERT`, and `ops.ALERT_USER_STATE`; evaluate business conditions against existing quotation/task/shipment/document/customs/finance/operation data; persist lifecycle; expose query/read APIs; connect Header bell and a full alerts page.

**Tech Stack:** SQL Server, .NET 8, EF Core 8, ASP.NET Core, React + TypeScript + Vite.

**Spec:** `docs/superpowers/specs/2026-08-27-alerts-notifications-design.md`

## Global Constraints
- Work against `ImportOps_DB_COMPLETA_ACTUAL.sql` as the database baseline.
- No email/WhatsApp/external channels in V1.
- Client users must never see another client's alerts or internal-only content.
- Thresholds live in `cfg.ALERT_RULE`, not magic constants in services.
- Alert lifecycle is persisted and deduplicated.

---

### Task 1: Alert domain and SQL migration
- [ ] Add failing constant/rule tests.
- [ ] Add domain entities and status/type constants.
- [ ] Add `V1_14_ALERTS_NOTIFICATIONS.sql` with rules, permissions, PK/FK/indexes.
- [ ] Add EF configurations and DbSets.
- [ ] Run tests/build.

### Task 2: Evaluation engine
- [ ] Add failing tests for dedup key and audience policy.
- [ ] Implement rule evaluator for existing entities.
- [ ] Implement reconciliation ACTIVE/RESOLVED.
- [ ] Add periodic hosted service.
- [ ] Run tests/build.

### Task 3: Query/read API
- [ ] Add application DTOs/interface.
- [ ] Implement scoped query service.
- [ ] Add AlertsController endpoints and permissions.
- [ ] Run tests/build.

### Task 4: Frontend notification center
- [ ] Add API/types.
- [ ] Replace hard-coded Header alert count.
- [ ] Add notification dropdown and `/alertas` page.
- [ ] Add route/navigation.
- [ ] Run npm build.

### Task 5: Release packaging and verification
- [ ] Run dotnet test/build.
- [ ] Run npm build.
- [ ] Package backend/frontend and SQL incremental.
- [ ] Document exact test flow.
