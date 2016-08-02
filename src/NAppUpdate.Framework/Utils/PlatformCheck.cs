using System;

namespace NAppUpdate.Framework.Utils
{
    public static class PlatformCheck
    {
        public static bool CurrentlyRunningInWindows()
        {
            var os = Environment.OSVersion;
            var pid = os.Platform;

            switch (pid)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return true;
                case PlatformID.Unix:
                    return false;
                default:
                    return false;
            }
        }
    }
}
