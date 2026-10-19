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
        /// Returns a copy of the script with every accXXX/accxxx slot-name string
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
            for (var i = 1; i <= bytes.Length - from.Length; i++)
            {
                if (!IsWholeString(bytes, i, from))
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

        // Menu scripts store each argument as a length-prefixed string (BinaryWriter
        // format; the prefix is a single byte below 128). Only an argument that is
        // exactly the token, i.e. preceded by its own length, is a slot name. The
        // token inside a longer string, such as a file name "item(accxxx).model",
        // must stay as it is or the referenced file can no longer be found.
        private static bool IsWholeString(byte[] bytes, int offset, byte[] token)
        {
            if (bytes[offset - 1] != token.Length)
            {
                return false;
            }

            for (var j = 0; j < token.Length; j++)
            {
                if (bytes[offset + j] != token[j])
                {
                    return false;
                }
            }

            return true;
        }

        private static byte[] Ascii(string value)
        {
            return Encoding.ASCII.GetBytes(value);
        }
    }
}
