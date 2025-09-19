var results = numbers.Select(n => Result<int>.Success(n)).ToList();
         1         2         3         4         5    
12345678901234567890123456789012345678901234567890

Position analysis:
- Column 39: R (from "Result<int>")
- Column 43: S (from "Success")

The issue is that the analyzer is detecting the invocation but pointing to 
column 43 (Success method) instead of column 39 (Result<int> generic type).

This suggests memberAccess.Expression.GetLocation() is not pointing where we expect.