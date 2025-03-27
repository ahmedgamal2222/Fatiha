using Fatiha__app.Models;
using System.Globalization;

namespace Fatiha__app.Extension
{
    public static class Functions
    {
        public static string GetCulture(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
            {
                return "en-US"; // Default culture code
            }
            Console.WriteLine("Language Name: " + languageName);

            switch (languageName.ToLowerInvariant())
            {
                case "arabic":
                    return "ar-SA";
                case "english":
                    return "en-US";
                case "mandarin chinese":
                    return "zh-CN"; // Assuming simplified Chinese for Mandarin
                case "spanish":
                    return "es-ES";
                case "hindi":
                    return "hi-IN";
                case "french":
                    return "fr-FR";
                case "russian":
                    return "ru-RU";
                case "bengali":
                    return "bn-BD"; // Assuming Bangladesh Bengali
                case "portuguese":
                    return "pt-BR";
                case "urdu":
                    return "ur-PK"; // Assuming Pakistan Urdu
                case "indonesian":
                    return "id-ID";
               

                default:
                    // Handle the case where the language name is not recognized
                    return "en-US"; // Set a default culture code or handle this case as needed
            }
        }
        public static string FormatDate(object dateObj)
        {
            if (dateObj == null)
            {
                // Handle null object, for example, return an empty string or a default date string
                return string.Empty;
            }

            if (dateObj is DateTime date)
            {
                CultureInfo culture = new CultureInfo("en-US");
                return date.ToString("d", culture); // "d" represents a short date pattern
            }
            else
            {
                // Handle the case where dateObj is not a DateTime
                throw new ArgumentException("The provided object is not a DateTime", nameof(dateObj));
            }
        }
        public static string GetStatusIcon(FatihaRequestStatus status)
        {
            return status switch
            {
                FatihaRequestStatus.Open => "fas fa-clock",
                FatihaRequestStatus.Processing => "fas fa-cog",
                FatihaRequestStatus.Closed => "fas fa-check-circle",
                FatihaRequestStatus.Qualified => "fas fa-check-circle",
                _ => "fas fa-question-circle", // default case
            };
        }
    }
}
