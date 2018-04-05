namespace Albedo
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;

    public class TestModule : ModuleBase
    {
        [Command("echo"), Summary("Echoes a message.")]
        public async Task Echo([Remainder, Summary("The text to echo")] string echo)
        {
            await ReplyAsync(echo);
        }
    }
}