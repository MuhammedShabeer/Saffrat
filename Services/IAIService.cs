using System.Threading.Tasks;

namespace Saffrat.Services
{
    public interface IAIService
    {
        Task<string> GetResponseAsync(string prompt, string apiKey = null);
    }
}
