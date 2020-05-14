using System;
using System.Collections.Generic;
using System.Text;

namespace GridShared.Events
{
    public class GridRefreshEventArgs : EventArgs
    {
        public bool ReloadData { get; set; }
    }
}
