using System;
using System.Collections.Generic;
using System.Text;

namespace Technocore
{
    /// <summary>Minimal base58btc (Bitcoin alphabet) codec.</summary>
    public static class Base58
    {
        private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

        public static string Encode(byte[] data)
        {
            int zeros = 0;
            while (zeros < data.Length && data[zeros] == 0) zeros++;

            var input = (byte[])data.Clone();
            var encoded = new List<char>();
            int start = zeros;
            while (start < input.Length)
            {
                int remainder = 0;
                for (int i = start; i < input.Length; i++)
                {
                    int acc = remainder * 256 + input[i];
                    input[i] = (byte)(acc / 58);
                    remainder = acc % 58;
                }
                encoded.Add(Alphabet[remainder]);
                if (input[start] == 0) start++;
            }
            encoded.Reverse();
            return new string('1', zeros) + new string(encoded.ToArray());
        }

        public static byte[] Decode(string s)
        {
            var num = new List<byte>(); // big-endian base-256
            foreach (char c in s)
            {
                int val = Alphabet.IndexOf(c);
                if (val < 0) throw new FormatException($"invalid base58 character: {c}");
                int carry = val;
                for (int i = num.Count - 1; i >= 0; i--)
                {
                    carry += num[i] * 58;
                    num[i] = (byte)(carry & 0xff);
                    carry >>= 8;
                }
                while (carry > 0)
                {
                    num.Insert(0, (byte)(carry & 0xff));
                    carry >>= 8;
                }
            }
            int leading = 0;
            foreach (char c in s) { if (c == '1') leading++; else break; }
            var result = new byte[leading + num.Count];
            num.CopyTo(result, leading);
            return result;
        }
    }
}
