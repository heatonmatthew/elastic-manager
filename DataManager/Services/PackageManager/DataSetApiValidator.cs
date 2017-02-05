using System;
using System.Collections.Generic;
using System.Linq;
using DataManager.Api.v1;
using DataManager.Schemas.v1.Jobs;
using DataManager.Services.PackageManager.Settings;
using DataManager.Services.Scoping;
using DataManager.Services.Variables;
using EnumStringValues;
using Newtonsoft.Json.Linq;

namespace DataManager.Services.PackageManager
{
    /// <summary>
    /// Handles loading and validating a <c>DataSet</c>
    /// definition received over the API.
    /// </summary>
    public class DataSetApiValidator
    {
        public string PackageId { get; set; }
        public NestedScope Scope { get; set; }
        public IErrorCache ErrorCache { get; set; }

        #region Maintenance Jobs and Settings

        public IEnumerable<JobBase> DetermineJobs(DataSet dataSet)
        {
            if (dataSet.Grain == PartitionGrain.None) yield break;

            var manageInfo = dataSet.Manage;

            yield return CreateScheduledJob(dataSet, manageInfo.ActiveAge, Constants.GlobalSettings.ActiveAge);
            yield return CreateScheduledJob(dataSet, manageInfo.RetentionAge, Constants.GlobalSettings.RetentionPeriod);

            // TODO - archive setting
        }

        private ScheduledJob CreateScheduledJob(DataSet dataSet, SettingValue settingValue, string settingName)
        {
            var variable = LocateSettingVariable(settingValue, settingName);
            if (variable == null) return null;

            var job = new ScheduledJob
            {
                Name = settingName,
                PackageId = PackageId,
                Dataset = dataSet.Name,
                SettingName = settingName,
                VariableName = variable.Ref.ToFullName(),
                NextExecution = DateTime.Now.Add(TimeSpan.FromSeconds(20))
            };

            job.Created = job.Updated = DateTime.Now;
            job.CreatedBy = job.UpdatedBy = Constants.PackageManagerIdentity;

            return job;
        }

        public VariableInfo LocateSettingVariable(SettingValue setting, string settingName)
        {
            switch (setting?.ValueType)
            {
                case SettingValueType.Variable:
                    if (string.IsNullOrWhiteSpace(setting.FromVariable.PackageId))
                    {
                        setting.FromVariable.PackageId = PackageId;
                    }
                    return LookupReferencedVariable(setting.FromVariable);

                case SettingValueType.Constant:
                    return LoadConstantSetting(setting.ConstantValue, settingName);
            }

            return LookupGlobalDefaultSetting(settingName);
        }

        public VariableInfo LoadConstantSetting(Constant constant, string settingName)
        {
            // Lookup a previously stored version of this variable
            var variableRef = new VariableRef
            {
                PackageId = PackageId,
                VariableName = $"constant.{settingName}"
            };
            if (!Scope.TryFindVariable(variableRef, out var variable))
            {
                // This is the first time a constant has been used here
                // so create a new variable for it
                variable = new Variable {Name = variableRef.VariableName};
            }

            variable.Value = variable.Default = constant.Value.ToString();
            variable.Units = constant.Units.GetStringValue();
            variable.Type = constant.SupportedType.GetStringValue();
            variable.IsGenerated = true;

            return VariableInfo.Extract(variable, PackageId, ErrorCache);
        }

        public VariableInfo LookupReferencedVariable(VariableRef variableRef)
        {
            if (!Scope.TryFindVariable(variableRef, out var variable))
            {
                ErrorCache.AddError($"Could not find variable: '{variableRef.ToFullName()}'");
                return null;
            }
            return VariableInfo.Extract(variable, variableRef.PackageId, ErrorCache);
        }

        public VariableInfo LookupGlobalDefaultSetting(string settingName)
        {
            var globalRef = new VariableRef
            {
                PackageId = Constants.PackageManagerIdentity,
                VariableName = settingName
            };

            if (!Scope.TryFindVariable(globalRef, out var variable))
            {
                ErrorCache.AddError($"Could not find global default variable: '{globalRef.ToFullName()}'");
                return null;
            }
            return VariableInfo.Extract(variable, globalRef.PackageId, ErrorCache);
        }

        #endregion

        #region Index Template Settings

        public void ApplyTemplateSettings(DataSet dataSet)
        {
            switch (dataSet.Grain)
            {
                case PartitionGrain.PerDay:
                    ApplyTimeSeriesTemplateSettings(dataSet);
                    break;
                case PartitionGrain.None:
                    // Nothing to do
                    break;
                default:
                    throw new InvalidOperationException($"Unhandled parition grain '{dataSet.Grain}' in {nameof(ApplyTemplateSettings)}");
            }
        }

        private void ApplyTimeSeriesTemplateSettings(DataSet dataSet)
        {
            var templateSettings = dataSet.Template.Settings;

            var settings = TimeSeriesSettings.SettingForLifestage(Scope, ErrorCache, active: true);
            foreach (var pair in settings.Where(p => !templateSettings.ContainsKey(p.Item1)))
            {
                templateSettings[pair.Item1] = JToken.FromObject(pair.Item2);
            }
        }

        #endregion

        #region Index Template Aliases

        #endregion
    }
}
