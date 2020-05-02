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
    public class StoredSave : INotifyPropertyChanged
    {
        public StoredSave(string name, DateTime date)
        {
            this.Name = name;
            this.LastChangedDate = date;
        }

        public string Name { get; set; }
        public DateTime LastChangedDate { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool equalDate(DateTime d)
        {
            return LastChangedDate.Year.Equals(d.Year) &&
                   LastChangedDate.Month.Equals(d.Month) &&
                   LastChangedDate.Day.Equals(d.Day) &&
                   LastChangedDate.Hour.Equals(d.Hour) &&
                   LastChangedDate.Minute.Equals(d.Minute) &&
                   LastChangedDate.Second.Equals(d.Second);
        }

        public override bool Equals(object o)
        {
            return o is StoredSave && ((StoredSave) o).Name.Equals(this.Name) &&
                   equalDate(((StoredSave) o).LastChangedDate);
        }
    }
}
