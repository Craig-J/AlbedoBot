namespace Albedo
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System.Threading.Tasks;
    
    public class CoreModule : ModuleBase
    {
        [Command("echo"), Summary("Echoes a message.")]
        public async Task Echo([Remainder, Summary("The text to echo")] string echo)
        {
            await ReplyAsync(echo);
        }

        [Command("userinfo"), Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")] IUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}