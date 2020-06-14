using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTCPayServer.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BTCPayServer.Filters
{
    public class OnlyIfSupportAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _bitcoinCode;

        public OnlyIfSupportAttribute(string bitcoinCode)
        {
            _bitcoinCode = bitcoinCode;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var options = context.HttpContext.RequestServices.GetService(typeof(BTCPayServerOptions)) as BTCPayServerOptions;
            if (options.NetworkProvider.GetNetwork(_bitcoinCode) == null)
            {
                context.Result = new NotFoundResult();
                return;
            }
            await next();   
        }
    }
}
