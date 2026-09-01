using MediatR;
using Microsoft.Extensions.Options;
using Vasl.ApplicationService.Urls.Commands.Create;
using Vasl.Infrastructure;

namespace Vasl.WebAPI.Endpoints;

public static class Write
{
    public static void AddWriteEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapPost("/CreateShortUrl", async (CreateUrlCommand createUrl,
            IMediator mediator,
            IOptions<AppSettings> options,
            CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(createUrl, cancellationToken);
            return Results.Ok(new
            {
                Code = result.Code,
                Url = $"{options.Value.RedirectUrl}/{result.Code}"
            });
        });
    }
}