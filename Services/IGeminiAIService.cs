using System.Threading.Tasks;

namespace Saffrat.Services
{
    public interface IGeminiAIService
    {
        Task<string> GetResponseAsync(string prompt, string apiKey);
    }
}
