using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using SaveSwitcher2.Annotations;
using SaveSwitcher2.Miscellaneous;
using SaveSwitcher2.Model;
using SaveSwitcher2.Services;
using FileNotFoundException = System.IO.FileNotFoundException;
using Path = System.IO.Path;

namespace SaveSwitcher2
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        TimeService _timeService = new TimeService();
        public string SteamPath { get; set; }
        public string GamePath { get; set; }
        public string SavePath { get; set; }

        public Visibility GamePathVisibility
        {
            get { return SteamGameSelected ? Visibility.Collapsed : Visibility.Visible; }
        }
        public Visibility SteamPathVisibility
        {
            get { return SteamGameSelected ? Visibility.Visible : Visibility.Collapsed; }
        }

        public ObservableCollection<SteamGame> SteamAvailableGames { get; set; }

        public string InfoLabelText { get; set; }

        public Visibility ProgressBarVisibility { get; set; }

        public List<StoredSave> StoredSaves { get; set; }

        public StoredSave SelectedItem { get; set; }

        public bool ItemSelected
        {
            get { return SelectedItem != null; }
        }

        public bool InactiveItemSelected
        {
            get { return ItemSelected && !SelectedItem.Equals(ActiveSave); }
        }

        private bool _launchEnabled;

        public bool LaunchEnabled
        {
            get { return _launchEnabled && ActiveSave != null; }
            set { _launchEnabled = value; }
        }

        private StoredSave _activeSave;

        public StoredSave ActiveSave
        {
            get { return _activeSave; }
            set
            {
                _activeSave = value;
                //ActiveLabelText =  ((value != null) ? value.Name : "");
            }
        }
        public bool Unsynced { get; set; }

        public TimeSpan UnsyncedPlaytime { get; set; }

        public string ActiveLabelText
        {
            get { return ActiveSave != null ? ActiveSave.Name + (Unsynced? " (Unsynced Playtime: "+((int)UnsyncedPlaytime.TotalHours)+"h "+UnsyncedPlaytime.Minutes+"m)": "") : ""; }
        }

        public string DialogLabelText { get; set; }

        public bool AutoSyncChecked { get; set; }

        public bool SteamGameSelected { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            ToggleProcess("Initializing", true);

            StoredSaves = FileService.LoadStoredSaves();
            SteamAvailableGames = RegistryService.GetAvailableGames();

            string[] paths = FileService.readPath();

            GamePath = paths[0];
            SavePath = paths[1];
            SteamGameSelected = Boolean.Parse(paths[2]);
            if (SteamAvailableGames.FirstOrDefault(x => x.SteamGameId.Equals(paths[3])) != null)
            {
                SteamPath = paths[3];
            }

            LaunchEnabled = true;
            AutoSyncChecked = true;

            StoredSave storedActive = FileService.readActive();
            if (storedActive != null && new DirectoryInfo(SavePath).Exists)
            {
                ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(storedActive.Name));
                if (ActiveSave != null)
                {
                    if (new DirectoryInfo(SavePath).LastWriteTime > ActiveSave.LastChangedDate)
                    {
                        //MessageBox.Show(new DirectoryInfo(SavePath).LastWriteTime +" " + ActiveSave.LastChangedDate);
                        string tmpName = FileService.FindNewProfileName("Online_Save");
                        try
                        {
                            FileService.StoreSaveFile(SavePath, tmpName, TimeSpan.Zero);
                            RefreshDataSet();
                            ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(tmpName));
                            FileService.SaveActive(ActiveSave);

                            DialogName = tmpName.ToString();
                            _dialogBackupName = tmpName.ToString();
                            DialogCheckboxVisible = Visibility.Collapsed;
                            DialogSaveEnabled = false;
                            DialogLabelText =
                                "Active save seems to be newer than stored backup. \nProbably due to online synchronyzation. This will \nalso happen if you leave your backup unsynced \nwhen closing the app. \nSelect a profile name for the found data or click \naway for automatic naming.";
                            IsDialogOpen = true;
                        }
                        catch (FileNotFoundException e)
                        {
                            MessageBox.Show(e.Message);
                            ActiveSave = null;
                        }

                    }
                }
                else
                {
                    FileService.SaveActive(null);
                }
            }
            else if (!new DirectoryInfo(SavePath).Exists)
            {
                DrawerHost.IsTopDrawerOpen = true;
                if (storedActive != null)
                {
                    MessageBox.Show("Warning: \nSavegame directory '" + SavePath + "' does not seem to exist.");
                    FileService.SaveActive(null);
                }
            }

            ToggleProcess();

            if (!FileService.HasBeenStartedBefore())
            {
                WhereAreMyBackups("You seem to be starting this version of the SaveSwitcher for the first time. \n");
            }
        }

        #region launchgame
        private void Launch_OnClick(object sender, RoutedEventArgs e)
        {
            LaunchEnabled = false;
            ToggleProcess("Game running (Max. startup time: 20s)", true);
            OnLaunchGameEvent();
        }

        private async void OnLaunchGameEvent()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = SteamGameSelected? "steam://rungameid/" + SteamPath : GamePath,
                    //Arguments = "-applaunch 212680",
                    WorkingDirectory = Path.GetDirectoryName(GamePath)
                }
            };
            try
            {
                _timeService.Start();
                process.Start();
                if (!SteamGameSelected)
                {
                    process.WaitForExit();
                }
                else
                {
                    int counter = 0;
                    string runId = null;
                    while ((runId = RegistryService.CheckSteamRunning()) != null && runId.Equals("0"))
                    {
                        if (counter++ > 20)
                        {
                            throw new Exception();
                        }
                        Thread.Sleep(1000);
                    }
                    while ((runId = RegistryService.CheckSteamRunning()) != null && !runId.Equals("0"))
                    {
                        Thread.Sleep(1000);
                    }
                }

                TimeSpan runDuration = _timeService.End() + TimeSpan.FromMinutes(1);

                Unsynced = false;
                if (AutoSyncChecked)
                {
                    ToggleProcess("Game closed. Synchronizing Backup.", true);
                    try
                    {
                        FileService.StoreSaveFile(SavePath, ActiveSave.Name, ActiveSave.PlayTime + runDuration + UnsyncedPlaytime);
                    }
                    catch (FileNotFoundException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    UnsyncedPlaytime = TimeSpan.Zero;
                }
                else if (Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                    new MessageContainer("Do you want to refresh the backup for profile '" + ActiveLabelText +
                                         "'? (overwrite)"), "YesNoDialog")))
                {
                    ToggleProcess("Synchronizing Backup.", true);
                    try
                    {
                        FileService.StoreSaveFile(SavePath, ActiveSave.Name, ActiveSave.PlayTime + runDuration + UnsyncedPlaytime);
                    }
                    catch (FileNotFoundException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    UnsyncedPlaytime = TimeSpan.Zero;
                }
                else
                {
                    //FileService.SaveActive(null);
                    Unsynced = true;
                    UnsyncedPlaytime += runDuration;
                }
            }
            catch (Exception ex)
            {
                ToggleProcess("ERROR, game .exe could not be found");
            }

            RefreshDataSet();
            ToggleProcess();
            LaunchEnabled = true;
        }

        #endregion

        #region Fody

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region gamepaths

        private bool _pathChanged = false;

        private void SteamPathComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _pathChanged = true;
        }

        private void GamePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _pathChanged = true;
        }

        private void SavePathTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            _pathChanged = true;
        }

        private void PathTextBox_OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (_pathChanged)
            {
                FileService.SavePath(GamePathTextBox.Text, SavePathTextBox.Text, SteamPath, SteamGameSelected);
            }
            _pathChanged = false;
        }

        private void SteamToggleButton_OnClick(object sender, RoutedEventArgs e)
        {
            FileService.SavePath(GamePathTextBox.Text, SavePathTextBox.Text, SteamPath, SteamGameSelected);
        }

        private void SavePathTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {

            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = SavePath;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SavePath = dialog.FileName;
            }
        }

        private void GamePathTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = GamePath;
            dialog.IsFolderPicker = false;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                GamePath = dialog.FileName;
            }

        }
        #endregion

        #region ButtonListeners
        private void EditButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogName = SelectedItem.Name.ToString();
            _dialogBackupName = SelectedItem.Name.ToString();
            DialogCheckboxVisible = Visibility.Collapsed;
            DialogSaveEnabled = false;
            DialogLabelText = "Edit Profile: " + _dialogBackupName;
            IsDialogOpen = true;
        }

        private async void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            string yesNoString = (string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                new MessageContainer("Do you really want to delete profile '" + SelectedItem.Name + "' permanently?"),
                "YesNoDialog");
            if (Boolean.Parse(yesNoString))
            {
                ToggleProcess("Deleting profile " + SelectedItem.Name, true);
                FileService.DeleteSaveFile(SelectedItem.Name);
                RefreshDataSet();

                ToggleProcess();
            }
        }

        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogName = "";
            _dialogBackupName = null;
            DialogCheckboxVisible = Visibility.Visible;
            DialogDublicateSave = true;
            DialogSaveEnabled = false;
            DialogLabelText = "New Profile";
            IsDialogOpen = true;
        }

        private async void RefreshButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                new MessageContainer("Do you really want to overwrite the data stored for profile '" + SelectedItem.Name + "'?"), "YesNoDialog"))) return;
            
            ToggleProcess("Updating Profile " + SelectedItem.Name, true);
            //store new data
            try
            {
                FileService.StoreSaveFile(SavePath, SelectedItem.Name, ActiveSave != null? ActiveSave.PlayTime + UnsyncedPlaytime : TimeSpan.Zero ,null);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message);
                ToggleProcess("Updating Profile " + SelectedItem.Name + " (ERROR)");
                return;
            }

            FileService.SaveActive(new StoredSave(SelectedItem.Name, DateTime.Now));
            Unsynced = false;
            UnsyncedPlaytime = TimeSpan.Zero;
            RefreshDataSet();
            ToggleProcess();
        }

        private async void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Unsynced)
            {
                if (!Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                    new MessageContainer("Active profile seems to be unsynced. Do you really want to load profile '" + SelectedItem.Name + "'? \nUnsynced changes will be lost."), "YesNoDialog")))
                {
                    return;
                }
            }
            ToggleProcess("Loading Profile " + SelectedItem.Name, true);
            try
            {
                FileService.LoadSaveFile(SavePath, SelectedItem.Name);
                FileService.SaveActive(SelectedItem);
                RefreshDataSet();
                ToggleProcess();
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message);
                FileService.SaveActive(null);
                RefreshDataSet();
                ToggleProcess("Loading Profile " + SelectedItem.Name + " (ERROR)");
                DrawerHost.IsTopDrawerOpen = true;
            }

            Unsynced = false;
            UnsyncedPlaytime = TimeSpan.Zero;
        }
        #endregion

        #region DialogStuff
        public bool IsDialogOpen { get; set; }

        public string DialogName { get; set; }

        private string _dialogBackupName;

        public bool DialogContentChanged
        {
            get { return _dialogBackupName != null ? !_dialogBackupName.Equals(DialogName) : true; }
        }

        public bool DialogDublicateSave { get; set; }

        public Visibility DialogCheckboxVisible { get; set; }

        public bool DialogSaveEnabled { get; set; }

        private async void DialogSaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (StoredSaves.FirstOrDefault(x => x.Name.Equals(DialogName)) != null)
            {
                if (Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                    new MessageContainer("Do you want to overwrite profile '" + DialogName + "'?"), "YesNoDialog")))
                {
                    IsDialogOpen = false;
                    bool overwritePlaytime = Boolean.Parse((string) await MaterialDesignThemes.Wpf.DialogHost.Show(
                        new MessageContainer("Do you want to overwrite/reset the old playtime of profile '" +
                                             DialogName + "'?"),
                        "YesNoDialog"));

                        FinishDialog(true, overwritePlaytime);
                }
            }
            else
            {
                IsDialogOpen = false;
                FinishDialog(true);
            }
        }

        private void DialogHost_OnDialogClosing(object sender, DialogClosingEventArgs eventargs)
        {
            FinishDialog();
        }

        private void FinishDialog(bool saving = false, bool overwritePlaytime = true)
        {
            if (saving)
            {
                bool clearNewProfile = false;


                ToggleProcess("Saving Profile " + DialogName, true);
                //store new data
                try
                {
                    if (overwritePlaytime)
                    {
                        FileService.StoreSaveFile(SavePath, DialogName, ActiveSave != null ? ActiveSave.PlayTime+UnsyncedPlaytime : TimeSpan.Zero,_dialogBackupName, !DialogDublicateSave);
                    }
                    else FileService.StoreSaveFile(SavePath, DialogName, oldName:_dialogBackupName, clearProfile:!DialogDublicateSave);
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show(ex.Message);
                    ToggleProcess("Saving Profile " + DialogName + " (ERROR)");
                    DialogName = null;
                    _dialogBackupName = null;
                    return;
                }

                if (ActiveSave != null && ActiveSave.Name.Equals(_dialogBackupName))
                {
                    ActiveSave.Name = DialogName;
                    FileService.SaveActive(ActiveSave);
                }
                else if (_dialogBackupName == null && DialogDublicateSave)
                {
                    //addbutton
                    FileService.SaveActive(new StoredSave(DialogName, DateTime.Now));
                    Unsynced = false;
                    UnsyncedPlaytime = TimeSpan.Zero;
                }

                RefreshDataSet();
                ToggleProcess();
            }

            DialogName = null;
            _dialogBackupName = null;
        }
        private void DialogNameTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            DialogSaveEnabled = !DialogNameTextBox.Text.Equals("") &&
                                !DialogNameTextBox.Text.Equals(_dialogBackupName) &&
                                !DialogNameTextBox.Text.Equals(ActiveLabelText);
            //For some reason property is correct in messagebox but not when being assigned.
            //DialogSaveEnabled = !DialogNameTextBox.Text.Equals("") && DialogContentChanged;
            //MessageBox.Show("" + DialogContentChanged);
        }
        #endregion

        private void RefreshDataSet()
        {
            //refresh dataset
            StoredSaves = FileService.LoadStoredSaves();
            //refresh activesave variable 
            ActiveSave = null;
            StoredSave storedActive = FileService.readActive();
            if (storedActive != null) ActiveSave = StoredSaves.FirstOrDefault(x => x.Name.Equals(storedActive.Name));
        }

        private async void ToggleProcess(string name = null, bool on = false)
        {
            string finishedHeader = "Finished: ";
            string newName = name != null ? name : InfoLabelText;
            ProgressBarVisibility = on ? Visibility.Visible : Visibility.Collapsed;
            InfoLabelText = (on ? "" : finishedHeader) + newName.Replace(finishedHeader, "");
            ExtensionMethods.Refresh(ProgressBar);
            ExtensionMethods.Refresh(InfoLabel);
            ExtensionMethods.Refresh(ProcessPanel);
        }

        private void WhereAreMyBackupsButton_OnClick(object sender, RoutedEventArgs e)
        {
            WhereAreMyBackups();
        }


        private void WhereAreMyBackups(string header = null)
        {
            var curDir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            Process.Start("explorer.exe", curDir.Parent.FullName);
            MessageBox.Show((header == null? "" : header) +
            "In case you just upgraded from version 2.0.11.1 or older, your backups might still be located in a deprecated directory. With this version they have been moved to: \n'" + FileService.StorePath + "' \nfor easier access. That way they can also be kept automatically when updating the app. \nTo access your old backups please copy them to the new directory. Look for a folder called 'Savegames\\*Profilename*' in: \n'" + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Apps\\2.0\\") + "' \nThe folder to look in should just have opened.");
        }
    }
}