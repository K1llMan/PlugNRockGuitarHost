namespace PlugNRockGuitarHost.Server.Commands
{
    public class GetDriversCommand : BasicCommand<object, string[]>
    {
        public override string Type => "drivers";

        public GetDriversCommand(Func<object?, string[]?> action) : base(action)
        {
        }
    }
}