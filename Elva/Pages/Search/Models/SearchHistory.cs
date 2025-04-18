using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elva.Pages.Search.Models
{
    /// <summary>
    /// Represents a single history entry for the search system
    /// </summary>
    public class SearchHistoryEntry
    {
        /// <summary>
        /// The raw input string with all tags, sources, etc.
        /// </summary>
        public string FullInput { get; }

        /// <summary>
        /// The parsed search text without tags, sources, etc.
        /// </summary>
        public string SearchText { get; }

        /// <summary>
        /// When this search was performed
        /// </summary>
        public DateTime Timestamp { get; }

        public SearchHistoryEntry(string fullInput, string searchText)
        {
            FullInput = fullInput ?? string.Empty;
            SearchText = searchText ?? string.Empty;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Manages the history of search queries
    /// </summary>
    public class SearchHistory
    {
        // Store history as an array for simpler index navigation
        private readonly List<SearchHistoryEntry> _history = new();
        private int _currentIndex = -1;

        /// <summary>
        /// The maximum number of entries to keep in history
        /// </summary>
        public int MaxHistorySize { get; set; } = 5;

        /// <summary>
        /// Adds a new search entry to the history
        /// </summary>
        /// <param name="fullInput">The complete input with all tags and parameters</param>
        /// <param name="searchText">The parsed search text only</param>
        public void AddEntry(string fullInput, string searchText)
        {
            try
            {
                // Skip empty inputs
                if (string.IsNullOrWhiteSpace(fullInput))
                    return;

                // Skip duplicates of most recent entry
                if (_history.Count > 0 && _history[0].FullInput == fullInput)
                    return;

                Debug.WriteLine($"Adding history entry: Full='{fullInput}', Text='{searchText}'");

                // Add new entry at beginning of list
                _history.Insert(0, new SearchHistoryEntry(fullInput, searchText));

                // Trim history to maximum size
                while (_history.Count > MaxHistorySize)
                    _history.RemoveAt(_history.Count - 1);

                // Reset position
                _currentIndex = -1;

                Debug.WriteLine($"History size: {_history.Count} entries");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding history entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Navigate to previous entry in history (older)
        /// </summary>
        /// <param name="fullHistory">Whether to return full input or just search text</param>
        /// <returns>The previous history entry or empty if none</returns>
        public string NavigateUp(bool fullHistory)
        {
            if (_history.Count == 0)
            {
                Debug.WriteLine("NavigateUp: History is empty");
                return string.Empty;
            }

            // Move to next older item if possible
            if (_currentIndex < _history.Count - 1)
                _currentIndex++;

            Debug.WriteLine($"NavigateUp: Current index = {_currentIndex}, Full mode = {fullHistory}");

            if (_currentIndex >= 0 && _currentIndex < _history.Count)
            {
                string result = fullHistory
                    ? _history[_currentIndex].FullInput
                    : _history[_currentIndex].SearchText;

                Debug.WriteLine($"NavigateUp returning: '{result}'");
                return result;
            }

            Debug.WriteLine("NavigateUp: No valid entry found");
            return string.Empty;
        }

        /// <summary>
        /// Navigate to next entry in history (newer)
        /// </summary>
        /// <param name="fullHistory">Whether to return full input or just search text</param>
        /// <returns>The next history entry or empty if at beginning</returns>
        public string NavigateDown(bool fullHistory)
        {
            if (_history.Count == 0 || _currentIndex <= 0)
            {
                Debug.WriteLine("NavigateDown: At beginning or empty history");
                _currentIndex = -1;
                return string.Empty;
            }

            // Move to next newer item
            _currentIndex--;

            Debug.WriteLine($"NavigateDown: Current index = {_currentIndex}, Full mode = {fullHistory}");

            if (_currentIndex >= 0 && _currentIndex < _history.Count)
            {
                string result = fullHistory
                    ? _history[_currentIndex].FullInput
                    : _history[_currentIndex].SearchText;

                Debug.WriteLine($"NavigateDown returning: '{result}'");
                return result;
            }

            Debug.WriteLine("NavigateDown: No valid entry found");
            return string.Empty;
        }

        /// <summary>
        /// Resets the navigation position in history
        /// </summary>
        public void ResetNavigation()
        {
            Debug.WriteLine("ResetNavigation: Resetting current index");
            _currentIndex = -1;
        }

        /// <summary>
        /// Gets all history entries
        /// </summary>
        /// <returns>List of history entries</returns>
        public IReadOnlyList<SearchHistoryEntry> GetEntries()
        {
            return _history.AsReadOnly();
        }

        /// <summary>
        /// Debug method to print current history state
        /// </summary>
        public void DebugPrintHistory()
        {
            Debug.WriteLine($"=== History ({_history.Count} entries, current index: {_currentIndex}) ===");
            for (int i = 0; i < _history.Count; i++)
            {
                Debug.WriteLine($"[{i}] Full: '{_history[i].FullInput}', Text: '{_history[i].SearchText}'");
            }
            Debug.WriteLine("===================================");
        }
    }
}