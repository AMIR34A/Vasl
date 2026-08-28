using System.Text;
using Vasl.Domain.Contracts;

namespace Vasl.Infrastructure.Services.CodeGenerators;

public class Base62CodeGenerator : ICodeGenerator
{
    private const int Base = 62;
    private const long Modulus = 1L << 40;
    private const long Prime = 3_935_559_000_370_003_845;
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public string GenerateCode(long id)
    {
        long scrambled = (id * Prime) % Modulus;
        return Base62Raw(scrambled);
    }

    private string Base62Raw(long value)
    {
        if (value == 0)
            return Alphabet[0].ToString();

        var sb = new StringBuilder();

        while (value > 0)
        {
            sb.Insert(0, Alphabet[(int)(value % Base)]);
            value /= Base;
        }

        return sb.ToString();
    }
}