using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Saffrat.Models;
using Saffrat.Services;

namespace Saffrat.Controllers
{
    public class BaseController : Controller
    {
        private readonly ILanguageService _languageService;
        private readonly ILocalizationService _localizationService;
        protected readonly IDateTimeService _dateTimeService;
        private AppSetting _setting;

        public BaseController(ILanguageService languageService, ILocalizationService localizationService, IDateTimeService dateTimeService)
        {
            _languageService = languageService;
            _localizationService = localizationService;
            _dateTimeService = dateTimeService;
        }

        public String Localize(string resourceKey, params object[] args)
        {
            var currentCulture = Thread.CurrentThread.CurrentUICulture.Name;

            var language = _languageService.GetLanguageByCulture(currentCulture);
            if (language != null)
            {
                var stringResource = _localizationService.GetStringResource(resourceKey, language.Id);
                if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
                {
                    return new String(resourceKey);
                }

                return new String((args == null || args.Length == 0)
                    ? stringResource.Value
                    : string.Format(stringResource.Value, args));
            }

            return new String(resourceKey);
        }

        public String Localize(string resourceKey, int lang)
        {
            var stringResource = _localizationService.GetStringResource(resourceKey, lang);
            if (stringResource == null || string.IsNullOrEmpty(stringResource.Value))
            {
                return new String(resourceKey);
            }

            return stringResource.Value;
        }

        public AppSetting GetSetting
        {
            get
            {
                _setting ??= _localizationService.GetSetting();
                return _setting;
            }
        }

        public string GetCurrency(decimal amount)
        {
            if (GetSetting.CurrencyPosition == 0)
            {
                return String.Format("{0}{1}", GetSetting.CurrencySymbol, amount);
            }
            else if (GetSetting.CurrencyPosition == 1)
            {
                return String.Format("{0}{1}", amount, GetSetting.CurrencySymbol);
            }
            else if (GetSetting.CurrencyPosition == 2)
            {
                return String.Format("{0} {1}", GetSetting.CurrencySymbol, amount);
            }
            else if (GetSetting.CurrencyPosition == 3)
            {
                return String.Format("{0} {1}", amount, GetSetting.CurrencySymbol);
            }
            else
            {
                return amount.ToString();
            }
        }

        public DateTime EndOfDay(DateTime? d)
        {
            return _dateTimeService.EndOfDay(d);
        }
        public DateTime StartOfDay(DateTime? d)
        {
            return _dateTimeService.StartOfDay(d);
        }

        public DateTime CurrentDateTime()
        {
            return _dateTimeService.Now();
        }

        //Get Model First Error

        public string FirstError(ModelStateDictionary modelState)
        {
            foreach (var pair in modelState)
            {
                if (pair.Value.Errors.Count > 0)
                {
                    return pair.Value.Errors.Select(error => error.ErrorMessage).First();
                }
            }

            return "";
        }

        public void SaveLog(AuditLog log, RestaurantDBContext dBContext)
        {
            try
            {
                dBContext.AuditLogs.Add(log);
                dBContext.SaveChanges();
            }
            catch { }
        }
    }
}
