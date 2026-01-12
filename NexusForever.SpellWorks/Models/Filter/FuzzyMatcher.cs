using System;

namespace NexusForever.SpellWorks.Models.Filter
{
    internal static class FuzzyMatcher
    {
        public static double GetNormalizedSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
                return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
                return 0.0;

            a = a.Trim();
            b = b.Trim();

            int dist = LevenshteinDistance(a, b);
            int max = Math.Max(a.Length, b.Length);
            if (max == 0) return 1.0;
            return 1.0 - (double)dist / max;
        }

        public static bool IsFuzzyMatch(string text, string pattern, double threshold = 0.7)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return true;

            if (string.IsNullOrEmpty(text))
                return false;

            if (text.IndexOf(pattern, StringComparison.InvariantCultureIgnoreCase) >= 0)
                return true;

            double sim = GetNormalizedSimilarity(
                text.ToLowerInvariant(),
                pattern.ToLowerInvariant());

            return sim >= threshold;
        }
        // Note: If performance becomes an issue, consider adding a dependency for fuzzy matching
        private static int LevenshteinDistance(string s, string t)
        {
            if (s == t) return 0;
            if (s.Length == 0) return t.Length;
            if (t.Length == 0) return s.Length;

            if (s.Length < t.Length)
            {
                var tmp = s; s = t; t = tmp;
            }

            int n = s.Length;
            int m = t.Length;
            int[] previous = new int[m + 1];
            int[] current = new int[m + 1];

            for (int j = 0; j <= m; j++)
                previous[j] = j;

            for (int i = 1; i <= n; i++)
            {
                current[0] = i;
                char si = s[i - 1];

                for (int j = 1; j <= m; j++)
                {
                    int cost = (si == t[j - 1]) ? 0 : 1;
                    int insertion = current[j - 1] + 1;
                    int deletion = previous[j] + 1;
                    int substitution = previous[j - 1] + cost;
                    int value = insertion;
                    if (deletion < value) value = deletion;
                    if (substitution < value) value = substitution;
                    current[j] = value;
                }

                // swap arrays
                var tmp = previous;
                previous = current;
                current = tmp;
            }

            return previous[m];
        }
    }
}