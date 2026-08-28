using MediatR;
using StackExchange.Redis;
using Vasl.Domain.Contracts;
using Vasl.Domain.Entities;
using Vasl.Infrastructure.Data;

namespace Vasl.ApplicationService.Urls.Commands.Create;

public class CreateUrlCommandHandler : IRequestHandler<CreateUrlCommand, CreateUrlCommandResponse>
{
    private readonly IDatabase _cache;
    private readonly VaslDbContext _dbContext;
    private readonly ICodeGenerator _codeGenerator;

    public CreateUrlCommandHandler(IConnectionMultiplexer multiplexer, VaslDbContext dbContext, ICodeGenerator codeGenerator)
    {
        _cache = multiplexer.GetDatabase();
        _dbContext = dbContext;
        _codeGenerator = codeGenerator;
    }

    public async Task<CreateUrlCommandResponse> Handle(CreateUrlCommand request, CancellationToken cancellationToken)
    {
        long lastId = await _cache.StringIncrementAsync("url:id");
        string code = _codeGenerator.GenerateCode(lastId);

        Url url = Url.Create(lastId, code, request.Url, request.ExpirationTime);

        _dbContext.Urls.Add(url);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateUrlCommandResponse(code);
    }
}