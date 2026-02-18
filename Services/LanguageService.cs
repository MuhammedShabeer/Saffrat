using Saffrat.Models;

namespace Saffrat.Services
{
    public class LanguageService : ILanguageService
    {
        private readonly RestaurantDBContext _dbContext;

        public LanguageService(RestaurantDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Retrieve all languages.
        public IEnumerable<Language> GetLanguages()
        {
            return _dbContext.Languages.ToList();
        }

        // Retrieve a language based on its culture.
        public Language GetLanguageByCulture(string culture)
        {
            return _dbContext.Languages.FirstOrDefault(x =>
                x.Culture.Trim().ToLower() == culture.Trim().ToLower());
        }

        // Retrieve the default language.
        public string GetDefaultLanguage()
        {
            var sett = _dbContext.AppSettings.FirstOrDefault(x => x.Id == 1);
            return sett.DefaultLanguage;
        }

        // Retrieve the default region.
        public string GetDefaultRegion()
        {
            var sett = _dbContext.AppSettings.FirstOrDefault(x => x.Id == 1);
            return sett.DefaultRegion;
        }
    }
}
