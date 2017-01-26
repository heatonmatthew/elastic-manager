using System;

namespace DataManager.Schemas.v1.Jobs
{
    public abstract class JobBase
    {
        // Identity properties
        public string Name { get; set; }
        public string PackageId { get; set; }
        public string Dataset { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public DateTime Updated { get; set; }
        public string UpdatedBy { get; set; }

        // Current state information
        public DateTime? StartedExecution { get; set; }
        public DateTime? ProgressUpdate { get; set; }
        public string ExecutingOn { get; set; }
        public bool? IsDisabled { get; set; }

        // Historical summary
        public Guid? LastExecutionId { get; set; }
        public DateTime? LastExecuted { get; set; }
        public string LastExecuteMessage { get; set; }
        public int LastExecuteDuration { get; set; }

        public DateTime? LastSuccess { get; set; }
        public Guid? LastSuccessId { get; set; }
        public string LastSuccessMessage { get; set; }
        public int LastSuccessDuration { get; set; }
    }
}
