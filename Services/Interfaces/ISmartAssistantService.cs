using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ISmartAssistantService
    {
        Task<List<OptimizationRecommendation>> GetSmartRecommendationsAsync();
    }
}
