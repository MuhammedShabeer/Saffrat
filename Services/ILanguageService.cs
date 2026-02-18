using Saffrat.Models;

namespace Saffrat.Services
{
    public interface ILanguageService
    {
        IEnumerable<Language> GetLanguages();
        Language GetLanguageByCulture(string culture);
        string GetDefaultRegion();
        string GetDefaultLanguage();
    }
}
