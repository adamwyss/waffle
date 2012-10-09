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
    /// Interaction logic for QBView.xaml
    /// </summary>
    public partial class QBView : UserControl
    {
        bool registered = false;

        public QBView()
        {
            this.Quarterbacks = new ObservableCollection<QB>();

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
                                            new Action<IEnumerable<QB>>(this.Refresh), 
                                            QB.ConvertAndInitialize(season));
                                    });
                                this.registered = true;
                                break;
                            }
                        }
                    }
                };
        }

        private void Refresh(IEnumerable<QB> quarterbacks)
        {
            lock (((ICollection)this.Quarterbacks).SyncRoot)
            {
                this.Quarterbacks.Clear();
                foreach (QB qb in quarterbacks)
                {
                    this.Quarterbacks.Add(qb);
                }
            }
        }

        public ObservableCollection<QB> Quarterbacks { get; private set; }


        /// <summary />
        private void GoToCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Item item = (Item)e.Parameter;
            int espn_id = item.PlayerData.ESPN_Identifier;
            string url = string.Format("http://sports.espn.go.com/nfl/players/profile?playerId={0}", espn_id);

            // tell the explorer to 
            Process.Start(url);
        }

        /// <summary />
        private void CanExecuteGoToCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is Item;
            e.CanExecute = true;
        }

    }
}
