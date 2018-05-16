using Microsoft.AspNetCore.Html;
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
            this._htmlGenerator = htmlGenerator;
            this._htmlEncoder = htmlEncoder;
        }

        private const string ForAttributeName = "asp-for";
        private const string ItemsAttributeName = "asp-items";
        private const string InputTypeName = "type";
        private const string InputTypeSelect = "select";
        private const string InputTypeDateTimeOffset = "datetime";
        private const string InputTypeText = "text";
        private const string InputTypePassword = "password";
        private const string InputTypeCheckbox = "checkbox";

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
                    if (Items != null)
                    {
                        throw new ArgumentException("Items is only relevant if type is select.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown input type {InputType} for expression {For.Name}, known types are select, datetime, text, password, checkbox. Omit type to get a default input.");
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
                if(For.ModelExplorer.ModelType == typeof(bool))
                {
                    InputType = InputTypeCheckbox;
                }
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;

            using (var writer = new StringWriter())
            {
                switch (InputType)
                {
                    case InputTypeSelect:
                        output.Attributes.Add("class", "form-group");
                        WriteLabel(writer);
                        WriteSelect(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeDateTimeOffset:
                        output.Attributes.Add("class", "form-group");
                        WriteDateTimeOffsetBlock(writer);
                        break;
                    case InputTypePassword:
                        output.Attributes.Add("class", "form-group");
                        WriteLabel(writer);
                        WritePassword(writer);
                        WriteValidation(writer);
                        break;
                    case InputTypeCheckbox:
                        output.Attributes.Add("class", "checkbox");
                        WriteCheckBoxInLabel(writer);
                        WriteValidation(writer);
                        break;
                    default:
                        WriteLabel(writer);
                        WriteInput(writer);
                        WriteValidation(writer);
                        break;
                }
                output.Content.AppendHtml(writer.ToString());
            }
        }

        private void WriteLabel(TextWriter writer)
        {
            TagBuilder tagBuilder = GenerateLabel();

            tagBuilder.WriteTo(writer, _htmlEncoder);
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

        private void WriteDateTimeOffsetBlock(TextWriter writer)
        {
            // First write a label
            writer.WriteLine($"<label>{_htmlGenerator.Encode(For.ModelExplorer.Metadata.DisplayName)}</label>");

            // Then open the inline form
            writer.WriteLine("<div class=\"form-inline\">");
            writer.WriteLine("<div class=\"input-group date\">");

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

            var tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                dateModelExplorer,
                dateFieldName,
                value: dateValue,
                format: "{0:yyyy-MM-dd}",
                htmlAttributes: new { @class = "form-control datepicker", placeholder = "ÅÅÅÅ-MM-DD", type = "text" });

            RemoveRequiredIfNullable(tagBuilder);
            tagBuilder.WriteTo(writer, _htmlEncoder);

            writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-calendar\"></span></div></div>"); //input-group date

            writer.WriteLine("<div class=\"input-group time\">");

            tagBuilder = _htmlGenerator.GenerateTextBox(
                ViewContext,
                timeModelExplorer,
                timeFieldName,
                value: timeValue,
                format: "{0:hh\\:mm}",
                htmlAttributes: new
                {
                    @class = "form-control",
                    placeholder = "HH:MM",
                    data_val_regex_pattern = "^(([0-1]?[0-9])|(2[0-3])):[0-5][0-9]$",
                    data_val_regex = "Ange tid som HH:MM"
                });

            RemoveRequiredIfNullable(tagBuilder);
            tagBuilder.WriteTo(writer, _htmlEncoder);

            writer.WriteLine("<div class=\"input-group-addon\"><span class=\"glyphicon glyphicon-time\"></span></div></div>"); //input-group time

            writer.WriteLine("</div>"); // form-inline

            WriteValidation(writer, dateModelExplorer, dateFieldName);
            writer.WriteLine();
            WriteValidation(writer, timeModelExplorer, timeFieldName);
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

            if (currentValues == null)
            {
                var existingOptionsBuilder = new HtmlContentBuilder();
                tagBuilder.InnerHtml.MoveTo(existingOptionsBuilder);

                tagBuilder.InnerHtml.Clear();
                tagBuilder.InnerHtml.AppendHtml("<option disabled selected value style=\"display:none\"> --- Välj --- </option>");
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
