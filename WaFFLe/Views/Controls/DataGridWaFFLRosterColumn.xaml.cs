using System;
using System.Reflection;
using System.Windows.Controls;

namespace WaFFL.Evaluation
{
    /// <summary>
    /// Interaction logic for DataGridWaFFLRosterColumn.xaml
    /// </summary>
    public partial class DataGridWaFFLRosterColumn : DataGridTemplateColumn
    {
        public DataGridWaFFLRosterColumn()
        {
            this.InitializeComponent();
        }

        public override object OnCopyingCellClipboardContent(object item)
        {
            Type itemType = item.GetType();
            PropertyInfo info = itemType.GetProperty("IsAvailable");
            if (info != null && info.PropertyType == typeof(bool))
            {
                bool value = (bool)info.GetValue(item, null);
                return value ? " " : "*";
            }

            return base.OnCopyingCellClipboardContent(item);
        }
    }
}
