using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace Announcement.Services
{
    /// <summary>
    /// Service utility to register Shell shortcuts with custom AppUserModelID metadata.
    /// This is a strict requirement for Win32 Desktop apps sending Windows 10/11 UWP Toast Notifications.
    /// </summary>
    public class ShortcutService
    {
        /// <summary>
        /// Installs a shortcut under the user's Start Menu programs if it does not already exist.
        /// </summary>
        /// <param name="appId">Unique AppUserModelID assigned to this application.</param>
        public static void EnsureStartMenuShortcut(string appId)
        {
            string shortcutPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                "Energy Saving Alert !.lnk");

            if (File.Exists(shortcutPath))
            {
                return;
            }

            try
            {
                Program.Log($"Shortcut target path: {shortcutPath}");
                Program.Log("Initiating shortcut installation COM request.");
                InstallShortcut(shortcutPath, appId);
                Program.Log("Shortcut created successfully.");
            }
            catch (Exception ex)
            {
                Program.Log($"Failed to create shortcut: {ex.Message}");
            }
        }

        private static void InstallShortcut(string shortcutPath, string appId)
        {
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            int result = InstallShortcutCOM(shortcutPath, exePath, appId);
            if (result != 0)
            {
                throw new Exception($"Error registering Start Menu shortcut COM property. HRESULT: {result}");
            }
        }

        #region COM Interfaces (ShellLink & PropertyStore)

        [ComImport, Guid("00021401-0000-0000-C000-000000000046"), ClassInterface(ClassInterfaceType.None)]
        private class ShellLink { }

        [ComImport, Guid("000214F9-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short pwHotkey);
            void GetShowCmd(out uint piShowCmd);
            void SetShowCmd(uint piShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
        }

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            void GetCount(out uint cProps);
            void GetAt(uint iProp, out PropertyKey pkey);
            void GetValue(ref PropertyKey key, out PROPVARIANT pv);
            void SetValue(ref PropertyKey key, ref PROPVARIANT propvar);
            void Commit();
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PROPVARIANT
        {
            [FieldOffset(0)] public ushort vt;
            [FieldOffset(8)] public IntPtr pwszVal;
        }

        /// <summary>
        /// Employs COM interfaces to write AppUserModelID properties into shell link files.
        /// </summary>
        private static int InstallShortcutCOM(string shortcutPath, string exePath, string appId)
        {
            IShellLinkW newShortcut = (IShellLinkW)new ShellLink();
            newShortcut.SetPath(exePath);
            newShortcut.SetWorkingDirectory(Path.GetDirectoryName(exePath));

            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;
            
            // AppUserModelID Property Key format: {9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3}, pid = 5
            PropertyKey appIdKey = new PropertyKey { fmtid = new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), pid = 5 };
            PROPVARIANT var = new PROPVARIANT { vt = 31 }; // VT_LPWSTR string pointer type
            var.pwszVal = Marshal.StringToCoTaskMemUni(appId);

            newShortcutProperties.SetValue(ref appIdKey, ref var);
            newShortcutProperties.Commit();

            IPersistFile newShortcutSave = (IPersistFile)newShortcut;
            newShortcutSave.Save(shortcutPath, true);

            Marshal.FreeCoTaskMem(var.pwszVal);
            return 0; // Success code
        }

        #endregion
    }
}
