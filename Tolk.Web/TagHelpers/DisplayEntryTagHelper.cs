using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Tolk.BusinessLogic.Utilities;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.TagHelpers
{
    public class DisplayEntryTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly HtmlEncoder _htmlEncoder;

        public DisplayEntryTagHelper(IHtmlGenerator htmlGenerator, HtmlEncoder htmlEncoder)
        {
            this._htmlGenerator = htmlGenerator;
            this._htmlEncoder = htmlEncoder;
        }

        private const string ForAttributeName = "asp-for";
        private const string LabelOverrideattributeName = "label-override";
        private const string ValuePrefixAttributeName = "asp-value-prefix";
        private const string EmptyAttributeName = "asp-empty";
        private const string TextAppendAttributeName = "text-append";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(LabelOverrideattributeName)]
        public string LabelOverride { get; set; }

        [HtmlAttributeName(ValuePrefixAttributeName)]
        public string ValuePrefix { get; set; } = "";

        [HtmlAttributeName(EmptyAttributeName)]
        public string Empty { get; set; } = "-";

        [HtmlAttributeName(TextAppendAttributeName)]
        public string TextAppend { get; set; } = string.Empty;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("class", "form-group");
            using (var writer = new StringWriter())
            {            // Check if label will be displayed
                var type = For.ModelExplorer.Metadata.ContainerType;
                var isDisplayed = true;
                var isSubItem = false;
                var property = type?.GetProperty(For.ModelExplorer.Metadata.PropertyName);
                if (property != null)
                {
                    isDisplayed = !Attribute.IsDefined(property, typeof(NoDisplayNameAttribute));
                    isSubItem = Attribute.IsDefined(property, typeof(SubItemAttribute));
                }
                if (isDisplayed)
                {
                    WriteLabel(writer, isSubItem);
                }
                WriteDetails(writer);
                output.Content.AppendHtml(writer.ToString());
            }
        }

        private void WriteLabel(TextWriter writer, bool isSubItem = false)
        {
            var tagBuilder = _htmlGenerator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                labelText: LabelOverride,
                htmlAttributes: new { @class = isSubItem ? "subitem control-label" : "control-label" });

            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private enum OutputType { Text, DateTimeOffset, Bool, Enum, Currency, MultilineText, TimeSpan, TimeRange, RadioButtonGroup, CheckboxGroup }

        private void WriteDetails(TextWriter writer)
        {
            string text = string.Empty;
            string className = "detail-text";
            switch (GetOutputType())
            {
                case OutputType.Enum:
                    text = ValuePrefix + EnumHelper.GetDescription(For.ModelExplorer.ModelType, (Enum)For.ModelExplorer.Model);
                    break;
                case OutputType.Bool:
                    text = ValuePrefix + (((bool)For.ModelExplorer.Model) ? "Ja" : "Nej");
                    break;
                case OutputType.DateTimeOffset:
                    text = ValuePrefix + ((DateTimeOffset?)For.ModelExplorer.Model)?.ToSwedishString("yyyy-MM-dd HH:mm");
                    break;
                case OutputType.TimeRange:
                    var timeRange = (TimeRange)For.ModelExplorer.Model;
                    text = ValuePrefix + timeRange.StartDate.ToSwedishString("yyyy-MM-dd") + " "
                        + timeRange.StartTime.ToSwedishString("hh\\:mm") + "-"
                        + timeRange.EndTime.ToSwedishString("hh\\:mm");
                    break;
                case OutputType.Currency:
                    text = ValuePrefix + ((decimal?)For.ModelExplorer.Model)?.ToSwedishString("#,0.00 SEK");
                    break;
                case OutputType.MultilineText:
                    className += " line-break";
                    text = ValuePrefix + _htmlGenerator.Encode(For.ModelExplorer.Model);
                    break;
                case OutputType.TimeSpan:
                    var time = ((TimeSpan?)For.ModelExplorer.Model) ?? TimeSpan.Zero;
                    text = ValuePrefix + (time.Hours > 0 ? $"{time.Hours} timmar {time.Minutes} minuter" : $"{time.Minutes} minuter");
                    break;
                case OutputType.RadioButtonGroup:
                    text = ValuePrefix + ((RadioButtonGroup)For.ModelExplorer.Model).SelectedItem.Text;
                    break;
                case OutputType.CheckboxGroup:
                    className += " line-break";
                    text = ValuePrefix + ((CheckboxGroup)For.ModelExplorer.Model).SelectedItems
                        .Select(item => item.Text)
                        .Aggregate((current, next) => current + "\n" + next);
                    break;
                default:
                    text = ValuePrefix + _htmlGenerator.Encode(For.ModelExplorer.Model);
                    break;
            }
            if (string.IsNullOrEmpty(text))
            {
                className += " no-value-info";
                text = Empty;
            }

            text += TextAppend;

            writer.WriteLine($"<div class=\"{className}\">{text}</div>");
        }

        private OutputType GetOutputType()
        {
            if (For.ModelExplorer.Model is Enum)
            {
                return OutputType.Enum;
            }
            if (For.ModelExplorer.ModelType == typeof(DateTimeOffset)
                || For.ModelExplorer.ModelType == typeof(DateTimeOffset?))
            {
                return OutputType.DateTimeOffset;
            }
            if (For.ModelExplorer.ModelType == typeof(bool))
            {
                return OutputType.Bool;
            }
            if (For.ModelExplorer.Metadata.DataTypeName == "Currency")
            {
                return OutputType.Currency;
            }
            if (For.ModelExplorer.Metadata.DataTypeName == "MultilineText")
            {
                return OutputType.MultilineText;
            }
            if (For.ModelExplorer.ModelType == typeof(TimeSpan)
                || For.ModelExplorer.ModelType == typeof(TimeSpan?))
            {
                return OutputType.TimeSpan;
            }
            if (For.ModelExplorer.ModelType == typeof(TimeRange))
            {
                return OutputType.TimeRange;
            }
            if (For.ModelExplorer.ModelType == typeof(RadioButtonGroup))
            {
                return OutputType.RadioButtonGroup;
            }
            if (For.ModelExplorer.ModelType == typeof(CheckboxGroup))
            {
                return OutputType.CheckboxGroup;
            }
            return OutputType.Text;
        }
    }
}
