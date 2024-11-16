using System;

namespace ItemFilterLibraryDatabase.Api;

public class ApiException : Exception
{
    public ApiException(string message, string errorCode = null) : base(message)
    {
        ErrorCode = errorCode;
        ItemFilterLibraryDatabase.Main.LogError($"ApiException: {message} (Code: {errorCode})", 30f);
    }

    public ApiException(string message, Exception innerException) : base(message, innerException)
    {
        ItemFilterLibraryDatabase.Main.LogError($"ApiException: {message}\n{innerException}", 30f);
    }

    public string ErrorCode { get; }
}

public class ApiAuthenticationException : ApiException
{
    public ApiAuthenticationException(string message) : base(message)
    {
        ItemFilterLibraryDatabase.Main.LogError($"ApiAuthenticationException: {message}", 30f);
    }

    public ApiAuthenticationException(string message, string detailedMessage) : base(message)
    {
        DetailedMessage = detailedMessage;
        ItemFilterLibraryDatabase.Main.LogError($"ApiAuthenticationException: {message}\nDetails: {detailedMessage}", 30f);
    }

    public string DetailedMessage { get; }
}

public class ApiRateLimitException : ApiException
{
    public ApiRateLimitException(string message) : base(message)
    {
        ItemFilterLibraryDatabase.Main.LogError($"ApiRateLimitException: {message}", 30f);
    }
}