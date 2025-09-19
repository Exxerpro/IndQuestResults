// Simple debug test code
var test = string.Join(", ", _result.Errors);

// This should be detected as a string.Join call on _result.Errors
// Let's trace through the syntax tree:
// - InvocationExpressionSyntax: string.Join(", ", _result.Errors)
// - Expression: MemberAccessExpressionSyntax: string.Join
//   - Expression: IdentifierNameSyntax: string
//   - Name: IdentifierNameSyntax: Join
// - ArgumentList: ArgumentListSyntax
//   - Arguments[0]: ", "
//   - Arguments[1]: _result.Errors (MemberAccessExpressionSyntax)
//     - Expression: _result
//     - Name: Errors