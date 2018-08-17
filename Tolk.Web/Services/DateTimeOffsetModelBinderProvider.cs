﻿using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tolk.Web.Services
{
    public class DateTimeOffsetModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if(context.Metadata.ModelType == typeof(DateTimeOffset) || 
                context.Metadata.ModelType == typeof(DateTimeOffset?))
            {
                return new BinderTypeModelBinder(typeof(DateTimeOffsetModelBinder));
            }

            return null;
        }
    }
}
