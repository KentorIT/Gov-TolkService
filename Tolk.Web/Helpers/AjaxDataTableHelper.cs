using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Tolk.Web.Attributes;
using System.Linq.Dynamic.Core;

namespace Tolk.Web.Helpers
{
    public static class AjaxDataTableHelper
    {
        public static IEnumerable<ColumnDefinition> GetColumnDefinitions<TModel>()
        {
            var t = typeof(TModel);
            return t.GetProperties()
                 .Where(p => AttributeHelper.IsAttributeDefined<ColumnDefinitionsAttribute>(t, p.Name))
                 .OrderBy(p => ((ColumnDefinitionsAttribute)AttributeHelper.GetAttribute<ColumnDefinitionsAttribute>(t, p.Name)).Index)
                 .Select(p => ((ColumnDefinitionsAttribute)AttributeHelper.GetAttribute<ColumnDefinitionsAttribute>(t, p.Name)).ColumnDefinition);

        }

        public static IActionResult GetData<T>(IDataTablesRequest request, int totalCount, IQueryable<T> filteredData)
        {
            var sortColumns = request.Columns.Where(c => c.Sort != null).OrderBy(c => c.Sort.Order).Select(c => c);
            if (sortColumns.Any())
            {
                var sortColumn = sortColumns.First();
                string sort = $"{sortColumn.Name} {(sortColumn.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}";
                foreach (var col in sortColumns.Skip(1))
                {
                    sort += $", {col.Name} {(col.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}"; ;
                }
                filteredData = filteredData.OrderBy(sort);
            }

            var dataPage = filteredData.Skip(request.Start).Take(request.Length);
            var response = DataTablesResponse.Create(request, totalCount, filteredData.Count(), dataPage);
            return new DataTablesJsonResult(response, true);
        }

    }
}
