using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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

        public static readonly DependencyProperty IsScopeQBProperty = DependencyProperty.Register(
            "IsScopeQB",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(true, WhenFilterPropertyChanged));

        public static readonly DependencyProperty IsScopeRBProperty = DependencyProperty.Register(
            "IsScopeRB",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(true, WhenFilterPropertyChanged));

        public static readonly DependencyProperty IsScopeWRProperty = DependencyProperty.Register(
            "IsScopeWR",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(true, WhenFilterPropertyChanged));

        public static readonly DependencyProperty IsScopeKProperty = DependencyProperty.Register(
            "IsScopeK",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(true, WhenFilterPropertyChanged));

        public static readonly DependencyProperty IsScopeAvailableProperty = DependencyProperty.Register(
            "IsScopeAvailable",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(false, WhenFilterPropertyChanged));

        public static readonly DependencyProperty IsScopeHighlightedProperty = DependencyProperty.Register(
            "IsScopeHighlighted",
            typeof(bool),
            typeof(PlayerView),
            new PropertyMetadata(false, WhenFilterPropertyChanged));

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

        public ObservableCollection<Item> Players { get; private set; }

        /// <summary />
        public string Filter
        {
            get { return (string)this.GetValue(FilterProperty); }
            set { this.SetValue(FilterProperty, value); }
        }

        /// <summary />
        public bool IsScopeQB
        {
            get { return (bool)this.GetValue(IsScopeQBProperty); }
            set { this.SetValue(IsScopeQBProperty, value); }
        }

        /// <summary />
        public bool IsScopeRB
        {
            get { return (bool)this.GetValue(IsScopeRBProperty); }
            set { this.SetValue(IsScopeRBProperty, value); }
        }

        /// <summary />
        public bool IsScopeWR
        {
            get { return (bool)this.GetValue(IsScopeWRProperty); }
            set { this.SetValue(IsScopeWRProperty, value); }
        }

        /// <summary />
        public bool IsScopeK
        {
            get { return (bool)this.GetValue(IsScopeKProperty); }
            set { this.SetValue(IsScopeKProperty, value); }
        }

        /// <summary />
        public bool IsScopeAvailable
        {
            get { return (bool)this.GetValue(IsScopeAvailableProperty); }
            set { this.SetValue(IsScopeAvailableProperty, value); }
        }

        /// <summary />
        public bool IsScopeHighlighted
        {
            get { return (bool)this.GetValue(IsScopeHighlightedProperty); }
            set { this.SetValue(IsScopeHighlightedProperty, value); }
        }

        public Item SelectedItem
        {
            get { return this.dg.SelectedItem as Item; }
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
                    if (!this.IsScopeQB) return false;
                    break;
                case FanastyPosition.RB:
                    if (!this.IsScopeRB) return false;
                    break;
                case FanastyPosition.WR:
                    if (!this.IsScopeWR) return false;
                    break;
                case FanastyPosition.K:
                    if (!this.IsScopeK) return false;
                    break;
            }

            bool a = true;
            bool h = true;
            if (this.IsScopeAvailable && player.IsAvailable)
                a = false;
            if (this.IsScopeHighlighted && !player.IsHighlighted)
                h = false;

            if (!a && !this.IsScopeHighlighted)
                return false;
            else if (!h && !this.IsScopeAvailable)
                return false;
            else if (!a && !h)
                return false;

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
    }
}
