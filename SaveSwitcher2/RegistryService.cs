using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static ObservableCollection<SteamGame> GetAvailableGames ()
        {
            ObservableCollection<SteamGame> res = new ObservableCollection<SteamGame>();
            try
            {
                using (RegistryKey steamKey = Registry.CurrentUser.OpenSubKey(_steamKey + @"\Apps"))
                {
                    //if it does exist, retrieve the stored values  
                    if (steamKey != null)
                    {
                        foreach (string gameKeyName in steamKey.GetSubKeyNames())
                        {
                            using (RegistryKey gameKey = steamKey.OpenSubKey(gameKeyName))
                            {
                                object installedVal = gameKey.GetValue("Installed");
                                if (installedVal != null && installedVal.ToString().Equals("1"))
                                {
                                    //Game should be installed
                                    object nameVal = gameKey.GetValue("Name");
                                    if (nameVal != null)
                                    {
                                        res.Add(new SteamGame(nameVal.ToString(), gameKeyName));
                                    }
                                }
                                gameKey.Close();
                            }
                        }
                    }
                    steamKey.Close();
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }

            return res;
        }
    }
}
