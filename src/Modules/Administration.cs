﻿using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using DEA.Database.Repositories;
using System.Linq;
using MongoDB.Bson;
using DEA.Common;
using DEA.Common.Preconditions;
using DEA.Common.Extensions;
using DEA.Common.Extensions.DiscordExtensions;

namespace DEA.Modules
{
    [Require(Attributes.Admin)]
    public class Administration : DEAModule
    {
        private readonly GuildRepository _guildRepo;

        public Administration(GuildRepository guildRepo)
        {
            _guildRepo = guildRepo;
        }

        [Command("RoleIDs")]
        [Summary("Gets the ID of all roles in the guild.")]
        public async Task RoleIDs()
        {
            string message = null;
            foreach (var role in Context.Guild.Roles)
                message += $"{role.Name}: {role.Id}\n";

            var channel = await Context.User.CreateDMChannelAsync();
            await channel.SendAsync(message);

            await ReplyAsync("All Role IDs have been DMed to you.");
        }

        [Command("SetPrefix")]
        [Summary("Sets the guild specific prefix.")]
        public async Task SetPrefix(string prefix)
        {
            if (prefix.Length > 3)
                await ErrorAsync("The maximum character length of a prefix is 3.");

            await _guildRepo.ModifyAsync(Context.Guild.Id, x => x.Prefix, prefix);

            await ReplyAsync($"You have successfully set the prefix to {prefix}.");
        }

        [Command("SetMutedRole")]
        [Alias("SetMuteRole")]
        [Summary("Sets the muted role.")]
        public async Task SetMutedRole([Remainder] IRole mutedRole)
        {
            if (mutedRole.Position > Context.Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First().Position)
                await ErrorAsync($"DEA must be higher in the heigharhy than {mutedRole.Mention}.");

            await _guildRepo.ModifyAsync(Context.Guild.Id, x => (decimal)x.MutedRoleId, (decimal)mutedRole.Id);

            await ReplyAsync($"You have successfully set the muted role to {mutedRole.Mention}.");
        }

        [Command("AddRank")]
        [Summary("Adds a rank role for the DEA cash system.")]
        public async Task AddRank(IRole rankRole, double cashRequired = 500)
        {
            if (rankRole.Position > Context.Guild.CurrentUser.Roles.OrderByDescending(x => x.Position).First().Position)
                await ErrorAsync($"DEA must be higher in the heigharhy than {rankRole.Mention}.");

            if (Context.DbGuild.RankRoles.ElementCount == 0)
                await _guildRepo.ModifyAsync(Context.Guild.Id, x => x.RankRoles, new BsonDocument()
                {
                    { rankRole.Id.ToString(), cashRequired }
                });
            else
            {
                if (Context.DbGuild.RankRoles.Any(x => x.Name == rankRole.Id.ToString()))
                    await ErrorAsync("This role is already a rank role.");
                if (cashRequired == 500)
                    cashRequired = Context.DbGuild.RankRoles.OrderByDescending(x => x.Value).First().Value.AsDouble * 2;
                if (Context.DbGuild.RankRoles.Any(x => (int)x.Value.AsDouble == (int)cashRequired))
                    await ErrorAsync("There is already a role set to that amount of cash required.");

                Context.DbGuild.RankRoles.Add(rankRole.Id.ToString(), cashRequired);

                await _guildRepo.ModifyAsync(Context.Guild.Id, x => x.RankRoles, Context.DbGuild.RankRoles);
            }
            
            await ReplyAsync($"You have successfully added the {rankRole.Mention} rank.");
        }

        [Command("RemoveRank")]
        [Summary("Removes a rank role for the DEA cash system.")]
        public async Task RemoveRank([Remainder] IRole rankRole)
        {
            if (Context.DbGuild.RankRoles.ElementCount == 0)
                await ErrorAsync("There are no ranks yet.");
            if (!Context.DbGuild.RankRoles.Any(x => x.Name == rankRole.Id.ToString()))
                await ErrorAsync("This role is not a rank role.");

            Context.DbGuild.RankRoles.Remove(rankRole.Id.ToString());

            await _guildRepo.ModifyAsync(Context.Guild.Id, x => x.RankRoles, Context.DbGuild.RankRoles);

            await ReplyAsync($"You have successfully removed the {rankRole.Mention} rank.");
        }

        [Command("ModifyRank")]
        [Summary("Modfies a rank role for the DEA cash system.")]
        public async Task ModifyRank(IRole rankRole, double newCashRequired)
        {
            if (Context.DbGuild.RankRoles.ElementCount == 0)
                await ErrorAsync("There are no ranks yet.");
            if (!Context.DbGuild.RankRoles.Any(x => x.Name == rankRole.Id.ToString()))
                await ErrorAsync("This role is not a rank role.");
            if (Context.DbGuild.RankRoles.Any(x => (int)x.Value.AsDouble == (int)newCashRequired))
                await ErrorAsync("There is already a role set to that amount of cash required.");

            Context.DbGuild.RankRoles[rankRole.Id.ToString()] = newCashRequired;
            await _guildRepo.ModifyAsync(Context.Guild.Id, x => x.RankRoles, Context.DbGuild.RankRoles);

            await ReplyAsync($"You have successfully set the cash required for the {rankRole.Mention} rank to {((decimal)newCashRequired).USD()}.");
        }

        [Command("SetModLog")]
        [Summary("Sets the moderation log.")]
        public async Task SetModLogChannel([Remainder] ITextChannel modLogChannel)
        {
            await _guildRepo.ModifyAsync(Context.Guild.Id, x => (decimal)x.ModLogId, (decimal)modLogChannel.Id);
            await ReplyAsync($"You have successfully set the moderator log channel to {modLogChannel.Mention}.");
        }

        [Command("SetGambleChannel")]
        [Alias("SetGamble")]
        [Summary("Sets the gambling channel.")]
        public async Task SetGambleChannel([Remainder] ITextChannel gambleChannel)
        {
            await _guildRepo.ModifyAsync(Context.Guild.Id, x => (decimal)x.GambleId, (decimal)gambleChannel.Id);
            await ReplyAsync($"You have successfully set the gamble channel to {gambleChannel.Mention}.");
        }

    }
}
