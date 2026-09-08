namespace Vasl.WebAPI.Endpoints;

public interface IEndpoint
{
    EndpointType Type { get; }

    void AddEndpoint(IEndpointRouteBuilder app);
}