using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Tolk.Web.Helpers;
using Tolk.Web.Models;

namespace Tolk.Web.TagHelpers
{
    public class FormEntryTagHelper : TagHelper
    {
        private readonly IHtmlGenerator _htmlGenerator;
        private readonly HtmlEncoder _htmlEncoder;

        public FormEntryTagHelper(IHtmlGenerator htmlGenerator, HtmlEncoder htmlEncoder)
        {
            _htmlGenerator = htmlGenerator;
            _htmlEncoder = htmlEncoder;
        }

        private const string ForAttributeName = "asp-for";
        private const string ItemsAttributeName = "asp-items";
        private const string InputTypeName = "type";
        private const string InputTypeSelect = "select";
        private const string InputTypeDateTimeOffset = "datetime";
        private const string InputTypeText = "text";
        private const string InputTypePassword = "password";
        private const string InputTypeCheckbox = "checkbox";
        private const string InputTypeTextArea = "textarea";
        private const string InputTypeTime = "time";
        private const string InputTypeDateRange = "date-range";
        private const string InputTypeTimeRange = "time-range";
        private const string InputTypeSplitTimeRange = "splittime-range";
        private const string InputTypeHiddenTimeRangeHidden = "time-range-hidden";
        private const string InputTypeRadioButtonGroup = "radio-group";
        private const string InputTypeCheckboxGroup = "checkbox-group";

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(ItemsAttributeName)]
        public IEnumerable<SelectListItem> Items { get; set; }

        [HtmlAttributeName("type")]
        public string InputType { get; set; }

        [HtmlAttributeName("layout-option")]
        public string LayoutOption { get; set; }

        [HtmlAttributeName("label-override")]
        public string LabelOverride { get; set; }

        [HtmlAttributeName("help-link")]
        public string HelpLink { get; set; }

        // Applicable for manual html generation
        [HtmlAttributeName("id-override")]
        public string IdOverride { get; set; }

        [HtmlAttributeName("checked-index")]
        public string CheckedIndex { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override void Init(TagHelperContext context)
        {
            base.Init(context);

            InitInputType();

            switch (InputType)
            {
                case InputTypeSelect:
                case InputTypeRadioButtonGroup:
                case InputTypeCheckboxGroup:
                    if (Items == null)
                    {
                        throw new ArgumentNullException("Items", "Items must be set if type is select, radio-group or checkbox-group.");
                    }
                    break;
                case null:
                case InputTypePassword:
                case InputTypeText:
                case InputTypeCheckbox:
                case InputTypeDateTimeOffset:
                case InputTypeTime:
                case InputTypeDateRange:
                case InputTypeHiddenTimeRangeHidden:
                case InputTypeTextArea:
                case InputTypeTimeRange:
                case InputTypeSplitTimeRange:
                    if (Items != null)
                    {
                        throw new ArgumentException("Items are only relevant if type is select, radio-group or checkbox-group.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown input type {InputType} for expression {For.Name}. Omit type to get a default input.");
            }
        }

        private void InitInputType()
        {
            if (InputType == null)
            {
                if (For.ModelExplorer.ModelType == typeof(DateTimeOffset)
                    || For.ModelExplorer.ModelType == typeof(DateTimeOffset?))
                {
                    InputType = InputTypeDateTimeOffset;
                    return;
                }
                if (For.ModelExplorer.Metadata.DataTypeName == "Password")
                {
                    InputType = InputTypePassword;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(bool))
                {
                    InputType = InputTypeCheckbox;
                    return;
                }
                if (For.ModelExplorer.Metadata.DataTypeName == "MultilineText")
                {
                    InputType = InputTypeTextArea;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(TimeSpan)
                    || For.ModelExplorer.ModelType == typeof(TimeSpan?))
                {
                    InputType = InputTypeTime;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(TimeRange))
                {
                    InputType = InputTypeTimeRange;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(SplitTimeRange))
                {
                    InputType = InputTypeSplitTimeRange;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(DateRange) || For.ModelExplorer.ModelType == typeof(RequiredDateRange))
                {
                    InputType = InputTypeDateRange;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(RadioButtonGroup))
                {
                    InputType = InputTypeRadioButtonGroup;
                    return;
                }
                if (For.ModelExplorer.ModelType == typeof(CheckboxGroup))
                {
                    InputType = InputTypeCheckboxGroup;
                    return;
                }
            }
            else if (InputType == "hidden" && For.ModelExplorer.ModelType == typeof(TimeRange))
            {
                InputType = InputTypeHiddenTimeRangeHidden;
                return;
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            string className = "form-group";

            using (var writer = new StringWriter())
            {
                switch (InputType)
                {
                    case InputTypeSelect:
                        WriteLabel(writer);
                        WriteSelect(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeRadioButtonGroup:
                        WriteLabel(writer);
                        WriteRadioGroup(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeCheckboxGroup:
                        WriteLabel(writer);
                        WriteCheckboxGroup(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeDateTimeOffset:
                        WriteDateTimeOffsetBlock(writer);
                        break;
                    case InputTypePassword:
                        WriteLabel(writer);
                        WritePassword(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeCheckbox:
                        className = "checkbox";
                        WriteCheckBoxInLabel(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeTextArea:
                        WriteLabel(writer);
                        WriteTextArea(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeTime:
                        WriteLabel(writer);
                        WriteTimeBox(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeDateRange:
                        WriteDateRangeBlock(writer);
                        break;
                    case InputTypeTimeRange:
                        WriteTimeRangeBlock(writer);
                        break;
                    case InputTypeSplitTimeRange:
                        WriteSplitTimeRangeBlock(writer);
                        break;
                    case InputTypeHiddenTimeRangeHidden:
                        WriteTimeRangeBlock(writer, true);
                        break;
                    default:
                        WriteLabel(writer);
                        WriteInput(writer);
                        WriteValidation(writer);
                        break;
                }

                output.AddClass(className, _htmlEncoder);
                output.Content.AppendHtml(writer.ToString());
            }
        }

        private const string RequiredStarSpan = "<span class=\"required-star\">*</span>";
        private const string InformationSpan = " <span class=\"form-entry-information glyphicon glyphicon-info-sign\" title=\"{0}\"></span>";
        private const string HelpAnchor = " <a href=\"{0}\" target=\"_blank\"><span class=\"form-entry-help glyphicon glyphicon-question-sign\"></span></a>";

        private void WritePrefix(TextWriter writer, PrefixAttribute.Position condition)
        {
            var Prefix = (PrefixAttribute)AttributeHelper.GetAttribute<PrefixAttribute>(
                For.ModelExplorer.Metadata.ContainerType,
                For.ModelExplorer.Metadata.PropertyName);

            if (Prefix != null && Prefix.PrefixPosition == condition)
            {
                writer.WriteLine(Prefix.Text);
            }
        }

        private void WriteLabel(TextWriter writer)
        {
            var IsDisplayed = !AttributeHelper.IsAttributeDefined<NoDisplayNameAttribute>(
                For.ModelExplorer.Metadata.ContainerType,
                For.ModelExplorer.Metadata.PropertyName);

            if (IsDisplayed)
            {
                bool IsSubItem = AttributeHelper.IsAttributeDefined<SubItem>(
                For.ModelExplorer.Metadata.ContainerType,
                For.ModelExplorer.Metadata.PropertyName);

                TagBuilder tagBuilder = GenerateLabel(IsSubItem);

                if (For.ModelExplorer.Metadata.IsRequired)
                {
                    tagBuilder.InnerHtml.AppendHtml(RequiredStarSpan);
                }
                WritePrefix(writer, PrefixAttribute.Position.Label);
                tagBuilder.WriteTo(writer, _htmlEncoder);

                WriteInfoIfDescription(writer);
                WriteHelpIfHelpLink(writer);
            }
        }

        private void WriteInfoIfDescription(TextWriter writer)
        {
            if (!string.IsNullOrEmpty(For.ModelExplorer.Metadata.Description))
            {
                writer.WriteLine(string.Format(InformationSpan, For.ModelExplorer.Metadata.Description));
            }
        }

        private void WriteHelpIfHelpLink(TextWriter writer)
        {
            if (!string.IsNullOrEmpty(HelpLink))
            {
                writer.WriteLine(string.Format(HelpAnchor, HelpLink));
            }
        }

        private TagBuilder GenerateLabel(bool isSubItem = false)
        {
            return _htmlGenerator.GenerateLabel(
               ViewContext,
               For.ModelExplorer,
               For.Name,
               labelText: LabelOverride,
               htmlAttributes: new { @class = isSubItem ? "subitem control-label" : "control-label" });
        }

        private void WriteInput(TextWriter writer)
        {
            bool IsNoAutoComplete = AttributeHelper.IsAttributeDefined<NoAutoComplete>(
            For.ModelExplorer.Metadata.ContainerType,
            For.ModelExplorer.Metadata.PropertyName);

            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.Model,
                format: null,
                htmlAttributes: new { @class = IsNoAutoComplete ? "form-control no-auto-complete" : "form-control" });

            if (!string.IsNullOrEmpty(For.Metadata.Description))
            {
                tagBuilder.Attributes.Add("placeholder", For.Metadata.Description);
            }
            //The regular expressions are not added as client side valdations for some reason.
            // Remove this code if this is fixed in future versions of .Net Core, or by some better setup of things that I have not found....
            if (For.Metadata.ValidatorMetadata.SingleOrDefault(m => m.GetType() == typeof(RegularExpressionAttribute)) is RegularExpressionAttribute regex &&
                !tagBuilder.Attributes.Any(a => a.Key == "data-val-regex"))
            {
                tagBuilder.Attributes.Add("data-val-regex", regex.ErrorMessage);
                tagBuilder.Attributes.Add("data-val-regex-pattern", regex.Pattern);
            }
            // This is how we override unobtrusive validation error messages, because globaLIES.js is not doing what we expect it to.
            if (tagBuilder.Attributes.Any(a => a.Key == "data-val-number"))
            {
                tagBuilder.Attributes["data-val-number"] = $"Fältet {For.Metadata.DisplayName} måste vara en siffra.";
            }
            WritePrefix(writer, PrefixAttribute.Position.Value);
            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteTextArea(TextWriter writer)
        {

            var tagBuilder = _htmlGenerator.GenerateTextArea(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                rows: 5,
                columns: 80,
                htmlAttributes: new { @class = "form-control" });
            if (!string.IsNullOrEmpty(For.Metadata.Description))
            {
                tagBuilder.Attributes.Add("placeholder", For.Metadata.Description);
            }
            WritePrefix(writer, PrefixAttribute.Position.Value);
            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteCheckBoxInLabel(TextWriter writer)
        {
            WriteCheckBoxInLabel(writer, For.ModelExplorer, For.Name);
        }

        private void WriteCheckBoxInLabel(TextWriter writer, ModelExplorer modelExplorer, string name)
        {
            var labelBuilder = GenerateLabel();

            var checkboxBuilder = _htmlGenerator.GenerateCheckBox(
                ViewContext,
                modelExplorer,
                name,
                isChecked: Equals(For.Model, true),
                htmlAttributes: null);

            var htmlBuilder = new HtmlContentBuilder();
            htmlBuilder.AppendHtml(labelBuilder.RenderStartTag());
            htmlBuilder.AppendHtml(checkboxBuilder.RenderStartTag());
            htmlBuilder.AppendHtml(labelBuilder.InnerHtml);
            if (!string.IsNullOrEmpty(For.Metadata.Description))
            {
                htmlBuilder.AppendHtml(string.Format(InformationSpan, For.Metadata.Description));
            }
            htmlBuilder.AppendHtml(labelBuilder.RenderEndTag());

            WritePrefix(writer, PrefixAttribute.Position.Value);
            htmlBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WritePassword(TextWriter writer)
        {
            bool IsNoAutoComplete = AttributeHelper.IsAttributeDefined<NoAutoComplete>(
            For.ModelExplorer.Metadata.ContainerType,
            For.ModelExplorer.Metadata.PropertyName);


            var tagBuilder = _htmlGenerator.GeneratePassword(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.Model,
                htmlAttributes: new { @class = IsNoAutoComplete ? "form-control no-auto-complete" : "form-control" });

            if (IsNoAutoComplete)
            {
                tagBuilder.Attributes.Add("autocomplete", "new-password");
            }
            WritePrefix(writer, PrefixAttribute.Position.Value);
            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteDateRangeBlock(TextWriter writer)
        {
            WriteLabelWithoutFor(writer);
            writer.WriteLine("<div class=\"form-inline\">");

            var fromModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Start");
            var fromFieldName = $"{For.Name}.Start";
            var toModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "End");
            var toFieldName = $"{For.Name}.End";
            object fromValue = fromModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date")?.Model;
            object toValue = toModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date")?.Model;

            WritePrefix(writer, PrefixAttribute.Position.Value);

            WriteDatePickerInput(fromModelExplorer, fromFieldName, fromValue, writer);
            WriteRightArrowSpan(writer);
            WriteDatePickerInput(toModelExplorer, toFieldName, toValue, writer);
            writer.WriteLine("<div class=\"col-sm-6 no-padding\">");
            WriteValidation(writer, fromModelExplorer, fromFieldName);
            writer.WriteLine("</div>");
            writer.WriteLine("<div class=\"col-sm-6 no-padding\">");
            WriteValidation(writer, toModelExplorer, toFieldName);
            writer.WriteLine("</div>");
            writer.WriteLine("</div>"); // form-inline.
        }

        private static void WriteRightArrowSpan(TextWriter writer)
        {
            // Using &nbsp; is an ugly hack. Should be fixed when layout is finalized.
            writer.WriteLine("&nbsp;&nbsp;<span class=\"glyphicon glyphicon-arrow-right\"></span>&nbsp;&nbsp;");
        }

        private void WriteTimeBox(TextWriter writer)
        {
            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.Model,
                format: "{0:hh\\:mm}",
                htmlAttributes: new
                {
                    @class = "form-control",
                    placeholder = "HH:MM",
                    data_val_regex_pattern = "^(([0-1]?[0-9])|(2[0-3])):?[0-5][0-9]$",
                    data_val_regex = "Ange tid som HH:MM eller HHMM",
                    data_val_required = "Tid måste anges.",
                    data_val = true
                });
            RemoveRequiredIfNullable(tagBuilder);
            WritePrefix(writer, PrefixAttribute.Position.Value);
            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteDateTimeOffsetBlock(TextWriter writer)
        {
            // First write a label
            WriteLabelWithoutFor(writer);

            // Then open the inline form
            writer.WriteLine("<div class=\"form-inline\">");
            var dateModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date");
            var dateFieldName = $"{For.Name}.Date";

            var timeHourModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Hour");
            var timeMinutesModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Minute");
            var timeHourFieldName = $"{For.Name}.Hour";
            var timeMinuteFieldName = $"{For.Name}.Minute";

            var timeModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "TimeOfDay");
            var timeFieldName = $"{For.Name}.TimeOfDay";
            object dateValue = null;
            object timeHourValue = null;
            object timeMinuteValue = null;

            if (!Equals(For.Model, default(DateTimeOffset)))
            {
                dateValue = dateModelExplorer.Model;
                timeHourValue = timeHourModelExplorer.Model;
                timeMinuteValue = timeMinutesModelExplorer.Model;
            }

            WritePrefix(writer, PrefixAttribute.Position.Value);
            writer.WriteLine("<div class=\"col-sm-6 no-padding\">");
            WriteDatePickerInput(dateModelExplorer, dateFieldName, dateValue, writer);
            writer.WriteLine("</div>");
            writer.WriteLine("<div class=\"col-sm-6\">");
            WriteSplitTimePickerInput(timeHourModelExplorer, timeHourFieldName, timeHourValue, writer, true);
            WriteSplitTimePickerInput(timeMinutesModelExplorer, timeMinuteFieldName, timeMinuteValue, writer, false);
            writer.WriteLine("</div>");

            writer.WriteLine("</div>"); // form-inline

            WriteValidation(writer, dateModelExplorer, dateFieldName);
            writer.WriteLine();
            WriteValidation(writer, timeHourModelExplorer, timeHourFieldName);
            WriteValidation(writer, timeMinutesModelExplorer, timeMinuteFieldName);
        }

        private void WriteTimePickerInput(ModelExplorer timeModelExplorer, string timeFieldName, object timeValue, TextWriter writer, IDictionary<string, string> extraAttributes = null)
        {
            writer.WriteLine("<div class=\"input-group time\">");

            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                timeModelExplorer,
                timeFieldName,
                value: timeValue,
                format: "{0:hh\\:mm}",
                htmlAttributes: new
                {
                    @class = "form-control time-range-part",
                    placeholder = "HH:MM",
                    data_val_regex_pattern = "^(([0-1]?[0-9])|(2[0-3])):?[0-5][0-9]$",
                    data_val_regex = "Ange tid som HH:MM eller HHMM",
                    data_val_required = "Tid måste anges.",
                });
            if (extraAttributes != null)
            {
                foreach (var pair in extraAttributes)
                {
                    tagBuilder.Attributes.Add(pair.Key, pair.Value);
                }
            }

            RemoveRequiredIfNullable(tagBuilder);
            tagBuilder.WriteTo(writer, _htmlEncoder);
            writer.WriteLine("</div>");
            //writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-time\"></span></div></div>"); //input-group time
        }

        private void WriteSplitTimePickerInput(ModelExplorer timeModelExplorer, string timeFieldName, object timeValue, TextWriter writer, bool hour)
        {
            writer.WriteLine("<div class=\"input-group time timesplit\">");
            WriteSelect(GetSplitTImeValues(hour), writer, timeFieldName, timeModelExplorer, hour ? "tim" : "min");
            writer.WriteLine("</div>");
            //writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-time\"></span></div></div>"); //input-group time
            WriteValidation(writer, timeModelExplorer, timeFieldName);
        }

        private IEnumerable<SelectListItem> GetSplitTImeValues(bool hour)
        {
            List<SelectListItem> list = new List<SelectListItem>();

            int start = hour ? 8 : 0;
            int max = hour ? 23 : 55;
            int jump = hour ? 1 : 5;

            for (int i = start; i <= max; i += jump)
            {
                list.Add(new SelectListItem() { Text = i < 10 ? 0 + i.ToString() : i.ToString(), Value = i.ToString() });
            }
            if (hour)
            {
                for (int i = 0; i <= 7; i += jump)
                {
                    list.Add(new SelectListItem() { Text = i < 10 ? 0 + i.ToString() : i.ToString(), Value = i.ToString() });
                }
            }
            return list;
        }

        private void WriteLabelWithoutFor(TextWriter writer)
        {
            writer.Write($"<label>{_htmlGenerator.Encode(For.ModelExplorer.Metadata.DisplayName)}");
            if (For.ModelExplorer.Metadata.IsRequired)
            {
                writer.Write(RequiredStarSpan);
            }
            writer.WriteLine("</label>");
            WriteInfoIfDescription(writer);
            WriteHelpIfHelpLink(writer);
        }

        private void WriteLabelWithoutFor(ModelExplorer modelExplorer, TextWriter writer)
        {
            writer.Write($"<label>{_htmlGenerator.Encode(modelExplorer.Metadata.DisplayName)}");
            if (modelExplorer.Metadata.IsRequired)
            {
                writer.Write(RequiredStarSpan);
            }
            writer.WriteLine("</label>");
        }

        private void WriteDatePickerInput(
            ModelExplorer dateModelExplorer,
            string dateFieldName,
            object dateValue,
            TextWriter writer,
            string extraGroupDivClass = "",
            IDictionary<string, string> extraAttributes = null)
        {
            writer.WriteLine("<div class=\"input-group date " + extraGroupDivClass + "\">");

            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                dateModelExplorer,
                dateFieldName,
                value: dateValue,
                format: "{0:yyyy-MM-dd}",
                htmlAttributes: new
                {
                    @class = "form-control datepicker time-range-part",
                    placeholder = "ÅÅÅÅ-MM-DD",
                    type = "text",
                    data_val_required = "Datum måste anges.",
                });
            if (extraAttributes != null)
            {
                foreach (var pair in extraAttributes)
                {
                    tagBuilder.Attributes.Add(pair.Key, pair.Value);
                }
            }
            RemoveRequiredIfNullable(tagBuilder);

            WritePrefix(writer, PrefixAttribute.Position.Value);
            tagBuilder.WriteTo(writer, _htmlEncoder);

            writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-calendar\"></span></div></div>"); //input-group date
        }

        private void WriteTimeRangeBlock(TextWriter writer, bool isHidden = false)
        {
            var dateModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(TimeRange.StartDate));
            var dateFieldName = $"{For.Name}.{nameof(TimeRange.StartDate)}";
            var startTimeModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(TimeRange.StartTime));
            var startTimeFieldName = $"{For.Name}.{nameof(TimeRange.StartTime)}";
            var endTimeModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(TimeRange.EndTime));
            var endTimeFieldName = $"{For.Name}.{nameof(TimeRange.EndTime)}";

            object dateValue = null;
            object startTimeValue = null;
            object endTimeValue = null;

            if (For.Model != null)
            {
                dateValue = dateModelExplorer.Model;
                startTimeValue = startTimeModelExplorer.Model;
                endTimeValue = endTimeModelExplorer.Model;
            }

            if (isHidden)
            {
                //Make three hidden fields
                var tagBuilder = _htmlGenerator.GenerateHidden(ViewContext, dateModelExplorer, dateFieldName, ((DateTime)dateValue).ToString("yyyy-MM-dd"), false, null);
                tagBuilder.WriteTo(writer, _htmlEncoder);
                var tagBuilder2 = _htmlGenerator.GenerateHidden(ViewContext, startTimeModelExplorer, startTimeFieldName, ((TimeSpan)startTimeValue).ToString(@"hh\:mm"), false, null);
                tagBuilder2.WriteTo(writer, _htmlEncoder);
                var tagBuilder3 = _htmlGenerator.GenerateHidden(ViewContext, endTimeModelExplorer, endTimeFieldName, ((TimeSpan)endTimeValue).ToString(@"hh\:mm"), false, null);
                tagBuilder3.WriteTo(writer, _htmlEncoder);

            }
            else
            {
                // Check if label will be displayed
                var type = For.ModelExplorer.Metadata.ContainerType;
                var property = type.GetProperty(For.ModelExplorer.Metadata.PropertyName);
                var stayWithinAttribute = property.CustomAttributes.SingleOrDefault(c => c.AttributeType == typeof(StayWithinOriginalRangeAttribute));

                IDictionary<string, string> extraAttributes = null;

                WriteLabelWithoutFor(writer);
                var inlineClass = "form-inline";

                if (stayWithinAttribute != null)
                {
                    inlineClass += " staywithin-fields";
                    extraAttributes = new Dictionary<string, string>
                    {
                        { "data-staywithin-validator", $"{For.Name}_validator" },
                    };
                }
                writer.WriteLine($"<div class=\"{inlineClass}\">");
                WriteDatePickerInput(dateModelExplorer, dateFieldName, dateValue, writer, extraAttributes: extraAttributes);
                WriteTimePickerInput(startTimeModelExplorer, startTimeFieldName, startTimeValue, writer, extraAttributes);
                WriteRightArrowSpan(writer);
                WriteTimePickerInput(endTimeModelExplorer, endTimeFieldName, endTimeValue, writer, extraAttributes);

                writer.WriteLine("</div>"); // form-inline.

                WriteValidation(writer, dateModelExplorer, dateFieldName);
                WriteValidation(writer, startTimeModelExplorer, startTimeFieldName);
                WriteValidation(writer, endTimeModelExplorer, endTimeFieldName);
                if (stayWithinAttribute != null)
                {
                    var hiddenValidationField = _htmlGenerator.GenerateHidden(ViewContext, For.ModelExplorer, $"{For.Name}_validator", null, false, null);
                    hiddenValidationField.Attributes.Add(new KeyValuePair<string, string>("data-rule-staywithin", ".time-range-part"));
                    hiddenValidationField.Attributes.Add(new KeyValuePair<string, string>("data-msg-staywithin", (string)stayWithinAttribute.NamedArguments.Single(a => a.MemberName == "ErrorMessage").TypedValue.Value));
                    hiddenValidationField.Attributes.Add(new KeyValuePair<string, string>("data-rule-otherproperty", (string)stayWithinAttribute.NamedArguments.Single(a => a.MemberName == "OtherRangeProperty").TypedValue.Value));
                    hiddenValidationField.AddCssClass("force-validation");
                    hiddenValidationField.Attributes.Remove("data-val-required");
                    hiddenValidationField.WriteTo(writer, _htmlEncoder);

                    WriteValidation(writer, For.ModelExplorer, $"{For.Name}_validator");
                }
            }
        }

        private void WriteSplitTimeRangeBlock(TextWriter writer, bool isHidden = false)
        {
            var dateModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(SplitTimeRange.StartDate));
            var dateFieldName = $"{For.Name}.{nameof(SplitTimeRange.StartDate)}";

            var startTimeHourModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(SplitTimeRange.StartTimeHour));
            var startTimeHourFieldName = $"{For.Name}.{nameof(SplitTimeRange.StartTimeHour)}";
            var startTimeMinutesModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(SplitTimeRange.StartTimeMinutes));
            var startTimeMinutesFieldName = $"{For.Name}.{nameof(SplitTimeRange.StartTimeMinutes)}";

            var endTimeHourModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(SplitTimeRange.EndTimeHour));
            var endTimeHourFieldName = $"{For.Name}.{nameof(SplitTimeRange.EndTimeHour)}";
            var endTimeMinutesModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == nameof(SplitTimeRange.EndTimeMinutes));
            var endTimeMinutesFieldName = $"{For.Name}.{nameof(SplitTimeRange.EndTimeMinutes)}";

            object dateValue = null;
            object startTimeHourValue = null;
            object startTimeMinutesValue = null;
            object endTimeHourValue = null;
            object endTimeMinutesValue = null;

            if (For.Model != null)
            {
                dateValue = dateModelExplorer.Model;
                startTimeHourValue = startTimeHourModelExplorer.Model;
                startTimeMinutesValue = startTimeMinutesModelExplorer.Model;
                endTimeHourValue = endTimeHourModelExplorer.Model;
                endTimeMinutesValue = endTimeMinutesModelExplorer.Model;
            }

            writer.WriteLine("<div class=\"form-inline\">");
            writer.WriteLine("<div class=\"row\">");

            writer.WriteLine("<div class=\"col-sm-4\">");
            WriteLabelWithoutFor(dateModelExplorer, writer);
            writer.WriteLine("<br \\>");
            WriteDatePickerInput(dateModelExplorer, dateFieldName, dateValue, writer);
            WriteValidation(writer, dateModelExplorer, dateFieldName);
            writer.WriteLine("</div>");

            writer.WriteLine("<div class=\"col-sm-4\">");
            WriteLabelWithoutFor(startTimeHourModelExplorer, writer);
            writer.WriteLine("<br \\>");
            WriteSplitTimePickerInput(startTimeHourModelExplorer, startTimeHourFieldName, startTimeHourValue, writer, true);
            WriteSplitTimePickerInput(startTimeMinutesModelExplorer, startTimeMinutesFieldName, startTimeMinutesValue, writer, false);
            writer.WriteLine("</div>");

            writer.WriteLine("<div class=\"col-sm-4\">");
            WriteLabelWithoutFor(endTimeHourModelExplorer, writer);
            WriteInfoIfDescription(writer);
            WriteHelpIfHelpLink(writer);
            writer.WriteLine("<br \\>");
            WriteSplitTimePickerInput(endTimeHourModelExplorer, endTimeHourFieldName, endTimeHourValue, writer, true);
            WriteSplitTimePickerInput(endTimeMinutesModelExplorer, endTimeMinutesFieldName, endTimeMinutesValue, writer, false);
            writer.WriteLine("</div>");

            writer.WriteLine("</div>"); //row
            writer.WriteLine("</div>"); //form-inline.
        }

        private void RemoveRequiredIfNullable(TagBuilder tagBuilder)
        {
            if (!For.Metadata.IsRequired)
            {
                tagBuilder.Attributes.Remove("data-val-required");
            }
        }

        private void WriteSelect(TextWriter writer)
        {
            WriteSelect(Items, writer, For.Name, For.ModelExplorer);
        }

        private void WriteSelect(IEnumerable<SelectListItem> selectList, TextWriter writer, string expression, ModelExplorer modelExplorer, string placeholder = "-- Välj --")
        {
            var prefixAttribute = (PrefixAttribute)AttributeHelper.GetAttribute<PrefixAttribute>(
                For.ModelExplorer.Metadata.ContainerType,
                For.ModelExplorer.Metadata.PropertyName);
            bool writePrefix = prefixAttribute != null && prefixAttribute.PrefixPosition == PrefixAttribute.Position.Value;
            var realModelType = modelExplorer.ModelType;
            var allowMultiple = typeof(string) != realModelType &&
                typeof(IEnumerable).IsAssignableFrom(realModelType);

            var currentValues = _htmlGenerator.GetCurrentValues(
                ViewContext,
                modelExplorer,
                expression: expression,
                allowMultiple: allowMultiple);

            var tagBuilder = _htmlGenerator.GenerateSelect(
                ViewContext,
                modelExplorer,
                optionLabel: null,
                expression: expression,
                selectList: selectList,
                currentValues: currentValues,
                allowMultiple: allowMultiple,
                htmlAttributes: new { @class = "form-control" });
            tagBuilder.Attributes.Add("data-placeholder", placeholder);
            if (For.Model == null)
            {
                var existingOptionsBuilder = new HtmlContentBuilder();
                tagBuilder.InnerHtml.MoveTo(existingOptionsBuilder);

                tagBuilder.InnerHtml.Clear();
                tagBuilder.InnerHtml.AppendHtml("<option value></option>");
                tagBuilder.InnerHtml.AppendHtml(existingOptionsBuilder);
            }
            if (writePrefix)
            {
                writer.Write("<span class=\"prefix\">");
            }
            WritePrefix(writer, PrefixAttribute.Position.Value);
            if (writePrefix)
            {
                writer.Write("</span>");

            }
            tagBuilder.WriteTo(writer, _htmlEncoder);

        }

        private void WriteRadioGroup(TextWriter writer)
        {
            bool isRow = LayoutOption == "row";
            var id = IdOverride ?? For.Name;
            int? ci = CheckedIndex == null ? 0
                : CheckedIndex == "none" ? (int?)null
                : int.Parse(CheckedIndex);

            writer.WriteLine($"<div id=\"{id}\">");

            var itArr = Items.ToArray();
            for (int i = 0; i < itArr.Length; i++)
            {
                var item = itArr[i];
                var itemName = $"{id}_{i}";
                bool isChecked = CheckedIndex == "none" ? false : i == ci;
                var checkedAttr = isChecked ? "checked=\"checked\"" : "";
                // Done manually because GenerateRadioButton automatically sets id=For.Name, which it shouldn't
                var inputElem = $"<input id=\"{itemName}\" name=\"{For.Name}\" type=\"radio\" value=\"{item.Value}\" {checkedAttr}/>";

                if (isRow)
                {
                    writer.WriteLine($"<label for=\"{itemName}\">");
                    WritePrefix(writer, PrefixAttribute.Position.Value);
                    writer.WriteLine(inputElem);
                    writer.WriteLine($"{item.Text}</label>");
                }
                else
                {
                    writer.WriteLine($"<label for=\"{itemName}\" class=\"radiocontainer\"> ");
                    WritePrefix(writer, PrefixAttribute.Position.Value);
                    writer.WriteLine(inputElem);
                    writer.WriteLine($"<span class=\"checkmark\"></span > <span class=\"radio-text\">{item.Text}</span ></label><br><div class=\"radiobutton-row-space\"></div>");
                }
            }

            writer.WriteLine($"</div>"); //groupId
        }

        private void WriteCheckboxGroup(TextWriter writer)
        {
            var htmlBuilder = new HtmlContentBuilder();

            var id = IdOverride ?? For.Name;

            var checkboxGroupBuilder = new TagBuilder("div");
            checkboxGroupBuilder.Attributes.Add("id", id);
            checkboxGroupBuilder.Attributes.Add("for", For.Name);
            checkboxGroupBuilder.Attributes.Add("name", For.Name);

            htmlBuilder.AppendHtml(checkboxGroupBuilder.RenderStartTag());

            var itemsArr = Items.ToArray();

            for (var i = 0; i < itemsArr.Count(); i++)
            {
                var item = itemsArr[i];
                var itemName = $"{id}_{i}";
                bool isChecked = ((CheckboxGroup)For.ModelExplorer.Model)?.SelectedItems?.Contains(item) ?? false;
                var checkedAttr = isChecked ? "checked=\"checked\"" : "";
                // Done manually because GenerateCheckbox automatically sets id=For.Name, which it shouldn't
                var inputElem = $"<input data-checkbox-group=\"{For.Name}\" id=\"{itemName}\" name=\"{For.Name}\" type=\"checkbox\" value=\"{item.Value}\" {checkedAttr}/>";

                var labelBuilder = _htmlGenerator.GenerateLabel(
                    ViewContext,
                    For.ModelExplorer,
                    itemName,
                    labelText: item.Text,
                    htmlAttributes: new { @class = "control-label detail-text" });

                htmlBuilder.AppendHtml(labelBuilder.RenderStartTag());
                htmlBuilder.AppendHtml(inputElem);
                htmlBuilder.AppendHtml(labelBuilder.InnerHtml);
                htmlBuilder.AppendHtml(labelBuilder.RenderEndTag());

                htmlBuilder.AppendHtml("<br/>");
            }

            var hiddenBuilder = _htmlGenerator.GenerateCheckBox(
                ViewContext,
                For.ModelExplorer,
                $"{For.Name}_cbHidden",
                null,
                new { @class = "force-validation" });

            htmlBuilder.AppendHtml(hiddenBuilder.RenderSelfClosingTag());

            htmlBuilder.AppendHtml(checkboxGroupBuilder.RenderEndTag());

            htmlBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteValidation(TextWriter writer)
        {
            WriteValidation(writer, For.ModelExplorer, For.Name);
        }

        private void WriteValidation(TextWriter writer, ModelExplorer modelExplorer, string Name)
        {
            var tagBuilder = _htmlGenerator.GenerateValidationMessage(
                ViewContext,
                modelExplorer,
                Name,
                message: null,
                tag: null,
                htmlAttributes: new { @class = "text-danger" });

            tagBuilder.WriteTo(writer, _htmlEncoder);
        }
    }
}
