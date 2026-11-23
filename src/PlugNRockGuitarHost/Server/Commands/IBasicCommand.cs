namespace PlugNRockGuitarHost.Server.Commands
{
    public interface IBasicCommand
    {
        string Type { get; }
        object? Execute(object? input = null);
    }
}