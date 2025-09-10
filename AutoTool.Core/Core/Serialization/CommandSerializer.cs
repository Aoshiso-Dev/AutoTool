using System.Text.Json;


namespace AutoTool.Core.Serialization;


public static class CommandSerializer
{
    public static async Task SaveAsync(IEnumerable<AutoTool.Core.Commands.IAutoToolCommand> root,
    Stream stream,
    AutoTool.Core.Descriptors.ICommandRegistry registry,
    JsonSerializerOptions? json = null,
    CancellationToken ct = default)
    {
        var mapper = new CommandMapper(registry, json);
        var dto = new ScriptDto
        {
            SchemaVersion = 1,
            Root = root.Select(mapper.ToDto).ToList()
        };
        await JsonSerializer.SerializeAsync(stream, dto, json ?? JsonOptions.Create(), ct);
    }


    public static async Task<List<AutoTool.Core.Commands.IAutoToolCommand>> LoadAsync(Stream stream,
    IServiceProvider services,
    AutoTool.Core.Descriptors.ICommandRegistry registry,
    JsonSerializerOptions? json = null,
    CancellationToken ct = default)
    {
        var mapper = new CommandMapper(registry, json);
        var dto = await JsonSerializer.DeserializeAsync<ScriptDto>(stream, json ?? JsonOptions.Create(), ct)
        ?? throw new InvalidOperationException("Failed to read script.");


        return dto.Root.Select(d => mapper.FromDto(d, services)).ToList();
    }
}