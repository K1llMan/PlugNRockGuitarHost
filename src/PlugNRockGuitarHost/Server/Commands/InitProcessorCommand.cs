namespace PlugNRockGuitarHost.Server.Commands
{
    public class InitProcessorCommand : BasicCommand<string, object>
    {
        public override string Type => "initProcessor";

        public InitProcessorCommand(Func<string?, object?> action) : base(action)
        {
        }
    }
}