﻿using System;
using System.Linq;

namespace ItemFilterLibraryDatabase.Utilities;

public static class FuzzyMatcher
{
    private static int LevenshteinDistance(string s1, string s2)
    {
        var costs = new int[s2.Length + 1];
        for (var i = 0; i <= s1.Length; i++)
        {
            var lastValue = i;
            for (var j = 0; j <= s2.Length; j++)
            {
                if (i == 0)
                {
                    costs[j] = j;
                }
                else if (j > 0)
                {
                    var newValue = costs[j - 1];
                    if (s1[i - 1] != s2[j - 1])
                    {
                        newValue = Math.Min(Math.Min(newValue, lastValue), costs[j]) + 1;
                    }

                    costs[j - 1] = lastValue;
                    lastValue = newValue;
                }
            }

            if (i > 0) costs[s2.Length] = lastValue;
        }

        return costs[s2.Length];
    }

    public static bool FuzzyMatch(string pattern, string input, double threshold = 0.7)
    {
        if (string.IsNullOrEmpty(pattern)) return true;
        if (string.IsNullOrEmpty(input)) return false;

        pattern = pattern.ToLowerInvariant();
        input = input.ToLowerInvariant();

        var patternWords = pattern.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        var inputWords = input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

        foreach (var patternWord in patternWords)
        {
            var wordMatched = false;

            foreach (var inputWord in inputWords)
            {
                if (inputWord.Contains(patternWord))
                {
                    wordMatched = true;
                    break;
                }

                if (inputWord.StartsWith(patternWord))
                {
                    wordMatched = true;
                    break;
                }

                if (patternWord.Length >= 3)
                {
                    var distance = LevenshteinDistance(patternWord, inputWord);
                    var similarity = 1 - (double)distance / Math.Max(patternWord.Length, inputWord.Length);

                    if (similarity >= threshold)
                    {
                        wordMatched = true;
                        break;
                    }
                }
            }

            if (!wordMatched)
            {
                return false;
            }
        }

        return true;
    }

    public static int GetMatchScore(string pattern, string input)
    {
        if (string.IsNullOrEmpty(pattern)) return 100;
        if (string.IsNullOrEmpty(input)) return 0;

        pattern = pattern.ToLowerInvariant();
        input = input.ToLowerInvariant();

        var patternWords = pattern.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        var inputWords = input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        var wordScores = new int[patternWords.Length];

        for (var i = 0; i < patternWords.Length; i++)
        {
            var patternWord = patternWords[i];
            var bestWordScore = 0;

            foreach (var inputWord in inputWords)
            {
                var score = 0;

                if (inputWord.Equals(patternWord))
                {
                    score = 100;
                }
                else if (inputWord.Contains(patternWord))
                {
                    score = 75;
                }
                else if (inputWord.StartsWith(patternWord))
                {
                    score = 60;
                }
                else if (patternWord.Length >= 3)
                {
                    var distance = LevenshteinDistance(patternWord, inputWord);
                    var similarity = 1 - (double)distance / Math.Max(patternWord.Length, inputWord.Length);
                    score = (int)(similarity * 50);
                }

                bestWordScore = Math.Max(bestWordScore, score);
            }

            wordScores[i] = bestWordScore;
        }

        return (int)wordScores.Average();
    }
}