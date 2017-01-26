using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataManager.Api.v1;
using DataManager.Services.Variables;
using Nest;

namespace DataManager.Services.Scoping
{
    public static class VariableSetExtensions
    {
        public static NestedScope Store(this NestedScope scope, Variable variable)
        {
            scope.GetAspect<VariableSet>().Store(variable);
            return scope;
        }

        public static IEnumerable<Variable> Variables(this NestedScope scope, bool recurse = true)
        {
            return !recurse
                ? scope.GetAspect<VariableSet>()
                : scope.Flatten().SelectMany(s => s.GetAspect<VariableSet>());
        }

        public static Variable FindVariable(this NestedScope scope, VariableRef refName)
        {
            return scope.GetAspect<VariableSet>().Find(refName);
        }
        public static bool TryFindVariable(this NestedScope scope, VariableRef refName, out Variable result)
        {
            return scope.GetAspect<VariableSet>().TryFind(refName, out result);
        }
        public static Variable FindVariable(this NestedScope scope, string fullName)
        {
            return scope.GetAspect<VariableSet>().Find(fullName);
        }
        public static bool TryFindVariable(this NestedScope scope, string fullName, out Variable result)
        {
            return scope.GetAspect<VariableSet>().TryFind(fullName, out result);
        }

        public static VariableInfo FirstVarOfName(this NestedScope scope, string variableName, IErrorCache errorCache)
        {
            return scope.GetAspect<VariableSet>().FirstVarOfName(variableName, errorCache);
        }
    }

    /// <summary>
    /// The set of variables applicable to the
    /// current scope.
    /// </summary>
    public class VariableSet : INestedAspect, IEnumerable<Variable>
    {
        // Member data
        private readonly Dictionary<string, Variable> byFullName = new Dictionary<string, Variable>();
        private readonly Dictionary<string, Variable> byLocalName = new Dictionary<string, Variable>();

        public NestedScope Scope { get; set; }

        public INestedAspect Clone()
        {
            var result = new VariableSet();
            foreach (var pair in byLocalName)
            {
                var variableClone = pair.Value.Clone();
                result.Store(variableClone);
            }
            return result;
        }

        public virtual void Store(Variable variable)
        {
            var varRef = new VariableRef {PackageId = variable.PackageId, VariableName = variable.Name};

            byFullName[varRef.ToFullName()] = variable;
            byLocalName[variable.Name] = variable;
        }

        public virtual Variable Find(VariableRef refName)
        {
            return Find(refName.ToFullName());
        }

        public virtual bool TryFind(VariableRef refName, out Variable result)
        {
            return TryFind(refName.ToFullName(), out result);
        }

        public virtual Variable Find(string fullName)
        {
            if (!TryFind(fullName, out Variable result))
            {
                throw new ArgumentOutOfRangeException(nameof(fullName), fullName,
                    $"Could not find variable in the {nameof(VariableSet)}.");
            }
            return result;
        }

        public virtual bool TryFind(string fullName, out Variable result)
        {
            if (byFullName.TryGetValue(fullName, out result))
            {
                return true;
            }

            var inParent = Scope?.Parent?.TryFindVariable(fullName, out result);
            if (inParent.GetValueOrDefault())
            {
                return true;
            }

            return false;
        }

        public virtual VariableInfo FirstVarOfName(string variableName, IErrorCache errorCache)
        {
            if (byLocalName.TryGetValue(variableName, out var result))
            {
                return VariableInfo.Extract(result, result.PackageId, errorCache);
            }

            if (Scope?.Parent != null)
            {
                return Scope.Parent.FirstVarOfName(variableName, errorCache);
            }

            errorCache.AddError($"Could not FirstVarOfName for variable called {variableName}");
            return null;
        }

        public void RecordChanges(INestedAspect compareTo, BulkDescriptor bulk)
        {
            var correlation = byLocalName.CorrelateWith((compareTo as VariableSet)?.byLocalName);

            bulk.IndexMany(correlation.SourceItems.Where(t => t.Item.IsChanged(t.PairedWith)).Select(t => t.Item));
            bulk.DeleteMany(correlation.NonSourceItems.Select(v => v.Item.Id));
        }

        public void RemoveAll(BulkDescriptor bulk)
        {
            bulk.DeleteMany(byLocalName.Values.Select(v => v.Id));
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<Variable> GetEnumerator()
        {
            return byLocalName.Values.GetEnumerator();
        }
    }
}
