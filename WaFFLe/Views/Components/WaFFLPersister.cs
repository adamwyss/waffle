using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WaFFL.Evaluation
{
    /// <summary />
    internal class WaFFLPersister
    {
        /// <summary />
        private const string DataFolder = "WaFFL";

        /// <summary />
        private const string DataFile = "WaFFLSeason.data";

        /// <summary />
        public static void SaveSeason(FanastySeason data)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderpath = Path.Combine(root, DataFolder);

            if (!Directory.Exists(folderpath))
            {
                Directory.CreateDirectory(folderpath);
            }

            string filepath = Path.Combine(folderpath, DataFile);

            using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, data);
            }
        }

        /// <summary />
        public static bool TryLoadSeason(out FanastySeason data)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderpath = Path.Combine(root, DataFolder);

            if (Directory.Exists(folderpath))
            {
                string filepath = Path.Combine(folderpath, DataFile);
                try
                {
                    using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        try
                        {
                            data = (FanastySeason)formatter.Deserialize(stream);
                            return true;
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

            data = null;
            return false;
        }
    }
}
