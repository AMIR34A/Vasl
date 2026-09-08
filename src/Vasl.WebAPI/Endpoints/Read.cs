using MediatR;
using Microsoft.AspNetCore.Mvc;
using Vasl.ApplicationService.Urls.Queries.Get;

namespace Vasl.WebAPI.Endpoints;

public class Read : IEndpoint
{
    public EndpointType Type => EndpointType.Read;

    public void AddEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/{code}", async ([FromRoute] string code,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetUrlQuery(code), cancellationToken);
            return result.OriginalUrl.Equals(GetUrlQueryHandler.NotFoundMarker) ?
                Results.NotFound() :
                Results.Redirect(result.OriginalUrl);
        });
    }
}