namespace BasisBank.Identity.Api.Exceptions {
    public enum ApiErrorCode {
        None = 0,
        InvalidCredentials = 1,
        MfaRequired = 2,
        AccountLocked = 3,
        UserNotFound = 4,
        TokenExpired = 5,
        ValidationError = 6,
        InternalServerError = 99,
        OtpExpired = 103,
        InvalidOtp = 104,
        Unauthorized = 401,
        BadRequest = 400
    }
}
