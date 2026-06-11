namespace Clinic.Application.DTOs;

public class ApiResponse<T>
{
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Success(T data, string message = "Success")
        => new() { Data = data, Message = message };

    public static ApiResponse<T> Fail(string message)
        => new() { Message = message };
}

public class ApiResponse
{
    public string Message { get; set; } = string.Empty;

    public static ApiResponse Success(string message = "Success")
        => new() { Message = message };

    public static ApiResponse Fail(string message)
        => new() { Message = message };
}
