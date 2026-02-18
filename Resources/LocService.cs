using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Saffrat.Models;
using Saffrat.Services;

namespace Saffrat.Resources
{
    public abstract class LocService<TModel> : Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>
    {
        [RazorInject]
        public ILanguageService LanguageService { get; set; }

        [RazorInject]
        public ILocalizationService LocalizationService { get; set; }

        public delegate HtmlString Localizer(string resourceKey);
        private Localizer _localizer;
        private AppSetting _setting;
        private List<Language> _languages;

        public Localizer Localize
        {
            get
            {
                if (_localizer == null)
                {
                    var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

                    var language = LanguageService.GetLanguageByCulture(currentCulture);
                    if (language != null)
                    {
                        _localizer = (resourceKey) =>
                        {
                            var stringResource = LocalizationService.GetStringResource(resourceKey, language.Id);

                            if (stringResource == null)
                            {
                                return new HtmlString(resourceKey);
                            }

                            return new HtmlString(stringResource.Value);
                        };
                    }
                }
                return _localizer;
            }
        }

        public AppSetting GetSetting
        {
            get
            {
                if (_setting == null)
                {
                    _setting = LocalizationService.GetSetting();
                    return _setting;
                }
                return _setting;
            }
        }

        public List<Language> GetLanguages
        {
            get
            {
                if (_languages == null)
                {
                    _languages = LanguageService.GetLanguages().ToList();
                    return _languages;
                }
                return _languages;
            }
        }

        public string GetCurrency(decimal amount)
        {
            if (GetSetting.CurrencyPosition == 0)
            {
                return String.Format("{0}{1}", GetSetting.CurrencySymbol, amount);
            }
            else if (_setting.CurrencyPosition == 1)
            {
                return String.Format("{0}{1}", amount, GetSetting.CurrencySymbol);
            }
            else if (_setting.CurrencyPosition == 2)
            {
                return String.Format("{0} {1}", GetSetting.CurrencySymbol, amount);
            }
            else if (_setting.CurrencyPosition == 3)
            {
                return String.Format("{0} {1}", amount, GetSetting.CurrencySymbol);
            }
            else
            {
                return amount.ToString();
            }
        }
    }

    public abstract class LocService : LocService<dynamic>
    { }
}