using System.Text;

namespace Snail.MCP.Vault.Application.Excalidraw;

/// <summary>Base64 variant of pieroxy's LZ-String, bit-compatible with the JavaScript original used by the Obsidian Excalidraw plugin.</summary>
public static class LzString
{
    private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=";

    private static readonly int[] Base64Values = BuildReverseAlphabet();

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

    public static string DecompressFromBase64(string input)
    {
        if (input.Length == 0)
            return "";

        return Decompress(input, 32, static character => Base64Values[character]);
    }

    private static string Compress(string input, int bitsPerChar, Func<int, char> toChar)
    {
        Dictionary<string, int> dictionary = [];
        HashSet<string> pendingFirstUse = [];
        var writer = new BitWriter(bitsPerChar, toChar);
        var current = "";
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

        void EmitCode(string symbol)
        {
            if (pendingFirstUse.Remove(symbol))
            {
                var character = symbol[0];
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
                writer.Write(dictionary[symbol], numBits);
            }

            GrowCodeWidth();
        }

        foreach (var character in input)
        {
            var symbol = character.ToString();
            if (dictionary.TryAdd(symbol, dictSize))
            {
                dictSize++;
                pendingFirstUse.Add(symbol);
            }

            var extended = current + symbol;
            if (dictionary.ContainsKey(extended))
            {
                current = extended;
                continue;
            }

            EmitCode(current);
            dictionary[extended] = dictSize++;
            current = symbol;
        }

        if (current.Length > 0)
            EmitCode(current);

        writer.Write(2, numBits);
        return writer.Finish();
    }

    private static string Decompress(string input, int resetValue, Func<char, int> toValue)
    {
        var reader = new BitReader(input, resetValue, toValue);
        var firstCode = reader.Read(2);
        if (firstCode == 2)
            return "";

        var firstSymbol = ((char)reader.Read(firstCode == 0 ? 8 : 16)).ToString();
        List<string?> dictionary = [null, null, null, firstSymbol];
        var result = new StringBuilder(firstSymbol);
        var previous = firstSymbol;
        var enlargeIn = 4;
        var dictSize = 4;
        var numBits = 3;

        while (true)
        {
            if (reader.Exhausted)
                return "";

            var code = reader.Read(numBits);
            switch (code)
            {
                case 0:
                case 1:
                    dictionary.Add(((char)reader.Read(code == 0 ? 8 : 16)).ToString());
                    code = dictSize++;
                    enlargeIn--;
                    break;
                case 2:
                    return result.ToString();
            }

            if (enlargeIn == 0)
            {
                enlargeIn = 1 << numBits;
                numBits++;
            }

            string entry;
            if (code < dictionary.Count && dictionary[code] is { } known)
                entry = known;
            else if (code == dictSize)
                entry = previous + previous[0];
            else
                throw new FormatException("The LZ-String stream is corrupted.");

            result.Append(entry);
            dictionary.Add(previous + entry[0]);
            dictSize++;
            enlargeIn--;
            previous = entry;

            if (enlargeIn == 0)
            {
                enlargeIn = 1 << numBits;
                numBits++;
            }
        }
    }

    private static int[] BuildReverseAlphabet()
    {
        var values = new int[128];
        for (var i = 0; i < Base64Alphabet.Length; i++)
            values[Base64Alphabet[i]] = i;

        return values;
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

        public bool Exhausted => _index > input.Length;

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
