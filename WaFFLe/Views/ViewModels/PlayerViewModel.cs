using System;
using System.Diagnostics;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using WaFFL.Evaluation.Parsers;

namespace WaFFL.Evaluation
{
    /// <summary />
    [DebuggerDisplay("{PlayerData}")]
    public class PlayerViewModel : ViewModelBase
    {
        /// <summary />
        public PlayerViewModel(NFLPlayer p)
        {
            this.PlayerData = p;

            // cache the name+injury status here.
            this.Name = this.PlayerData.Name + GetInjuryStatus();

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

        public string InjuryStatus
        {
            get { return this.PlayerData.Status?.Reason; }
        }

        /// <summary />
        public bool IsHighlighted
        {
            get { return MarkedPlayers.IsMarked(this.PlayerData.Name); }
        }

        /// <summary />
        public string Name
        {
            get; private set;
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
        public int PointsOverReplacement { get; set; }

        /// <summary />
        public int WeightedPointsOverReplacement { get; set; }

        /// <summary />
        public int ConsistentPointsOverReplacement { get; set; }

        /// <summary />
        public int FanastyPoints { get; set; }

        /// <summary />
        public int TotalBonuses { get; set; }

        /// <summary />
        public int Mean { get; set; }

        /// <summary />
        public int StandardDeviation { get; set; }

        /// <summary />
        public int CoefficientOfVariation { get; set; }

        private string GetScoreForWeek(int week)
        {
            foreach (Game g in this.PlayerData.GameLog)
            {
                if (g.Week == week)
                {
                    return g.GetFanastyPoints().ToString();
                }
            }

            return string.Empty;
        }

        private int GetScoreForWeekSortable(int week)
        {
            foreach (Game g in this.PlayerData.GameLog)
            {
                if (g.Week == week)
                {
                    return g.GetFanastyPoints();
                }
            }

            return 0;
        }

        private bool IsByeWeek(int week)
        {
            return ByeWeeks.IsByeWeek(Team, week);
        }

        public string Week1Score
        {
            get { return GetScoreForWeek(1); }
        }

        public int Week1ScoreSortable
        {
            get { return GetScoreForWeekSortable(1); }
        }

        public bool IsWeek1Bye
        {
            get { return IsByeWeek(1); }
        }

        public string Week2Score
        {
            get { return GetScoreForWeek(2); }
        }

        public int Week2ScoreSortable
        {
            get { return GetScoreForWeekSortable(2); }
        }

        public bool IsWeek2Bye
        {
            get { return IsByeWeek(2); }
        }

        public string Week3Score
        {
            get { return GetScoreForWeek(3); }
        }

        public int Week3ScoreSortable
        {
            get { return GetScoreForWeekSortable(3); }
        }

        public bool IsWeek3Bye
        {
            get { return IsByeWeek(3); }
        }

        public string Week4Score
        {
            get { return GetScoreForWeek(4); }
        }

        public int Week4ScoreSortable
        {
            get { return GetScoreForWeekSortable(4); }
        }

        public bool IsWeek4Bye
        {
            get { return IsByeWeek(4); }
        }

        public string Week5Score
        {
            get { return GetScoreForWeek(5); }
        }

        public int Week5ScoreSortable
        {
            get { return GetScoreForWeekSortable(5); }
        }

        public bool IsWeek5Bye
        {
            get { return IsByeWeek(5); }
        }

        public string Week6Score
        {
            get { return GetScoreForWeek(6); }
        }

        public int Week6ScoreSortable
        {
            get { return GetScoreForWeekSortable(6); }
        }

        public bool IsWeek6Bye
        {
            get { return IsByeWeek(6); }
        }

        public string Week7Score
        {
            get { return GetScoreForWeek(7); }
        }

        public int Week7ScoreSortable
        {
            get { return GetScoreForWeekSortable(7); }
        }

        public bool IsWeek7Bye
        {
            get { return IsByeWeek(7); }
        }

        public string Week8Score
        {
            get { return GetScoreForWeek(8); }
        }

        public int Week8ScoreSortable
        {
            get { return GetScoreForWeekSortable(8); }
        }

        public bool IsWeek8Bye
        {
            get { return IsByeWeek(8); }
        }

        public string Week9Score
        {
            get { return GetScoreForWeek(9); }
        }

        public int Week9ScoreSortable
        {
            get { return GetScoreForWeekSortable(9); }
        }

        public bool IsWeek9Bye
        {
            get { return IsByeWeek(9); }
        }

        public string Week10Score
        {
            get { return GetScoreForWeek(10); }
        }

        public int Week10ScoreSortable
        {
            get { return GetScoreForWeekSortable(10); }
        }

        public bool IsWeek10Bye
        {
            get { return IsByeWeek(10); }
        }

        public string Week11Score
        {
            get { return GetScoreForWeek(11); }
        }

        public int Week11ScoreSortable
        {
            get { return GetScoreForWeekSortable(11); }
        }

        public bool IsWeek11Bye
        {
            get { return IsByeWeek(11); }
        }

        public string Week12Score
        {
            get { return GetScoreForWeek(12); }
        }

        public int Week12ScoreSortable
        {
            get { return GetScoreForWeekSortable(12); }
        }

        public bool IsWeek12Bye
        {
            get { return IsByeWeek(12); }
        }

        public string Week13Score
        {
            get { return GetScoreForWeek(13); }
        }

        public int Week13ScoreSortable
        {
            get { return GetScoreForWeekSortable(13); }
        }

        public bool IsWeek13Bye
        {
            get { return IsByeWeek(13); }
        }

        public string Week14Score
        {
            get { return GetScoreForWeek(14); }
        }

        public int Week14ScoreSortable
        {
            get { return GetScoreForWeekSortable(14); }
        }

        public bool IsWeek14Bye
        {
            get { return IsByeWeek(14); }
        }

        public string Week15Score
        {
            get { return GetScoreForWeek(15); }
        }

        public int Week15ScoreSortable
        {
            get { return GetScoreForWeekSortable(15); }
        }

        public bool IsWeek15Bye
        {
            get { return IsByeWeek(15); }
        }

        public string Week16Score
        {
            get { return GetScoreForWeek(16); }
        }

        public int Week16ScoreSortable
        {
            get { return GetScoreForWeekSortable(16); }
        }

        public bool IsWeek16Bye
        {
            get { return IsByeWeek(16); }
        }

        public string Week17Score
        {
            get { return GetScoreForWeek(17); }
        }

        public int Week17ScoreSortable
        {
            get { return GetScoreForWeekSortable(17); }
        }
        
        public bool IsWeek17Bye
        {
            get { return IsByeWeek(17); }
        }

        private string GetInjuryStatus()
        {
            if (this.PlayerData.Status != null)
            {
                if (this.PlayerData.Status.Status == PlayerInjuryStatus.InjuredReserve || this.PlayerData.Status.Status == PlayerInjuryStatus.Out)
                {
                    return " [OUT]";
                }
                else if (this.PlayerData.Status.Status == PlayerInjuryStatus.Doubtful)
                {
                    return " [D]";
                }
                else if (this.PlayerData.Status.Status == PlayerInjuryStatus.Questionable)
                {
                    return " [Q]";
                }
            }

            return string.Empty;
        }

    }
}
