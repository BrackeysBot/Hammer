# Context Menus & Reactions

Hammer makes use of modern Discord API features such as slash commands and context menus. Below is a list of the available context
menus. Each context menu is also available as a message react which serves as a failsafe in the event that a context menu action
fails to register or execute.

### âï¸ Message > Apps > Report Message

**Public action.** Allows members to report a message which may be rule-breaking. This prevents members spam-pinging the staff
team.
A report is sent to the log channel, and an `@here` ping is used. If more reports are received than the threshold defined in the
config, `@everyone` is pinged. Only one report per-user-per-message is acknowledged. The same user reporting the same message will
have no effect.

The default reaction equivalent is the ð© emoji (`:triangular_flag_on_post:`). The config key
is `GUILD_ID.reactions.reportReaction`.

### âï¸ Message > Apps > Delete Message

**Staff action.** Removes a message, and sends a private message to the author notifying them of the deletion, indicating that
their message violated server rules.

The default reaction equivalent is the ðï¸ emoji (`:wastebasket:`). The config key is `GUILD_ID.reactions.deleteMessageReaction`.

### ð¤ User > Apps > Gag

**Staff action.** Gags a user. This places a configurable timeout on the user (by default, this timeout is 5 minutes), allowing
staff to write a more formal and concrete infraction (warning, mute, ban, etc.)

The default reaction equivalent is the ðï¸emoji (`:mute:`). The config key is `GUILD_ID.reactions.gagReaction`.

### ð¤ User > Apps > View Infraction History

**Staff action.** Views the infraction history of a user. This is identical to the `/history` command, and the response is
therefore **NOT ephemeral**.

The default reaction equivalent is the ð emoji (`:clock4:`). The config key is `GUILD_ID.reactions.historyReaction`.k4: ). When
this reaction is used, the history is instead sent as a DM to the staff member.

# Slash Commands

Below is an outline of every slash command currently implemented in Hammer, along with their descriptions and parameters.

## v3 Migration

Upon first startup, it is recommended to perform migration of the v3 infraction database using the `/migrate` command.
This command starts a wizard which will walk you through the process of upgrading a v3 users.json file to the v4 database.

Infraction IDs are maintained where possible, but in the event that an ID is occupied, a new ID is generated.
For this reason, the best results come from performing migration on initial setup, and only once.

## Message Report Blocking

As mentioned above, users have the ability to report messages. However, this opens up the potential to be abused. If a user is
sending too many frivolous reports, their reports can be blocked so that their reports are no longer acknowledged.

### `/blockreports`

Prevent a user's message reports from being acknowledged.

| Parameter | Required | Type               | Description                      |
|:----------|:---------|:-------------------|:---------------------------------|
| user      | â Yes    | User mention or ID | The user whose reports to block. |

### `/unblockreports`

Unblocks a user, so that their message reports are acknowledged.

| Parameter | Required | Type               | Description                        |
|:----------|:---------|:-------------------|:-----------------------------------|
| user      | â Yes    | User mention or ID | The user whose reports to unblock. |

## Issuing and revoking infractions

### `/ban`

Temporarily, or permanently, bans a user from the guild. This creates an infraction on the user's record.

| Parameter | Required | Type               | Description                                                      |
|:----------|:---------|:-------------------|:-----------------------------------------------------------------|
| user      | â Yes    | User mention or ID | The user to ban.                                                 |
| reason    | â No     | String             | The reason for the ban.                                          |
| duration  | â No     | Duration           | The duration of the ban. If not specified, the ban is permanent. |
| rule      | â No     | Integer            | The rule which was broken.                                       |

### `/kick`

Kicks a member from the guild. This creates an infraction on the user's record.

| Parameter | Required | Type                 | Description                |
|:----------|:---------|:---------------------|:---------------------------|
| member    | â Yes    | Member mention or ID | The member to kick.        |
| reason    | â No     | String               | The reason for the kick.   |
| rule      | â No     | Integer              | The rule which was broken. |

### `/mute`

Temporarily, or permanently, mutes a user. This creates an infraction on the user's record.

| Parameter | Required | Type               | Description                                                        |
|:----------|:---------|:-------------------|:-------------------------------------------------------------------|
| user      | â Yes    | User mention or ID | The user to mute.                                                  |
| reason    | â No     | String             | The reason for the mute.                                           |
| duration  | â No     | Duration           | The duration of the mute. If not specified, the mute is permanent. |
| rule      | â No     | Integer            | The rule which was broken.                                         |

### `/unban`

Unbans a previously banned user.

| Parameter | Required | Type               | Description                        |
|:----------|:---------|:-------------------|:-----------------------------------|
| user      | â Yes    | User mention or ID | The user to unban.                 |
| reason    | â No     | String             | The reason for the ban revocation. |

### `/unmute`

Unmutes a previously muted user.

| Parameter | Required | Type               | Description                         |
|:----------|:---------|:-------------------|:------------------------------------|
| user      | â Yes    | User mention or ID | The user to unmute.                 |
| reason    | â No     | String             | The reason for the mute revocation. |

### `/warn`

Issues a warning to a user. This creates an infraction on the user's record.

| Parameter | Required | Type               | Description                 |
|:----------|:---------|:-------------------|:----------------------------|
| user      | â Yes    | User mention or ID | The user to warn.           |
| reason    | â Yes    | String             | The reason for the warning. |
| rule      | â No     | Integer            | The rule which was broken.  |

## Infraction management

### `/history`

Displays all infractions for a user.

| Parameter | Required | Type               | Description                         |
|:----------|:---------|:-------------------|:------------------------------------|
| user      | â Yes    | User mention or ID | The user whose infractions to view. |

### `/infraction clear`

Clears all infractions for a specified user.

| Parameter | Required | Type               | Description                          |
|:----------|:---------|:-------------------|:-------------------------------------|
| user      | â Yes    | User mention or ID | The user whose infractions to clear. |

### `/infraction copy`

Copies all infractions from a one user to another user. **This operation does not copy infraction IDs.**

| Parameter   | Required | Type               | Description                                |
|:------------|:---------|:-------------------|:-------------------------------------------|
| source      | â Yes    | User mention or ID | The user whose infractions to copy.        |
| destination | â Yes    | User mention or ID | The user who will receive the infractions. |

### `/infraction delete`

Deletes an infraction. **This process does NOT revoke the ban or mute associated with it, if any exist.**

| Parameter  | Required | Type    | Description                         |
|:-----------|:---------|:--------|:------------------------------------|
| infraction | â Yes    | Integer | The ID of the infraction to delete. |

### `/infraction move`

Moves all infractions from a one user to another user. **This operation maintains infraction IDs.**

| Parameter   | Required | Type               | Description                                |
|:------------|:---------|:-------------------|:-------------------------------------------|
| source      | â Yes    | User mention or ID | The user whose infractions to move.        |
| destination | â Yes    | User mention or ID | The user who will receive the infractions. |

### `/infraction prune`

Removes all infractions from the database for users which no longer exist. **â ï¸ This process is slow, use sparingly!**

| Parameter   | Required | Type               | Description                                |
|:------------|:---------|:-------------------|:-------------------------------------------|
| -           | -        | -                  | -                                          |

### `/infraction view`

View the details of a specific infraction.

| Parameter  | Required | Type    | Description                       |
|:-----------|:---------|:--------|:----------------------------------|
| infraction | â Yes    | Integer | The ID of the infraction to view. |

### `/selfhistory`

**Public command.** Displays all infractions for yourself.

| Parameter | Required | Type | Description |
|:----------|:---------|:-----|:------------|
| -         | -        | -    | -           |

## Soft moderation

### `/rule`

**Public command.** Displays the rule with the specified ID.

| Parameter | Required | Type    | Description                    |
|:----------|:---------|:--------|:-------------------------------|
| rule      | â Yes    | Integer | The ID of the rule to display. |

### `/message`

Sends a private message, through Hammer, to the member. This is logged, but does not create an infraction on the users' record.

| Parameter | Required | Type                 | Description            |
|:----------|:---------|:---------------------|:-----------------------|
| member    | â Yes    | Member mention or ID | The member to message. |
| content   | â Yes    | String               | The message to send.   |

### `/note create`

**Intended for Staff + Guru use.** Create a note on the user. The type of the note depends on the permission level of the
creator. If a Guru runs this command, a Guru note is created. If a Moderator or above runs the command, a Staff note is
created. **This operation does NOT notify the user.**

| Parameter | Required | Type               | Description                                |
|:----------|:---------|:-------------------|:-------------------------------------------|
| user      | â Yes    | User mention or ID | The user with whom the note is associated. |
| content   | â Yes    | String             | The content of the note.                   |

### `/note delete`

**Intended for Staff only use.** Deletes a note by its ID.

| Parameter | Required | Type    | Description                   |
|:----------|:---------|:--------|:------------------------------|
| note      | â Yes    | Integer | The ID of the note to delete. |

### `/note editcontent`

**Intended for Staff only use.** Edit the content of a note.

| Parameter | Required | Type    | Description                  |
|:----------|:---------|:--------|:-----------------------------|
| note      | â Yes    | Integer | The ID of the note to edit.  |
| content   | â Yes    | String  | The new content of the note. |

### `/note edittype`

**Intended for Staff only use.** Edit the type of a note (to switch between Staff and Guru notes).

| Parameter | Required | Type    | Description                  |
|:----------|:---------|:--------|:-----------------------------|
| note      | â Yes    | Integer | The ID of the note to edit.  |
| content   | â Yes    | String  | The new content of the note. |

### `/note view`

**Intended for Staff + Guru use.** View the details of a note. If a Guru runs this command, the note is only returned if it's a
Guru note.

| Parameter | Required | Type    | Description                 |
|:----------|:---------|:--------|:----------------------------|
| note      | â Yes    | Integer | The ID of the note to view. |

### `/note viewall`

**Intended for Staff + Guru use.** View all notes stored on a user. If a Guru runs this command, only Guru notes are visible.

| Parameter | Required | Type               | Description                   |
|:----------|:---------|:-------------------|:------------------------------|
| user      | â Yes    | User mention or ID | The user whose notes to view. |

## Rule management

### `/rules add`

Adds a new rule to the guild.

| Parameter | Required | Type | Description |
|:----------|:---------|:-----|:------------|
| -         | -        | -    | -           |

### `/rules delete`

Deletes a rule from the guild.

| Parameter | Required | Type    | Description                   |
|:----------|:---------|:--------|:------------------------------|
| rule      | â Yes    | Integer | The ID of the rule to delete. |

### `/rules display`

Sends an embed displaying the guild rules are they are currently defined to the current - or specified - channel.

| Parameter | Required | Type                  | Description                                                                                              |
|:----------|:---------|:----------------------|:---------------------------------------------------------------------------------------------------------|
| channel   | â No     | Channel mention or ID | The channel to which the rules will be sent. If not specified, the embed is sent to the current channel. |

### `/rules edit`

Modifies a rule.

| Parameter | Required | Type    | Description                   |
|:----------|:---------|:--------|:------------------------------|
| rule      | â Yes    | Integer | The ID of the rule to modify. |

# Ephemeral responses

Below is a table outlining all the commands and whether or not they have ephemeral responses.

| Command                 | Ephemeral Response                                |
|:------------------------|:--------------------------------------------------|
| `/ban`                  | â Yes                                             |
| `/blockreports`         | â Yes                                             |
| `/history`              | â No                                              |
| `/infraction clear`     | â No                                              |
| `/infraction copy`      | â No                                              |
| `/infraction delete`    | â No                                              |
| `/infraction edit`      | â No                                              |
| `/infraction move`      | â No                                              |
| `/infraction prune`     | â No                                              |
| `/infraction view`      | â No                                              |
| `/migrate`              | â No                                              |
| `/mute`                 | â Yes                                             |
| `/note create`          | â Yes                                             |
| `/note delete`          | â Yes                                             |
| `/note editcontent`     | â Yes                                             |
| `/note edittype`        | â Yes                                             |
| `/note view`            | â Yes                                             |
| `/note viewall`         | â Yes                                             |
| `/rule`                 | â No                                              |
| `/rules add`            | â ï¸ If guild isn't configured                      |
| `/rules delete`         | â ï¸ If guild isn't configured                      |
| `/rules display`        | â Interaction response is, resulting embed is not |
| `/rules setbrief`       | â ï¸ If guild isn't configured                      |
| `/rules setdescription` | â ï¸ If guild isn't configured                      |
| `/selfhistory`          | â Response sent as DM                             |
| `/unmute`               | â Yes                                             |
| `/unban`                | â Yes                                             |
| `/unblockreports`       | â Yes                                             |
| `/warn`                 | â Yes                                             |
