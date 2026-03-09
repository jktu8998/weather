namespace Weather.Extensions;

using System.Text.RegularExpressions;

 
public static class CityNameValidator
{
     private static readonly Regex ValidCityRegex = new(@"^[\p{L}\s\-]+$", RegexOptions.Compiled);

    public static bool IsValid(string cityName, out string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(cityName))
        {
            errorMessage = "City name cannot be empty.";
            return false;
        }

        if (cityName.Length < 2 || cityName.Length > 50)
        {
            errorMessage = "City name must be between 2 and 50 characters.";
            return false;
        }

        if (!ValidCityRegex.IsMatch(cityName))
        {
            errorMessage = "City name can only contain letters, spaces, and hyphens.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}