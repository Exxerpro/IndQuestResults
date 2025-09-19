using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

class Program
{
    static void Main()
    {
        var code = @"var results = numbers.Select(n => Result<int>.Success(n)).ToList();";
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();
        
        var invocations = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>();
        
        foreach (var inv in invocations)
        {
            if (inv.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax ma &&
                ma.Name.Identifier.Text == "Success")
            {
                var lineSpan = ma.Expression.GetLocation().GetLineSpan();
                System.Console.WriteLine($"Success invocation found");
                System.Console.WriteLine($"MemberAccess.Expression: {ma.Expression}");
                System.Console.WriteLine($"MemberAccess.Expression location: column {lineSpan.StartLinePosition.Character + 1}");
                System.Console.WriteLine($"Full expression: {ma}");
            }
        }
    }
}