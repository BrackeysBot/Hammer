# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Fixed
- Fixed a bug with rule autocompletion (hopefully for the last time).
- Fixed parsing of /gag duration.

### Changed
- Temporary ban DM no longer contains ModMail notice.

### Removed
- Removed `/migrate` command.

## [5.6.1] - 2023-07-24

### Added
- Added link to the bot source code in the `/info` command.

## [5.6.0] - 2023-07-22

### Added
- Infractions now store the rule text to avoid breaking changes to server rule list.

## [5.5.2] - 2023-06-09

### Added
- Added a notice about infractions on alternate accounts

## [5.5.1] - 2023-06-09

### Fixed
- Fixed an issue with the alt log embed mentioning the wrong user
- Fixed an issue where the author of the alt embed was linked, rather than icon url'd

## [5.5.0] - 2023-06-09

### Added
- Added `/alt` command

## [5.4.3] - 2023-06-05

### Added
- Added support for new usernames (#75)

## [5.4.2] - 2023-06-02

### Fixed
- Fixed a bug with rule autocompletion (again)

## [5.4.1] - 2023-06-01

### Fixed
- Fixed a bug with rule autocompletion

## [5.4.0] - 2023-06-01

### Added
- Added better support for rule autocompletion

## [5.3.1] - 2023-04-30

### Fixed
- `/staffhistory` infractions now order by date descending
- Numeric totals are now formatted with commas

## [5.3.0] - 2023-04-30

### Added
- Added `/staffhistory` command

## [5.2.4] - 2023-04-12

### Fixed
- Fixed NRE being thrown when embed content is null

## [5.2.3] - 2023-03-04

### Fixed
- Fixed /messagehistory failing to enumerate message history due to SQLite not supporting OrderBy for DateTimeOffset.

## [5.2.2] - 2023-03-04

### Changed
- /messagehistory command now lists messages in reverse-chronological order, to match /history.

## [5.2.1] - 2023-03-04

### Fixed
- Fixed an issue with /mute always locking duration to max specified. (#70)

## [5.2.0] - 2023-03-25

### Fixed

- `clearMessageHistory` is now honoured when `/ban` is issued against a user. (#62)

### Changed

- Slight performance increase for some infraction operations.
- Report enumerations are no longer async. This should mildly (though negligibly) improve performance as we no longer allocate an
  async state machine.
- Services now inject an `ILogger` rather than having a static NLog `Logger` in the class.

## [5.1.1] - 2023-03-21

### Fixed

- Fixed an issue where excessive empty embeds were sent when `/history` search query filtered more than 10 infractions.

## [5.1.0] - 2023-03-21

### Added

- `/history` now allows filtering to before/after date or ID, as well as by type.

## [5.0.4] - 2023-03-11

### Fixed

- Fixed the numeric formatting for the ban and mute ratio output.

### Changed

- Guild branding now uses the PNG icon of the server rather than GIF, which should massively improve performance for all users.

## [5.0.3] - 2023-03-08

### Changed

- Permanent:Temporary infraction ratio is now simplified to 1:x or x:1, depending on which is greater.

## [5.0.2] - 2023-03-08

### Changed

- Infraction statistics are now handled by a dedicated service, which allows for code reuse. This might negligibly improve
  performance.

## [5.0.1] - 2023-03-08

### Fixed

- Fixed an issue where remaining durations for statistics embed were incorrectly calculated.

### Changed

- Reworded statistics embed to read "Remaining" duration rather than "Total" duration.

## [5.0.0] - 2023-03-08

### Added

- Added `/infraction stats` command which displays infraction statistics for the current guild. (#55)
- Added support to potentially filter infractions by type.

### Changed

- The bot now warns when attempting to ban a user that's already banned. This potentially fixes #61.
- Optimized searching when `clearHistory` flag is set when temporarily banning.

### Removed

- The bot no longer fetches its profile picture from the repository as introduced in 3.2.0. (#67)

## [4.1.0] - 2023-03-07

### Fixed

- Fixed an issue where the bot would try to fetch bans from a non-existent database, resulting in a failure to clean install. (
  #65)

## [4.0.1] - 2023-02-19

### Fixed

- Fixed an issue where staff members were not being logged for deleted messages.

## [4.0.0] - 2023-02-06

### Added

- Added the ability to search for rules using the `/rule` command.

### Removed

- Removed autocomplete for the `/rule` command. This was necessary to support custom search queries.

## [3.2.0] - 2022-12-25

### Added

- The bot now sets its profile picture to the icon.png in the repository root.

## [3.1.1] - 2022-12-25

### Fixed

- Fixed an issue where attachment URIs were not correctly read from the database provider.

## [3.1.0] - 2022-12-21

### Fixed

- Fixed an issue where temporary infraction durations were poorly parsed. (#54)
- Fixed an issue where admins could not temporarily mute people. (#64)

### Changed

- docker-compose no longer uses env_file.
- docker-compose now binds to preset system paths.

## [3.0.3] - 2022-12-02

### Changed

- Message reports are now purged for channels which get deleted.
- docker-compose now uses `build` pull policy.

## [3.0.2] - 2022-11-18

### Added

- Added the now-required Message Content intent to read message contents.
  See [here](https://support-dev.discord.com/hc/en-us/articles/4404772028055-Message-Content-Privileged-Intent-FAQ) for more
  details.

## [3.0.1] - 2022-11-17

### Added

- Clearing, copying, deleting, editing, and moving infractions, are now audit logged.

### Fixed

- Fixed an issue where an exception was thrown if a moderator did not specify the duration for a mute. The mute duration is now
  correctly clamped. (#63)

### Changed

- When copying or moving infractions, the "To" field is now populated with the user's mention rather than their ID. (#47)

## [3.0.0] - 2022-11-15

### Changed

- `/message` now triggers a modal to write the message.

### Fixed

- Fixed an issue where the message object itself, rather than its content, was written to the embed.

## [2.7.0] - 2022-11-05

### Added

- Added `/messagehistory` command
- Added `/viewmessage` command

## [2.6.2] - 2022-09-25

### Changed

- "View Infraction History" now responds ephemerally when triggered from the context menu.

## [2.6.1] - 2022-09-25

### Fixed

- Fixed an issue where temporary bans were not loaded from the database on startup. (#60)

## [2.6.0] - 2022-09-25

### Added

- Added `/viewreports` and `/viewsupportedreports` so that reports made by (and against) users can be listed. (#58)

### Fixed

- Fixed an issue where the message report cache was not being loaded, causing a failure of report listing.

### Removed

- Removed the guild branding from staff message embed, which was causing lag for mobile staff members. (#57)

## [2.5.3] - 2022-08-17

No substantial changes. Commit 3b8259a6cfb82ec0f5f51804c1ac7f1f5880d014 fixed an incorrect version bump.

## [2.5.2] - 2022-08-17

### Added

- Kick embed now displays the infraction ID. (#46)

### Fixed

- Fixed an issue where the bot could not delete messages with a content length greater than 1024. This is the maximum length an
  embed Description can be. (#45)
- Fixed an issue where the specified reason was not displaying in the kick embed response. (#48)

## [2.5.1] - 2022-08-10

### Fixed

- Fixed an issue with the bot attempting to clear 0 messages.

## [2.5.0] - 2022-08-10

### Added

- Added the option to clear message history of a user when banning or kicking them. (#44)

## [2.4.1] - 2022-08-05

### Fixed

- Fixed a grammatical mistake. "Infraction" is displayed instead of "Infractions" when there's only 1 infraction. (#42)

### Removed

- Removed the `AllowInteractionAuthorDeletion` configuration key. It was unused.

## [2.4.0] - 2022-08-04

### Added

- When deleting a message that is the response of a bot interaction, the interactor (not just the bot) is now captured in the
  details embed. (#39)
- The "rule broken" property of an infraction is now displayed in the log embed. (#41)

### Fixed

- Fixed an issue where member rank was incorrectly determined. (#40)

### Removed

- Staff members can no longer delete their own messages.

## [2.3.0] - 2022-08-03

### Added

- Temporary infraction are now notified with a duration when sent to the user. (#34)

### Fixed

- Fixed an issue where re-muting an already-muted user would throw an exception. (#37)
- Fixed an issue where members would not be sent a DM notifying them that they were kicked or banned.

## [2.2.3] - 2022-08-03

### Fixed

- Fixed an issue that prevented messages from being deleted if the author had no roles.

## [2.2.2] - 2022-08-03

### Added

- Added an error response if a message could not be deleted.

### Changed

- The "delete message" reaction is now deleted by the bot, before deleting the message. This serves as feedback; if the bot reacts
  but the message is not deleted, we know the event was received.

## [2.2.1] - 2022-08-03

### Fixed

- Fixed an issue that prevented messages from being deleted if the author had left the guild.

## [2.2.0] - 2022-08-02

### Changed

- `/rules add` and `/rules edit` now present a modal for entering rule text.

## [2.1.0] - 2022-08-02

### Added

- Added an `/info` command which shows latency and version information.

## [2.0.1] - 2022-08-01

### Changed

- Infraction history are no longer paginated. Multiple embeds are now sent.

## [2.0.0] - 2022-08-01

### Added

- Infractions now have an "Additional Information" property, which specifies the duration of temporary infractions.
- Support for partial migrations (overwriting infraction details without creating new ones), to copy over legacy "Additional
  Information".

### Fixed

- LoggingService is now registered first.
- Modifying an infraction now updates the hot cache .

### Changed

- `Type` field in infraction embed is now rendered in a more readable form.

## [1.0.4] - 2022-07-31

### Fixed

- `/pruneinfractions` is now actually registered.

## [1.0.3] - 2022-07-31

### Fixed

- IDs in `/selfhistory` embed are now correctly sequential. (#33)

### Changed

- `/infraction prune` has been renamed to `/pruneinfractions`. (#35)

## [1.0.2] - 2022-07-31

### Added

- `/infraction view` embed now display a timestamp.

## [1.0.1] - 2022-07-31

### Added

- Log embeds now display a timestamp.

## [1.0.0] - 2022-07-31

### Added

- Hammer is released.

[5.6.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.6.1
[5.6.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.6.0
[5.5.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.5.2
[5.5.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.5.1
[5.5.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.5.0
[5.4.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.4.3
[5.4.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.4.2
[5.4.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.4.1
[5.4.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.4.0
[5.3.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.3.1
[5.3.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.3.0
[5.2.4]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.2.4
[5.2.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.2.3
[5.2.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.2.2
[5.2.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.2.1
[5.2.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.2.0
[5.1.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.1.1
[5.1.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.1.0
[5.0.4]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.0.4
[5.0.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.0.3
[5.0.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.0.2
[5.0.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.0.1
[5.0.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v5.0.0
[4.1.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v4.1.0
[4.0.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v4.0.1
[4.0.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v4.0.0
[3.2.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.2.0
[3.1.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.1.1
[3.1.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.1.0
[3.0.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.0.3
[3.0.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.0.2
[3.0.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.0.1
[3.0.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v3.0.0
[2.7.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.7.0
[2.6.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.6.2
[2.6.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.6.1
[2.6.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.6.0
[2.5.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.5.3
[2.5.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.5.2
[2.5.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.5.1
[2.5.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.5.0
[2.4.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.4.1
[2.4.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.4.0
[2.3.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.3.0
[2.2.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.2.3
[2.2.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.2.2
[2.2.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.2.1
[2.2.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.2.0
[2.1.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.1.0
[2.0.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.0.1
[2.0.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v2.0.0
[1.0.4]: https://github.com/BrackeysBot/Hammer/releases/tag/v1.0.4
[1.0.3]: https://github.com/BrackeysBot/Hammer/releases/tag/v1.0.3
[1.0.2]: https://github.com/BrackeysBot/Hammer/releases/tag/v1.0.2
[1.0.1]: https://github.com/BrackeysBot/Hammer/releases/tag/v1.0.1
[1.0.0]: https://github.com/BrackeysBot/Hammer/releases/tag/v1.0.0
