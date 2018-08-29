﻿using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

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

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        [HtmlAttributeName(ItemsAttributeName)]
        public IEnumerable<SelectListItem> Items { get; set; }

        [HtmlAttributeName("type")]
        public string InputType { get; set; }

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
                    if (Items == null)
                    {
                        throw new ArgumentNullException("Items", "Items must be set if type is select");
                    }
                    break;
                case null:
                case InputTypePassword:
                case InputTypeText:
                case InputTypeCheckbox:
                case InputTypeDateTimeOffset:
                case InputTypeTime:
                case InputTypeDateRange:
                case InputTypeTextArea:
                    if (Items != null)
                    {
                        throw new ArgumentException("Items is only relevant if type is select.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown input type {InputType} for expression {For.Name}, known types are select, datetime, text, password, checkbox, textarea, time. Omit type to get a default input.");
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
                }
                if (For.ModelExplorer.Metadata.DataTypeName == "Password")
                {
                    InputType = InputTypePassword;
                }
                if (For.ModelExplorer.ModelType == typeof(bool))
                {
                    InputType = InputTypeCheckbox;
                }
                if(For.ModelExplorer.Metadata.DataTypeName == "MultilineText")
                {
                    InputType = InputTypeTextArea;
                }
                if (For.ModelExplorer.ModelType == typeof(TimeSpan)
                    || For.ModelExplorer.ModelType == typeof(TimeSpan?))
                {
                    InputType = InputTypeTime;
                }
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
                        WriteLabel(writer);
                        WriteDateRangeBlock(writer);
                        WriteValidation(writer);
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
        private const string InformationSpan = "<span class=\"form-entry-information glyphicon glyphicon-info-sign\" title=\"{0}\"></span>";

        private void WriteLabel(TextWriter writer)
        {
            TagBuilder tagBuilder = GenerateLabel();

            if (For.ModelExplorer.Metadata.IsRequired)
            {
                tagBuilder.InnerHtml.AppendHtml(RequiredStarSpan);
            }
            tagBuilder.WriteTo(writer, _htmlEncoder);

            WriteInfoIfDescription(writer);
        }

        private void WriteInfoIfDescription(TextWriter writer)
        {
            if (!string.IsNullOrEmpty(For.ModelExplorer.Metadata.Description))
            {
                writer.WriteLine(string.Format(InformationSpan, For.ModelExplorer.Metadata.Description));
            }
        }

        private TagBuilder GenerateLabel()
        {
            return _htmlGenerator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                labelText: null,
                htmlAttributes: new { @class = "control-label" });
        }

        private void WriteInput(TextWriter writer)
        {
            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.Model,
                format: null,
                htmlAttributes: new { @class = "form-control" });

            if(!string.IsNullOrEmpty(For.Metadata.Description))
            {
                tagBuilder.Attributes.Add("placeholder", For.Metadata.Description);
            }

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

            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteCheckBoxInLabel(TextWriter writer)
        {
            var labelBuilder = GenerateLabel();

            var checkboxBuilder = _htmlGenerator.GenerateCheckBox(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                isChecked: Equals(For.Model, true),
                htmlAttributes: null);

            var htmlBuilder = new HtmlContentBuilder();
            htmlBuilder.AppendHtml(labelBuilder.RenderStartTag());
            htmlBuilder.AppendHtml(checkboxBuilder.RenderStartTag());
            htmlBuilder.AppendHtml(labelBuilder.InnerHtml);
            htmlBuilder.AppendHtml(labelBuilder.RenderEndTag());

            htmlBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WritePassword(TextWriter writer)
        {
            var tagBuilder = _htmlGenerator.GeneratePassword(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.Model,
                htmlAttributes: new { @class = "form-control" });

            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteDateRangeBlock(TextWriter writer)
        {
            writer.WriteLine("<div class=\"form-inline\">");

            var fromModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Start");
            var fromFieldName = $"{For.Name}.Start";
            var toModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "End");
            var toFieldName = $"{For.Name}.End";
            object fromValue = fromModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date")?.Model;
            object toValue = toModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date")?.Model;

            WriteDatePickerInput(fromModelExplorer, fromFieldName, fromValue, writer);

            // Using &nbsp; is an ugly hack. Should be fixed when layout is finalized.
            writer.WriteLine("&nbsp;&nbsp;<span class=\"glyphicon glyphicon-arrow-right\"></span>");

            WriteDatePickerInput(toModelExplorer, toFieldName, toValue, writer, "pull-right");

            writer.WriteLine("</div>"); // form-inline.
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
            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private void WriteDateTimeOffsetBlock(TextWriter writer)
        {
            // First write a label
            writer.Write($"<label>{_htmlGenerator.Encode(For.ModelExplorer.Metadata.DisplayName)}");
            if (For.ModelExplorer.Metadata.IsRequired)
            {
                writer.Write(RequiredStarSpan);
            }
            writer.WriteLine("</label>");
            WriteInfoIfDescription(writer);

            // Then open the inline form
            writer.WriteLine("<div class=\"form-inline\">");

            var dateModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "Date");
            var dateFieldName = $"{For.Name}.Date";
            var timeModelExplorer = For.ModelExplorer.Properties.Single(p => p.Metadata.PropertyName == "TimeOfDay");
            var timeFieldName = $"{For.Name}.TimeOfDay";
            object dateValue;
            object timeValue;

            if (Equals(For.Model, default(DateTimeOffset)))
            {
                dateValue = null;
                timeValue = null;
            }
            else
            {
                dateValue = dateModelExplorer.Model;
                timeValue = timeModelExplorer.Model;
            }

            WriteDatePickerInput(dateModelExplorer, dateFieldName, dateValue, writer);

            writer.WriteLine("<div class=\"input-group time\">");

            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                timeModelExplorer,
                timeFieldName,
                value: timeValue,
                format: "{0:hh\\:mm}",
                htmlAttributes: new
                {
                    @class = "form-control",
                    placeholder = "HH:MM",
                    data_val_regex_pattern = "^(([0-1]?[0-9])|(2[0-3])):?[0-5][0-9]$",
                    data_val_regex = "Ange tid som HH:MM eller HHMM",
                    data_val_required = "Tid måste anges."
                });

            RemoveRequiredIfNullable(tagBuilder);
            tagBuilder.WriteTo(writer, _htmlEncoder);

            writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-time\"></span></div></div>"); //input-group time

            writer.WriteLine("</div>"); // form-inline

            WriteValidation(writer, dateModelExplorer, dateFieldName);
            writer.WriteLine();
            WriteValidation(writer, timeModelExplorer, timeFieldName);
        }

        private void WriteDatePickerInput(
            ModelExplorer dateModelExplorer,
            string dateFieldName,
            object dateValue,
            TextWriter writer,
            string extraGroupDivClass = "")
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
                    @class = "form-control datepicker",
                    placeholder = "ÅÅÅÅ-MM-DD",
                    type = "text",
                    data_val_required = "Datum måste anges."
                });

            RemoveRequiredIfNullable(tagBuilder);

            tagBuilder.WriteTo(writer, _htmlEncoder);

            writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-calendar\"></span></div></div>"); //input-group date
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
            var realModelType = For.ModelExplorer.ModelType;
            var allowMultiple = typeof(string) != realModelType &&
                typeof(IEnumerable).IsAssignableFrom(realModelType);

            var currentValues = _htmlGenerator.GetCurrentValues(
                ViewContext,
                For.ModelExplorer,
                expression: For.Name,
                allowMultiple: allowMultiple);

            var tagBuilder = _htmlGenerator.GenerateSelect(
                ViewContext,
                For.ModelExplorer,
                optionLabel: null,
                expression: For.Name,
                selectList: Items,
                currentValues: currentValues,
                allowMultiple: allowMultiple,
                htmlAttributes: new { @class = "form-control" });

            tagBuilder.Attributes.Add("data-placeholder", "--- Välj ---");
            if (For.Model == null)
            {
                var existingOptionsBuilder = new HtmlContentBuilder();
                tagBuilder.InnerHtml.MoveTo(existingOptionsBuilder);

                tagBuilder.InnerHtml.Clear();
                tagBuilder.InnerHtml.AppendHtml("<option value></option>");
                tagBuilder.InnerHtml.AppendHtml(existingOptionsBuilder);
            }

            tagBuilder.WriteTo(writer, _htmlEncoder);
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
