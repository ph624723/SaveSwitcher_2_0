using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SaveSwitcher2.Annotations;

namespace SaveSwitcher2
{
    public class SteamGame : INotifyPropertyChanged
    {
        public SteamGame(string name, string gameid)
        {
            Name = name;
            SteamGameId = gameid;
        }

        public string Name { get; set; }
        public string SteamGameId { get; set; }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
