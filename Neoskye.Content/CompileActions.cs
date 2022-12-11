using Neoskye.Content;

public static class CompileActions
{
    public static Result<object?, Exception> Copy(string src, string dest)
    {
        try
        {
            var file = new FileInfo(src);
            Directory.CreateDirectory(
                new FileInfo(dest).Directory?.ToString()!
            );
            file.CopyTo(dest);
        }
        catch (Exception e)
        {
            return Result<object?, Exception>.Err(e);
        }

        return Result<object?, Exception>.Ok(null);
    }
}