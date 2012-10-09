using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;
using System.Collections;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for DSTView.xaml
    /// </summary>
    public partial class DSTView : UserControl
    {
        private Action<IEnumerable<NFLTeam>> refreshDelegate;

        bool registered = false;

        public DSTView()
        {
            this.DefenseSpecialTeams = new ObservableCollection<Item_DST>();
            this.refreshDelegate = new Action<IEnumerable<NFLTeam>>(this.Refresh);

            this.InitializeComponent();
            this.DataContext = this;

            this.Loaded += delegate
            {
                if (!this.registered)
                {
                    for (DependencyObject obj = this; obj != null; obj = VisualTreeHelper.GetParent(obj))
                    {
                        MainWindow window = obj as MainWindow;
                        if (window != null)
                        {
                            window.RegisterForSeasonChanges(
                                delegate(FanastySeason season)
                                {
                                    IEnumerable<NFLTeam> teams = season.GetAllTeams();
                                    this.Dispatcher.BeginInvoke(this.refreshDelegate, teams);
                                });
                            this.registered = true;
                            break;
                        }
                    }
                }
            };
        }

        private void Refresh(IEnumerable<NFLTeam> teams)
        {
            lock (((ICollection)this.DefenseSpecialTeams).SyncRoot)
            {
                this.DefenseSpecialTeams.Clear();

                foreach (NFLTeam team in teams)
                {
                    Item_DST dst = new Item_DST(team);
                    this.DefenseSpecialTeams.Add(dst);
                }
            }
        }

        public ObservableCollection<Item_DST> DefenseSpecialTeams { get; private set; }
    }

    public class Item_DST
    {
        private NFLTeam model;
        private string displayName;

        public Item_DST(NFLTeam dst)
        {
            this.model = dst;
            this.displayName = DataConverter.ConvertToCode(this.model.TeamCode);
        }

        public bool IsAvailable
        {
            get { return WaFFLRoster.IsActive(this.displayName); }
        }

        public string TeamName
        {
            get { return this.displayName; }
        }

        public int EstimatedPoints
        {
            get
            {
                ESPNDefenseLeaders td = this.model.ESPNTeamDefense;
                if (td != null)
                {
                    return td.Estimate_Points();
                }

                return 0;
            }
        }

        public int Sacks
        {
            get
            {
                ESPNDefenseLeaders td = this.model.ESPNTeamDefense;
                if (td != null)
                {
                    return td.SACK;
                }

                return 0;
            }
        }

        public int Interceptions
        {
            get
            {
                ESPNDefenseLeaders td = this.model.ESPNTeamDefense;
                if (td != null)
                {
                    return td.INT;
                }

                return 0;
            }
        }

        public int Fumbles
        {
            get
            {
                ESPNDefenseLeaders td = this.model.ESPNTeamDefense;
                if (td != null)
                {
                    return td.REC;
                }

                return 0;
            }
        }
    }
}
