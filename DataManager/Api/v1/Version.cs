using System;
using System.Text.RegularExpressions;
using DataManager.Services.Converters;
using Newtonsoft.Json;

namespace DataManager.Api.v1
{
    [JsonConverter(typeof(VersionConverter))]
    public class Version : IComparable<Version>, IEquatable<Version>
    {
        /// <summary>
        /// A new version of the schema, previously logged data requires a
        /// migration to be supported under the new API.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// A minor change to the schema that should be backwards compatible.
        /// </summary>
        /// <remarks>
        /// Basically fields can be added and others deprecated, but the client
        /// should be able to infer how to handle the differences implicitly.
        /// </remarks>
        public int Minor { get; set; }

        /// <summary>
        /// Revision changes shouldn't require any migration.
        /// </summary>
        public int Revision { get; set; }

        public string ToVersionString()
        {
            return $"{Major}.{Minor}.{Revision}";
        }

        public static Version Parse(string value)
        {
            var match = Regex.Match(value, @"^\s*(\d+)\.(\d+)\.(\d+)\s*$");
            if (!match.Success) throw new ArgumentException("Could not parse as a version number", nameof(value));

            return new Version
            {
                Major = int.Parse(match.Groups[1].Value),
                Minor = int.Parse(match.Groups[2].Value),
                Revision = int.Parse(match.Groups[3].Value)
            };
        }

        public int CompareTo(Version other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            var majorComparison = Major.CompareTo(other.Major);
            if (majorComparison != 0) return majorComparison;
            var minorComparison = Minor.CompareTo(other.Minor);
            if (minorComparison != 0) return minorComparison;
            return Revision.CompareTo(other.Revision);
        }

        public bool Equals(Version other)
        {
            return CompareTo(other) == 0;
        }
    }
}
