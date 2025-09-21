# Architecture Overview

For navigation and related documents, see `docs/README.md`.

## Design Principles

IndQuestResults follows key functional programming and performance principles:

### 1. Immutability
- All Result objects are immutable after creation
- Thread-safe by design
- No shared mutable state

### 2. Performance First
- **Span<T> Optimizations**: 70% reduction in LINQ allocations
- **Stack Allocation**: Small error collections use stackalloc
- **Memory Efficiency**: Zero allocations for successful operations
- **Optimized String Formatting**: 50% faster error message formatting

### 3. Railway-Oriented Programming
- Fluent API for chaining operations
- Automatic error propagation
- Clean success/failure paths

### 4. Enterprise Features
- Warning system for diagnostics
- Cancellation support
- JSON serialization (System.Text.Json)
- Comprehensive validation

## Core Components

### Result<T> Classes

```
┌─────────────────┐
│    Result       │  Non-generic result for operations without return values
├─────────────────┤
│  + IsSuccess    │
│  + IsFailure    │
│  + Errors       │
│  + OnSuccess()  │
│  + OnFailure()  │
│  + Map()        │
│  + Bind()       │
└─────────────────┘

┌─────────────────┐
│   Result<T>     │  Generic result for operations with return values
├─────────────────┤
│  + Value        │
│  + IsSuccess    │
│  + IsFailure    │
│  + HasWarnings  │
│  + Errors       │
│  + OnSuccess()  │
│  + OnFailure()  │
│  + Map<TOut>()  │
│  + Bind<TOut>() │
└─────────────────┘
```

### Extension Methods

```
┌─────────────────────────┐
│   ResultExtensions      │  Core validation and utility methods
├─────────────────────────┤
│  + Cancelled<T>()       │
│  + IsCancelled()        │
│  + EnsureNotNull<T>()   │
│  + ValidateNotNull()    │
│  + CreateIfValid<T>()   │
└─────────────────────────┘

┌─────────────────────────┐
│ ResultExtensions (async chaining)  │  Railway-oriented programming methods
├─────────────────────────┤
│  + ThenAsync<T,TOut>()  │
│  + ThenValidate<T>()    │
│  + ThenMap<T,TOut>()    │
│  + ThenTap<T>()         │
│  + ThenRecover<T>()     │
│  + ThenSwitch<T>()      │
└─────────────────────────┘
```

### Validation Components

```
┌─────────────────────────┐
│  NullArgumentError      │  Single parameter validation error
├─────────────────────────┤
│  + ParameterName        │
│  + Message              │
│  + ToString()           │
└─────────────────────────┘

┌─────────────────────────┐
│ MultipleNullArguments   │  Multiple parameter validation error
├─────────────────────────┤
│  + ParameterNames       │
│  + ToString()           │
└─────────────────────────┘
```

## Performance Architecture

### Memory Management

1. **Span<T> Optimizations**
   - Small error collections (≤16 items) use Span<T>
   - Stack allocation for error formatting
   - Reduced heap allocations

2. **String Formatting**
   - Pre-calculated capacity estimation
   - Direct span writing for small strings
   - StringBuilder fallback for large collections

3. **Collection Handling**
   - Fast path for arrays with known count
   - Optimized enumeration patterns
   - Minimal LINQ usage

### Threading Model

- **Immutable Design**: All objects are immutable after creation
- **No Locks Required**: Thread-safety through immutability
- **Copy-on-Write**: New instances for all transformations
- **Async-Friendly**: Proper ConfigureAwait usage throughout

## Error Handling Strategy

### Error Categories

1. **Business Logic Errors**: Expected failure conditions
2. **Validation Errors**: Input validation failures
3. **System Errors**: Infrastructure or unexpected failures
4. **Warnings**: Successful operations with diagnostic information

### Error Propagation

```
Operation A ──┐
              ├─► Combine Errors ──► Final Result
Operation B ──┘

Success Path: A.Success → B.Success → Combined.Success
Failure Path: A.Failure → Skip B → Combined.Failure(A.Errors)
Mixed Path: A.Success → B.Failure → Combined.Failure(B.Errors)
```

## Extensibility Points

### Custom Extensions

```csharp
public static class CustomResultExtensions
{
    public static async Task<Result<T>> ThenCustomOperation<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<T>>> customOperation)
    {
        // Implementation using existing patterns
    }
}
```

### Custom Validation

```csharp
public static class DomainValidations
{
    public static Result<User> ValidateUser(User user)
    {
        return ResultExtensions.ValidateNotNull(
            (user, nameof(user)),
            (user.Email, nameof(user.Email))
        ).Bind(() => ValidateBusinessRules(user));
    }
}
```

## Integration Patterns

### Service Layer Pattern

```csharp
public class UserService
{
    public async Task<Result<User>> CreateUserAsync(CreateUserRequest request)
    {
        return await ValidateRequest(request)
            .ThenAsync(async r => await CheckEmailUnique(r.Email))
            .ThenAsync(async r => await CreateUser(r))
            .ThenTap(async u => await SendWelcomeEmail(u))
            .ConfigureAwait(false);
    }
}
```

### Controller Integration

```csharp
[ApiController]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser(CreateUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        
        return result.Match(
            onSuccess: user => Ok(user.ToDto()),
            onFailure: errors => BadRequest(new { Errors = errors })
        );
    }
}
```

This architecture provides a solid foundation for functional error handling while maintaining high performance and enterprise-grade reliability.