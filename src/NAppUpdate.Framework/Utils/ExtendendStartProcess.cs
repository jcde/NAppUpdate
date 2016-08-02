using System;
using System.Diagnostics;

namespace NAppUpdate.Framework.Utils
{
    public static class ExtendendStartProcess
    {
        public static Process Start(ProcessStartInfo processStartInfo)
        {
            if (!PlatformCheck.CurrentlyRunningInWindows())
                MakeFileRunnable(processStartInfo);

            return Process.Start(processStartInfo);
        }

        private static void MakeFileRunnable(ProcessStartInfo processStartInfo)
        {
            var ps = new ProcessStartInfo("chmod", "+x " + processStartInfo.FileName)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            using (var p = Process.Start(ps))
            {
                var output = p.StandardOutput.ReadToEnd();

                // waits for the process to exit
                // Must come *after* StandardOutput is "empty"
                // so that we don't deadlock because the intermediate
                // kernel pipe is full.
                p.WaitForExit();
                if (p.ExitCode > 0)
                    Console.WriteLine(string.Format("Could not make '{0}' file runnable. Error code: '{1}'. chmod returned: '{2}'", processStartInfo.FileName, p.ExitCode, output));
            }
        }
    }
}