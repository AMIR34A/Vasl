using MediatR;

namespace Vasl.ApplicationService.Urls.Commands.Create;

public record CreateUrlCommand(string Url, DateTime ExpirationTime) : IRequest<CreateUrlCommandResponse>;