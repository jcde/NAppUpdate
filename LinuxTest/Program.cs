using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using NAppUpdate.Framework;
using NAppUpdate.Framework.Common;
using NAppUpdate.Framework.Sources;

namespace LinuxTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("v2");
            try
            {
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Console.WriteLine("Starting!");
                //if (UpdateManager.Instance.State != UpdateManager.UpdateProcessState.Checked);
                //    UpdateManager.Instance.CleanUp();
                // UpdateManager initialization
                UpdateManager updManager = UpdateManager.Instance;
                updManager.UpdateSource = new SimpleWebSource("http://rauchfrei.mariaebene.at/downloads/NAU-Mono_LinuxTest/UpdateFeed.xml");
                updManager.Config.TempFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LinuxTest");

                // If you don't call this method, the updater.exe will continually attempt to connect the named pipe and get stuck.
                // Therefore you should always implement this method call.               
                updManager.ReinstateIfRestarted();

                if (UpdateManager.Instance.State == UpdateManager.UpdateProcessState.Checked ||
                    UpdateManager.Instance.State == UpdateManager.UpdateProcessState.AfterRestart ||
                    UpdateManager.Instance.State == UpdateManager.UpdateProcessState.AppliedSuccessfully)
                    UpdateManager.Instance.CleanUp();
                
                Console.WriteLine("Checking for updates...");
                DumpUpdateManagerState();
                CheckForUpdates();
                DumpUpdateManagerState();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadLine();
            }
            Console.WriteLine("Exiting after 5 seconds.");
            Thread.Sleep(5 * 1000);
            Console.WriteLine("Return.");
            Environment.Exit(0);
        }

        private static void DumpUpdateManagerState()
        {
            Console.WriteLine("UpdateManager.Instance.State: " + UpdateManager.Instance.State);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e);
            Console.ReadLine();
        }


        private static void CheckForUpdates()
        {
            // Get a local pointer to the UpdateManager instance
            UpdateManager updManager = UpdateManager.Instance;

            //// Only check for updates if we haven't done so already
            //if (updManager.State != UpdateManager.UpdateProcessState.NotChecked)
            //{
            //    Console.WriteLine("Update process has already initialized; current state: " + updManager.State.ToString());
            //    return;
            //}

            try
            {
                // Check for updates - returns true if relevant updates are found (after processing all the tasks and
                // conditions)
                // Throws exceptions in case of bad arguments or unexpected results
                updManager.CheckForUpdates();
            }
            catch (Exception ex)
            {
                if (ex is NAppUpdateException)
                {
                    // This indicates a feed or network error; ex will contain all the info necessary
                    // to deal with that
                }
                else Console.WriteLine(ex.ToString());
                return;
            }


            if (updManager.UpdatesAvailable == 0)
            {
                Console.WriteLine("Your software is up to date");
                return;
            }

            Console.WriteLine("Updates are available to your software ({0} total). Downloading and preparing...", updManager.UpdatesAvailable);


            try
            {
                updManager.PrepareUpdates();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Updates preperation failed. Check the feed and try again.{0}{1}", Environment.NewLine, ex));
                return;
            }

            OnPrepareUpdatesCompleted();
        }

        private static void OnPrepareUpdatesCompleted()
        {
            // Get a local pointer to the UpdateManager instance
            UpdateManager updManager = UpdateManager.Instance;

            Console.WriteLine("Updates are ready to install. Do you wish to install them now?", "Software updates ready");

            // This is a synchronous method by design, make sure to save all user work before calling
            // it as it might restart your application
            try
            {
                updManager.ApplyUpdates(true, true, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("Error while trying to install software updates{0}{1}", Environment.NewLine, ex));
            }

            if (UpdateManager.Instance.State == UpdateManager.UpdateProcessState.RollbackRequired)
                UpdateManager.Instance.RollbackUpdates();
        }
    }
}
