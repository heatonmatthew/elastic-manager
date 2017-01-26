using System.Collections.Generic;

namespace DataManager.Services
{
    public interface IErrorCache
    {
        int Count { get; }
        void AddError(string message);
    }

    /// <summary>
    /// A location where we can store error messages during operations.
    /// </summary>
    public class ErrorCache : List<string>, IErrorCache
    {
        public void AddError(string message)
        {
            Add(message);
        }
    }
}
