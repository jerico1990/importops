# Proforma Economic Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Align database, backend and frontend proforma calculations with the approved real quotation.

**Architecture:** Store customs-tax calculation mode/rate in cfg.CUSTOMS_TAX_CONCEPT. Backend owns all calculations and persists the calculated duty lines for traceability. Frontend renders the backend result and only edits Ad Valorem among the standard duties.

**Tech Stack:** SQL Server, .NET 8/EF Core, React/TypeScript/Vite.

**Spec:** `docs/superpowers/specs/2026-09-17-proforma-economic-model-design.md`

## Global Constraints
- Existing non-draft quotations are not migrated/recalculated.
- Standard rates are database configuration, never frontend constants.
- Existing concept IGV logic remains backend-owned.

---

### Task 1: Customs duty calculator
**Files:** Modify calculator and its xUnit tests.
- [ ] Add failing reference-case tests for 16% IGV, 2% IPM, 3.5% perception and manual Ad Valorem.
- [ ] Add calculation methods and verify reference values.

### Task 2: Persistence and API
**Files:** Domain entities, EF configurations, DTOs, service and controller.
- [ ] Add calculation metadata to customs-tax catalog and quotation duty lines.
- [ ] Recalculate/upsert automatic standard duties whenever draft proforma inputs or Ad Valorem change.
- [ ] Add PUT endpoint for manual Ad Valorem and reject manual writes to calculated concepts.

### Task 3: Database V1.20
**Files:** New incremental SQL and consolidated clean-install SQL.
- [ ] Add calculation_mode/default_rate and duty traceability columns.
- [ ] Configure ADVALOREM=MANUAL, IGV=CUSTOMS_VALUE_PERCENT/16, IPM=CUSTOMS_VALUE_PERCENT/2, PERCEPCION=PERCEPTION_BASE_PERCENT/3.5.
- [ ] Do not recalculate historical quotation data during migration.

### Task 4: Frontend V11.10
**Files:** Proforma types/API/panel.
- [ ] Render standard customs duties as a fixed summary.
- [ ] Edit only Ad Valorem; show automatic rates and perception base.
- [ ] Present concept base and tax condition, with aggregate service IGV.
- [ ] Verify TypeScript/Vite build.

### Task 5: Packaging and verification
- [ ] Run available backend tests/build; report environment limitations explicitly.
- [ ] Run frontend build.
- [ ] Validate SQL structure and package frontend/backend/database deliverables.
