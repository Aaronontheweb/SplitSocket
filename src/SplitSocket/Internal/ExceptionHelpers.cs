using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SplitSocket.Internal;

internal static class ExceptionHelpers
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
        {
            Throw(paramName);
        }
    }
    
    [DoesNotReturn]
    private static void Throw(string? paramName) =>
        throw new ArgumentNullException(paramName);
}