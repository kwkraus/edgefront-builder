# SPEC-100 — Frontend Shell & UX Contract (Build Ready)

## Scope
Authenticated web UI for Series, Sessions, Metrics.

## Screens
- Series List (summary metrics, default sort by created desc)
- Series Detail (SeriesMetrics + Sessions list)
- Session Create/Edit (Draft)
- Session Edit (Published → Save & Publish atomic)
- Publish Series flow
- Drift + Reconcile status display

## UX Contracts
- Published sessions require Save & Publish
- Unsynced edits discarded if leaving on failure
- Metrics read-only from API

## Definition of Done
- Core flows implemented
- Minimal UI tests for publish & atomic edit behavior

