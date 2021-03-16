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
        public object Object { get; set; }

        protected override void OnParametersSet()
        {
            if (!_initComplete) return;
            
            // add an empty dictionary if column is not in the dictionary
            if (GridComponent.CheckboxesKeyed.Get(_columnName) == null)
                GridComponent.CheckboxesKeyed.Add(_columnName, new QueryDictionary<(CheckboxComponent<T>, bool)>());

            _value = CalculateIsChecked();
        }

        private bool _initComplete;
        protected override void OnInitialized()
        {
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

            _initComplete = true;
            base.OnInitialized();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                GridComponent.HeaderCheckboxChanged += HeaderCheckboxChanged;
                
                var header = GridComponent.HeaderComponents.Get(_columnName);
                if (header?.Column?.SingleCheckbox == true)
                    GridComponent.RowCheckboxChanged += SingleCheckboxModeCheckboxChanged;
            }
        }

        private async Task ChangeHandler()
        {
            await SetChecked(!_value);
        }

        private async Task SingleCheckboxModeCheckboxChanged(CheckboxEventArgs<T> e)
        {
            if (e.ColumnName != _columnName || _readonly || !e.SingleCheckboxMode) return;

            var exceptCheckedRows = GridComponent.CheckboxesKeyed.Get(_columnName);
            string stringKeys = GetStringKeys();
            var oldValue = _value;
            _value = exceptCheckedRows.ContainsKey(stringKeys);

            if (oldValue != _value)
                await InvokeAsync(StateHasChanged);
        }

        private async Task HeaderCheckboxChanged(HeaderCheckboxEventArgs<T> e)
        {
            if (e.ColumnName != _columnName || _readonly) return;
            if (e.StringKey != GetStringKeys())
            {
                var updateValue = e.HeaderValue == CheckboxValue.Checked || (e.HeaderValue == CheckboxValue.Unchecked ? false : _value);
                await SetChecked(updateValue, false);
            }
        }

        private bool CalculateIsChecked()
        {
            var header = GridComponent.HeaderComponents.Get(_columnName);
            var exceptCheckedRows = GridComponent.CheckboxesKeyed.Get(_columnName);
            string stringKeys = GetStringKeys();
            if (string.IsNullOrWhiteSpace(stringKeys)) return false;

            var outValue = false;
            if (exceptCheckedRows?.ContainsKey(stringKeys) == true)
                outValue = exceptCheckedRows.Get(stringKeys).Item2;
            else if (header?.Column.HeaderCheckbox == true)
                outValue = (header.IsChecked() == null && header.LastHeaderCheckedValue) || header.IsChecked() == true;
            
            return outValue;
        }

        public bool IsChecked()
        {
            return _value;
        }

        public async Task SetChecked(bool value)
        {
            await SetChecked(value, true);
        }
        
        private async Task SetChecked(bool value, bool sendEvents)
        {
            _value = value;

            var header = GridComponent.HeaderComponents.Get(_columnName);
            var checkboxesKeyed = GridComponent.CheckboxesKeyed.Get(_columnName);
            string stringKeys = GetStringKeys();
            if (string.IsNullOrWhiteSpace(stringKeys)) return;

            var args = new CheckboxEventArgs<T>();
            if (header?.Column?.SingleCheckbox == true)
            {
                var checkedRows = new QueryDictionary<(CheckboxComponent<T>, bool)>();
                checkedRows.Add(stringKeys, (this, value));
                GridComponent.CheckboxesKeyed.AddOrSet(_columnName, checkedRows);

                args.ColumnName = _columnName;
                args.SingleCheckboxMode = true;
            }
            else
            {
                if (checkboxesKeyed.ContainsKey(stringKeys))
                    checkboxesKeyed[stringKeys] = (this, value);
                else
                    checkboxesKeyed.Add(stringKeys, (this, value));

                args.ColumnName = _columnName;
                args.Item = Item;
                args.StringKey = stringKeys;
                args.Value = _value ? CheckboxValue.Checked : CheckboxValue.Unchecked;
            }
            
            if (sendEvents)
                await GridComponent.OnRowCheckboxChanged(args);
            else
                await InvokeAsync(StateHasChanged);
        }

        private string GetStringKeys()
        {
            var keys = GridComponent.Grid.GetPrimaryKeyValues(Item);
            return string.Join('_', keys);
        }

    }
}
