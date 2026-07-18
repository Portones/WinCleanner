using System.Threading.Tasks;

namespace WinCleaner.Services.Interfaces
{
    public interface IReportGeneratorService
    {
        Task<string> GenerateAndOpenReportAsync();
    }
}
