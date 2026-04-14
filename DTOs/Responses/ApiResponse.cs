namespace BasisBank.Identity.Api.DTOs.Responses {
    public class ApiResponse<T> {
        public T? Result { get; set; }
        public int HttpCode { get; set; }
        public int? ErrorCode { get; set; }

        public ApiResponse(T? result, int httpCode = 200, int? errorCode = null) {
            Result = result;
            HttpCode = httpCode;
            ErrorCode = errorCode;
        }
    }
}
