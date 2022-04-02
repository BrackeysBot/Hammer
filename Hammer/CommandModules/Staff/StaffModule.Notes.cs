using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BrackeysBot.API.Extensions;
using BrackeysBot.Core.API;
using BrackeysBot.Core.API.Attributes;
using BrackeysBot.Core.API.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Hammer.Data;
using Hammer.Resources;
using SmartFormat;

namespace Hammer.CommandModules.Staff;

internal sealed partial class StaffModule
{
    [Command("notes")]
    [Description("Retrieves the notes for a specified user.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task NotesCommandAsync(CommandContext context,
        [Description("The ID of the user whose notes to retrieve.")]
        ulong userId)
    {
        DiscordUser user = await context.Client.GetUserAsync(userId);

        if (user is null)
        {
            var embed = new DiscordEmbedBuilder();
            embed.WithColor(0xFF0000);
            embed.WithTitle(EmbedTitles.NoSuchUser);
            embed.WithDescription(EmbedMessages.NoSuchUser.FormatSmart(new {id = userId}));
            await context.RespondAsync(embed);
            return;
        }

        await NotesCommandAsync(context, user);
    }

    [Command("notes")]
    [Description("Retrieves the notes for a specified user.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task NotesCommandAsync(CommandContext context,
        [Description("The user whose notes to retrieve.")]
        DiscordUser user)
    {
        await context.AcknowledgeAsync();
        await context.TriggerTypingAsync();

        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);

        try
        {
            var builder = new StringBuilder();

            // guru can only retrieve guru notes
            IAsyncEnumerable<MemberNote> notes = context.Member.GetPermissionLevel(context.Guild) >= PermissionLevel.Moderator
                ? _memberNoteService.GetNotesAsync(user, context.Guild)
                : _memberNoteService.GetNotesAsync(user, context.Guild, MemberNoteType.Guru);

            await foreach (MemberNote note in notes)
            {
                if (note.GuildId != context.Guild.Id) continue;
                builder.AppendLine($"\u2022 [{note.Id}] {note.Content}");
            }

            if (builder.Length == 0)
                builder.AppendLine($"No notes saved for {user.Mention}");

            embed.WithAuthor(user);
            embed.WithTitle("Saved Notes");
            embed.WithDescription(builder.ToString().Trim());
            embed.AddField(EmbedFieldNames.User, user.Mention);
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to retrieve notes for {user}");
            embed.WithColor(0xFF0000);
            embed.WithTitle(exception.GetType().Name);
            embed.WithDescription(exception.Message);
            embed.WithFooter("See log for more details.");
        }

        await context.RespondAsync(embed);
    }
}
