using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;

namespace DataTable.Components
{
    public partial class GenericTable<T>
    {
        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        [Parameter]
        public ICollection<T> Items { get; set; }

        [Parameter]
        public bool ShowAddButton { get; set; }

        [Parameter]
        public Dictionary<string, string> Columns { get; set; }

        [Parameter]
        public List<string> VisibleColumns { get; set; }

        [Parameter]
        public EventCallback<T> OnRowClicked { get; set; }

        [Parameter]
        public RenderFragment ButtonGroup { get; set; }

        /// <summary>
        /// This can be used to set the template for a row of the table. Leave this null to use the default template.
        /// Make sure to use the same order as the columns variable, otherwise the table won't make any sense
        /// </summary>
        [Parameter]
        public RenderFragment<T> RowTemplate { get; set; }

        [Parameter]
        public RenderFragment<T> LastColumnTemplate { get; set; }

        /// <summary>
        /// This parameter can be used to make rows selectable. When they are selectable a checkbox will be added to the beginning of the row.
        /// The selected items can be retrieved using <see cref="SelectedItems"/>.
        /// </summary>
        [Parameter]
        public bool SelectableRows { get; set; }

        /// <summary>
        /// This list contains the selected items. This only works when <see cref="SelectableRows"/> is true.
        /// </summary>
        public List<T> SelectedItems
        {
            get
            {
                return localItemsDictionary.Where(x => SelectedKeys.Contains(x.Key)).Select(x => x.Value).ToList();
            }
        }

        private List<int> SelectedKeys { get; set; }

        //private List<T> localItems;
        private Dictionary<int, T> localItemsDictionary;

        private readonly string ColumnClassBottom = " oi oi-caret-bottom";
        private readonly string ColumnClassTop = "oi oi-caret-top";
        private readonly string ColumnClassDefault = "oi oi-minus";
        private string ItemNameClasses;
        private string UnitClasses;
        private string QuantityClasses;
        private string LastSortColumn;
        private bool SortAsc = true;

        private string _searchText;

        protected override void OnInitialized()
        {
            InitializeTable();

            if (SelectedKeys == null)
            {
                SelectedKeys = new List<int>();
            }
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            ItemsHasChanged();
        }

        public void ItemsHasChanged()
        {
            InitializeTable();
            base.StateHasChanged();
        }

        private void InitializeTable()
        {
            ItemNameClasses = ColumnClassDefault;
            UnitClasses = ColumnClassDefault;
            QuantityClasses = ColumnClassDefault;

            int i = 0;
            //localItems = Items.ToList();
            localItemsDictionary = Items.ToDictionary(key => i++, value => value);
            LastSortColumn = Columns.Keys.ToList()[0];

            SearchTable(_searchText);
        }

        private async void RowClicked(KeyValuePair<int, T> row)
        {
            if (SelectableRows)
            {
                if (SelectedKeys.Contains(row.Key))
                {
                    SelectedKeys.Remove(row.Key);
                }
                else
                {
                    SelectedKeys.Add(row.Key);
                }
                StateHasChanged();
            }

            await OnRowClicked.InvokeAsync(row.Value);
        }

        private void SortList(string columnName)
        {
            if (columnName == LastSortColumn)
            {
                SortAsc = !SortAsc;
            }
            else
            {
                SortAsc = true;
            }
            LastSortColumn = columnName;
            //localItems = localItems.AsQueryable().OrderBy(columnName + " " + (SortAsc ? "ascending" : "descending")).ToList();
            localItemsDictionary = localItemsDictionary.AsQueryable().OrderBy("x => x.Value." + columnName + " " + (SortAsc ? "ascending" : "descending")).ToDictionary(x => x.Key, x => x.Value);
        }

        private void SearchTable(string searchText)
        {
            _searchText = searchText;
            //var tempItems = Items.ToList();
            int i = 0;
            var tempDictionary = Items.ToDictionary(key => i++, value => value);


            //construct search query
            string query = null;
            foreach (var column in Columns.Keys)
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    query = $"(x => x.Value.{column} != null && x.Value.{column}.ToString().Contains(\"{searchText}\", StringComparison.OrdinalIgnoreCase))";
                }
                else
                {
                    query += $" || (x => x.Value.{column} != null && x.Value.{column}.ToString().Contains(\"{searchText}\", StringComparison.OrdinalIgnoreCase))";
                }
            }

            try
            {
                //localItems = tempItems.AsQueryable().Where(query).ToList();
                localItemsDictionary = tempDictionary.AsQueryable().Where(query).ToDictionary(x => x.Key, x => x.Value);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            SortList(LastSortColumn);
            SortList(LastSortColumn);
        }

        private void VisiblityClicked(string columnName)
        {
            if (VisibleColumns.Contains(columnName))
            {
                VisibleColumns.Remove(columnName);
            }
            else
            {
                VisibleColumns.Add(columnName);
            }
            JsRuntime.InvokeVoidAsync("ToggleDropdown");
        }
    }
}
