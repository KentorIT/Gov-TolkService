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
using Tolk.BusinessLogic.Utilities;

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

        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

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
            {
                WriteLabel(writer);
                WriteDetails(writer);
                output.Content.AppendHtml(writer.ToString());
            }
        }

        private void WriteLabel(TextWriter writer)
        {
            var tagBuilder = _htmlGenerator.GenerateLabel(
                ViewContext,
                For.ModelExplorer,
                For.Name,
                labelText: null,
                htmlAttributes: new { @class = "control-label" });

            tagBuilder.WriteTo(writer, _htmlEncoder);
        }

        private enum OutputType {Text, DateTimeOffset, Bool, Enum, Currency  }

        private void WriteDetails(TextWriter writer)
        {
            string text = string.Empty;
            var type = GetOutputType();
            switch(GetOutputType())
            {
                case OutputType.Enum:
                text = EnumHelper.GetDescription(For.ModelExplorer.ModelType, (Enum)For.ModelExplorer.Model);
                    break;
                case OutputType.Bool:
                    text = ((bool)For.ModelExplorer.Model) ? "Ja" : "Nej";
                    break;
                 case OutputType.DateTimeOffset:
                    text = ((DateTimeOffset)For.ModelExplorer.Model).ToString("yyyy-MM-dd HH:mm");
                    break;
                case OutputType.Currency:
                    text = ((decimal)For.ModelExplorer.Model).ToString("#,#.00 SEK");
                    break;
                default:
                    text = _htmlGenerator.Encode(For.ModelExplorer.Model);
                    break;
            }
            writer.WriteLine($"<div class=\"detail-text\">{text}</div>");
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
            return OutputType.Text;
        }
    }
}
