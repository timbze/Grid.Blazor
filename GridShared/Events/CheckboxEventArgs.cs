﻿using GridShared.Columns;
using System;

namespace GridShared.Events
{
    public class CheckboxEventArgs<T> : EventArgs
    {
        public string ColumnName { get; set; }
        public CheckboxValue Value { get; set; }
        public CheckboxValue HeaderValue { get; set; }
        public T Item { get; set; }
        public int RowId { get; set; }
        public bool SingleCheckboxMode { get; set; }
    }
}
