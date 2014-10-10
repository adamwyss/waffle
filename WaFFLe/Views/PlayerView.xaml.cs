using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System;
using System.Linq;
using System.Collections;
using System.Windows.Input;
using System.Diagnostics;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for PlayerView.xaml
    /// </summary>
    public partial class PlayerView : UserControl, ISelectable
    {
        /// <summary />
        public static readonly DependencyProperty FilterProperty = DependencyProperty.Register(
            "Filter",
            typeof(string),
            typeof(PlayerView),
            new PropertyMetadata(null, WhenFilterPropertyChanged));

        private static void WhenFilterPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PlayerView view = (PlayerView)sender;
            view.ApplyFilter();
        }

        bool registered = false;

        List<Item> allPlayers;

        public PlayerView()
        {
            this.Players = new ObservableCollection<Item>();

            this.InitializeComponent();
            this.DataContext = this;

            this.Loaded += delegate
                {
                    if (!this.registered)
                    {
                        for (DependencyObject obj = this; obj != null; obj = LogicalTreeHelper.GetParent(obj))
                        {
                            MainWindow window = obj as MainWindow;
                            if (window != null)
                            {
                                window.RegisterForSeasonChanges(
                                    delegate(FanastySeason season)
                                    {
                                        this.Dispatcher.BeginInvoke(
                                            new Action<IEnumerable<QB>, IEnumerable<RB>, IEnumerable<WR>, IEnumerable<K>>(this.Refresh), 
                                            QB.ConvertAndInitialize(season),
                                            RB.ConvertAndInitialize(season),
                                            WR.ConvertAndInitialize(season),
                                            K.ConvertAndInitialize(season));
                                    });
                                this.registered = true;
                                break;
                            }
                        }
                    }
                };
        }

        private void Refresh(IEnumerable<QB> qbs, IEnumerable<RB> rbs, IEnumerable<WR> wrs, IEnumerable<K> ks)
        {
            this.allPlayers = new List<Item>();
            this.Load(qbs);
            this.Load(rbs);
            this.Load(wrs);
            this.Load(ks);
            this.ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Item> filteredList = this.allPlayers.Where(IsNotFiltered);

            this.Players.Clear();
            foreach (Item player in filteredList)
            {
                this.Players.Add(player);
            }
        }

        private bool IsNotFiltered(Item player)
        {
            switch (player.PlayerData.Position)
            {
                case FanastyPosition.QB:
                    if (this.QBFilter.IsChecked == false) return false;
                    break;
                case FanastyPosition.RB:
                    if (this.RBFilter.IsChecked == false) return false;
                    break;
                case FanastyPosition.WR:
                    if (this.WRFilter.IsChecked == false) return false;
                    break;
                case FanastyPosition.K:
                    if (this.KFilter.IsChecked == false) return false;
                    break;
            }

            if (this.Filter == null)
            {
                return true;
            }

            return player.Name.IndexOf(Filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private void Load(IEnumerable<QB> quarterbacks)
        {
            foreach (Item qb in quarterbacks)
            {
                this.allPlayers.Add(qb);
            }
        }

        private void Load(IEnumerable<RB> runningbacks)
        {
            foreach (RB rb in runningbacks)
            {
                this.allPlayers.Add(rb);
            }
        }

        private void Load(IEnumerable<WR> widereceivers)
        {
            foreach (WR qb in widereceivers)
            {
                this.allPlayers.Add(qb);
            }
        }

        private void Load(IEnumerable<K> kickers)
        {
            lock (((ICollection)this.Players).SyncRoot)
            {
                foreach (K k in kickers)
                {
                    this.allPlayers.Add(k);
                }
            }
        }

        public ObservableCollection<Item> Players { get; private set; }

        /// <summary />
        public string Filter
        {
            get { return (string)this.GetValue(FilterProperty); }
            set { this.SetValue(FilterProperty, value); }
        }

        public Item SelectedItem
        {
            get { return this.dg.SelectedItem as Item; }
        }

        private void WhenFilterTogglesClicked(object sender, RoutedEventArgs e)
        {
            this.ApplyFilter();
        }
    }
}
