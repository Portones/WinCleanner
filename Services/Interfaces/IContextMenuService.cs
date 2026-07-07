using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WinCleaner.Models;

namespace WinCleaner.Services.Interfaces
{
    public interface IContextMenuService
    {
        Task<List<ContextMenuItem>> GetContextMenuItemsAsync(CancellationToken cancellationToken);
        Task<bool> ToggleContextMenuItemAsync(ContextMenuItem item, bool enable, CancellationToken cancellationToken);
    }
}
