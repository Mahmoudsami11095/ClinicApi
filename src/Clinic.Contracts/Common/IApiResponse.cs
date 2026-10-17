namespace Clinic.Contracts.Common;

public interface IApiResponse
{
    string Message { get; set; }
}

public interface IApiResponse<T> : IApiResponse
{
    T? Data { get; set; }
}
