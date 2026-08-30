using MediatR;

namespace Vasl.ApplicationService.Urls.Queries.Get;

public record GetUrlQuery(string Code) : IRequest<GetUrlQueryResponse>;