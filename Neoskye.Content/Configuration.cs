namespace Neoskye.Content;

internal sealed class Configuration {
    public ContentType ContentType { get; set; }
    public CompileAction CompileAction { get; set; }
}

internal enum ContentType {
    Sprite,
    Font
}

internal enum CompileAction {
    Copy,
}