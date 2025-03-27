using Microsoft.AspNetCore.Localization;

namespace Fatiha__app.Extension
{
    public static class Language
    {
        // Method to get the current culture from the cookie.
        public static string GetCulture(IRequestCookieCollection requestCookies)
        {
            // The default culture.
            var culture = "en-US";

            if (requestCookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out string cookieValue))
            {
                var cookieRequestCulture = CookieRequestCultureProvider.ParseCookieValue(cookieValue);
                culture = cookieRequestCulture.Cultures[0].Value; // Get the first culture from the cookie.
            }

            return culture;
        }

        // Method to set the culture in a cookie.
        public static void SetCulture(IResponseCookies responseCookies, string culture, DateTimeOffset? expires)
        {
            if (string.IsNullOrEmpty(culture))
            {
                throw new ArgumentNullException(nameof(culture), "Culture cannot be null or empty");
            }

            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));

            responseCookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                cookieValue,
                new CookieOptions
                {
                    Expires = expires ?? DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true
                });
        }
    }
}
