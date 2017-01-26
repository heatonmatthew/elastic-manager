using System;

namespace DataManager.Schemas.v1.Jobs
{
    public class ScheduledJob : JobBase
    {
        public string SettingName { get; set; }
        public string VariableName { get; set; }

        public DateTime NextExecution { get; set; }
    }
}
