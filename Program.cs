using Microsoft.EntityFrameworkCore;
using GlucoTrack_monitoringTask.Data;
using GlucoTrack_monitoringTask.Models;
using GlucoTrack_monitoringTask.Monitoring;
using GlucoTrack_monitoringTask.Utils;
// Monitoring Task - GlucoTrack
// This scheduled task periodically analyzes the consistency of patients' medication intakes and glycemic measurements, generating automatic alerts in the database.
// Types of alerts to generate (domain requirements):
// 1. Therapy non-adherence: the patient did not take one or more drugs scheduled in the therapy plan (MedicationSchedule) at the expected times/days.
// 2. Out-of-range glycemia: glycemic values (GlycemicMeasurements) above or below the patient's personalized thresholds.
// 3. No measurements: the patient did not register glycemic measurements for a period longer than the expected threshold.
// 4. Critical symptoms: the patient reported symptoms considered critical (e.g. loss of consciousness, seizures, etc.).
// 5. Reported clinical conditions: the patient entered clinical conditions requiring immediate attention (e.g. infections, ketoacidosis, etc.).
// 6. Hypo/hyperglycemia risk: patterns of glycemic values suggesting imminent risk.
// 7. Other custom alerts: e.g. drug interactions, log anomalies, etc.
// The task must analyze recent data, generate and save alerts in the Alerts table, and notify the intended recipients (AlertRecipients).
// NOTE: Update this list if domain requirements change.
//
// ---------------------------------------------------------------------------------------------------------------------------------------
// | LABEL                         | Description                                                               | Recipients            |
// |-------------------------------|---------------------------------------------------------------------------|-----------------------|
// | ADHERENCE_MISSING             | Patient did not register the intake of one or more scheduled drugs         | Patient               |
// | ADHERENCE_MISSING_3DAYS       | Patient did not follow therapy for more than 3 consecutive days            | Patient, Doctor       |
// | GLYCEMIA_MILD                 | Moderately out-of-range glycemia (e.g. 180-250 mg/dL)                      | Patient               |
// | GLYCEMIA_SEVERE               | Severely out-of-range glycemia (e.g. 251-350 mg/dL)                        | Patient, Doctor       |
// | GLYCEMIA_CRITICAL             | Critically out-of-range glycemia (>350 mg/dL or <60 mg/dL)                 | Doctor                |
// | NO_MEASUREMENTS               | No glycemic measurements for day x                                         | Patient, Doctor       |
// | PARTIAL_MEASUREMENTS          | Less than 6 glycemic measurements for day x                                | Patient, Doctor       |
// | REPEATED_PARTIAL_MEASUREMENTS | Less than 6 glycemic measurements for 3 consecutive days                   | Patient, Doctor       |
// | CRITICAL_SYMPTOM              | Critical symptom reported                                                  | Doctor                |
// | CRITICAL_CONDITION            | Severe clinical condition reported                                         | Doctor                |
// | HYPO_HYPER_RISK               | Hypo/hyperglycemia risk pattern                                            | Patient, Doctor       |
// | CUSTOM_ALERT                  | Other custom alerts (e.g. interactions, anomalies, etc.)                   | Patient, Doctor       |
// ---------------------------------------------------------------------------------------------------------------------------------------


// The system must verify that patients' medication intakes are consistent with the prescribed therapies.
// The system must prompt the patient to complete the entries for medication intakes, so as to manage both alerts to the patient (if they forget to take the drugs) and to the doctor (if the patient does not follow the prescriptions for more than 3 consecutive days).
// The system also notifies doctors of all patients who register glycemic values above the indicated thresholds, with different modes depending on severity.



using System.Linq;

var optionsBuilder = new DbContextOptionsBuilder<GlucoTrackDBContext>();
optionsBuilder.UseSqlServer("Server=localhost,1433;Database=GlucoTrackDB;User Id=sa;Password=baseBase100!;TrustServerCertificate=True;");
using var db = new GlucoTrackDBContext(optionsBuilder.Options);

DateTime today = DateTime.Today;

// Esegui tutti i controlli
GlycemicChecks.RunAll(db, today);
TherapyAdherenceChecks.RunAll(db, today);

// --- REPORT ALERTS GENERATI OGGI ---
var alertsToday = db.Alerts
    .Where(a => a.CreatedAt != null && a.CreatedAt.Value.Date == today)
    .ToList();
var alertTypes = db.AlertTypes.ToDictionary(at => at.AlertTypeId, at => at.Label);
var users = db.Users.ToDictionary(u => u.UserId, u => (u.FirstName, u.LastName));

Console.WriteLine("\n================= ALERTS REPORT =================");
Console.WriteLine($"Date: {today:yyyy-MM-dd}");
Console.WriteLine($"Total alerts generated today: {alertsToday.Count}");
if (alertsToday.Count > 0)
{
    var grouped = alertsToday.GroupBy(a => a.AlertTypeId).OrderByDescending(g => g.Count());
    Console.WriteLine("\n| Alert Type              | Count | Users");
    Console.WriteLine("|-------------------------|-------|------------------------------");
    foreach (var g in grouped)
    {
        string type = alertTypes.ContainsKey(g.Key) ? alertTypes[g.Key] : g.Key.ToString();
        string userList = string.Join(", ", g.Select(a => users.ContainsKey(a.UserId) ? $"{users[a.UserId].FirstName} {users[a.UserId].LastName}" : $"UserId {a.UserId}").Distinct());
        Console.WriteLine($"| {type.PadRight(23)} | {g.Count(),5} | {userList}");
    }
}
else
{
    Console.WriteLine("No alerts generated today.");
}
Console.WriteLine("=================================================\n");