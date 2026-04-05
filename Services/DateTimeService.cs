using System;
using Saffrat.Models;

namespace Saffrat.Services
{
    public class DateTimeService : IDateTimeService
    {
        private readonly ILocalizationService _localizationService;

        public DateTimeService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        public DateTime Now()
        {
            var setting = _localizationService.GetSetting();
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(setting.Timezone);
            var utc = DateTime.UtcNow;
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZoneInfo);
        }

        public DateTime StartOfDay(DateTime? d = null)
        {
            DateTime date = Now();
            if (d != null)
                date = (DateTime)d;
            return date.Date;
        }

        public DateTime EndOfDay(DateTime? d = null)
        {
            DateTime date = Now();
            if (d != null)
                date = (DateTime)d;

            return date.Date.AddDays(1).AddTicks(-1);
        }
    }
}
