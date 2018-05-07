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
    public class FormEntryTagHelper: TagHelper
    {
        private readonly IHtmlGenerator htmlGenerator;
        private readonly HtmlEncoder htmlEncoder;

        public FormEntryTagHelper(IHtmlGenerator htmlGenerator, HtmlEncoder htmlEncoder)
        {
            this.htmlGenerator = htmlGenerator;
            this.htmlEncoder = htmlEncoder;
        }

        private const string ForAttributeName = "asp-for";
        private const string ItemsAttributeName = "asp-items";
        private const string InputTypeName = "type";
        private const string InputTypeSelect = "select";

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

            switch(InputType)
            {
                case InputTypeSelect:
                    if(Items == null)
                    {

                    }
                    break;
                case null:
                    if (Items != null)
                    {
                        throw new ArgumentException("Items are only relevant if type is select.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown input type {InputType} for expression {For.Name}, known types are select. Omit type to get a normal string input.");
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.Attributes.Add("class", "form-group");

            using (var writer = new StringWriter())
            {
                WriteLabel(writer);
                switch(InputType)
                {
                    case InputTypeSelect:
                        WriteSelect(writer);
                        break;
                    default:
                        WriteInput(writer);
                        break;
                }
                WriteValidation(writer);
                output.Content.AppendHtml(writer.ToString());
            }
        }

        private void WriteLabel(TextWriter writer)
        {
            var tagBuilder = htmlGenerator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                labelText: null,
                htmlAttributes: new { @class = "control-label" });

            tagBuilder.WriteTo(writer, htmlEncoder);
        }

        private void WriteInput(TextWriter writer)
        {
            var tagBuilder = htmlGenerator.GenerateTextBox(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                value: For.ModelExplorer.Model,
                format: null,
                htmlAttributes: new { @class = "form-control" });

            tagBuilder.WriteTo(writer, htmlEncoder);
        }

        private void WriteSelect(TextWriter writer)
        {
            var realModelType = For.ModelExplorer.ModelType;
            var allowMultiple = typeof(string) != realModelType &&
                typeof(IEnumerable).IsAssignableFrom(realModelType);

            var currentValues = htmlGenerator.GetCurrentValues(
                ViewContext,
                For.ModelExplorer,
                expression: For.Name,
                allowMultiple: allowMultiple);

            var tagBuilder = htmlGenerator.GenerateSelect(
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
                tagBuilder.InnerHtml.AppendHtml("<option disabled selected value style=\"display:none\"> --- Välj --- </option>");
            }

            tagBuilder.WriteTo(writer, htmlEncoder);
        }

        private void WriteValidation(TextWriter writer)
        {
            var tagBuilder = htmlGenerator.GenerateValidationMessage(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                message: null,
                tag: null,
                htmlAttributes: new { @class = "text-danger" });

            tagBuilder.WriteTo(writer, htmlEncoder);
        }
    }
}
