using DataManager.Schemas.v1;
using Nest;

namespace DataManager.Services
{
    /// <summary>
    /// Interface to a service that logs activity about what's going on.
    /// </summary>
    public interface IActivityLoggingService
    {
        void LogActivity(IngestActivity activity);
    }

    public class ActivityLoggingService : IActivityLoggingService
    {
        public ActivityLoggingService(IElasticClient client)
        {
            this.client = client;
        }

        // Members
        private readonly IElasticClient client;

        public void LogActivity(IngestActivity activity)
        {
            // TODO - hmm.
        }
    }
}
