namespace Albedo
{
    using Discord;
    using Discord.Commands;
    using Discord.WebSocket;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class ResponseModule : ModuleBase
    {
        //IDictionary<string, IList<string>> _responses = new Dictionary<string, IList<string>>();

        //[Command("respond"), Summary("Adds a string which Albedo will respond to with the specified response.")]
        //public async Task Response(string )

        IList<string> _badWordList = new List<string>();

        public ResponseModule(DiscordSocketClient client)
        {
            client.MessageReceived += CheckMessageForBadWord;
        }

        private async Task CheckMessageForBadWord(SocketMessage msg)
        {
            var usermsg = msg as SocketUserMessage;
            Console.WriteLine($"Checking message \"{msg.Content}\" by {msg.Author} for bad word.");
            if (usermsg == null)
            {
                Console.WriteLine("Failed usermsg cast.");
                return;
            }
            foreach (var badWord in _badWordList)
            {
                Console.WriteLine($"Checking against {badWord}.");
                if (usermsg.Content.Contains(badWord))
                {
                    Console.WriteLine($"Contains {badWord}.");
                    try
                    {
                        if (!Emote.TryParse("<:ABABABABA:431588021234696202>", out Emote emote))
                            Console.WriteLine("Failed to parse emote.");
                        else
                            Console.WriteLine(emote);
                        await usermsg.AddReactionAsync(emote, new RequestOptions() { Timeout = 1000 });
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    continue;
                }
                Console.WriteLine($"Does not contain {badWord}.");
            }
            await Task.CompletedTask;
        }


        [RequireOwner(Group = "A"),
            RequireUserPermission(GuildPermission.Administrator, Group = "A")]
        [Command("badword"), Summary("Adds a bad word that Albedo will react to.")]
        public async Task AddBadWord([Remainder, Summary("Text to search for.")] string text)
        {
            _badWordList.Add(text);
            await ReplyAsync($"Added \"{text}\" to the bad word list.");
        }
    }
}