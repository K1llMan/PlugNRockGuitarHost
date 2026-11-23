using MessagePack;

namespace PlugNRockGuitarHost.Server.Models
{
    [MessagePackObject]
    public class CommandData
    {
        [Key("type")]
        public string Type { get; set; }
        [Key("parameters")]
        public string? Parameters { get; set; }

        public static CommandData Error(Exception ex)
        {
            return new CommandData {
                Type = "error",
                Parameters = ex.Message
            };
        }
    }
}