using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ApexSyncTool.Core
{
    /// <summary>
    /// Handles Steam launch parameter parsing, validation, and formatting
    /// </summary>
    public class LaunchParameterFormatter
    {
        /// <summary>
        /// Preset launch parameters
        /// </summary>
        public static Dictionary<string, string> GetPresets()
        {
            return new Dictionary<string, string>
            {
                { "跳过片头", "-dev -novid" },
                { "英语配音", "+miles_language english" }
            };
        }

        /// <summary>
        /// Format and deduplicate launch parameters
        /// </summary>
        public static string Format(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
                return string.Empty;

            // Split by whitespace
            var parts = Regex.Split(parameters, @"\s+")
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            // Deduplicate consecutive identical parameters
            var deduplicated = new List<string>();
            foreach (var part in parts)
            {
                if (!deduplicated.Contains(part))
                {
                    deduplicated.Add(part);
                }
            }

            // Join with single space
            return string.Join(" ", deduplicated).Trim();
        }

        /// <summary>
        /// Toggle a parameter group in the launch parameters
        /// </summary>
        public static string ToggleParameter(string currentParameters, string parameterGroup)
        {
            if (string.IsNullOrWhiteSpace(parameterGroup))
                return currentParameters ?? string.Empty;

            var params_ = (currentParameters ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var groupParams = parameterGroup.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if all parameters in the group exist
            bool allExist = groupParams.All(p => params_.Contains(p));

            if (allExist)
            {
                // Remove all parameters in the group
                foreach (var param in groupParams)
                {
                    params_.RemoveAll(p => p == param);
                }
            }
            else
            {
                // Remove any existing parameters from the group
                foreach (var param in groupParams)
                {
                    params_.RemoveAll(p => p == param);
                }
                // Add all parameters from the group
                foreach (var param in groupParams)
                {
                    params_.Add(param);
                }
            }

            // Format the result
            return Format(string.Join(" ", params_));
        }

        /// <summary>
        /// Check if a parameter group is active
        /// </summary>
        public static bool IsParameterActive(string currentParameters, string parameterGroup)
        {
            if (string.IsNullOrWhiteSpace(parameterGroup))
                return false;

            var params_ = (currentParameters ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var groupParams = parameterGroup.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Check if all parameters in the group exist
            return groupParams.All(p => params_.Contains(p));
        }

        /// <summary>
        /// Validate launch parameters format
        /// </summary>
        public static bool Validate(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
                return true;

            var parts = parameters.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                if (!part.StartsWith("-") && !part.StartsWith("+"))
                    return false;
            }
            return true;
        }
    }
}
