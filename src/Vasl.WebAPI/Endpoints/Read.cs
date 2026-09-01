using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Vasl.ApplicationService.Urls.Queries.Get;

namespace Vasl.WebAPI.Endpoints;

public static class Read
{
    public static void AddReadEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/{code}", async ([FromRoute] string code,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(new GetUrlQuery(code), cancellationToken);
            return result.OriginalUrl.IsNullOrEmpty() ? Results.NotFound() : Results.Redirect(result.OriginalUrl);
        });
    }
}