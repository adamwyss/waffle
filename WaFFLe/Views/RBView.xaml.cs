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
    /// Interaction logic for RBView.xaml
    /// </summary>
    public partial class RBView : UserControl
    {
        bool registered = false;

        public RBView()
        {
            this.Runningbacks = new ObservableCollection<RB>();

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
                                        new Action<IEnumerable<RB>>(this.Refresh),
                                        RB.ConvertAndInitialize(season));
                                });
                            this.registered = true;
                            break;
                        }
                    }
                }
            };
        }

        private void Refresh(IEnumerable<RB> runningbacks)
        {
            lock (((ICollection)this.Runningbacks).SyncRoot)
            {
                this.Runningbacks.Clear();
                foreach (RB rb in runningbacks)
                {
                    this.Runningbacks.Add(rb);
                }
            }
        }

        public ObservableCollection<RB> Runningbacks { get; private set; }
    }
}
