namespace Template.Api.Dtos;

public sealed record ResponseDto<T>
{
    public T? Data { get; set; }
    public string? Message { get; set; }
    public bool Success { get; set; } = true;
}