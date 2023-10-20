using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;
using System.Collections;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using WaFFL.Evaluation.Parsers;

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

    public class Item_DST : ViewModelBase
    {
        private NFLTeam model;
        private string displayName;

        public Item_DST(NFLTeam dst)
        {
            this.model = dst;
            this.displayName = DataConverter.ConvertToCode(this.model.TeamCode);

            Messenger.Default.Register<MarkedPlayerChanged>(this,
                (m) =>
                {
                    if (m.Name == this.displayName)
                    {
                        this.RaisePropertyChanged("IsHighlighted");
                    }
                });
        }

        public bool IsAvailable
        {
            get { return WaFFLTeam.IsRostered(this.displayName); }
        }

        /// <summary />
        public bool IsHighlighted
        {
            get { return MarkedPlayers.IsMarked(this.displayName); }
        }

        public string TeamName
        {
            get { return this.displayName; }
        }

        public string TeamCode
        {
            get { return this.model.TeamCode; }
        }

        public int ByeWeek
        {
            get
            {
                return ByeWeeks.ByeWeek(this.model.TeamCode);
            }
        }
    }
}
