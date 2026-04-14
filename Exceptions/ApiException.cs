namespace BasisBank.Identity.Api.Exceptions {
    public class ApiException : Exception {
        public int StatusCode { get; }
        public ApiErrorCode ErrorCode { get; }

        public ApiException(ApiErrorCode errorCode, string message, int statusCode = 400)
            : base(message) {
            ErrorCode = errorCode;
            StatusCode = statusCode;
        }
    }
}
