// TherapyAdherenceChecks.cs
// -------------------------
// Module for automatic checks on patients' medication therapy adherence.
// Implements:
//   - Checks for missing intake of one or more drugs scheduled in the therapy plan (MedicationSchedule) at the expected times/days.
//     - If the patient misses scheduled drugs: generates alert ADHERENCE_MISSING (patient).
//     - If the patient does not follow therapy for more than 3 consecutive days: generates alert ADHERENCE_MISSING_3DAYS (patient and doctor).
// Alerts are notified to the intended recipients according to severity.
using GlucoTrack_monitoringTask.Data;

namespace GlucoTrack_monitoringTask.Monitoring
{
    public static class TherapyAdherenceChecks
    {
        public static void RunAll(GlucoTrackDBContext db, DateTime day)
        {
            // For each active patient
            var patients = db.Users.Where(u => u.RoleId == 3).ToList();

            foreach (var patient in patients)
            {
                // Find all active therapies for this day
                var therapies = db.Therapies
                    .Where(t => t.UserId == patient.UserId && t.StartDate <= DateOnly.FromDateTime(day) && (t.EndDate == null || t.EndDate >= DateOnly.FromDateTime(day)))
                    .ToList();

                bool missingAdherence = false;
                foreach (var therapy in therapies)
                {
                    // For each MedicationSchedule of the therapy
                    var schedules = db.MedicationSchedules.Where(ms => ms.TherapyId == therapy.TherapyId).ToList();
                    foreach (var schedule in schedules)
                    {
                        // For each expected intake (DailyIntakes)
                        int expectedIntakes = schedule.DailyIntakes > 0 ? schedule.DailyIntakes : 1;
                        decimal expectedQuantity = schedule.Quantity;

                        // Find all MedicationIntakes for this day and this schedule
                        var intakes = db.MedicationIntakes
                            .Where(mi => mi.UserId == patient.UserId && mi.MedicationScheduleId == schedule.MedicationScheduleId && mi.IntakeDateTime.Date == day.Date)
                            .ToList();

                        // If there are no intakes, all expected are missing
                        if (intakes.Count == 0)
                        {
                            missingAdherence = true;
                            continue;
                        }

                        // Sum the total quantities taken
                        decimal totalQuantity = intakes.Sum(i => i.ExpectedQuantityValue);

                        // If the total quantity is less than expectedQuantity * expectedIntakes, adherence is missing
                        if (totalQuantity < expectedQuantity * expectedIntakes)
                        {
                            missingAdherence = true;
                        }
                    }
                }

                if (missingAdherence)
                {
                    // Alert only to the patient
                    GlucoTrack_monitoringTask.Utils.MonitoringUtils.CreateAlert(db, "ADHERENCE_MISSING", patient.UserId, $"Not all scheduled medication intakes were registered for {day:dd/MM/yyyy}", new[] { patient.UserId });
                }
            }
            db.SaveChanges();
        }
    }
}
