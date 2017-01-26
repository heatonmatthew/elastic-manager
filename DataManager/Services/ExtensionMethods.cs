using System;
using System.Collections.Generic;
using System.Linq;

namespace DataManager.Services
{
    public static class ExtensionMethods
    {
        public class Correlated<TKey, TValue>
        {
            public Correlated(TKey key, TValue item)
            {
                Key = key;
                Item = item;
            }
            public Correlated(TKey key, TValue item, TValue pairedWith) : this(key, item)
            {
                PairedWith = pairedWith;
            }

            public static implicit operator Correlated<TKey, TValue>(KeyValuePair<TKey, TValue> pair)
            {
                return new Correlated<TKey, TValue>(pair.Key, pair.Value);
            }

            public TKey Key { get; set; }
            public TValue Item { get; set; }
            public TValue PairedWith { get; set; }
        }
        public class Correlation<TKey, TValue>
        {
            public IEnumerable<Correlated<TKey, TValue>> SourceItems { get; set; }
            public IEnumerable<Correlated<TKey, TValue>> NonSourceItems { get; set; }
        }

        public static Correlation<TKey, TValue> CorrelateWith<TKey, TValue>(
            this IDictionary<TKey, TValue> source,
            IDictionary<TKey, TValue> comparedTo)
        {
            if (comparedTo == null)
            {
                return new Correlation<TKey, TValue>
                {
                    SourceItems = source.Select(pair => (Correlated<TKey, TValue>) pair),
                    NonSourceItems = Enumerable.Empty<Correlated<TKey, TValue>>()
                };
            }

            return new Correlation<TKey, TValue>
            {
                SourceItems = source.Select(pair =>
                {
                    comparedTo.TryGetValue(pair.Key, out TValue pairedTo);
                    return new Correlated<TKey, TValue>(pair.Key, pair.Value, pairedTo);
                }),
                NonSourceItems = comparedTo
                    .Where(p => !source.ContainsKey(p.Key))
                    .Select(p => new Correlated<TKey, TValue>(p.Key, p.Value))
            };
        }
    }
}
