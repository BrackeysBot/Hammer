using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Hammer.Data.ValueConverters;

/// <summary>
///     Converts a <see cref="List{T}" /> of <see cref="Uri" /> values to and from an array of bytes.
/// </summary>
internal sealed class UriListToBytesConverter : ValueConverter<IReadOnlyList<Uri>, byte[]>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="UriListToBytesConverter" /> class.
    /// </summary>
    public UriListToBytesConverter()
        : this(null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="UriListToBytesConverter" /> class.
    /// </summary>
    public UriListToBytesConverter(ConverterMappingHints? mappingHints)
        : base(v => ToBytes(v), v => FromBytes(v), mappingHints)
    {
    }

    private static byte[] ToBytes(IReadOnlyList<Uri> list)
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write7BitEncodedInt(list.Count);

        for (var index = 0; index < list.Count; index++)
        {
            var uri = list[index].ToString();
            writer.Write(uri);
        }

        return stream.ToArray();
    }

    private static IReadOnlyList<Uri> FromBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        using var reader = new BinaryReader(stream);
        int listCount = reader.Read7BitEncodedInt();

        var list = new List<Uri>(listCount);

        for (var index = 0; index < list.Count; index++) list.Add(new Uri(reader.ReadString()));

        return list.AsReadOnly();
    }
}
