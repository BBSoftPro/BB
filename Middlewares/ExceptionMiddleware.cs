using BasisBank.Identity.Api.Exceptions;

namespace BasisBank.Identity.Api.Middlewares {
    public class ExceptionMiddleware {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context) {
            try {
                await _next(context);
            }
            catch (Exception ex) {
                // თუ პასუხის გაგზავნა უკვე დაწყებულია, სხვა არაფერი ვქნათ
                if (context.Response.HasStarted)
                    return;

                context.Response.ContentType = "application/json";

                // ნებისმიერ შეცდომაზე (მათ შორის NullReference-ზე) დავაბრუნოთ 400
                context.Response.StatusCode = 400;

                var response = new {
                    ErrorCode = (int)ApiErrorCode.BadRequest,
                    Message = "BadRequest"
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}