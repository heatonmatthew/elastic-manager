using System;
using System.Collections.Generic;
using System.Linq;
using Nest;

namespace DataManager.Services.Scoping
{
    /// <summary>
    /// Provides the ability to have nested scope so that
    /// lookup occurs from the innermost to top.
    /// </summary>
    public class NestedScope
    {
        public NestedScope(NestedScope parent = null)
        {
            Parent = parent;
            Children = new List<NestedScope>();
            Aspects = new Dictionary<Type, INestedAspect>();

            parent?.Children.Add(this);
        }

        public NestedScope Parent { get; }
        public List<NestedScope> Children { get; }
        public Dictionary<Type, INestedAspect> Aspects { get; }
        public string Name { get; set; }

        public bool IsTopMost => Parent == null;

        public T GetAspect<T>()
            where T : class, INestedAspect, new()
        {
            if (!Aspects.TryGetValue(typeof(T), out var result))
            {
                result = SetAspect(new T());
            }
            return (T) result;
        }

        public T SetAspect<T>(T aspect)
            where T : class, INestedAspect, new()
        {
            aspect.Scope = this;
            Aspects[typeof(T)] = aspect;
            return aspect;
        }

        public NestedScope Clone()
        {
            var stack = new Stack<string>();

            var root = this;
            while (root.Parent != null)
            {
                stack.Push(root.Name);
                root = root.Parent;
            }

            var result = root.Clone(null);
            while (stack.Count > 0)
            {
                var n = stack.Pop();
                result = result.Children.First(c => c.Name == n);
            }
            return result;
        }

        // TODO - Need to test these methods
        private NestedScope Clone(NestedScope parent)
        {
            var clone = new NestedScope(parent) {Name = Name};
            foreach (var pair in Aspects)
            {
                var aspectClone = pair.Value.Clone();
                aspectClone.Scope = clone;
                clone.Aspects.Add(pair.Key, aspectClone);
            }
            return clone;
        }

        public IEnumerable<NestedScope> Flatten()
        {
            yield return this;
            foreach (var scope in Children.SelectMany(c => c.Flatten()))
            {
                yield return scope;
            }
        }

        public IDictionary<string, NestedScope> ToDictionary()
        {
            return Flatten().ToDictionary(i => i.Name, i => i);
        }

        public static IDictionary<string, NestedScope> BuildHeirarchy(IEnumerable<string> packageNames, IErrorCache errorCache, IDictionary<string, NestedScope> existing = null)
        {
            var orderedPairs = packageNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(name => existing == null || !existing.ContainsKey(name))
                .Select(Parse)
                .OrderBy(t => t.Item1.Length);

            var dict = existing ?? new Dictionary<string, NestedScope>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in orderedPairs)
            {
                NestedScope parent = null;
                if (pair.Item1 != "" && !dict.TryGetValue(pair.Item1, out parent))
                {
                    errorCache.AddError($"Could not locate parent '{pair.Item1}' when building heirarchy.");
                }

                var scope = new NestedScope(parent) {Name = pair.Item2};
                dict.Add(pair.Item2, scope);
            }

            return dict;
        }

        private static Tuple<string, string> Parse(string name)
        {
            var index = name.LastIndexOf('.');
            return index <= 0
                ? Tuple.Create("", name)
                : Tuple.Create(name.Substring(0, index), name);
        }
    }

    public interface INestedAspect
    {
        NestedScope Scope { get; set; }
        INestedAspect Clone();
        void RecordChanges(INestedAspect compareTo, BulkDescriptor bulk);
        void RemoveAll(BulkDescriptor bulk);
    }
}
