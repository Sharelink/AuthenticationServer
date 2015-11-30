using Microsoft.AspNet.Mvc.Filters;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AuthenticationServer
{

    public class LogExceptionFilter : ActionFilterAttribute, IExceptionFilter, IAsyncExceptionFilter
    {
        private NLog.ILogger Logger { get; set; }
        public LogExceptionFilter()
        {
            this.Logger = LogManager.GetLogger("Exception Logger");
        }
        public void OnException(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = 500;
            Logger.Error(context.Exception, context.Exception.Message);
            Logger.Debug(context.Exception.StackTrace);
            context.Exception = null;
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            context.HttpContext.Response.StatusCode = 500;
            return Task.Run(() => {
                Logger.Error(context.Exception, context.Exception.Message);
                Logger.Debug(context.Exception.StackTrace);
                context.Exception = null;
            });
        }
    }

}
