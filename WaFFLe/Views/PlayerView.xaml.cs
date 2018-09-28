using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WaFFL.Evaluation.Views.ViewModels;

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

        public static readonly DependencyProperty IsScopeDSTProperty = DependencyProperty.Register(
            "IsScopeDST",
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
                                        var qb = new PlayerLoader<QB>(FanastyPosition.QB, season.ReplacementValue.QB, p => new QB(p));
                                        var rb = new PlayerLoader<RB>(FanastyPosition.RB, season.ReplacementValue.RB, p => new RB(p));
                                        var wr = new PlayerLoader<WR>(FanastyPosition.WR, season.ReplacementValue.WR, p => new WR(p));
                                        var k = new PlayerLoader<K>(FanastyPosition.K, season.ReplacementValue.K, p => new K(p));
                                        var dst = new PlayerLoader<DST>(FanastyPosition.DST, season.ReplacementValue.DST, p => new DST(p));

                                        this.Dispatcher.BeginInvoke(
                                            new Action<IEnumerable<QB>, IEnumerable<RB>, IEnumerable<WR>, IEnumerable<K>, IEnumerable<DST>>(this.Refresh), 
                                            qb.GetViewModels(season),
                                            rb.GetViewModels(season),
                                            wr.GetViewModels(season),
                                            k.GetViewModels(season),
                                            DST.ConvertAndInitialize(season));
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
        public bool IsScopeDST
        {
            get { return (bool)this.GetValue(IsScopeDSTProperty); }
            set { this.SetValue(IsScopeDSTProperty, value); }
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

        private void Refresh(IEnumerable<QB> qbs, IEnumerable<RB> rbs, IEnumerable<WR> wrs, IEnumerable<K> ks, IEnumerable<DST> dsts)
        {
            this.allPlayers = new List<Item>();
            this.Load(qbs);
            this.Load(rbs);
            this.Load(wrs);
            this.Load(ks);
            this.Load(dsts);
            this.ApplyFilter();
        }

        private void ApplyFilter()
        {
            IEnumerable<Item> filteredList = this.allPlayers.Where(IsNotFiltered);

            var remove = this.Players.Except(filteredList).ToArray();
            var add = filteredList.Except(this.Players).ToArray();

            foreach (Item player in remove)
            {
                this.Players.Remove(player);
            }

            foreach (Item player in add)
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
                case FanastyPosition.DST:
                    if (!this.IsScopeDST) return false;
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

        private void Load<T>(IEnumerable<T> players) where T : Item
        {
            foreach (T player in players)
            {
                this.allPlayers.Add(player);
            }
        }
    }
}
