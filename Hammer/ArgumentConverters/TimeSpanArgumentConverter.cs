using System;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Converters;
using DisCatSharp.Entities;

namespace Hammer.ArgumentConverters;

/// <summary>
///     Represents a converter which converts a command argument to <see cref="TimeSpan" />.
/// </summary>
internal sealed class TimeSpanArgumentConverter : IArgumentConverter<TimeSpan>
{
    /// <inheritdoc />
    public Task<Optional<TimeSpan>> ConvertAsync(string value, CommandContext ctx)
    {
        TimeSpan result = TimeSpan.Zero;
        var unitValue = 0;

        for (var index = 0; index < value.Length; index++)
        {
            char current = value[index];
            switch (current)
            {
                case var digitChar when char.IsDigit(digitChar):
                    var digit = (int) char.GetNumericValue(digitChar);
                    unitValue = unitValue * 10 + digit;
                    break;

                case 'y':
                    result += TimeSpan.FromDays(unitValue * 365);
                    unitValue = 0;
                    break;

                case 'm':
                    if (index < value.Length - 1 && value[index + 1] == 'o')
                    {
                        index++;
                        result += TimeSpan.FromDays(unitValue * 30);
                    }
                    else
                    {
                        result += TimeSpan.FromMinutes(unitValue);
                    }

                    unitValue = 0;
                    break;

                case 'w':
                    result += TimeSpan.FromDays(unitValue * 7);
                    unitValue = 0;
                    break;

                case 'd':
                    result += TimeSpan.FromDays(unitValue);
                    unitValue = 0;
                    break;

                case 'h':
                    result += TimeSpan.FromHours(unitValue);
                    unitValue = 0;
                    break;

                case 's':
                    result += TimeSpan.FromSeconds(unitValue);
                    unitValue = 0;
                    break;
            }
        }

        return Task.FromResult<Optional<TimeSpan>>(result);
    }
}
