using System;

namespace Saffrat.Services
{
    public interface IDateTimeService
    {
        DateTime Now();
        DateTime StartOfDay(DateTime? d = null);
        DateTime EndOfDay(DateTime? d = null);
    }
}
