using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using SaveSwitcher2.Model;

namespace SaveSwitcher2.Services
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
                                    if (nameVal == null)
                                    {
                                        if (gameKeyName.Equals("1091500"))
                                        {
                                            nameVal = "Cyberpunk 2077";
                                        }
                                        else
                                        {
                                            nameVal = "Steam GameID: " + gameKeyName;
                                        }
                                    }
                                    res.Add(new SteamGame(nameVal.ToString(), gameKeyName));
                                }
                                gameKey.Close();
                            }
                        }

                        res = new ObservableCollection<SteamGame>(res.OrderBy( x=> x.Name));
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
