using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IBootAnalyzerService
    {
        Task<BootInfo> GetBootInfoAsync();
    }
}
