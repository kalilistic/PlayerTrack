using System;
using System.Collections.Generic;
using System.Linq;
using PlayerTrack.Models;

namespace PlayerTrack.Domain;

public static class PlayerSearchService
{
    private static readonly Dictionary<string, (Func<Player, string, bool> match, Func<Player, bool> hasValue)> Handlers =
        new()
        {
            ["fc"] = (
                (p, v) => MatchPrefix(p.FreeCompany.Value, v),
                p => !string.IsNullOrEmpty(p.FreeCompany.Value)
            ),
            ["tags"] = (
                (p, v) => p.AssignedTags.Any(tag => MatchPrefix(tag.Name, v)),
                p => p.AssignedTags.Count > 0
            ),
            ["notes"] = (
                (p, v) => MatchPrefix(p.Notes, v),
                p => !string.IsNullOrEmpty(p.Notes)
            ),
            ["race"] = (
                (p, v) => MatchPrefix(p.RaceName(), v),
                p => !string.IsNullOrEmpty(p.RaceName().ToString())
            ),
            ["gender"] = (
                (p, v) => MatchPrefix(p.GenderName(), v),
                p => !string.IsNullOrEmpty(p.GenderName().ToString())
            ),
            ["world"] = (
                (p, v) => MatchPrefix(p.WorldName(), v),
                p => !string.IsNullOrEmpty(p.WorldName().ToString())
            ),
            ["dc"] = (
                (p, v) => MatchPrefix(p.DataCenterName(), v),
                p => !string.IsNullOrEmpty(p.DataCenterName().ToString())
            ),
        };

    public static Func<Player, bool> GetSearchFilter(string searchString, SearchType searchType)
    {
        if (string.IsNullOrWhiteSpace(searchString)) return _ => true;
        var tokens = searchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var filters = tokens.Select(token => CreateSingleFilter(token, searchType)).ToList();
        return player => filters.All(f => f(player));
    }

    private static Func<Player, bool> CreateSingleFilter(string token, SearchType searchType)
    {
        var (isNegated, key, value) = ParseSearchString(token);
        if (Handlers.TryGetValue(key, out var h))
        {
            return player =>
            {
                var result = value == "!"
                    ? !h.hasValue(player)
                    : h.match(player, value);
                return isNegated ? !result : result;
            };
        }
        else
        {
            return player =>
            {
                var result = Match(player.Name, value, searchType);
                return isNegated ? !result : result;
            };
        }
    }

    private static (bool isNegated, string key, string value) ParseSearchString(string searchString)
    {
        var isGloballyNegated = searchString.StartsWith("!");
        var normalized = isGloballyNegated ? searchString[1..] : searchString;
        var parts = normalized.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) return (isGloballyNegated, "default", normalized);
        var key = parts[0].ToLowerInvariant();
        var value = parts[1];
        var isValueNegated = value.StartsWith("!");
        if (isValueNegated) value = value[1..];
        var isNegated = isGloballyNegated || isValueNegated;
        return (isNegated, key, value);
    }

    private static bool MatchPrefix(string field, string pattern)
    {
        if (string.IsNullOrEmpty(field)) return false;
        return pattern switch
        {
            "*" => true,
            not null when pattern.StartsWith("*") && pattern.EndsWith("*") =>
                field.Contains(pattern.Trim('*'), StringComparison.OrdinalIgnoreCase),
            not null when pattern.StartsWith("*") =>
                field.EndsWith(pattern.TrimStart('*'), StringComparison.OrdinalIgnoreCase),
            not null when pattern.EndsWith("*") =>
                field.StartsWith(pattern.TrimEnd('*'), StringComparison.OrdinalIgnoreCase),
            _ => field.Equals(pattern, StringComparison.OrdinalIgnoreCase),
        };
    }

    private static bool Match(string? field, string searchValue, SearchType searchType)
    {
        if (string.IsNullOrEmpty(field)) return false;
        return searchType switch
        {
            SearchType.Contains => field.Contains(searchValue, StringComparison.OrdinalIgnoreCase),
            SearchType.StartsWith => field.StartsWith(searchValue, StringComparison.OrdinalIgnoreCase),
            SearchType.Exact => field.Equals(searchValue, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    public static bool IsValidSearch(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString)) return true;
        var tokens = searchString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return tokens.All(IsValidToken);
    }

    private static bool IsValidToken(string token)
    {
        var colonIndex = token.IndexOf(':');
        if (colonIndex == -1) return !token.Contains('!') && !token.Contains('*');
        if (colonIndex == token.Length - 1) return false;
        for (var i = 0; i < colonIndex; i++)
        {
            if (token[i] == '!' || token[i] == '*') return false;
        }
        return true;
    }
}