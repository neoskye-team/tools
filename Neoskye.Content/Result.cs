public class Result<T, E>
where E : Exception
{
    private T? response;
    private E? exception;

    private Result()
    {

    }

    public static Result<T, E> Ok(T response)
    {
        var ret = new Result<T, E>();
        ret.response = response;
        return ret;
    }

    public static Result<T, E> Err(E exception)
    {
        var ret = new Result<T, E>();
        ret.exception = exception;
        return ret;
    }

    public T Unwrap()
    {
        if (this.response is not null)
            return this.response;
        else
            throw this.exception!;
    }

    public E UnwrapE() =>
        this.exception!;

    public void MapE(Action<E> action)
    {
        if (this.exception is not null)
            action.Invoke(this.exception);
    }
}