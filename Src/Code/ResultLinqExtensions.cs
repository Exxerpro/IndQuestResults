namespace IndQuestResults.Operations
{
    public static class ResultLinqExtensions
    {
        public static Result<TResult> SelectMany<TSource, TBind, TResult>(
            this Result<TSource> source,
            Func<TSource, Result<TBind>> bind,
            Func<TSource, TBind, TResult> project)
        {
            if (source.IsFailure)
                return Result<TResult>.WithFailure(source.Errors);

            var bound = bind(source.Value!);
            if (bound.IsFailure)
                return Result<TResult>.WithFailure(bound.Errors);

            return Result<TResult>.Success(project(source.Value!, bound.Value!));
        }

        public static Result<TResult> Select<TSource, TResult>(
            this Result<TSource> source,
            Func<TSource, TResult> selector)
        {
            if (source.IsFailure)
                return Result<TResult>.WithFailure(source.Errors);

            return Result<TResult>.Success(selector(source.Value!));
        }

        public static Result<TSource> Where<TSource>(
            this Result<TSource> source,
            Func<TSource, bool> predicate)
        {
            if (source.IsFailure)
                return source;

            if (!predicate(source.Value!))
                return Result<TSource>.WithFailure("Where predicate returned false");

            return source;
        }
    }
}