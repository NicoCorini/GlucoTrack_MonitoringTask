// GlycemicChecks.cs
// ------------------
// Module for automatic checks on patients' glycemic measurements.
// Implements:
//   - Daily check: verifies if the patient has registered at least 6 glycemic measurements for the current day.
//     - If no measurement: generates alert NO_MEASUREMENTS.
//     - If less than 6 measurements: generates alert PARTIAL_MEASUREMENTS.
//   - 3-day consecutive check: verifies if for 3 consecutive days the patient always has less than 6 measurements per day.
//     - If so: generates alert REPEATED_PARTIAL_MEASUREMENTS.
// Alerts are notified to both the patient and the associated doctor.
using GlucoTrack_monitoringTask.Data;

namespace GlucoTrack_monitoringTask.Monitoring
{
    public static class GlycemicChecks
    {
        public static void RunAll(GlucoTrackDBContext db, DateTime day)
        {
            CheckDailyGlycemicMeasurements(db, day);
            CheckRepeatedPartialMeasurements(db, day);
        }

        public static void CheckDailyGlycemicMeasurements(GlucoTrackDBContext db, DateTime day)
        {
            // For each active patient
            var patients = db.Users
                .Where(u => u.RoleId == 3)
                .ToList();

            foreach (var patient in patients)
            {
                var count = db.GlycemicMeasurements
                    .Where(m => m.UserId == patient.UserId &&
                                m.MeasurementDateTime.Date == day.Date)
                    .Count();

                if (count == 0)
                {
                    GlucoTrack_monitoringTask.Utils.MonitoringUtils.CreateAlert(db, "NO_MEASUREMENTS", patient.UserId, $"No glycemic measurements registered for {day:dd/MM/yyyy}", new[] { patient.UserId, GlucoTrack_monitoringTask.Utils.MonitoringUtils.GetDoctorId(db, patient.UserId) ?? 0 });
                }
                else if (count < 6)
                {
                    GlucoTrack_monitoringTask.Utils.MonitoringUtils.CreateAlert(db, "PARTIAL_MEASUREMENTS", patient.UserId, $"Only {count} glycemic measurements registered for {day:dd/MM/yyyy}", new[] { patient.UserId, GlucoTrack_monitoringTask.Utils.MonitoringUtils.GetDoctorId(db, patient.UserId) ?? 0 });
                }
            }
            db.SaveChanges();
        }

        public static void CheckRepeatedPartialMeasurements(GlucoTrackDBContext db, DateTime day)
        {
            // For each active patient
            var patients = db.Users
                .Where(u => u.RoleId == 3)
                .ToList();

            foreach (var patient in patients)
            {
                // Check for the previous 3 days (including today)
                int days = 3;
                bool allPartial = true;
                List<int> dailyCounts = new();
                for (int i = 0; i < days; i++)
                {
                    var d = day.AddDays(-i);
                    var count = db.GlycemicMeasurements
                        .Where(m => m.UserId == patient.UserId && m.MeasurementDateTime.Date == d.Date)
                        .Count();
                    dailyCounts.Add(count);
                    if (count >= 6) allPartial = false;
                }
                if (allPartial)
                {
                    string msg = $"Less than 6 glycemic measurements for {days} consecutive days: " +
                        string.Join(", ", dailyCounts.Select((c, i) => $"{day.AddDays(-i):dd/MM}: {c}"));
                    GlucoTrack_monitoringTask.Utils.MonitoringUtils.CreateAlert(db, "REPEATED_PARTIAL_MEASUREMENTS", patient.UserId, msg, new[] { patient.UserId, GlucoTrack_monitoringTask.Utils.MonitoringUtils.GetDoctorId(db, patient.UserId) ?? 0 });
                }
            }
            db.SaveChanges();
        }
    }
}
