using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for WRView.xaml
    /// </summary>
    public partial class WRView : UserControl
    {
        bool registered = false;

        public WRView()
        {
            this.WideReceivers = new ObservableCollection<WR>();

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
                                        new Action<IEnumerable<WR>>(this.Refresh),
                                        WR.ConvertAndInitialize(season));
                                });
                            this.registered = true;
                            break;
                        }
                    }
                }
            };
        }

        private void Refresh(IEnumerable<WR> widereceivers)
        {
            lock (((ICollection)this.WideReceivers).SyncRoot)
            {
                this.WideReceivers.Clear();
                foreach (WR qb in widereceivers)
                {
                    this.WideReceivers.Add(qb);
                }
            }
        }

        public ObservableCollection<WR> WideReceivers { get; private set; }
    }
}
