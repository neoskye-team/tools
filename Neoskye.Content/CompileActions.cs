using Neoskye.Content;

public static class CompileActions
{
    public static Result<object?, Exception> Copy(string src, string dest)
    {
        try
        {
            var srcFile = new FileInfo(src);
            var destFile = new FileInfo(dest);
            Directory.CreateDirectory(
                destFile.Directory?.ToString()!
            );
            srcFile.CopyTo(dest, true);
        }
        catch (Exception e)
        {
            return Result<object?, Exception>.Err(e);
        }

        return Result<object?, Exception>.Ok(null);
    }
}