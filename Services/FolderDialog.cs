using System.Runtime.InteropServices;

namespace AstroDashboard.Services;

public static class FolderDialog
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SHBrowseForFolder(ref BROWSEINFO browseInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool SHGetPathFromIDList(IntPtr pidl, [Out] char[] pszPath);

    [StructLayout(LayoutKind.Sequential)]
    private struct BROWSEINFO
    {
        public IntPtr hwndOwner;
        public IntPtr pidlRoot;
        public string pszDisplayName;
        public string lpszTitle;
        public uint ulFlags;
        public IntPtr lpfn;
        public IntPtr lParam;
        public int iImage;
    }

    public static string? SelectFolder(string title, string? initialPath = null)
    {
        BROWSEINFO browseInfo = new()
        {
            lpszTitle = title,
            ulFlags = 0x0040, // BIF_NEWDIALOGSTYLE
            pszDisplayName = new string(' ', 260)
        };

        IntPtr pidl = SHBrowseForFolder(ref browseInfo);
        if (pidl != IntPtr.Zero)
        {
            try
            {
                char[] path = new char[260];
                if (SHGetPathFromIDList(pidl, path))
                {
                    return new string(path).TrimEnd('\0');
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(pidl);
            }
        }

        return null;
    }
}
