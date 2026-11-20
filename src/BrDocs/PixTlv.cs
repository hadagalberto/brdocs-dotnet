namespace BrDocs;

/// <summary>
/// One BR Code data object: a TLV (tag-length-value) node — a two-digit ID, a two-digit length
/// and the value — with support for the nested templates of the EMV® QRCPS-MPM structure the
/// BR Code follows.
/// </summary>
/// <remarks>
/// <para>
/// A leaf holds its <see cref="Value"/> directly. A template holds a sequence of child TLVs in
/// <see cref="Children"/>: the merchant account information templates (IDs 26–51 — "the ID can
/// be any one of the identifiers inside the 26-51 range", BCB, Manual de Padrões para Iniciação
/// do Pix, section 2.5, footnote 12), the additional data field template (ID 62) and the
/// unreserved templates (IDs 80–99). Nesting is one level deep: children are always leaves.
/// IDs 02–25 (MAI ranges of other payment systems) and 64 (language template) are outside the
/// documented Pix usage and are treated as plain leaves, preserved verbatim.
/// </para>
/// <para>
/// Every value fits in two length digits, so both a leaf value and a whole template hold at
/// most 99 characters. <see cref="Parse(string)"/>/<see cref="TryParse(ReadOnlySpan{char}, out PixTlv?)"/>
/// decode exactly one TLV; <see cref="ParseSequence(string)"/>/
/// <see cref="TryParseSequence(ReadOnlySpan{char}, out IReadOnlyList{PixTlv}?)"/> decode a whole
/// TLV stream. Decoding validates the structure only (headers, lengths, full consumption,
/// unique IDs per level and the recursive decomposition of templates) — use
/// <see cref="PixPayload"/> for the full Pix validation with CRC and required fields.
/// </para>
/// </remarks>
public sealed class PixTlv : IEquatable<PixTlv>
{
    private readonly string _value;
    private readonly List<PixTlv>? _children;
    private readonly int _encodedLength;

    private PixTlv(string id, string value, List<PixTlv>? children, int encodedLength)
    {
        Id = id;
        _value = value;
        _children = children;
        _encodedLength = encodedLength;
    }

    /// <summary>The two-digit ID of the data object, e.g. "26" or "00".</summary>
    public string Id { get; }

    /// <summary>The value of a leaf data object; the empty string for a template.</summary>
    public string Value => _value;

    /// <summary>The ordered child data objects of a template; <see langword="null"/> for a leaf.</summary>
    public IReadOnlyList<PixTlv>? Children => _children;

    /// <summary>
    /// Returns <see langword="true"/> for the templates (IDs 26–51, 62 and 80–99), whose content
    /// is a sequence of child TLVs.
    /// </summary>
    public bool IsTemplate => _children is not null;

    /// <summary>The total number of characters the object occupies when encoded ("ID" + "LL" + value).</summary>
    public int EncodedLength => _encodedLength;

    /// <summary>
    /// Creates a leaf data object: the ID must be exactly two digits and the value at most 99
    /// characters. Use <see cref="CreateTemplate(string, IEnumerable{PixTlv})"/> for the template IDs.
    /// </summary>
    /// <param name="id">The two-digit ID.</param>
    /// <param name="value">The value, at most 99 characters (the empty value is allowed).</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> is not exactly two ASCII digits, the value is longer than 99
    /// characters, or the ID belongs to a template.
    /// </exception>
    public static PixTlv Create(string id, string value)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(value);
        ValidateId(id, out int numericId);
        if (IsTemplateId(numericId))
        {
            throw new ArgumentException($"ID {id} is a template; use {nameof(CreateTemplate)} to build it from child data objects.", nameof(id));
        }
        if (value.Length > 99)
        {
            throw new ArgumentException("A data object value holds at most 99 characters (two length digits).", nameof(value));
        }
        return new PixTlv(id, value, null, 4 + value.Length);
    }

    /// <summary>
    /// Creates a template data object (IDs 26–51, 62 or 80–99) from its ordered children. Each
    /// child must be a leaf and the encoded children must fit in 99 characters.
    /// </summary>
    /// <param name="id">The two-digit template ID.</param>
    /// <param name="children">The child data objects, in encoding order; may be empty.</param>
    /// <exception cref="ArgumentNullException"><paramref name="id"/> or <paramref name="children"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> is not exactly two ASCII digits or not a template ID, a child is
    /// itself a template, or the encoded children exceed 99 characters.
    /// </exception>
    public static PixTlv CreateTemplate(string id, IEnumerable<PixTlv> children)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(children);
        ValidateId(id, out int numericId);
        if (!IsTemplateId(numericId))
        {
            throw new ArgumentException($"ID {id} is not a template (templates are 26-51, 62 and 80-99).", nameof(id));
        }
        List<PixTlv> list = new(children);
        int contentLength = 0;
        foreach (PixTlv child in list)
        {
            if (child.IsTemplate)
            {
                throw new ArgumentException($"Child {child.Id} is a template; template children must be leaves (nesting is one level deep).", nameof(children));
            }
            contentLength += child.EncodedLength;
        }
        if (contentLength > 99)
        {
            throw new ArgumentException("The template content holds at most 99 characters (two length digits).", nameof(children));
        }
        return new PixTlv(id, string.Empty, list, 4 + contentLength);
    }

    /// <summary>Parses exactly one TLV — a leaf or a template — from <paramref name="text"/>.</summary>
    /// <param name="text">The TLV text; nothing may precede or follow it.</param>
    /// <returns>The decoded data object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The text is not a single, structurally valid TLV.</exception>
    public static PixTlv Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!TryParse(text.AsSpan(), out PixTlv? result) || result is null)
        {
            throw new FormatException($"'{DocumentText.Preview(text)}' is not a single valid TLV data object.");
        }
        return result;
    }

    /// <summary>Attempts to parse exactly one TLV; see <see cref="Parse(string)"/>.</summary>
    /// <param name="text">The TLV text; nothing may precede or follow it.</param>
    /// <param name="result">The decoded data object; <see langword="null"/> when the parse fails.</param>
    /// <returns><see langword="true"/> when the text is a single, structurally valid TLV.</returns>
    public static bool TryParse(ReadOnlySpan<char> text, out PixTlv? result)
    {
        result = null;
        if (!TryDecodeOne(text, out int consumed, out PixTlv? decoded) || consumed != text.Length)
        {
            return false;
        }
        result = decoded;
        return true;
    }

    /// <summary>Parses a whole TLV stream into its ordered data objects; see <see cref="TryParseSequence(ReadOnlySpan{char}, out IReadOnlyList{PixTlv}?)"/>.</summary>
    /// <param name="text">The TLV stream.</param>
    /// <returns>The ordered data objects.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The stream is empty or not structurally valid.</exception>
    public static IReadOnlyList<PixTlv> ParseSequence(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!TryParseSequence(text.AsSpan(), out IReadOnlyList<PixTlv>? fields) || fields is null)
        {
            throw new FormatException($"'{DocumentText.Preview(text)}' is not a valid TLV stream.");
        }
        return fields;
    }

    /// <summary>
    /// Attempts to parse a whole TLV stream: every object must have a two-digit ID, a two-digit
    /// length and the declared value, IDs must be unique per level, the stream must be fully
    /// consumed and templates must decompose completely into child leaves.
    /// </summary>
    /// <param name="text">The TLV stream.</param>
    /// <param name="fields">The ordered data objects; <see langword="null"/> when the parse fails.</param>
    /// <returns><see langword="true"/> when the whole stream decodes.</returns>
    public static bool TryParseSequence(ReadOnlySpan<char> text, out IReadOnlyList<PixTlv>? fields)
    {
        fields = null;
        if (text.IsEmpty || !TryDecodeLevel(text, allowTemplates: true, requireAtLeastOne: true, out List<PixTlv>? list) || list is null)
        {
            return false;
        }
        fields = list;
        return true;
    }

    /// <summary>Encodes the data object: "ID" + two-digit length + value (or the children of a template).</summary>
    /// <returns>The encoded TLV, e.g. "62070503***".</returns>
    public string Encode() => string.Create(_encodedLength, this, static (buffer, state) => state.TryEncode(buffer, out _));

    /// <summary>Writes the encoded data object into a character span; see <see cref="Encode"/>.</summary>
    /// <param name="destination">The destination buffer.</param>
    /// <param name="charsWritten">The number of characters written; <c>0</c> when <see langword="false"/>.</param>
    /// <returns><see langword="false"/> when <paramref name="destination"/> is too small.</returns>
    /// <remarks>This method performs no allocations.</remarks>
    public bool TryEncode(Span<char> destination, out int charsWritten)
    {
        charsWritten = 0;
        if (destination.Length < _encodedLength)
        {
            return false;
        }
        ReadOnlySpan<char> id = Id;
        destination[0] = id[0];
        destination[1] = id[1];
        int contentLength = _encodedLength - 4;
        destination[2] = (char)('0' + contentLength / 10);
        destination[3] = (char)('0' + contentLength % 10);
        int position = 4;
        if (_children is null)
        {
            _value.AsSpan().CopyTo(destination[4..]);
        }
        else
        {
            foreach (PixTlv child in _children)
            {
                child.TryEncode(destination[position..], out _);
                position += child.EncodedLength;
            }
        }
        charsWritten = _encodedLength;
        return true;
    }

    /// <summary>Returns the encoded TLV; see <see cref="Encode"/>.</summary>
    public override string ToString() => Encode();

    /// <summary>Data objects are equal when their IDs, values and children match.</summary>
    public bool Equals(PixTlv? other)
    {
        if (other is null || Id != other.Id || _value != other._value)
        {
            return false;
        }
        if (_children is null || other._children is null)
        {
            return _children is null && other._children is null;
        }
        if (_children.Count != other._children.Count)
        {
            return false;
        }
        for (int i = 0; i < _children.Count; i++)
        {
            if (!_children[i].Equals(other._children[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is PixTlv other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Id, _value);

    /// <summary>Determines whether the given numeric ID is a template: 26–51, 62 or 80–99.</summary>
    internal static bool IsTemplateId(int id) => id is (>= 26 and <= 51) or 62 or (>= 80 and <= 99);

    /// <summary>Validates that <paramref name="id"/> is exactly two ASCII digits.</summary>
    private static void ValidateId(string id, out int numericId)
    {
        numericId = -1;
        if (id.Length != 2 || !char.IsAsciiDigit(id[0]) || !char.IsAsciiDigit(id[1]))
        {
            throw new ArgumentException($"The data object ID '{id}' must be exactly two ASCII digits.", nameof(id));
        }
        numericId = (id[0] - '0') * 10 + (id[1] - '0');
    }

    /// <summary>
    /// Decodes the first TLV of <paramref name="text"/>: header, bounds, value, and the children
    /// when the ID is a template.
    /// </summary>
    private static bool TryDecodeOne(ReadOnlySpan<char> text, out int consumed, out PixTlv? decoded)
    {
        consumed = 0;
        decoded = null;
        if (text.Length < 4 || !char.IsAsciiDigit(text[0]) || !char.IsAsciiDigit(text[1])
            || !char.IsAsciiDigit(text[2]) || !char.IsAsciiDigit(text[3]))
        {
            return false;
        }
        int id = (text[0] - '0') * 10 + (text[1] - '0');
        int length = (text[2] - '0') * 10 + (text[3] - '0');
        if (text.Length - 4 < length)
        {
            return false;
        }
        ReadOnlySpan<char> value = text.Slice(4, length);
        string idText = new string(text[..2]);
        if (!IsTemplateId(id))
        {
            consumed = 4 + length;
            decoded = new PixTlv(idText, new string(value), null, consumed);
            return true;
        }
        if (!TryDecodeLevel(value, allowTemplates: false, requireAtLeastOne: false, out List<PixTlv>? children))
        {
            return false;
        }
        consumed = 4 + length;
        decoded = new PixTlv(idText, string.Empty, children ?? new List<PixTlv>(), consumed);
        return true;
    }

    /// <summary>
    /// Decodes a whole level — a template content or a root stream — into a list of leaf or
    /// template objects, requiring full consumption and unique IDs. The list is allocated
    /// lazily, so a structurally invalid stream never allocates.
    /// </summary>
    /// <param name="text">The level content: the whole stream at the root, a template value below it.</param>
    /// <param name="allowTemplates">Whether template objects may appear at this level.</param>
    /// <param name="requireAtLeastOne">Whether an empty level is invalid (root streams must hold objects).</param>
    /// <param name="list">The decoded objects; <see langword="null"/> for an empty template content.</param>
    private static bool TryDecodeLevel(ReadOnlySpan<char> text, bool allowTemplates, bool requireAtLeastOne, out List<PixTlv>? list)
    {
        list = null;
        Span<bool> seen = stackalloc bool[100];
        int position = 0;
        while (position < text.Length)
        {
            if (!TryDecodeOne(text[position..], out int consumed, out PixTlv? decoded) || decoded is null)
            {
                return false;
            }
            if (!allowTemplates && decoded.IsTemplate)
            {
                return false;
            }
            int numericId = (decoded.Id[0] - '0') * 10 + (decoded.Id[1] - '0');
            if (seen[numericId])
            {
                return false;
            }
            seen[numericId] = true;
            (list ??= new List<PixTlv>()).Add(decoded);
            position += consumed;
        }
        return position == text.Length && (!requireAtLeastOne || list is not null);
    }
}
