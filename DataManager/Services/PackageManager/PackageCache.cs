using System.Linq;
using DataManager.Services.Scoping;
using Nest;

namespace DataManager.Services.PackageManager
{
    public class PackageCache
    {
        public PackageCache(IErrorCache errorCache)
        {
            ErrorCache = errorCache;
        }

        public IErrorCache ErrorCache { get; set; }

        public NestedScope OriginalState { get; set; }
        public NestedScope CurrentState { get; set; }

        public void SetOriginalState(NestedScope state)
        {
            OriginalState = state;
            CurrentState = state.Clone();
        }

        public void RecordChanges(BulkDescriptor bulk)
        {
            var originalDict = OriginalState.ToDictionary();
            var currentDict = CurrentState.ToDictionary();
            var correlation = currentDict.CorrelateWith(originalDict);

            // Ensure all of the current ones have changes recorded
            foreach (var tuple in correlation.SourceItems)
            {
                RecordChanges(tuple.Item, tuple.PairedWith, bulk);
            }

            // Remove anything no longer current
            foreach (var aspect in correlation.NonSourceItems.SelectMany(s => s.Item.Aspects.Values))
            {
                aspect.RemoveAll(bulk);
            }
        }

        private void RecordChanges(NestedScope current, NestedScope original, BulkDescriptor bulk)
        {
            var correlation = current.Aspects.CorrelateWith(original?.Aspects);

            // Ensure all of the current aspects have changes recorded
            foreach (var tuple in correlation.SourceItems)
            {
                tuple.Item.RecordChanges(tuple.PairedWith, bulk);
            }

            // Remove any now irrelevant aspects
            foreach (var aspect in correlation.NonSourceItems)
            {
                aspect.Item.RemoveAll(bulk);
            }
        }
    }
}
