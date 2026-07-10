using System.Collections.Generic;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface ITemperatureService
    {
        Task<List<TemperatureItem>> GetTemperaturesAsync();
    }
}
