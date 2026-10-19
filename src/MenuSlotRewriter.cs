using System;
using System.Text;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// Rewrites the slot name embedded in a binary .menu script so a menu
    /// written for the primary slot loads into the secondary slot.
    /// </summary>
    internal static class MenuSlotRewriter
    {
        private static readonly byte[][] FromTokens = { Ascii("accXXX"), Ascii("accxxx") };
        private static readonly byte[][] ToTokens = { Ascii("accVag"), Ascii("accvag") };

        /// <summary>
        /// Returns a copy of the script with every whole-token accXXX/accxxx
        /// replaced, or the original array when there is nothing to replace.
        /// </summary>
        public static byte[] PrimaryToSecondary(byte[] script)
        {
            if (script == null)
            {
                return null;
            }

            byte[] copy = null;
            for (var t = 0; t < FromTokens.Length; t++)
            {
                copy = ReplaceTokens(script, copy, FromTokens[t], ToTokens[t]);
            }

            return copy ?? script;
        }

        // Writes replacements into copy, cloning source on the first match when no
        // copy exists yet. Matches are read from copy when it exists so earlier
        // passes are respected. Returns copy (possibly still null).
        private static byte[] ReplaceTokens(byte[] source, byte[] copy, byte[] from, byte[] to)
        {
            var bytes = copy ?? source;
            for (var i = 0; i <= bytes.Length - from.Length; i++)
            {
                if (!MatchesAt(bytes, i, from) || IsTokenChar(bytes, i - 1) || IsTokenChar(bytes, i + from.Length))
                {
                    continue;
                }

                if (copy == null)
                {
                    copy = (byte[])source.Clone();
                    bytes = copy;
                }

                Buffer.BlockCopy(to, 0, copy, i, to.Length);
                i += from.Length - 1;
            }

            return copy;
        }

        private static bool MatchesAt(byte[] bytes, int offset, byte[] token)
        {
            for (var j = 0; j < token.Length; j++)
            {
                if (bytes[offset + j] != token[j])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsTokenChar(byte[] bytes, int index)
        {
            if (index < 0 || index >= bytes.Length)
            {
                return false;
            }

            var b = bytes[index];
            return (b >= '0' && b <= '9') || (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z') || b == '_';
        }

        private static byte[] Ascii(string value)
        {
            return Encoding.ASCII.GetBytes(value);
        }
    }
}
