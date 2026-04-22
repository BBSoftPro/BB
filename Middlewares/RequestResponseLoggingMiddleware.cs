using Serilog.Context;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace BasisBank.Identity.Api.Middlewares {
    public class RequestResponseLoggingMiddleware {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;
        private const int MaxDataLength = 4096; // truncate
        private static readonly string[] SensitiveKeys = new[] {
            "password", "pass", "pwd", "newPassword", "token", "refreshToken", "creditCard", "cardNumber", "cvv"
        };

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger) {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context) {
            var path = context.Request.Path.Value ?? string.Empty;
            // Skip noise endpoints
            if (path.StartsWith("/swagger") || path.StartsWith("/favicon") || path.StartsWith("/health") || path.StartsWith("/metrics")) {
                await _next(context);
                return;
            }

            // GroupId from header or new
            var groupIdHeader = context.Request.Headers["X-Group-Id"].FirstOrDefault()
                                ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            Guid groupId;
            if (!Guid.TryParse(groupIdHeader, out groupId)) {
                groupId = Guid.NewGuid();
            }

            // Only read request body for likely textual content types
            string requestData = string.Empty;
            try {
                var contentType = context.Request.ContentType ?? string.Empty;
                if (!string.IsNullOrEmpty(contentType) && (contentType.Contains("json") || contentType.Contains("application/x-www-form-urlencoded") || contentType.Contains("text"))) {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                    var rawRequest = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                    if (!string.IsNullOrWhiteSpace(rawRequest)) {
                        requestData = SanitizeAndTruncate(rawRequest, MaxDataLength);
                    }
                }
            }
            catch {
                requestData = string.Empty;
            }

            // capture response
            var originalResponseBody = context.Response.Body;
            await using var memStream = new MemoryStream();
            context.Response.Body = memStream;

            var sw = Stopwatch.StartNew();

            // Log request (mark for DB)
            using (LogContext.PushProperty("LogToDb", true))
            using (LogContext.PushProperty("GroupId", groupId))
            using (LogContext.PushProperty("ApplicationId", 1))
            using (LogContext.PushProperty("ApplicationName", "BasisBank.Identity.Api"))
            using (LogContext.PushProperty("ServiceName", "Identity"))
            using (LogContext.PushProperty("MethodName", $"{context.Request.Method} {context.Request.Path}"))
            using (LogContext.PushProperty("LocalDate", DateTime.UtcNow.Date))
            using (LogContext.PushProperty("LocalTime", DateTime.UtcNow.TimeOfDay))
            using (LogContext.PushProperty("IsRequest", true))
            using (LogContext.PushProperty("Data", requestData)) {
                _logger.LogInformation("Incoming request {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            // proceed
            await _next(context);

            sw.Stop();

            // read response body only for text/json types
            memStream.Seek(0, SeekOrigin.Begin);
            string responseBody = string.Empty;
            try {
                var respContentType = context.Response.ContentType ?? string.Empty;
                if (!string.IsNullOrEmpty(respContentType) && (respContentType.Contains("json") || respContentType.Contains("text"))) {
                    using var respReader = new StreamReader(memStream, Encoding.UTF8, leaveOpen: true);
                    var rawResponse = await respReader.ReadToEndAsync();
                    responseBody = SanitizeAndTruncate(rawResponse, MaxDataLength);
                }
            }
            catch {
                responseBody = string.Empty;
            }

            // copy back
            memStream.Seek(0, SeekOrigin.Begin);
            await memStream.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;

            // Log response (mark for DB)
            using (LogContext.PushProperty("LogToDb", true))
            using (LogContext.PushProperty("GroupId", groupId))
            using (LogContext.PushProperty("ApplicationId", 1))
            using (LogContext.PushProperty("ApplicationName", "BasisBank.Identity.Api"))
            using (LogContext.PushProperty("ServiceName", "Identity"))
            using (LogContext.PushProperty("MethodName", $"{context.Request.Method} {context.Request.Path}"))
            using (LogContext.PushProperty("LocalDate", DateTime.UtcNow.Date))
            using (LogContext.PushProperty("LocalTime", DateTime.UtcNow.TimeOfDay))
            using (LogContext.PushProperty("IsResponse", true))
            using (LogContext.PushProperty("Data", responseBody)) {
                _logger.LogInformation("Outgoing response {StatusCode} for {Path} processed in {ElapsedMs}ms",
                    context.Response.StatusCode, context.Request.Path, sw.ElapsedMilliseconds);
            }
        }

        private static string SanitizeAndTruncate(string raw, int maxLength) {
            if (string.IsNullOrEmpty(raw))
                return string.Empty;

            var trimmed = raw.Length <= maxLength ? raw : raw.Substring(0, maxLength);

            if (IsJson(trimmed)) {
                foreach (var key in SensitiveKeys) {
                    var pattern = $"(\"{Regex.Escape(key)}\"\\s*:\\s*\")([^\\\"]*)(\")";
                    trimmed = Regex.Replace(trimmed, pattern, $"$1[REDACTED]$3", RegexOptions.IgnoreCase);
                    pattern = $"(\"{Regex.Escape(key)}\"\\s*:\\s*)([^,\\}}\\]]+)?";
                    trimmed = Regex.Replace(trimmed, pattern, $"$1[REDACTED]", RegexOptions.IgnoreCase);
                }
            }
            else {
                trimmed = Regex.Replace(trimmed, "(?i)(password=)([^&\\s]+)", "$1[REDACTED]");
            }

            return trimmed;
        }

        private static bool IsJson(string s) {
            s = s.TrimStart();
            return s.StartsWith("{") || s.StartsWith("[");
        }
    }
}