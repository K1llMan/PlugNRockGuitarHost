namespace PlugNRockGuitarHost.Server.Commands
{
    public class StopProcessorCommand : BasicCommand<object, object>
    {
        public override string Type => "stopProcessor";

        public StopProcessorCommand(Func<object?, object?> action) : base(action)
        {
        }
    }
}