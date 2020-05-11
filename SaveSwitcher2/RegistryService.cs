using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace SaveSwitcher2
{
    static class RegistryService
    {
        private static string _steamKey = @"SOFTWARE\Valve\Steam";
        public static string CheckSteamRunning()
        {
            try
            {
                using (RegistryKey steamKey = Registry.CurrentUser.OpenSubKey(_steamKey))
                {
                    //if it does exist, retrieve the stored values  
                    if (steamKey != null)
                    {
                        object appId = null;
                        if ((appId = steamKey.GetValue("RunningAppID")) != null)
                        {
                            steamKey.Close();
                            return appId.ToString();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            return null;
        }

        public static string CheckGameName(string gameId)
        {
            try
            {
                using (RegistryKey gameKey = Registry.CurrentUser.OpenSubKey(_steamKey+@"\Apps\"+gameId))
                {
                    //if it does exist, retrieve the stored values  
                    if (gameKey != null)
                    {
                        object name = null;
                        if ((name = gameKey.GetValue("Name")) != null)
                        {
                            gameKey.Close();
                            return name.ToString();
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            return null;
        }
    }
}
