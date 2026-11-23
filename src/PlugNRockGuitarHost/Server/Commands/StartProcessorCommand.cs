namespace PlugNRockGuitarHost.Server.Commands
{
    public class StartProcessorCommand : BasicCommand<object, object>
    {
        public override string Type => "startProcessor";

        public StartProcessorCommand(Func<object?, object?> action) : base(action)
        {
        }
    }
}