using DataTables.AspNet.AspNetCore;
using DataTables.AspNet.Core;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Tolk.Web.Attributes;

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

        public static IActionResult GetData<T, TModel>(IDataTablesRequest request, int totalCount, IQueryable<T> filteredData, Func<IQueryable<T>, IQueryable<TModel>> getModel, Func<IQueryable<T>, IOrderedQueryable<T>> defaultSort = null)
        {
            if (request == null || getModel == null)
            {
                throw new ArgumentNullException(request == null ? nameof(request) : nameof(getModel));
            }
            var colDefs = GetColumnDefinitions<TModel>();
            var sortColumns = request.Columns.Where(c => c.Sort != null).OrderBy(c => c.Sort.Order).Select(c => c);
            IQueryable<TModel> list = null;
            string sort = string.Empty;
            bool sortOnWebServer = false;
            if (sortColumns.Any())
            {
                //If one has sort on server, all needs to be sorted on server
                // It might be possible to sort any columns on server up to the point where a column is sorted on web server, and henceforth all sorting is done on the webserver.
                sortOnWebServer = colDefs.Any(c => c.SortOnWebServer && sortColumns.Any(sc => sc.Name == c.Name));
                var sortColumn = sortColumns.First();
                sort = colDefs.GetSortDefinition(sortColumn, sortOnWebServer);
                foreach (var col in sortColumns.Skip(1))
                {
                    sort += $", {colDefs.GetSortDefinition(col, sortOnWebServer)}";
                }
            }
            else if (defaultSort != null)
            {
                filteredData = defaultSort(filteredData);
            }
            if (!sortOnWebServer)
            {
                if (sortColumns.Any())
                {
                    filteredData = filteredData.OrderBy(sort);

                }
                if (request.Length > -1)
                {
                    filteredData = filteredData.Skip(request.Start).Take(request.Length);
                }
                list = getModel(filteredData);
            }
            else
            {
                //Allways sorted if sortOnWebServer == true
                list = getModel(filteredData).ToList().AsQueryable().OrderBy(sort);
                if (request.Length > -1)
                {
                    list = list.Skip(request.Start).Take(request.Length);
                }
            }

            var response = DataTablesResponse.Create(request, totalCount, filteredData.Count(), list);
            return new DataTablesJsonResult(response, true);
        }

        private static string GetSortDefinition(this IEnumerable<ColumnDefinition> colDefs, IColumn sortColumn, bool sortOnWebServer)
        {
            return sortOnWebServer ?
                $"{sortColumn.Name} {(sortColumn.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}" :
                $"{colDefs.Single(d => d.Name == sortColumn.Name).ColumnName} {(sortColumn.Sort.Direction == SortDirection.Ascending ? "ASC" : "DESC")}";
        }
    }
}
