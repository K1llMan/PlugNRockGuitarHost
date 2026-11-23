namespace PlugNRockGuitarHost.Server.Commands
{
    public class SetVolumeBalanceCommand : BasicCommand<float?, object?>
    {
        public override string Type => "setVolumeBalance";

        public SetVolumeBalanceCommand(Func<float?, object?> action) : base(action)
        {
        }
    }
}