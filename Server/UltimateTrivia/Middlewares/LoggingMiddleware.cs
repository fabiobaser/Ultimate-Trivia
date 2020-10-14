using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UltimateTrivia.Middlewares
{
    public class LoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        
        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.Value.StartsWith("/api"))
            {
                await _next(context);
                return;
            }
            
            context.Request.EnableBuffering();

            await LogRequest(context);
            
            var originalBodyStream = context.Response.Body;
            await using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
                
                await LogResponse(context);
            }
            finally
            {
                context.Response.Body.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
                context.Response.Body = originalBodyStream;
            }
        }

        private async Task LogRequest(HttpContext context)
        {
            string bodyStr;
            var req = context.Request;

            req.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(req.Body, Encoding.UTF8, true, 1024, true);
            bodyStr = await reader.ReadToEndAsync();
            req.Body.Seek(0, SeekOrigin.Begin);

            bodyStr = Regex.Replace(bodyStr ?? string.Empty, @"\r\n?|\n", string.Empty);
            var msg = $"Received Request '{context.TraceIdentifier}' to {context.Request.Method} {context.Request.Path}{context.Request.QueryString}";
            msg += string.IsNullOrWhiteSpace(bodyStr) ? string.Empty : $" with body {bodyStr}";
            
            _logger.LogDebug(msg);
        }

        private async Task LogResponse(HttpContext context)
        {
            string bodyStr;
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using(var reader = new StreamReader(context.Response.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyStr = await reader.ReadToEndAsync();
            }

            var msg = $"Sending Response '{context.TraceIdentifier}' with status code {context.Response.StatusCode}";
            msg += string.IsNullOrWhiteSpace(bodyStr) ? string.Empty : $" and with body {bodyStr}";
                
            _logger.LogDebug(msg);
        }
    }
}