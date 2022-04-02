using System;
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
    [Command("note")]
    [Description("Creates a note for a specified user.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task CreateNoteCommandAsync(CommandContext context,
        [Description("The ID of the user to whom this note applies.")]
        ulong userId,
        [Description("The content of the note.")] [RemainingText]
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            // RemainingText seems to capture empty arguments too. so if no string is specified,
            // assume we wish to create a new note
            await ViewNoteCommandAsync(context, (long) userId);
            return;
        }

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

        await CreateNoteCommandAsync(context, user, content);
    }

    [Command("note")]
    [Description("Creates a note for a specified user.")]
    [RequirePermissionLevel(PermissionLevel.Guru)]
    public async Task CreateNoteCommandAsync(CommandContext context,
        [Description("The user to whom this note applies.")]
        DiscordUser user,
        [Description("The content of the note.")] [RemainingText]
        string content)
    {
        await context.AcknowledgeAsync();
        await context.TriggerTypingAsync();

        DiscordEmbedBuilder embed = context.Guild.CreateDefaultEmbed(false);

        try
        {
            MemberNote note = await _memberNoteService.CreateNoteAsync(user, context.Member, content);

            embed.WithColor(0x4CAF50);
            embed.WithTitle("Note Created");
            embed.WithDescription($"Successfully created note for {user.Mention}");
            embed.WithFooter($"Note #{note.Id}");
        }
        catch (Exception exception)
        {
            Logger.Error(exception, $"An exception was thrown when attempting to save a note for {user} (\"{content}\")");

            embed.WithColor(0xFF0000);
            embed.WithTitle(exception.GetType().Name);
            embed.WithDescription(exception.Message);
            embed.WithFooter("See log for more details.");
        }

        await context.RespondAsync(embed);
    }
}
