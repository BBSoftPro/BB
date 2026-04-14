using BasisBank.Identity.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BasisBank.Identity.Api.Filters {
    public class ApiExceptionFilter : IExceptionFilter {
        public void OnException(ExceptionContext context) {
            if (context.Exception is ApiException apiEx) {
                context.Result = new ObjectResult(new {
                    errorCode = (int)apiEx.ErrorCode,
                    message = apiEx.Message
                }) { StatusCode = apiEx.StatusCode };
            }
            else {
                context.Result = new ObjectResult(new {
                    errorCode = (int)ApiErrorCode.InternalServerError,
                    message = "მოხდა გაუთვალისწინებელი შეცდომა."
                }) { StatusCode = 500 };
            }
            context.ExceptionHandled = true;
        }
    }
}
