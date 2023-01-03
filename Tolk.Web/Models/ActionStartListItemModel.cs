using System;
using System.Collections.Generic;
using Tolk.Web.Attributes;

namespace Tolk.Web.Models
{
    public class ActionStartListItemModel : StartListItemModel
    {

        [ColumnDefinitions(Index = 6, Name = nameof(ActionColumn), ShowTitle = false, Sortable = false)]
        public string ActionColumn
            => $"<br /><span class=\"btn btn-primary pull-right\">Granska<span class=\"center-glyphicon glyphicon glyphicon-triangle-right\"></span>";
    }
}
