using System.Net;

namespace cashly.src.Exceptions;

/// <summary>
/// Eccezione personalizzata per errori di business che non devono essere loggati come errori critici.
/// </summary>
public class AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) : Exception(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
}
