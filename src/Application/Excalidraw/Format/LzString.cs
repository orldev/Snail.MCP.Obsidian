using System.Buffers;
using System.Text;

namespace Snail.MCP.Obsidian.Application.Excalidraw.Format;

/// <summary>Base64 variant of pieroxy's LZ-String, bit-compatible with the JavaScript original used by the Obsidian Excalidraw plugin.</summary>
public static class LzString
{
    private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

    private static readonly int[] Base64Values = BuildReverseAlphabet();

    /// <summary>Compresses text into the Base64 form the plugin writes.</summary>
    public static string CompressToBase64(string input)
    {
        var compressed = Compress(input, 6, static value => Base64Alphabet[value]);

        return (compressed.Length % 4) switch
        {
            1 => $"{compressed}===",
            2 => $"{compressed}==",
            3 => $"{compressed}=",
            _ => compressed,
        };
    }

    /// <summary>Decompresses the Base64 form the plugin writes; empty input gives empty text.</summary>
    public static string DecompressFromBase64(string input)
    {
        if (input.Length == 0)
            return "";

        return Decompress(input, 32, Base64Value);
    }

    /// <remarks>
    /// A phrase of the dictionary is keyed by the code of its prefix and its last character, never by its text: the
    /// text-keyed dictionary built a new string for every character read, so the 91 KB scene of the benchmark cost
    /// 6.6 MB of allocations to compress, and a 5 MB scene took 4.3 s. The phrases and the bits written are the same.
    /// </remarks>
    private static string Compress(string input, int bitsPerChar, Func<int, char> toChar)
    {
        Dictionary<char, int> characters = [];
        Dictionary<long, int> phrases = [];
        HashSet<char> pendingFirstUse = [];
        var writer = new BitWriter(bitsPerChar, toChar);
        var hasCurrent = false;
        var currentCode = 0;
        char? currentCharacter = null;
        var enlargeIn = 2;
        var dictSize = 3;
        var numBits = 2;

        void GrowCodeWidth()
        {
            enlargeIn--;
            if (enlargeIn != 0)
                return;

            enlargeIn = 1 << numBits;
            numBits++;
        }

        void EmitCurrent()
        {
            if (currentCharacter is { } character && pendingFirstUse.Remove(character))
            {
                if (character < 256)
                {
                    writer.Write(0, numBits);
                    writer.Write(character, 8);
                }
                else
                {
                    writer.Write(1, numBits);
                    writer.Write(character, 16);
                }

                GrowCodeWidth();
            }
            else
            {
                writer.Write(currentCode, numBits);
            }

            GrowCodeWidth();
        }

        foreach (var character in input)
        {
            if (characters.TryAdd(character, dictSize))
            {
                dictSize++;
                pendingFirstUse.Add(character);
            }

            if (!hasCurrent)
            {
                hasCurrent = true;
                currentCode = characters[character];
                currentCharacter = character;
                continue;
            }

            var extended = ((long)currentCode << 16) | character;
            if (phrases.TryGetValue(extended, out var extendedCode))
            {
                currentCode = extendedCode;
                currentCharacter = null;
                continue;
            }

            EmitCurrent();
            phrases[extended] = dictSize++;
            currentCode = characters[character];
            currentCharacter = character;
        }

        if (hasCurrent)
            EmitCurrent();

        writer.Write(2, numBits);

        return writer.Finish();
    }

    /// <remarks>
    /// Every phrase of the dictionary is a stretch of the text already decoded — the phrase before it and the next
    /// character — so a phrase is kept as an offset and a length into the output, not as a string of its own, and both
    /// the output and the dictionary are arrays rented from the shared pool: a string per step was 1.2 MB of the 2 MB
    /// it took to read the benchmark's scene.
    /// </remarks>
    private static string Decompress(string input, int resetValue, Func<char, int> toValue)
    {
        var reader = new BitReader(input, resetValue, toValue);
        var firstCode = reader.Read(2);
        if (firstCode == 2)
            return "";

        var firstCharacter = (char)reader.Read(firstCode == 0 ? 8 : 16);
        using var text = new PooledBuffer<char>(input.Length * 4);
        using var dictionary = new PooledBuffer<Phrase>(input.Length / 2);
        dictionary.Add(Phrase.None);
        dictionary.Add(Phrase.None);
        dictionary.Add(Phrase.None);
        dictionary.Add(Phrase.Of(firstCharacter));
        text.Add(firstCharacter);
        var previous = new Phrase(0, 1);
        var enlargeIn = 4;
        var dictSize = 4;
        var numBits = 3;

        while (true)
        {
            if (reader.IsExhausted)
                return "";

            var code = reader.Read(numBits);
            switch (code)
            {
                case 0:
                case 1:
                    dictionary.Add(Phrase.Of((char)reader.Read(code == 0 ? 8 : 16)));
                    code = dictSize++;
                    enlargeIn--;
                    break;
                case 2:
                    return new string(text.Items);
            }

            if (enlargeIn == 0)
            {
                enlargeIn = 1 << numBits;
                numBits++;
            }

            var start = text.Count;
            if (code < dictionary.Count && dictionary[code] is { IsKnown: true } known)
            {
                Write(text, known);
            }
            else if (code == dictSize)
            {
                Write(text, previous);
                text.Add(text[previous.Offset]);
            }
            else
            {
                throw new FormatException("The LZ-String stream is corrupted.");
            }

            dictionary.Add(new Phrase(previous.Offset, previous.Length + 1));
            dictSize++;
            enlargeIn--;
            previous = new Phrase(start, text.Count - start);

            if (enlargeIn == 0)
            {
                enlargeIn = 1 << numBits;
                numBits++;
            }
        }
    }

    /// <remarks>A character outside the alphabet — an editor's typographic quote in a damaged file — indexed past the table.</remarks>
    private static int Base64Value(char character) =>
        character < Base64Values.Length && Base64Values[character] >= 0
            ? Base64Values[character]
            : throw new FormatException($"The compressed drawing holds '{character}', which is not a Base64 character; the file is corrupted.");

    private static int[] BuildReverseAlphabet()
    {
        var values = new int[128];
        Array.Fill(values, -1);
        for (var i = 0; i < Base64Alphabet.Length; i++)
            values[Base64Alphabet[i]] = i;

        return values;
    }

    /// <summary>A phrase of the decoder's dictionary: a stretch of the decoded text, or one character not written yet.</summary>
    private readonly record struct Phrase(int Offset, int Length, char Character = '\0')
    {
        private const int SingleCharacter = -1;

        public static Phrase None { get; } = new(-2, 0);

        public bool IsKnown => Offset != None.Offset;

        public bool IsSingleCharacter => Offset == SingleCharacter;

        public static Phrase Of(char character) => new(SingleCharacter, 1, character);
    }

    private static void Write(PooledBuffer<char> text, Phrase phrase)
    {
        if (phrase.IsSingleCharacter)
            text.Add(phrase.Character);
        else
            text.AddCopyOf(phrase.Offset, phrase.Length);
    }

    /// <summary>A growing array rented from the shared pool, given back when disposed.</summary>
    private sealed class PooledBuffer<T>(int capacity) : IDisposable
    {
        private T[] _items = ArrayPool<T>.Shared.Rent(Math.Max(capacity, 16));

        public int Count { get; private set; }

        public ReadOnlySpan<T> Items => _items.AsSpan(0, Count);

        public T this[int index] => _items[index];

        public void Add(T item)
        {
            Reserve(1);
            _items[Count++] = item;
        }

        public void AddCopyOf(int offset, int count)
        {
            Reserve(count);
            Array.Copy(_items, offset, _items, Count, count);
            Count += count;
        }

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_items);
            _items = [];
        }

        private void Reserve(int count)
        {
            if (Count + count <= _items.Length)
                return;

            var grown = ArrayPool<T>.Shared.Rent(Math.Max(_items.Length * 2, Count + count));
            Array.Copy(_items, grown, Count);
            ArrayPool<T>.Shared.Return(_items);
            _items = grown;
        }
    }

    private sealed class BitWriter(int bitsPerChar, Func<int, char> toChar)
    {
        private readonly StringBuilder _data = new();
        private int _buffer;
        private int _position;

        public void Write(int value, int bitCount)
        {
            for (var i = 0; i < bitCount; i++)
            {
                _buffer = (_buffer << 1) | (value & 1);
                if (_position == bitsPerChar - 1)
                {
                    _position = 0;
                    _data.Append(toChar(_buffer));
                    _buffer = 0;
                }
                else
                {
                    _position++;
                }

                value >>= 1;
            }
        }

        public string Finish()
        {
            while (true)
            {
                _buffer <<= 1;
                if (_position == bitsPerChar - 1)
                {
                    _data.Append(toChar(_buffer));
                    return _data.ToString();
                }

                _position++;
            }
        }
    }

    private sealed class BitReader(string input, int resetValue, Func<char, int> toValue)
    {
        private readonly int _reset = resetValue;
        private int _buffer = toValue(input[0]);
        private int _position = resetValue;
        private int _index = 1;

        public bool IsExhausted => _index > input.Length;

        public int Read(int bitCount)
        {
            var bits = 0;
            var maxPower = 1 << bitCount;
            var power = 1;

            while (power != maxPower)
            {
                var bit = _buffer & _position;
                _position >>= 1;
                if (_position == 0)
                {
                    _position = _reset;
                    _buffer = _index < input.Length ? toValue(input[_index]) : 0;
                    _index++;
                }

                if (bit > 0)
                    bits |= power;

                power <<= 1;
            }

            return bits;
        }
    }
}
