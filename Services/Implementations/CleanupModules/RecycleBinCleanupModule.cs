using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations.CleanupModules
{
    public class RecycleBinCleanupModule : ICleanupModule
    {
        // Importación P/Invoke para consultar la papelera de reciclaje
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SHQUERYRBINFO
        {
            public uint cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHQueryRecycleBinW")]
        private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        // Importación P/Invoke para vaciar la papelera de reciclaje
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHEmptyRecycleBinW")]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        public string Id => "RecycleBin";
        public string Name => "Papelera de Reciclaje";
        public string Description => "Muestra el espacio y archivos acumulados en la Papelera de reciclaje de Windows y permite vaciarla.";

        public async Task<ScanResult> ScanAsync(string selectedDrive, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var result = new ScanResult();
            progress.Report(0);

            string? rootPath = (!string.IsNullOrEmpty(selectedDrive) && !selectedDrive.Equals("Todos", StringComparison.OrdinalIgnoreCase))
                ? selectedDrive.TrimEnd('\\') + @"\"
                : null;

            try
            {
                // Ejecutar la consulta en segundo plano de manera asíncrona
                var info = await Task.Run(() =>
                {
                    var rbInfo = new SHQUERYRBINFO();
                    rbInfo.cbSize = (uint)Marshal.SizeOf(typeof(SHQUERYRBINFO));
                    
                    int hresult = SHQueryRecycleBin(rootPath, ref rbInfo);
                    if (hresult == 0)
                    {
                        return rbInfo;
                    }
                    throw new COMException("Error al consultar la Papelera de reciclaje mediante Win32 API.", hresult);
                });

                progress.Report(50);

                if (info.i64NumItems > 0)
                {
                    string driveLabel = rootPath != null ? $" ({rootPath.TrimEnd('\\')})" : string.Empty;
                    var item = new CleanableItem
                    {
                        Path = rootPath ?? "RecycleBin", // Ruta del disco o identificador simbólico
                        Name = $"Papelera de reciclaje{driveLabel} ({info.i64NumItems} archivos)",
                        Size = info.i64Size,
                        LastModified = DateTime.Now,
                        FileType = "Papelera de Reciclaje",
                        ModuleId = Id
                    };
                    result.Items.Add(item);
                    result.TotalSize = info.i64Size;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear la Papelera de reciclaje.");
            }

            progress.Report(100);
            return result;
        }

        public async Task<int> CleanAsync(List<CleanableItem> itemsToClean, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (itemsToClean == null || itemsToClean.Count == 0) return 0;

            progress.Report(10);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                int hresult = await Task.Run(() =>
                {
                    string? rootPath = (itemsToClean[0].Path != "RecycleBin" && itemsToClean[0].Path.Contains(":")) ? itemsToClean[0].Path : null;
                    // Vaciado sin confirmación nativa de Windows (SHERB_NOCONFIRMATION), sin sonido y sin UI de progreso
                    return SHEmptyRecycleBin(IntPtr.Zero, rootPath, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                });

                progress.Report(90);

                if (hresult == 0)
                {
                    Log.Information("Papelera de reciclaje vaciada con éxito.");
                    progress.Report(100);
                    return itemsToClean.Count; // Devolvemos el número de elementos declarados limpios
                }
                else
                {
                    // Si retorna un error por estar ya vacía u otro código, lo registramos
                    Log.Warning("SHEmptyRecycleBin retornó HRESULT: {HResult}", hresult);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error crítico al intentar vaciar la Papelera de reciclaje.");
            }

            progress.Report(100);
            return 0;
        }
    }
}
