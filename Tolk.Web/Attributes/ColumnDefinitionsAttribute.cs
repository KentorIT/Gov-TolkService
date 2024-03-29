﻿using System;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;

namespace Tolk.Web.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ColumnDefinitionsAttribute : Attribute
    {
        public bool IsIdColumn { get; set; } = false;
        public int Index { get; set; } = int.MaxValue;
        public string Name { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string ColumnName { get; set; } = string.Empty;
        public bool IsLeftCssClassName { get; set; } = false;
        public bool Sortable { get; set; } = true;
        public bool Visible { get; set; } = true;
        public bool ShowTitle { get; set; } = true;
        /// <summary>
        /// If false the <see cref="ColumnName"/> is used to sort in the database.
        /// Useful when for example a date column is formatted.
        /// </summary>
        public bool SortOnWebServer { get; set; } = true;
        public bool IsOverrideClickLinkUrlColumn { get; set; } = false;

        public ColumnDefinitionsAttribute() { }

        public ColumnDefinition ColumnDefinition => new ColumnDefinition
        {
            IsIdColumn = IsIdColumn,
            Name = Name,
            Data = !string.IsNullOrEmpty(Data) ? Data : Name.ToLowerFirstChar(),
            ColumnName = !string.IsNullOrEmpty(ColumnName) ? ColumnName : (!string.IsNullOrEmpty(Data) ? Data : Name.ToLowerFirstChar()),
            Title = ShowTitle ? !string.IsNullOrEmpty(Title) ? Title : Name : string.Empty,
            Sortable = Sortable,
            Visible = Visible,
            SortOnWebServer = SortOnWebServer,
            IsLeftCssClassName = IsLeftCssClassName,
            IsOverrideClickLinkUrlColumn = IsOverrideClickLinkUrlColumn
        };
    }
}
