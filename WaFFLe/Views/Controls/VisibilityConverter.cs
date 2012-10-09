using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace WaFFL.Evaluation
{
    public class VisibilityConverter : IValueConverter
    {
        public VisibilityConverter()
        {
            this.TrueIs = Visibility.Visible;
            this.FalseIs = Visibility.Hidden;
        }

        public Visibility TrueIs { get; set; }

        public Visibility FalseIs { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool booleanValue = (bool)value;
            return booleanValue ? this.TrueIs : this.FalseIs;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Visibility visibilityValue = (Visibility)value;

            if (visibilityValue == this.TrueIs)
            {
                return true;
            }
            else if (visibilityValue == this.FalseIs)
            {
                return false;
            }

            throw new InvalidOperationException();
        }
    }
}
