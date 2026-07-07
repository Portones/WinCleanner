using System;
using System.Runtime.InteropServices;

namespace WinCleaner.Helpers
{
    public static class ShellFileOperations
    {
        private const int FO_DELETE = 3;
        private const int FOF_ALLOWUNDO = 0x0040;
        private const int FOF_NOCONFIRMATION = 0x0010; // Ocultar confirmación nativa de Windows
        private const int FOF_NOERRORUI = 0x0400;      // Ocultar diálogos de error de Windows
        private const int FOF_SILENT = 0x0004;         // Ocultar barra de progreso nativa

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, EntryPoint = "SHFileOperationW")]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        /// <summary>
        /// Envía un archivo o carpeta a la papelera de reciclaje de Windows de forma segura.
        /// </summary>
        /// <param name="path">Ruta absoluta del elemento.</param>
        /// <returns>True si el elemento se movió con éxito.</returns>
        public static bool SendToRecycleBin(string path)
        {
            try
            {
                // La API nativa SHFileOperation requiere que la cadena de origen (pFrom) 
                // esté delimitada por nulo y finalice con un doble carácter nulo (\0\0).
                var doubleNullPath = path + "\0\0";
                
                var fileOp = new SHFILEOPSTRUCT
                {
                    hwnd = IntPtr.Zero,
                    wFunc = FO_DELETE,
                    pFrom = doubleNullPath,
                    pTo = null,
                    fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT,
                    fAnyOperationsAborted = false,
                    hNameMappings = IntPtr.Zero,
                    lpszProgressTitle = null
                };

                int result = SHFileOperation(ref fileOp);
                return result == 0 && !fileOp.fAnyOperationsAborted;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error al enviar a la papelera el archivo/carpeta: {Path}", path);
                return false;
            }
        }
    }
}
