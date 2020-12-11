using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SaveSwitcher2.Model;

namespace SaveSwitcher2.Services
{
    static class FileService
    {
        

        private static string _pathStr = "GamePaths.txt";
        private static string _activeSavePath = "ActiveSave.txt";
        private static string _storePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "SaveSwitcher2\\CP77\\Savegames");

        private static string _fallbackGamePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "GalaxyClient\\Games\\Cyberpunk 2077\\bin\\x64\\Cyberpunk2077.exe");

        public static string FallbackGamePath
        {
            get { return _fallbackGamePath; }
        }

        private static string _fallbackSavesPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Saved Games\\CD Projekt Red\\Cyberpunk 2077");

        public static string FallbackSavesPath
        {
            get { return _fallbackSavesPath; }
        }

        private static string _fallbackSteamPath = "1091500";
        public static string FallbackSteamPath
        {
            get { return _fallbackSteamPath; }
        }

        public static string[] readPath()
        {
            FileInfo file = new FileInfo(_pathStr);
            string[] paths = new string[4];

            if (!file.Exists)
            {
                SavePath(_fallbackGamePath,_fallbackSavesPath, _fallbackSteamPath, false);
            }

            StreamReader sr = file.OpenText();

            
            for (int i = 0; i < 4; i++)
            {
                paths[i] = sr.ReadLine();
            }
            sr.Close();

            if (paths.Contains(null) || paths.Take(3).Contains(string.Empty))
            {
                SavePath(_fallbackGamePath, _fallbackSavesPath, _fallbackSteamPath, false);
                return readPath();

            }

            try
            {
                var tmp = new DirectoryInfo(paths[1]);
            }
            catch (Exception e)
            {
                SavePath(paths[0], _fallbackSavesPath, paths[3], Boolean.Parse(paths[2]));
                return readPath();
            }

            return paths;
        }

        public static void SavePath(string gamePath, string savePath, string steampath, bool steamToggle)
        {
            FileInfo file = new FileInfo(_pathStr);
            using (StreamWriter outputFile = file.CreateText())
            { 
                outputFile.WriteLine(gamePath);
                outputFile.WriteLine(savePath);
                outputFile.WriteLine(steamToggle.ToString());
                outputFile.WriteLine(steampath != null ? steampath : "");
            }
        }

        public static StoredSave readActive()
        {
            FileInfo file = new FileInfo(_activeSavePath);
            if (!file.Exists)
            {
                StreamWriter tmp = file.CreateText();
                tmp.Close();
            }
            StreamReader sr = new StreamReader(_activeSavePath);

            string[] data = new string[2];
            for (int i = 0; i < 2; i++)
            {
                data[i] = sr.ReadLine();
            }
            sr.Close();

            if (data.Contains(null) || data.Contains(string.Empty))
            {
                return null;
            }
            return new StoredSave(data[0], DateTime.Parse(data[1]));
        }

        public static void SaveActive(StoredSave active)
        {
            FileInfo file = new FileInfo(_activeSavePath);
            using (StreamWriter outputFile = file.CreateText())
            {
                outputFile.WriteLine(active != null? active.Name : "");
                if(active != null) outputFile.WriteLine(active.LastChangedDate);
            }
        }

        /// <summary>
        /// Rename existing save profile folder or create new one from active save. Overrides existing folder.
        /// </summary>
        /// <param name="savePath"></param>
        /// <param name="name"></param>
        /// <param name="oldName"></param>
        public static void StoreSaveFile(string savePath,string name, string oldName = null)
        {
            string targetPath = Path.Combine(_storePath, name);
            DirectoryInfo targetDir = new DirectoryInfo(targetPath);

            string sourcePath;
            if (oldName != null)
            {
                sourcePath = Path.Combine(_storePath, oldName);
                if (!new DirectoryInfo(sourcePath).Exists)
                {
                    throw new FileNotFoundException("ERROR: Copy not successfull.\nSave profile '" + oldName + "' does not seem to exist.");
                }
                if (targetDir.Exists) targetDir.Delete(recursive: true);
                targetDir.Create();
                DirectoryCopy(sourcePath, targetDir.FullName);
                DeleteSaveFile(oldName);
            }
            else
            {
                sourcePath = savePath;
                if (!new DirectoryInfo(sourcePath).Exists)
                {
                    throw new FileNotFoundException("ERROR: Copy not successfull.\nPath '" + sourcePath + "' does not seem to exist.");
                }
                if (targetDir.Exists) targetDir.Delete(recursive: true);
                targetDir.Create();
                DirectoryCopy(sourcePath, targetDir.FullName);
            }
        }

        public static void LoadSaveFile(string savePath, string name)
        {
            string sourcePath = Path.Combine(_storePath, name);
            DirectoryInfo sourceDir = new DirectoryInfo(sourcePath);

            if (!sourceDir.Exists)
            {
                throw new FileNotFoundException("ERROR: Copy not successfull.\nSave profile '" + name+"' does not seem to exist.");
            }

            if (!new DirectoryInfo(savePath).Exists)
            {
                throw new FileNotFoundException("ERROR: Copy not successfull.\nPath '" + savePath + "' does not seem to exist.");
            }
            ClearDirectory(new DirectoryInfo(savePath));
            DirectoryCopy(sourceDir.FullName,savePath);
            //Necessary because game files are going to be new and will seem to have changed otherwise
            Directory.SetLastWriteTime(sourceDir.FullName,DateTime.Now);
        }

        public static void DeleteSaveFile(string name)
        {
            DirectoryInfo dir = new DirectoryInfo(Path.Combine(_storePath,name));
            if (dir.Exists) dir.Delete(true);
        }

        public static List<StoredSave> LoadStoredSaves()
        {
            List<StoredSave> storedsaves = new List<StoredSave>();

            DirectoryInfo dir = new DirectoryInfo(_storePath);
            if (!dir.Exists)
            {
                dir.Create();
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo subDir in dirs)
            {
                storedsaves.Add(new StoredSave(subDir.Name,subDir.LastWriteTime));
            }

            return storedsaves;
        }

        private static void ClearDirectory(DirectoryInfo dir)
        {
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                subDir.Delete(true);
            }
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo sourceDir = new DirectoryInfo(sourceDirName);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] subDirs = sourceDir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, overwrite:true);
            }

            //Subdirectories
                foreach (DirectoryInfo subdir in subDirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath);
                }
            
        }

        public static string FindNewProfileName(string baseName)
        {
            string res = baseName;
            int i = 2;
            while (new DirectoryInfo(Path.Combine(_storePath,res)).Exists)
            {
                res = baseName + "_" + i++;
            }

            return res;
        }
    }
}
