using Saffrat.Models;

namespace Saffrat.Services
{
    public interface ILocalizationService
    {
        StringResource GetStringResource(string resourceKey, int languageId);
        AppSetting GetSetting();
    }
}
