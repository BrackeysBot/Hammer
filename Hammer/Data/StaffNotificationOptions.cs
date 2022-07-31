namespace Hammer.Data;

[Flags]
public enum StaffNotificationOptions
{
    None,
    Moderator = 1 << 0,
    Administrator = 1 << 1,
    Here = 1 << 2,
    Everyone = 1 << 3,
}
