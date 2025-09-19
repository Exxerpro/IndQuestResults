using System;

namespace IndQuestResults.Operations;

/// <summary>
/// LINQ query support for Result&lt;T&gt; (Select, SelectMany, Where).
/// </summary>
public static class ResultLinqExtensions
{
    /// <summary>
    /// LINQ Select: maps a successful value; propagates errors.
    /// </summary>
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);
        return result.Map(selector);
    }

    /// <summary>
    /// LINQ SelectMany: binds into another Result; propagates errors.
    /// </summary>
    public static Result<TOut> SelectMany<T, TOut>(this Result<T> result, Func<T, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);
        return result.Bind(binder);
    }

    /// <summary>
    /// LINQ SelectMany: binds into another Result; propagates errors.
    /// </summary>
    public static Result<TOut> SelectMany<T1, T2, TOut>(this Result<T1> result1, Result<T2> result2, Func<T1, T2, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(result1);
        ArgumentNullException.ThrowIfNull(result2);
        ArgumentNullException.ThrowIfNull(binder);
        return result1.Bind(x1 => result2.Bind(x2 => binder(x1, x2)));
    }

    /// <summary>
    /// Enables LINQ query syntax for Result&lt;T&gt;
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TCollection"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="source"></param>
    /// <param name="collectionSelector"></param>
    /// <param name="resultSelector"></param>
    /// <returns></returns>
    public static Result<TResult> SelectMany<TSource, TCollection, TResult>(
        this Result<TSource> source,
        Func<TSource, Result<TCollection>> collectionSelector,
        Func<TSource, TCollection, TResult> resultSelector)
    {
        if (source.IsFailure)
        {
            return Result<TResult>.WithFailure(source.Errors);
        }
        var collectionResult = collectionSelector(source.Value!);
        return collectionResult.IsFailure
            ? Result<TResult>.WithFailure(collectionResult.Errors)
            : Result<TResult>.Success(resultSelector(source.Value!, collectionResult.Value!));
    }

    /// <summary>
    /// LINQ Where: filters successful values with a predicate; returns failure if predicate fails.
    /// </summary>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Ensure(predicate, "Where predicate returned false");
    }
}
