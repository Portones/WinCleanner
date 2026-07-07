using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Serilog;
using WinCleaner.Models;
using WinCleaner.Services.Interfaces;

namespace WinCleaner.Services.Implementations
{
    public class ContextMenuService : IContextMenuService
    {
        public async Task<List<ContextMenuItem>> GetContextMenuItemsAsync(CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var list = new List<ContextMenuItem>();
                
                // Escanear las 3 categorías principales del menú contextual de Windows Explorer
                ScanRegistryHandlers(@"*\shellex\ContextMenuHandlers", "Archivos", list);
                ScanRegistryHandlers(@"Directory\shellex\ContextMenuHandlers", "Carpetas", list);
                ScanRegistryHandlers(@"Folder\shellex\ContextMenuHandlers", "Carpetas/Unidades", list);

                return list.OrderBy(x => x.Name).ToList();
            }, cancellationToken);
        }

        private void ScanRegistryHandlers(string parentKeyPath, string category, List<ContextMenuItem> list)
        {
            try
            {
                using (var parentKey = Registry.ClassesRoot.OpenSubKey(parentKeyPath, false))
                {
                    if (parentKey != null)
                    {
                        foreach (var subKeyName in parentKey.GetSubKeyNames())
                        {
                            // Ignorar algunos handlers nativos críticos de Windows que no se deben tocar
                            if (subKeyName.Equals("New", StringComparison.OrdinalIgnoreCase) || 
                                subKeyName.Equals("WorkFolders", StringComparison.OrdinalIgnoreCase) ||
                                subKeyName.Equals("Sharing", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            bool isEnabled = !subKeyName.StartsWith("-");
                            string cleanKeyName = isEnabled ? subKeyName : subKeyName.Substring(1);

                            // Obtener GUID si está guardado en el valor por defecto de la clave
                            string guid = string.Empty;
                            try
                            {
                                using (var subKey = parentKey.OpenSubKey(subKeyName, false))
                                {
                                    guid = subKey?.GetValue(null)?.ToString() ?? string.Empty;
                                }
                            }
                            catch { }

                            // Si la clave no contiene un GUID válido en su valor por defecto, 
                            // a veces el nombre de la clave misma es el GUID
                            string effectiveGuid = string.IsNullOrEmpty(guid) ? cleanKeyName : guid;

                            // Intentar resolver el nombre descriptivo amigable del GUID (ej. "Dropbox", "WinRAR")
                            string displayName = cleanKeyName;
                            if (effectiveGuid.StartsWith("{") && effectiveGuid.EndsWith("}"))
                            {
                                displayName = GetFriendlyNameFromGuid(effectiveGuid) ?? cleanKeyName;
                            }

                            list.Add(new ContextMenuItem
                            {
                                Name = displayName,
                                RegistryPath = parentKeyPath,
                                RegistryKeyName = subKeyName,
                                HandlerGuid = effectiveGuid,
                                Category = category,
                                IsEnabled = isEnabled
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al escanear menús contextuales en {Path}", parentKeyPath);
            }
        }

        public async Task<bool> ToggleContextMenuItemAsync(ContextMenuItem item, bool enable, CancellationToken cancellationToken)
        {
            if (item == null) return false;

            return await Task.Run(() =>
            {
                try
                {
                    using (var parentKey = Registry.ClassesRoot.OpenSubKey(item.RegistryPath, true))
                    {
                        if (parentKey != null)
                        {
                            string oldKeyName = item.RegistryKeyName;
                            string newKeyName;

                            if (enable)
                            {
                                // Activar: quitar guión "-" si existe al principio
                                if (oldKeyName.StartsWith("-"))
                                {
                                    newKeyName = oldKeyName.Substring(1);
                                }
                                else
                                {
                                    return true; // Ya está activado
                                }
                            }
                            else
                            {
                                // Desactivar: agregar guión "-" al principio del nombre de la clave
                                if (!oldKeyName.StartsWith("-"))
                                {
                                    newKeyName = "-" + oldKeyName;
                                }
                                else
                                {
                                    return true; // Ya está desactivado
                                }
                            }

                            // Leer valor por defecto de la clave vieja
                            string defaultValue = string.Empty;
                            using (var oldKey = parentKey.OpenSubKey(oldKeyName, false))
                            {
                                if (oldKey != null)
                                {
                                    defaultValue = oldKey.GetValue(null)?.ToString() ?? string.Empty;
                                }
                            }

                            // Crear nueva clave con el nombre cambiado
                            using (var newKey = parentKey.CreateSubKey(newKeyName, true))
                            {
                                newKey.SetValue(null, defaultValue);
                            }

                            // Eliminar la clave vieja
                            parentKey.DeleteSubKeyTree(oldKeyName);

                            // Actualizar el estado del objeto en la vista
                            item.RegistryKeyName = newKeyName;
                            Log.Information("Conmutado menú contextual {Name} a Estado: {State} (Clave: {NewKey})", item.Name, enable, newKeyName);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al conmutar el menú contextual {Name}", item.Name);
                    throw;
                }
                return false;
            }, cancellationToken);
        }

        private string? GetFriendlyNameFromGuid(string guid)
        {
            try
            {
                using (var clsidKey = Registry.ClassesRoot.OpenSubKey($@"CLSID\{guid}", false))
                {
                    var friendlyName = clsidKey?.GetValue(null)?.ToString();
                    if (!string.IsNullOrEmpty(friendlyName))
                    {
                        return friendlyName;
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
