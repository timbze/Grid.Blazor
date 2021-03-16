using GridShared.Columns;
using System;

namespace GridShared.Events
{
    public class HeaderCheckboxEventArgs<T> : CheckboxEventArgs<T>
    {
        public CheckboxValue HeaderValue { get; set; }
    }
}
