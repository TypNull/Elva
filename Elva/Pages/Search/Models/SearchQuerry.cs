using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Elva.Pages.Search.Models
{
    /// <summary>
    /// Represents a parsed search query with structured access to sources, tags, and search terms
    /// </summary>
    public class SearchQuery
    {
        // Parsed query components
        public List<string> Sources { get; } = new();
        public List<string> IncludeTags { get; } = new();
        public List<string> ExcludeTags { get; } = new();
        public Dictionary<string, string> Parameters { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string SearchText { get; private set; } = string.Empty;
        public bool AppendSources { get; private set; }

        // Original query for reference
        public string OriginalQuery { get; }

        // Regular expressions for parsing
        private static readonly Regex SourceRegex = new(@"@(\+)?([^\s@#;/+]+)");
        private static readonly Regex TagRegex = new(@"#(\[)?([^\s@#;/+\[\]]+)(\])?");
        private static readonly Regex ParamRegex = new(@"(author:|a:)([^\s@#;/+]+)");

        public SearchQuery(string queryText)
        {
            OriginalQuery = queryText;
            Parse(queryText);
        }

        private void Parse(string queryText)
        {
            string workingText = queryText;

            // Extract sources (@source, @+source)
            foreach (Match match in SourceRegex.Matches(workingText))
            {
                string plusSign = match.Groups[1].Value;
                string sourceName = match.Groups[2].Value;

                // If this is the first source and it has a plus, we're appending
                if (Sources.Count == 0 && plusSign == "+")
                    AppendSources = true;

                Sources.Add(sourceName);
                workingText = workingText.Replace(match.Value, " ");
            }

            // Extract tags (#tag, #[tag])
            foreach (Match match in TagRegex.Matches(workingText))
            {
                string openBracket = match.Groups[1].Value;
                string tag = match.Groups[2].Value;
                string closeBracket = match.Groups[3].Value;

                // Check if this is an exclusion tag
                if (openBracket == "[" && closeBracket == "]")
                    ExcludeTags.Add(tag);
                else
                    IncludeTags.Add(tag);

                workingText = workingText.Replace(match.Value, " ");
            }

            // Extract parameters (author:name, a:name)
            foreach (Match match in ParamRegex.Matches(workingText))
            {
                string key = match.Groups[1].Value.TrimEnd(':').ToLower();
                string value = match.Groups[2].Value;

                Parameters[key] = value;
                workingText = workingText.Replace(match.Value, " ");
            }

            // Remaining text is the search term (clean up extra whitespace)
            SearchText = Regex.Replace(workingText, @"\s+", " ").Trim();
        }
    }
}