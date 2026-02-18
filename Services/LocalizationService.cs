using Saffrat.Models;

namespace Saffrat.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly RestaurantDBContext _dbContext;
        private List<StringResource> _stringResource;

        public LocalizationService(RestaurantDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Retrieve a string resource based on resource key and language ID.
        public StringResource GetStringResource(string resourceKey, int LangId)
        {
            // Check if the string resources are not loaded or do not match the language ID.
            if (_stringResource == null || _stringResource.FirstOrDefault(x => x.LanguageId == LangId) == null)
            {
                // Load string resources for the specified language.
                _stringResource = _dbContext.StringResources.Where(x => x.LanguageId == LangId).ToList();
            }

            // Find and return the requested string resource.
            var res = _stringResource.FirstOrDefault(x =>
                    x.LanguageId == LangId &&
                    x.Name.Trim() == resourceKey.Trim());

            return res;
        }

        // Retrieve application settings.
        public AppSetting GetSetting()
        {
            // Find and return the application setting with ID 1.
            var setting = _dbContext.AppSettings.FirstOrDefault(x => x.Id == 1);
            return setting;
        }
    }
}
