// MonitoringUtils.cs
// ------------------
// Utility class for shared functions between monitoring modules (alert, lookup, etc).
using System;
using System.Linq;
using GlucoTrack_monitoringTask.Data;
using GlucoTrack_monitoringTask.Models;

namespace GlucoTrack_monitoringTask.Utils
{
    public static class MonitoringUtils
    {
        // Creates an alert only if an identical alert for the same user, type, and day does not already exist
        public static void CreateAlert(GlucoTrackDBContext db, string alertLabel, int userId, string message, int[] recipientIds)
        {
            // Get the alert type by label
            var alertType = db.AlertTypes.FirstOrDefault(a => a.Label == alertLabel);
            if (alertType == null) return;

            var today = DateTime.Today;
            // Duplicate check: same type, user, message, and day
            bool exists = db.Alerts.Any(a =>
                a.UserId == userId &&
                a.AlertTypeId == alertType.AlertTypeId &&
                a.Message == message &&
                a.CreatedAt.HasValue &&
                a.CreatedAt.Value.Date == today
            );
            if (exists) return;

            var alert = new Alerts
            {
                UserId = userId,
                AlertTypeId = alertType.AlertTypeId,
                Message = message,
                CreatedAt = DateTime.Now
            };
            db.Alerts.Add(alert);
            db.SaveChanges();

            foreach (var recipientId in recipientIds.Distinct())
            {
                if (recipientId == 0) continue;
                db.AlertRecipients.Add(new AlertRecipients
                {
                    AlertId = alert.AlertId,
                    RecipientUserId = recipientId,
                    IsRead = false
                });
            }
            db.SaveChanges();
        }

        // Returns the doctor ID associated with a patient
        public static int? GetDoctorId(GlucoTrackDBContext db, int patientId)
        {
            var pd = db.PatientDoctors.FirstOrDefault(x => x.PatientId == patientId);
            return pd?.DoctorId;
        }
    }
}
