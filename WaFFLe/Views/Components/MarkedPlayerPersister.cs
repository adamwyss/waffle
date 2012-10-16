using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;

namespace WaFFL.Evaluation
{
    /// <summary />
    internal class MarkedPlayerPersister
    {
        /// <summary />
        private const string DataFolder = "WaFFL";

        /// <summary />
        private const string DataFile = "MarkedPlayer.data";

        /// <summary />
        public static void SavePlayers(List<string> data)
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
        public static bool TryLoadPlayers(out List<string> data)
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
                            data = (List<string>)formatter.Deserialize(stream);
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
