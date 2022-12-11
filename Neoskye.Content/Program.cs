using Neoskye.Content;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

IDeserializer deser = new DeserializerBuilder()
    .WithNamingConvention(UnderscoredNamingConvention.Instance)
    .Build();

var cfg = deser.Deserialize<Dictionary<string, Configuration>>(
    File.ReadAllText($"{args[0]}/Content.yml")
);

// queue in all content
Dictionary<string, Configuration>
    LoadContentFromFolder(string root, DirectoryInfo directoryInfo, Dictionary<string, Configuration> compileCfg)
{
    var cfg = new Dictionary<string, Configuration>();

    foreach (var file in directoryInfo.GetFiles())
    {
        var fileName = $"{root}/{file.Name}";
        if (fileName.StartsWith("/"))
            cfg.Add(fileName.Substring(1), compileCfg[file.Extension.Substring(1)]);
        else
            cfg.Add(fileName, compileCfg[file.Extension.Substring(1)]);
    }

    foreach (DirectoryInfo dir in directoryInfo.GetDirectories())
        foreach (var p in LoadContentFromFolder(root + dir.Name, dir, compileCfg))
            cfg.Add(p.Key, p.Value);

    return cfg;
}

Dictionary<string, Configuration> content = LoadContentFromFolder("", new DirectoryInfo(args[1]), cfg);

foreach (KeyValuePair<string, Configuration> item in content)
{
    switch (item.Value.CompileAction)
    {
        case CompileAction.Copy:
            CompileActions.Copy(
                $"{args[1]}/{item.Key}",
                $"{args[2]}/{item.Value.ContentType}/{item.Key}"
            ).MapE(Console.Error.WriteLine);
            break;
    }
}