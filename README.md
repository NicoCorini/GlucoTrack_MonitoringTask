# GlucoTrack Monitoring Task

> Background process for periodic clinical data analysis: adherence auditing, missing measurements detection, symptom escalation & alert generation.

## 🎯 Purpose

Separates heavy, scheduled logic from the interactive API. Ensures patient / doctor experiences stay responsive while automated checks run on a cadence.

## ✅ Implemented Foundations

| Area               | Current Capability                                                                                          |
| ------------------ | ----------------------------------------------------------------------------------------------------------- |
| Scheduling (entry) | Program scaffold ready for host / scheduler integration (e.g. cron / Windows Task / container orchestrator) |
| Data Access        | Shared EF Core entities & DbContext model reused (consistency with API)                                     |
| Adherence Logic    | Medication schedule vs intake comparison primitives (extensible)                                            |
| Glycemic Analysis  | Placeholder logic structure for threshold & absence detection                                               |
| Alert Pipeline     | Creation via shared entities (Alerts, AlertRecipients)                                                      |
| Utilities          | Reusable helpers in `Utils/MonitoringUtils.cs`                                                              |

> Core scaffolding is complete; advanced analytics are scheduled as optional enhancements.

## 🔭 Roadmap (Optional Enhancements)

| Enhancement                   | Description                                                     |
| ----------------------------- | --------------------------------------------------------------- |
| Advanced Adherence Clustering | Group prolonged non‑adherence episodes & severity tiers         |
| Anomaly Detection             | Statistical / ML models for unusual glycemic patterns           |
| Symptom Severity Escalation   | Rule matrix → multi‑channel notification                        |
| Report Synthesis              | Daily digest generation (doctor & patient summaries)            |
| Observability                 | Structured logs + metrics export (OpenTelemetry)                |
| Configuration                 | Externalized schedule (cron expr / config file / feature flags) |

## 🧱 Architecture Overview

```
Monitoring Task
  → Loads configuration
  → Opens DbContext (read/write)
  → Executes analysis modules (adherence, glycemia, symptoms)
  → Persists Alerts + AlertRecipients
  → (Future) Queues notifications / digest generation
```

Modular execution allows enabling/disabling checks independently.

## 🔄 Execution Model

Designed to be invoked periodically (e.g. every 15 min / hourly / daily batch layers). Idempotent strategies encouraged (check existing open alerts before duplicating).

## 🗄 Data Model Reuse

Uses the same entity definitions as API (`Users`, `Therapies`, `MedicationSchedules`, `MedicationIntakes`, `GlycemicMeasurements`, `Alerts`, etc.) for consistency and single source of truth.

## 🚀 Running (Development)

Prerequisites: .NET 8 SDK, access to the same database the API uses.

```
cd monitoring
 dotnet run
```

Integrate with a scheduler (examples):

- Windows Task Scheduler command line invocation
- Cron inside container (Linux base image) launching `dotnet GlucoTrack_monitoringTask.dll`
- Orchestrator (Kubernetes CronJob / Azure WebJob / GitHub Actions nightly run)

## 🧩 Key Design Considerations

| Concern              | Decision                                                 |
| -------------------- | -------------------------------------------------------- |
| API Load Isolation   | Work moved out of request pipeline for performance       |
| Extensibility        | Separate modules per analysis area (future ML injection) |
| Consistency          | Shared entities avoid drift & duplicate mapping          |
| Alert Idempotency    | Strategy placeholder: check open alerts before insertion |
| Future Notifications | Hook point after alert persistence for push/email queue  |

## 🔐 Security & Access

Operates with DB credentials only; no direct external exposure. Future: service account scoping / least privilege DB user.

## 🧪 Testing Approach

Planned: focused unit tests for each analysis module using in‑memory context. (Leverages patterns from API test project.)

## 📈 Recruiter Snapshot

- Demonstrates forward planning for scalability & performance.
- Clear separation of analytic/background concerns from synchronous API.
- Provides scaffold for advanced analytics & notification orchestration.

## 📝 License

MIT (inherits root repository license).

---

This monitoring module README documents a completed scaffold with core foundations; roadmap items are optional growth areas.
