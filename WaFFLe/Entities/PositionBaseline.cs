using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WaFFL.Evaluation
{
    /// <summary />
    [Serializable]
    public class PositionBaseline
    {
        /// <summary />
        public int QB { get; set; }

        /// <summary />
        public int RB { get; set; }

        /// <summary />
        public int WR { get; set; }

        /// <summary />
        public int K { get; set; }

        /// <summary />
        public int DST { get; set; }

        public int Team
        {
            get
            {
                return this.QB + this.RB + this.WR * 2 + (this.RB > this.WR ? this.RB : this.WR) + this.K + this.DST;
            }
        }
    }
}
