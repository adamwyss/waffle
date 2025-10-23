using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WaFFL.Evaluation
{
    public class ApplicationState
    {
        private const string DataFolder = "WaFFL";
        private const string ApplicationData = "Application.state";

        public ApplicationState() { }

        public string LastOpenedFilePath { get; set; }

        public void Save()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderpath = Path.Combine(root, DataFolder);

            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }

            string filepath = Path.Combine(folderpath, ApplicationData);

            using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                var stateData = new AppState()
                {
                    LastOpenedFile = LastOpenedFilePath
                };

                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, stateData);
            }
        }

        public void Load()
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderpath = Path.Combine(root, DataFolder);

            LastOpenedFilePath = null;

            if (Directory.Exists(folderpath))
            {
                CleanupLegacyFiles(folderpath);

                string filepath = Path.Combine(folderpath, ApplicationData);
                try
                {
                    using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        try
                        {
                            var stateData = (AppState)formatter.Deserialize(stream);
                            LastOpenedFilePath = stateData.LastOpenedFile;
                        }
                        catch (SerializationException)
                        {
                            // we failed to deserialize the data.  We will assume it is
                            // corrupt and delete the storage file and rebuild it.
                            try
                            {
                                File.Delete(filepath);
                            }
                            catch (IOException)
                            {
                                // we failed to delete the file, nothing else to do.
                            }
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // nothign was found, we will return false
                }
            }
        }

        private void CleanupLegacyFiles(string folderpath)
        {
            var filesToDelete = new[]
            {
                "WaFFLSeasonV2.data",
                "WaFFLSeason.data",
                "MarkedPlayer.data"
            };

            foreach (var file in filesToDelete)
            {
                var filePathToDelete = Path.Combine(folderpath, file);
                if (!File.Exists(filePathToDelete))
                    continue;

                try
                {
                    File.Delete(filePathToDelete);
                }
                catch (IOException)
                {
                    // eat any delete exception -- if we can't delete it, then that's fine; we will move on
                }
            }
        }
    }

    [Serializable]
    public class AppState
    {
        /// <summary>
        /// Gets or sets the name and path of the last file opened by the app.
        /// </summary>
        public string LastOpenedFile { get; set; }
    }
}
