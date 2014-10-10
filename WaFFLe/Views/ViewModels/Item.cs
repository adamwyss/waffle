using System.Diagnostics;
using System.Text;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace WaFFL.Evaluation
{
    /// <summary />
    [DebuggerDisplay("{PlayerData}")]
    public abstract class Item : ViewModelBase
    {
        /// <summary />
        public Item(NFLPlayer p)
        {
            this.PlayerData = p;

            Messenger.Default.Register<MarkedPlayerChanged>(this,
                (m) =>
                {
                    if (m.Name == this.PlayerData.Name)
                    {
                        this.RaisePropertyChanged("IsHighlighted");
                    }
                });
        }

        /// <summary />
        public NFLPlayer PlayerData { get; private set; }

        /// <summary />
        public bool IsAvailable
        {
            get { return WaFFLRoster.IsActive(this.PlayerData.Name); }
        }

        /// <summary />
        public bool IsHighlighted
        {
            get { return MarkedPlayers.IsMarked(this.PlayerData.Name); }
        }

        /// <summary />
        public string Name
        {
            get { return this.PlayerData.Name; }
        }

        public string Position
        {
            get
            {
                FanastyPosition position = this.PlayerData.Position;
                if (position != FanastyPosition.UNKNOWN)
                {
                    return position.ToString();
                }

                return "n/a";
            }
        }

        /// <summary />
        public string Team
        {
            get
            {
                NFLTeam team = this.PlayerData.Team;
                if (team != null)
                {
                    return team.TeamCode;
                }

                return "n/a";
            }
        }

        /// <summary />
        public string WeekByWeekScores
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                foreach (Game g in this.PlayerData.GameLog)
                {
                    int points = g.GetFanastyPoints();
                    sb.Append(string.Format("({0}) ", g.Week));
                    sb.Append(points);
                    sb.Append(", ");
                }

                if (sb.Length > 2)
                    sb.Remove(sb.Length - 2, 2);

                return sb.ToString();
            }
        }
    }
}
