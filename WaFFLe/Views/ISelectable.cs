using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaFFL.Evaluation
{
    public interface ISelectable
    {
        PlayerViewModel SelectedItem { get; }
    }
}
