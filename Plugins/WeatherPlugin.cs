using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SKPractice.Plugins;

public class WeatherPlugin
{
    [KernelFunction, Description("Gets the current weather for a city")]
    public string GetWeather(
        [Description("The city name, e.g. 'Dallas', 'New York', 'London'")] string city)
    {
        // Fake data — in production you'd call a real weather API
        var weather = city.ToLower() switch
        {
            "dallas" or "coppell" => "82°F (28°C), Sunny, Humidity: 45%",
            "new york" => "65°F (18°C), Partly Cloudy, Humidity: 60%",
            "london" => "55°F (13°C), Rainy, Humidity: 80%",
            "sydney" => "72°F (22°C), Clear, Humidity: 55%",
            "san diego" => "70°F (21°C), Sunny, Humidity: 40%",
            _ => $"68°F (20°C), Fair conditions in {city}"
        };

        return $"Weather in {city}: {weather}";
    }
}