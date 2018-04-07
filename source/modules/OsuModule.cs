namespace Albedo
{
    using Discord;
    using Discord.Commands;
    using OsuSharp;
    using OsuSharp.BeatmapsEndpoint;
    using OsuSharp.Entities;
    using OsuSharp.UserBestEndpoint;
    using OsuSharp.UserEndpoint;
    using OsuSharp.UserRecentEndpoint;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class OsuModule : ModuleBase
    {
        private OsuApi _api;

        public OsuModule(OsuApi api)
        {
            _api = api;
        }

        private static string GetUserpageFromUserId(long userId) => $"https://osu.ppy.sh/u/{userId}";
        private static string GetAvatarUrlFromUserId(long userId) => $"https://a.ppy.sh/{userId}";
        private static string GetBeatmapUrlFromBeatmap(Beatmap beatmap) => $"https://osu.ppy.sh/b/{beatmap.BeatmapId}";
        private static string GetBeatmapApiUrlFromBeatmap(Beatmap beatmap) => $"https://osu.ppy.sh/osu/{beatmap.BeatmapId}";
        

        private static OppaiSharp.Beatmap GetOppaiBeatmapFromUrl(string url)
        {
            var data = new WebClient().DownloadData(url);
            var stream = new MemoryStream(data, false);
            var reader = new StreamReader(stream);
            return OppaiSharp.Beatmap.Read(reader);
        }

        [Command("recent"), Summary("Shows last play by requested user.")]
        [Alias("last")]
        public async Task Recent([Remainder] string username)
        {
            await Recent(1, username);
        }

        [Command("recent"), Summary("Shows last play by requested user.")]
        [Alias("last")]
        public async Task Recent(int count, [Remainder] string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                await ReplyAsync("Username is null/empty, command usage is \"a.recent <count=1> [username]\"");
                return;
            }
            var user = await _api.GetUserByNameAsync(username);
            if (user == null) return;
            var recentScores = await _api.GetUserRecentAndBeatmapByUsernameAsync(username, limit: 1);
            if (recentScores.Count < 1) return;
            if (count == 1)
            {
                await ReplyAsync($"**Most recent play for {user.Username}:**", embed: CreateSinglePlayEmbed(user, recentScores[0].UserRecent, recentScores[0].Beatmap));
            }
            else
            {
                await ReplyAsync($"**Most recent plays for {user.Username}:**", embed: CreateMultiplePlayEmbed(user, recentScores));
            }
        }

        [Command("top"), Summary("Shows top play by requested user.")]
        public async Task Top(int count, [Remainder] string username = "")
        {
            var topScores = await _api.GetUserBestAndBeatmapByUsernameAsync(username, limit: count);
            var user = await _api.GetUserByNameAsync(username);
            if (topScores.Count < 1) return;
            if (count == 1)
            {
                await ReplyAsync($"**Top play for {user.Username}:**", embed: CreateSinglePlayEmbed(user, topScores[0].UserBest, topScores[0].Beatmap));
            }
            else
            {
                await ReplyAsync($"**Most recent plays for {user.Username}:**", embed: CreateMultiplePlayEmbed(user, topScores));
            }
        }

        private static Embed CreateSinglePlayEmbed(User user, UserRecent score, Beatmap beatmap)
        {
            var oppaiBeatmap = GetOppaiBeatmapFromUrl(GetBeatmapApiUrlFromBeatmap(beatmap));
            var mods = (OppaiSharp.Mods)score.Mods;
            var diffCalc = new OppaiSharp.DiffCalc().Calc(oppaiBeatmap, mods);
            var playPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, score.Count50, score.Miss, score.MaxCombo.Value, score.Count300, mods));
            var fcPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, 0, c300: score.Count300 + score.Miss, mods: mods));
            EmbedBuilder embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.Username,
                    Url = GetUserpageFromUserId(user.Userid),
                    IconUrl = GetAvatarUrlFromUserId(user.Userid)
                },
                Title = $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] +{score.Mods} [{beatmap.DifficultyRating:F2}⚝]",
                Url = GetBeatmapUrlFromBeatmap(beatmap),
                ThumbnailUrl = beatmap.ThumbnailUrl,
                Color = Color.Purple,
                Description = $"◉ **{score.Rank} Rank** ◉ **{playPP.Total:F2}PP** ({fcPP.Total:F2} for {fcPP.ComputedAccuracy.Value() * 100:F2}% FC) ◉ {score.Accuracy:F2}% ◉ {score.ScorePoints} ◉ x{score.MaxCombo}/{beatmap.MaxCombo} ◉ [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]"
            };
            return embed;
        }

        private static Embed CreateMultiplePlayEmbed(User user, IList<UserRecentBeatmap> pairs)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.Username,
                    Url = GetUserpageFromUserId(user.Userid),
                    IconUrl = GetAvatarUrlFromUserId(user.Userid)
                },
                ThumbnailUrl = GetAvatarUrlFromUserId(user.Userid),
                Color = Color.Purple
            };
            var i = 1;
            foreach(var pair in pairs)
            {
                var score = pair.UserRecent;
                var beatmap = pair.Beatmap;
                var oppaiBeatmap = GetOppaiBeatmapFromUrl(GetBeatmapApiUrlFromBeatmap(beatmap));
                var mods = (OppaiSharp.Mods)score.Mods;
                var diffCalc = new OppaiSharp.DiffCalc().Calc(oppaiBeatmap, mods);
                var playPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, score.Count50, score.Miss, score.MaxCombo.Value, score.Count300, mods));
                var fcPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, 0, c300: score.Count300 + score.Miss, mods: mods));
                AddScoreFieldToEmbed(embed, i, beatmap, mods, score.Rank, playPP.Total, fcPP.Total, fcPP.ComputedAccuracy.Value() * 100d, score.Accuracy, score.MaxCombo.GetValueOrDefault(), beatmap.MaxCombo.GetValueOrDefault(), score.Count300, score.Count100, score.Count50, score.Miss);
                i++;
            }
            return embed;
        }

        private static Embed CreateSinglePlayEmbed(User user, UserBest score, Beatmap beatmap)
        {
            var oppaiBeatmap = GetOppaiBeatmapFromUrl(GetBeatmapApiUrlFromBeatmap(beatmap));
            var mods = (OppaiSharp.Mods)score.Mods;
            var diffCalc = new OppaiSharp.DiffCalc().Calc(oppaiBeatmap, mods);
            var fcPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, 0, c300: score.Count300 + score.Miss, mods: mods));
            EmbedBuilder embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.Username,
                    Url = GetUserpageFromUserId(user.Userid),
                    IconUrl = GetAvatarUrlFromUserId(user.Userid)
                },
                Title = $"{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] +{score.Mods} [{beatmap.DifficultyRating:F2}⚝]",
                Url = GetBeatmapUrlFromBeatmap(beatmap),
                ThumbnailUrl = beatmap.ThumbnailUrl,
                Color = Color.Purple,
                Description = $"◉ **{score.Rank} Rank** ◉ **{score.Pp:F2}PP** ({fcPP.Total:F2} for {fcPP.ComputedAccuracy.Value() * 100:F2}% FC) ◉ {score.Accuracy:F2}% ◉ {score.ScorePoints} ◉ x{score.MaxCombo}/{beatmap.MaxCombo} ◉ [{score.Count300}/{score.Count100}/{score.Count50}/{score.Miss}]"
            };
            return embed;
        }

        private static Embed CreateMultiplePlayEmbed(User user, IList<UserBestBeatmap> pairs)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    Name = user.Username,
                    Url = GetUserpageFromUserId(user.Userid),
                    IconUrl = GetAvatarUrlFromUserId(user.Userid)
                },
                ThumbnailUrl = GetAvatarUrlFromUserId(user.Userid),
                Color = Color.Purple
            };
            var i = 1;
            foreach (var pair in pairs)
            {
                var score = pair.UserBest;
                var beatmap = pair.Beatmap;
                var oppaiBeatmap = GetOppaiBeatmapFromUrl(GetBeatmapApiUrlFromBeatmap(beatmap));
                var mods = (OppaiSharp.Mods)score.Mods;
                var diffCalc = new OppaiSharp.DiffCalc().Calc(oppaiBeatmap, mods);
                var fcPP = new OppaiSharp.PPv2(new OppaiSharp.PPv2Parameters(oppaiBeatmap, diffCalc, score.Count100, 0, c300: score.Count300 + score.Miss, mods: mods));
                AddScoreFieldToEmbed(embed, i, beatmap, mods, score.Rank, score.Pp, fcPP.Total, fcPP.ComputedAccuracy.Value() * 100d, score.Accuracy, score.MaxCombo.GetValueOrDefault(), beatmap.MaxCombo.GetValueOrDefault(), score.Count300, score.Count100, score.Count50, score.Miss);
                i++;
            }
            return embed;
        }

        private static void AddScoreFieldToEmbed(EmbedBuilder embed, int scoreIndex, Beatmap beatmap, OppaiSharp.Mods mods, string rank, double pp, double fcPP, double fcAcc, double acc, int combo, int maxCombo, int c300, int c100, int c50, int misses)
        {
            embed.AddField($"**Score {scoreIndex}:**",
                   $"[{beatmap.Artist} - {beatmap.Title} [{beatmap.Difficulty}] +{mods} [{beatmap.DifficultyRating:F2}⚝]]({GetBeatmapUrlFromBeatmap(beatmap)})");
            embed.AddField("Details:", $"◉ **{rank} Rank** ◉ **{pp:F2}PP** ({fcPP:F2} for {fcAcc:F2}% FC) ◉ {acc:F2}% ◉ x{combo}/{maxCombo} ◉ [{c300}/{c100}/{c50}/{misses}]");
        }
    }
}