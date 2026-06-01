using System.Net;
using System.Text.Json;
using cashly.src.Exceptions;

namespace cashly.src.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            // Business logic errors are logged as information/warning or not at all
            logger.LogWarning("Richiesta non valida: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Un'eccezione non gestita si è verificata: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError; // 500 di default
        var result = "Si è verificato un errore interno al server.";

        if (exception is AppException appEx)
        {
            code = appEx.StatusCode;
            result = appEx.Message;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        var response = new
        {
            message = result,
            // In produzione non dovremmo mai inviare lo stack trace o i dettagli tecnici dell'eccezione
            // error = exception.Message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
