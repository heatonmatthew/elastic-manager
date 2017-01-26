using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DataManager.Services.Scoping;
using Nest;
using Newtonsoft.Json;

namespace DataManager.Services.GlobalSetup
{
    public interface IScriptLoader
    {
        string LoadGlobalDataJson();
        IDictionary<string, TemplateMapping> LoadTemplates();
    }

    /// <summary>
    /// Handles loading and substituting variables in scripts.
    /// </summary>
    public class ScriptLoader : IScriptLoader
    {
        public NestedScope Scope { get; set; }
        public IErrorCache ErrorCache { get; set; }

        // Member variables
        private readonly Regex variableMatcher = new Regex(variableRegex, RegexOptions.IgnoreCase);

        // Regular expression for parsing the scripts for variables
        private const string variableRegex = @"(\@\@(\w+)\@\@)";

        public string LoadGlobalDataJson()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, TemplateMapping> LoadTemplates()
        {
            throw new NotImplementedException();
        }

        public string SubstituteVariables(string script)
        {
            // Extract the variable tokens from the script
            var matches = variableMatcher.Matches(script);
            for (int i = 0; i < matches.Count; ++i)
            {
                var match = matches[i];
                var symbol = match.Groups[1].Value;
                var variableName = match.Groups[2].Value;

                // Get the variable and translate it for substitution
                var variable = Scope.FirstVarOfName(variableName, ErrorCache);
                if (variable != null)
                {
                    var value = variable.GetValueOrDefault();
                    var jsonValue = JsonConvert.ToString(value);
                    script = script.Replace(symbol, jsonValue);
                }
            }

            return script;
        }
    }
}
