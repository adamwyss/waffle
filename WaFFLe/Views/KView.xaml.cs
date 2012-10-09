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
    /// Interaction logic for KView.xaml
    /// </summary>
    public partial class KView : UserControl, ISelectable
    {
        bool registered = false;

        public KView()
        {
            this.Kickers = new ObservableCollection<K>();

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
                                    this.Dispatcher.BeginInvoke(
                                        new Action<IEnumerable<K>>(this.Refresh),
                                        K.ConvertAndInitialize(season));
                                });
                            this.registered = true;
                            break;
                        }
                    }
                }
            };
        }

        private void Refresh(IEnumerable<K> kickers)
        {
            lock (((ICollection)this.Kickers).SyncRoot)
            {
                this.Kickers.Clear();
                foreach (K k in kickers)
                {
                    this.Kickers.Add(k);
                }
            }
        }

        public ObservableCollection<K> Kickers { get; private set; }

        public Item SelectedItem
        {
            get { return this.dg.SelectedItem as Item; }
        }
    }
}
