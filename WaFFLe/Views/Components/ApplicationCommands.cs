using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace WaFFL.Evaluation
{
    /// <summary />
    public class ApplicationCommands
    {
        /// <summary />
        public readonly static RoutedUICommand Refresh = new RoutedUICommand(
            "Refresh",
            "RefreshCommand",
            typeof(ApplicationCommands));

        /// <summary />
        public readonly static RoutedUICommand GoTo = new RoutedUICommand(
            "GoTo",
            "GoToCommand",
            typeof(ApplicationCommands));

        /// <summary />
        public readonly static RoutedUICommand Flag = new RoutedUICommand(
            "Flag",
            "FlagCommand",
            typeof(ApplicationCommands));
    }
}
