using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SKPractice.Plugins;

public class TimePlugin
{
    [KernelFunction, Description("Gets the current time in a specified timezone")]
    public string GetCurrentTime(
        [Description("The timezone, e.g. 'Eastern', 'Central', 'Pacific', 'UTC', 'London', 'Sydney'")] string timezone)
    {
        var tz = timezone.ToLower() switch
        {
            "eastern" or "new york" or "est" => TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
            "central" or "dallas" or "cst" => TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"),
            "pacific" or "los angeles" or "pst" => TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time"),
            "london" or "gmt" or "uk" => TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time"),
            "sydney" or "aest" or "australia" => TimeZoneInfo.FindSystemTimeZoneById("AUS Eastern Standard Time"),
            "utc" => TimeZoneInfo.Utc,
            _ => TimeZoneInfo.Local
        };

        var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);
        return $"Current time in {timezone}: {time:hh:mm tt} ({time:dddd, MMMM dd, yyyy})";
    }
}