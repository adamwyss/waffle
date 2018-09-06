﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Controls.Ribbon;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        /// <summary>
        /// The year we want stats.
        /// </summary>
        const int YEAR = 2018;

        /// <summary />
        public static readonly DependencyProperty IsRefreshingDataProperty = DependencyProperty.Register(
            "IsRefreshingData",
            typeof(bool),
            typeof(MainWindow));

        /// <summary />
        public static readonly DependencyProperty ParsingStatusTextProperty = DependencyProperty.Register(
            "ParsingStatusText",
            typeof(string),
            typeof(MainWindow));

        /// <summary />
        public static readonly DependencyProperty CurrentSeasonProperty = DependencyProperty.Register(
            "CurrentSeason",
            typeof(FanastySeason),
            typeof(MainWindow));

        /// <summary />
        private static readonly TimeSpan MinimumRefreshDuration = TimeSpan.FromSeconds(1.0);

        /// <summary />
        private DateTime refreshTimeStamp;

        /// <summary />
        private ThreadStart refreshDelegate;

        /// <summary />
        private FanastySeason season;
        
        /// <summary />
        private Collection<Action<FanastySeason>> subscribers = new Collection<Action<FanastySeason>>();

        /// <summary />
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;

            this.refreshDelegate = new ThreadStart(this.RefreshAndParse);

            this.Loaded += this.OnWindowLoaded;
            this.Closing += this.OnWindowClosing;
        }

        /// <summary />
        public bool IsRefreshingData
        {
            get { return (bool)this.GetValue(IsRefreshingDataProperty); }
            set { this.SetValue(IsRefreshingDataProperty, value); }
        }

        /// <summary />
        public string ParsingStatusText
        {
            get { return (string)this.GetValue(ParsingStatusTextProperty); }
            set { this.SetValue(ParsingStatusTextProperty, value); }
        }

        /// <summary />
        public FanastySeason CurrentSeason
        {
            get { return (FanastySeason)this.GetValue(CurrentSeasonProperty); }
            set { this.SetValue(CurrentSeasonProperty, value); }
        }

        /// <summary />
        public void RegisterForSeasonChanges(Action<FanastySeason> callback)
        {
            this.subscribers.Add(callback);

            // if we have already populated our fananty season, notify our
            // subscriber.
            if (this.season != null)
            {
                this.Dispatcher.BeginInvoke(callback, this.season);
            }
        }

        /// <summary />
        private void RefreshAndParse()
        {
            this.SetIsRefreshingData(true);
            try
            {
                Action<string> x = new Action<string>(
                    delegate(string value)
                    {
                        this.Dispatcher.BeginInvoke(new Action<string>(delegate(string val)
                            {
                                this.ParsingStatusText = val;
                            }), value);
                    });

                ESPNEntityParser espn = new ESPNEntityParser(x);
                espn.ParseSeason(YEAR, ref this.season);

                if (this.subscribers != null)
                {
                    foreach (var subscriber in this.subscribers)
                    {
                        subscriber(this.season);
                    }
                }
            }
            finally
            {
                this.SetIsRefreshingData(false);
            }
        }

        /// <summary />
        private void SetIsRefreshingData(bool value)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                // re-execute this method on the ui thread.
                this.Dispatcher.BeginInvoke(new Action<bool>(this.SetIsRefreshingData), value);
            }
            else if (value)
            {
                this.refreshTimeStamp = DateTime.Now;
                this.IsRefreshingData = true;
            }
            else
            {
                this.CurrentSeason = this.season;

                // if the loading indicator has been displayed for more than the minimum amount
                // of time, we will stop refreshing our data.
                TimeSpan duration = DateTime.Now - this.refreshTimeStamp;
                if (duration < MinimumRefreshDuration)
                {
                    TimeSpan remaining = MinimumRefreshDuration.Subtract(duration);
                    new DispatcherTimer(remaining, DispatcherPriority.Normal, delegate { this.IsRefreshingData = false; }, this.Dispatcher).Start();
                }
                else
                {
                    this.IsRefreshingData = false;
                }
            }
        }

        /// <summary />
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            bool success = WaFFLPersister.TryLoadSeason(out this.season);

            if (!success)
            {
                // if we were unable to load the season from disk, we will
                // scrape the data from the web agian.
                Thread thread = new Thread(this.refreshDelegate);
                thread.Start();
            }
            else
            {
                this.CurrentSeason = this.season;
            }


            List<string> markedPlayers = null;
            success = MarkedPlayerPersister.TryLoadPlayers(out markedPlayers);
            if (!success)
            {
                markedPlayers = new List<string>();
            }
            MarkedPlayers.Players = markedPlayers;

        }

        /// <summary />
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // when the window closes, save the current season data
            // to disk, so we don't have to requery the server the
            // next time we open.
            WaFFLPersister.SaveSeason(this.season);
            MarkedPlayerPersister.SavePlayers(MarkedPlayers.Players);
        }

        /// <summary />
        private void RefreshCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Thread thread = new Thread(this.refreshDelegate);
            thread.Start();
        }

        /// <summary />
        private void CanExecuteRefreshCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !this.IsRefreshingData;
        }

        /// <summary />
        private void GoToCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Item item = this.playerView.SelectedItem;
            int espn_id = item.PlayerData.ESPN_Identifier;
            string url = string.Format("http://sports.espn.go.com/nfl/players/profile?playerId={0}", espn_id);

            // tell the explorer to 
            Process.Start(url);
        }

        /// <summary />
        private void CanExecuteGoToCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute =  this.playerView != null && this.playerView.SelectedItem != null;
        }

        /// <summary />
        private void FlagCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.playerView != null && this.playerView.Visibility == System.Windows.Visibility.Visible)
            {
                Item item = this.playerView.SelectedItem;
                MarkedPlayers.Evaluate(item.PlayerData.Name);
            }
            else if (this.defenseView != null && this.defenseView.Visibility == System.Windows.Visibility.Visible)
            {
                Item_DST item = this.defenseView.dg.SelectedItem as Item_DST;
                MarkedPlayers.Evaluate(item.TeamName);
            }
        }

        /// <summary />
        private void CanExecuteFlagCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            if (this.playerView != null && this.playerView.Visibility == System.Windows.Visibility.Visible)
            {
                e.CanExecute = this.playerView.SelectedItem != null;
            }
            else if (this.defenseView != null && this.defenseView.Visibility == System.Windows.Visibility.Visible)
            {
                e.CanExecute = this.defenseView.dg.SelectedItem != null;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void WhenExitClicked(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void WhenPlayerViewChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            this.playerView.Visibility = System.Windows.Visibility.Visible;
            this.playerSearchTools.Visibility = System.Windows.Visibility.Visible;
        }

        private void WhenPlayerViewUnchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            this.playerView.Visibility = System.Windows.Visibility.Collapsed;
            this.playerSearchTools.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void WhenDSTViewChecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;
            
            this.defenseView.Visibility = System.Windows.Visibility.Visible;
        }

        private void WhenDSTViewUnchecked(object sender, RoutedEventArgs e)
        {
            if (!this.IsLoaded)
                return;

            this.defenseView.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
