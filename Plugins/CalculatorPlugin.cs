using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SKPractice.Plugins;
public class CalculatorPlugin
{
    [KernelFunction, Description("Calculates tax amount given a price and tax rate percentage")]
    public string CalculateTax(
        [Description("The price amount in dollars")] double amount,
        [Description("The tax rate as a percentage, e.g. 8.25 for 8.25%")] double taxRate)
    {
        var tax = amount * (taxRate / 100);
        var total = amount + tax;
        return $"Amount: ${amount:F2}, Tax ({taxRate}%): ${tax:F2}, Total: ${total:F2}";
    }

    [KernelFunction, Description("Converts a temperature between Fahrenheit and Celsius")]
    public string ConvertTemperature(
        [Description("The temperature value")] double temperature,
        [Description("The unit to convert FROM: 'F' for Fahrenheit or 'C' for Celsius")] string fromUnit)
    {
        if (fromUnit.ToUpper() == "F")
        {
            var celsius = (temperature - 32) * 5 / 9;
            return $"{temperature}°F = {celsius:F1}°C";
        }
        else
        {
            var fahrenheit = (temperature * 9 / 5) + 32;
            return $"{temperature}°C = {fahrenheit:F1}°F";
        }
    }
}