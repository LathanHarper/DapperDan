using Microsoft.Maui.Layouts;

namespace Flexler.Models;

public sealed class FlexBasisOption
{
    public FlexBasisOption(string token, FlexBasis value)
    {
        Token = token;
        Value = value;
    }

    public string Token { get; }

    public FlexBasis Value { get; }

    public override string ToString() => Token;
}
