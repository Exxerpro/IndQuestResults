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
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The mapped value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="selector">Mapping function to apply when successful.</param>
    /// <returns>A Result containing the mapped value or the original errors.</returns>
    public static Result<TOut> Select<T, TOut>(this Result<T> result, Func<T, TOut> selector)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(selector);
        return result.Map(selector);
    }

    /// <summary>
    /// LINQ SelectMany: binds into another Result; propagates errors.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TOut">The bound value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="binder">Binding function to apply when successful.</param>
    /// <returns>The bound Result or a failure with propagated errors.</returns>
    public static Result<TOut> SelectMany<T, TOut>(this Result<T> result, Func<T, Result<TOut>> binder)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(binder);
        return result.Bind(binder);
    }

    /// <summary>
    /// LINQ SelectMany: binds into another Result; propagates errors.
    /// </summary>
    /// <typeparam name="T1">The first source value type.</typeparam>
    /// <typeparam name="T2">The second source value type.</typeparam>
    /// <typeparam name="TOut">The bound value type.</typeparam>
    /// <param name="result1">The first Result.</param>
    /// <param name="result2">The second Result.</param>
    /// <param name="binder">Binding function combining two values.</param>
    /// <returns>The bound Result or a failure with aggregated errors.</returns>
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
    /// <typeparam name="TSource">The source value type.</typeparam>
    /// <typeparam name="TCollection">The intermediate collection item type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The source Result.</param>
    /// <param name="collectionSelector">Selector producing an intermediate Result.</param>
    /// <param name="resultSelector">Projection combining source and intermediate values.</param>
    /// <returns>The projected Result or a failure with propagated errors.</returns>
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
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The input Result.</param>
    /// <param name="predicate">Predicate to validate the value.</param>
    /// <returns>The original Result if predicate passes; otherwise a failure.</returns>
    public static Result<T> Where<T>(this Result<T> result, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(predicate);
        return result.Ensure(predicate, "Where predicate returned false");
    }
}
