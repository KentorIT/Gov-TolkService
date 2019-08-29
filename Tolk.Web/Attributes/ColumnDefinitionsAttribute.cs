using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tolk.Web.Helpers;
using System.Runtime.CompilerServices;
using Tolk.BusinessLogic.Utilities;

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
        public bool IsLeftCssClassName { get; set; } = false;
        public bool Sortable { get; set; } = true;
        public bool Visible { get; set; } = true;

        public ColumnDefinitionsAttribute() { }

        public ColumnDefinition ColumnDefinition => new ColumnDefinition
        {
            IsIdColumn = IsIdColumn,
            Name = Name,
            Data = !string.IsNullOrEmpty(Data) ? Data : Name.ToLowerFirstChar(),
            Title = !string.IsNullOrEmpty(Title) ? Title : Name,
            Sortable = Sortable,
            Visible = Visible,
            IsLeftCssClassName = IsLeftCssClassName
        };
    }
}
