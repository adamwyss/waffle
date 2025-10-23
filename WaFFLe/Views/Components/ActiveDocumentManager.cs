using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace WaFFL.Evaluation
{
    /// <summary />
    internal class WaFFLPersister
    {
        public static void SaveSeason(string filepath, FanastySeason data, List<string> markedPlayers)
        {
            string root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (!Directory.Exists(root))
            {
                Directory.CreateDirectory(root);
            }

            var ss = new StorageStructure()
            {
                Season = data,
                MarkedPlayers = markedPlayers
            };

            using (FileStream stream = new FileStream(filepath, FileMode.Create, FileAccess.Write))
            {
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, ss);
            }
        }

        public static bool TryLoadSeason(string filepath, out FanastySeason data, out List<string> markedPlayers)
        {
            if (File.Exists(filepath))
            {
                try
                {
                    using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        try
                        {
                            var ss = (StorageStructure)formatter.Deserialize(stream);
                            data = ss.Season;
                            markedPlayers = ss.MarkedPlayers;
                            return true;
                        }
                        catch (SerializationException)
                        {
                            // we failed to deserialize the data.  We will assume it is
                            // corrupt and bail out
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    // nothign was found, we will return false
                }
            }

            data = null;
            markedPlayers = null;
            return false;
        }
    }

    public class ActiveDocumentManager
    {
        public ActiveDocumentManager()
        {
        }

        public event EventHandler StateChanged;

        public FanastySeason Season { get; private set; }

        public List<string> MarkedPlayers { get; private set; }

        public bool IsOpen { get; private set; }

        public void New(FanastySeason season, List<string> players)
        {
            IsOpen = true;
            Season = season;
            MarkedPlayers = players;
            OnStateChanged();
        }

        public void Open(string filePath)
        {
            IsOpen = WaFFLPersister.TryLoadSeason(filePath, out FanastySeason season, out List<string> markedPlayers);
            if (IsOpen)
            {
                Season = season;
                MarkedPlayers = markedPlayers;
                OnStateChanged();
            }
        }

        public void Save(string filePath)
        {
            WaFFLPersister.SaveSeason(filePath, Season, MarkedPlayers);
            OnStateChanged();
        }

        public void Close()
        {
            IsOpen = false;
            Season = null;
            MarkedPlayers = null;
            OnStateChanged();
        }

        protected virtual void OnStateChanged()
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Serializable]
    public class StorageStructure
    {
        public FanastySeason Season { get; set; }
        public List<string> MarkedPlayers { get; set; }
    }
}
