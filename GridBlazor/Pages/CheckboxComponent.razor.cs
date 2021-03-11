using GridShared.Columns;
using GridShared.Events;
using GridShared.Utility;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace GridBlazor.Pages
{
    public partial class CheckboxComponent<T> : ICustomGridComponent<T>
    {
        private Func<T, bool> _expr;
        private Func<T, bool> _readonlyExpr;
        private bool _value = false;
        private bool _readonly = false;
        private string _columnName;

        [CascadingParameter(Name = "GridComponent")]
        protected GridComponent<T> GridComponent { get; set; }

        [Inject] private ILogger<CheckboxComponent<T>> Logger { get; set; }
        [Parameter]
        public T Item { get; set; }

        [Parameter]
        public int RowId { get; set; }

        [Parameter]
        public object Object { get; set; }

        protected override void OnParametersSet()
        {
            Logger.LogDebug("Parameters set");
            if (Object.GetType() == typeof((string, Func<T, bool>)))
            {
                (_columnName, _expr) = ((string, Func<T, bool>))Object;
                _value = _expr(Item);
            }
            else if (Object.GetType() == typeof((string, Func<T, bool>, Func<T, bool>)))
            {
                (_columnName, _expr, _readonlyExpr) = ((string, Func<T, bool>, Func<T, bool>))Object;
                _value = _expr(Item);
                _readonly = _readonlyExpr(Item);
            }

            // add an empty dictionary if column is not in the dictionary
            if (GridComponent.CheckboxesKeyed.Get(_columnName) == null)
                GridComponent.CheckboxesKeyed.Add(_columnName, new QueryDictionary<(CheckboxComponent<T>, bool)>());

            // init value when header checkbox is enabled and is not null
            var header = GridComponent.HeaderComponents.Get(_columnName);
            var exceptCheckedRows = GridComponent.CheckboxesKeyed.Get(_columnName);
            var keys = GridComponent.Grid.GetPrimaryKeyValues(Item);
            string stringKeys = string.Join('_', keys);

            if (exceptCheckedRows != null && !string.IsNullOrWhiteSpace(stringKeys)
                && exceptCheckedRows.ContainsKey(stringKeys))
                _value = exceptCheckedRows.Get(stringKeys).Item2;
            else if (header != null && header.IsChecked().HasValue)
                _value = header?.IsChecked() == true;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                GridComponent.HeaderCheckboxChanged += HeaderCheckboxChanged;
            }
        }

        private async Task ChangeHandler()
        {
            await SetChecked(!_value);
        }

        private async Task HeaderCheckboxChanged(CheckboxEventArgs<T> e)
        {
            if (e.ColumnName == _columnName && !_readonly)
            {
                var header = GridComponent.HeaderComponents.Get(_columnName);
                if (header != null && header.Column != null && header.Column.SingleCheckbox)
                {
                    var exceptCheckedRows = GridComponent.CheckboxesKeyed.Get(_columnName);
                    var keys = GridComponent.Grid.GetPrimaryKeyValues(Item);
                    string stringKeys = string.Join('_', keys);
                    if (exceptCheckedRows.ContainsKey(stringKeys))
                    {
                        _value = true;
                    }
                    else
                    {
                        _value = false;
                    }
                }
                else
                {
                    if (e.Value == CheckboxValue.Checked)
                    {
                        _value = true;
                    }
                    else
                    {
                        _value = false;
                    }
                }
                StateHasChanged();
                await Task.CompletedTask;
            }
        }

        public bool IsChecked()
        {
            return _value;
        }

        public async Task SetChecked(bool value)
        {
            _value = value;

            var header = GridComponent.HeaderComponents.Get(_columnName);
            var checkBoxesKeyed = GridComponent.CheckboxesKeyed.Get(_columnName);
            var keys = GridComponent.Grid.GetPrimaryKeyValues(Item);
            string stringKeys = string.Join('_', keys);

            if (header != null && header.Column != null && header.Column.SingleCheckbox)
            {
                var checkedRows = new QueryDictionary<(CheckboxComponent<T>, bool)>();
                checkedRows.Add(stringKeys, (this, value));
                GridComponent.CheckboxesKeyed.AddOrSet(_columnName, checkedRows);

                CheckboxEventArgs<T> args = new CheckboxEventArgs<T>
                {
                    ColumnName = _columnName
                };
                await GridComponent.OnHeaderCheckboxChanged(args);
                StateHasChanged();
            }
            else
            {
                if (checkBoxesKeyed != null && !string.IsNullOrWhiteSpace(stringKeys))
                {
                    if (checkBoxesKeyed.ContainsKey(stringKeys))
                    {
                        if (header != null && header.IsChecked().HasValue && header.IsChecked().Value == value)
                            checkBoxesKeyed.Remove(stringKeys);
                        else
                            checkBoxesKeyed[stringKeys] = (this, value);
                    }
                    else
                    {
                        if (header == null || !header.IsChecked().HasValue || header.IsChecked().Value != value)
                            checkBoxesKeyed.Add(stringKeys, (this, value));
                    }
                }

                CheckboxEventArgs<T> args = new CheckboxEventArgs<T>
                {
                    ColumnName = _columnName,
                    Item = Item,
                    RowId = RowId
                };
                if (_value)
                {
                    args.Value = CheckboxValue.Checked;
                }
                else
                {
                    args.Value = CheckboxValue.Unchecked;
                }
                await GridComponent.OnRowCheckboxChanged(args);
                StateHasChanged();
            }
        }
    }
}
