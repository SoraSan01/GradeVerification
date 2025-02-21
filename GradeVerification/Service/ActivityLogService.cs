using GradeVerification.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GradeVerification.Service
{
    public class ActivityLogService
    {
        // Holds the collection of activity logs.
        public ObservableCollection<Activity> Activities { get; } = new ObservableCollection<Activity>();

        // Adds a new activity to the log.
        public void LogActivity(string title, string description, string icon)
        {
            var activity = new Activity
            {
                Title = title,
                Timestamp = DateTime.Now,
                Description = description,
                Icon = icon
            };

            // Insert at the top to display the most recent activity first.
            Activities.Insert(0, activity);
        }
    }
}
