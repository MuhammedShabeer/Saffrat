using System.Globalization;
using System.Net.Mail;

namespace Saffrat.Helpers
{
    // Helper class for general utility methods.
    public class General
    {
        // Check if the provided email address is valid.
        public static bool IsValidEmail(string email)
        {
            try
            {
                MailAddress mail = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Retrieve the two-letter country code from the provided CultureInfo.
        public static string GetTwoLetterCountryCode(CultureInfo c)
        {
            try
            {
                var r = new RegionInfo(c.LCID);
                return r.TwoLetterISORegionName;
            }
            catch
            {
                // Fallback for handling specific cases like Chinese.
                if (c.Name.Contains("zh"))
                    return "cn";
                else
                    return "";
            }
        }
    }
}
