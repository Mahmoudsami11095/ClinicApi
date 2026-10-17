using Clinic.Contracts.Common;

namespace Clinic.Application.DTOs;

public class ApiResponse<T> : Clinic.Contracts.Common.ApiResponse<T>
{
    public new static ApiResponse<T> Success(T data, string message = "Success")
        => new() { Data = data, Message = message };

    public new static ApiResponse<T> Fail(string message)
        => new() { Message = message };
}

public class ApiResponse : Clinic.Contracts.Common.ApiResponse
{
    public new static ApiResponse Success(string message = "Success")
        => new() { Message = message };

    public new static ApiResponse Fail(string message)
        => new() { Message = message };
}
