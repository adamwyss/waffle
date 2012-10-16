using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Messaging;

namespace WaFFL.Evaluation
{
    public class MarkedPlayerChanged
    {
        public string Name { get; set; }
    }

    public class MarkedPlayers
    {
        public static List<string> Players { get; set; }
        
        public static void Evaluate(string name)
        {
            if (Players != null)
            {
                bool exists = Players.Contains(name);
                if (exists)
                {
                    Players.Remove(name);
                    Messenger.Default.Send<MarkedPlayerChanged>(new MarkedPlayerChanged() { Name = name });
                }
                else
                {
                    Players.Add(name);
                    Messenger.Default.Send<MarkedPlayerChanged>(new MarkedPlayerChanged() { Name = name });
                }
            }
        }

        public static bool IsMarked(string name)
        {
            if (Players != null)
            {
                return Players.Contains(name);
            }

            return false;
        }

    }
}
