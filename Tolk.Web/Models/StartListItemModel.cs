using System;
using System.Collections;
using System.Collections.Generic;
using Tolk.BusinessLogic.Enums;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Attributes;
using Tolk.Web.Helpers;

namespace Tolk.Web.Models
{
    public class StartListItemModel
    {
        [ColumnDefinitions(IsIdColumn = true, Index = 0, Name = nameof(EntityId), Visible = false, Sortable = false)]
        public int EntityId { get; set; }

        [ColumnDefinitions(Index = 1, Name = nameof(OrderDescriptor), SortOnWebServer = true, Title = "Status")]
        public string OrderDescriptor
        {
            get
            {
                var result = $"<br /><span class=\"normal-weight\">{EnumHelper.GetDescription(Status)}</span><br /><br /><span class=\"startlist-subrow\">ID: {OrderNumber}</span>";

                if (!string.IsNullOrEmpty(ViewedByUser))
                {
                    result += $"<span class=\"glyphicon glyphicon-user color-red\" title=\"{ViewedByUser}\"></span>";
                }
                if (!string.IsNullOrEmpty(OrderGroupNumber))
                {
                    result += $"<br /><span class=\"small-red-border-left startlist-subrow\">{OrderGroupNumber}</span>";
                }
                return result;
            }
        }

        [ColumnDefinitions(Index = 2, Name = nameof(Interpreter), SortOnWebServer = true, Title = "Tolkens kompetens")]
        public string Interpreter
        {
            get
            {
                string result;
                if (HasExtraInterpreter)
                {
                    result = "<span class=\"small-red-border-left startlist-subrow\">Extra tolk</span><br />";
                }
                else
                {
                    result = "<br />";
                }
                result += $"{EnumHelper.GetDescription(CompetenceLevel)}<br />";
                if (HasExtraInterpreter)
                {
                    result += EnumHelper.GetDescription(ExtraCompetenceLevel);
                }
                result += $"<br /><span class=\"startlist-subrow\">{InfoDateDescription}<br />{InfoDate?.ToString("yyyy-MM-dd HH:mm") ?? "-"}</span>";
                return result;
            }
        }

        [ColumnDefinitions(Index = 3, Name = nameof(Language), ColumnName = nameof(LanguageName), SortOnWebServer = true, Title = "Språk")]
        public string Language
        {
            get
            {
                string result = $"<br />{LanguageName}<br /><br />";
                if (RequestAcceptAt != null)
                {
                    result += $"<span class=\"startlist-subrow-red\">Bekräfta innan:<br />{RequestAcceptAt?.ToString("yyyy-MM-dd HH:mm")}</span>";
                }
                else if (RequestAcceptedAt != null)
                {
                    result += $"<span class=\"startlist-subrow\">{RequestAcceptedAtDescription}<br />{RequestAcceptedAt?.ToString("yyyy-MM-dd HH:mm")}</span>";
                }
                return result;
            }
        }

        [ColumnDefinitions(Index = 4, Name = nameof(Customer), ColumnName = nameof(CustomerName), SortOnWebServer = true, Title = "Myndighet")]
        public string Customer => $"<br />{CustomerName}<br /><br />";

        [ColumnDefinitions(Index = 5, Name = nameof(OrderDate),ColumnName = nameof(OrderDateTimeRange), SortOnWebServer = true, Title = "Datum för uppdrag")]
        public string OrderDate
        {
            get
            {
                string result = string.Empty;
                if (!IsSingleOccasion)
                {
                    result = "<span class=\"small-red-border-left startlist-subrow\">OBS! Första av flera tillfällen</span>";
                }
                result += $"<br />{OrderDateTimeRange.AsSwedishString}<br /><br />";
                if (LatestDate != null)
                {
                    result += $"<span class=\"startlist-subrow-red\"> Tillsätt tolk innan:<br />{LatestDate?.ToString("yyyy-MM-dd HH:mm")}</span>";
                }

                return result;
            }
        }

        public string ViewedByUser { get; set; } = string.Empty;

        public TimeRange OrderDateTimeRange { get; set; }

        public StartListItemStatus Status { get; set; }

        public string OrderNumber { get; set; }

        public string LanguageName { get; set; }

        public string CustomerName { get; set; }

        public string OrderGroupNumber { get; set; }

        public bool IsSingleOccasion { get; set; } = true;

        public bool HasExtraInterpreter { get; set; } = false;

        public CompetenceAndSpecialistLevel? CompetenceLevel { get; set; }

        public CompetenceAndSpecialistLevel? ExtraCompetenceLevel { get; set; }

        public DateTime? InfoDate { get; set; }

        public DateTime? LatestDate { get; set; }

        public string InfoDateDescription { get; set; } = "Inkommen: ";

        public DateTime? RequestAcceptAt { get; set; }

        public DateTime? RequestAcceptedAt { get; set; }

        public string RequestAcceptedAtDescription { get; set; } = "Bekräftad: ";

        [ColumnDefinitions(IsOverrideClickLinkUrlColumn = true, Name = nameof(LinkOverride), Visible = false)]
        public string LinkOverride { get; set; }

        [ColumnDefinitions(IsLeftCssClassName = true, Name = nameof(ColorClassName), Visible = false)]
        public string ColorClassName => CssClassHelper.GetColorClassNameForStartListItem(Status);

    }
}

