using System;
using System.IO;
using System.Text;

namespace COM3D2.DoubleFrontHole
{
    /// <summary>
    /// Rewrites the slot name embedded in a binary .menu script so a menu
    /// written for the primary slot loads into the secondary or third slot.
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

        /// <summary>
        /// Returns a copy of the script with every accXXX/accxxx/accVag slot-name argument
        /// replaced by the extra slot name, or the original array when there is
        /// nothing to replace. The "category" line keeps accxxx: the game parses
        /// it as an MPN, and the extra slot has none of its own.
        /// </summary>
        public static byte[] PrimaryToExtra(byte[] script, string slotName)
        {
            if (script == null)
            {
                return null;
            }

            try
            {
                return RewriteCommands(script, slotName) ?? script;
            }
            catch (Exception ex)
            {
                DoubleFrontHolePlugin.Log.LogWarning("Menu script could not be parsed for the third slot, loading it unchanged: " + ex.Message);
                return script;
            }
        }

        // Layout read by Menu.ProcScriptBin: header (string "CM3D2_MENU", int32
        // version, 4 strings, int32 body size), then commands of one count byte
        // followed by that many strings, ended by a zero count byte.
        private static byte[] RewriteCommands(byte[] script, string slotName)
        {
            var utf8 = new UTF8Encoding(false);
            var reader = new BinaryReader(new MemoryStream(script), utf8);
            var output = new MemoryStream(script.Length + 64);
            var writer = new BinaryWriter(output, utf8);
            var changed = false;

            writer.Write(reader.ReadString());
            writer.Write(reader.ReadInt32());
            for (var h = 0; h < 4; h++)
            {
                writer.Write(reader.ReadString());
            }

            reader.ReadInt32();
            var sizeAt = output.Position;
            writer.Write(0);
            var bodyStart = output.Position;

            while (true)
            {
                var count = reader.ReadByte();
                writer.Write(count);
                if (count == 0)
                {
                    break;
                }

                var args = new string[count];
                for (var a = 0; a < count; a++)
                {
                    args[a] = reader.ReadString();
                }

                var isCategory = args[0] == "category";
                for (var a = 0; a < count; a++)
                {
                    if (a > 0 && !isCategory && SlotRouter.IsFrontHoleTag(args[a]))
                    {
                        args[a] = slotName;
                        changed = true;
                    }

                    writer.Write(args[a]);
                }
            }

            if (!changed)
            {
                return null;
            }

            var rest = (int)(script.Length - reader.BaseStream.Position);
            writer.Write(reader.ReadBytes(rest));
            writer.Flush();

            var bodySize = (int)(output.Position - bodyStart);
            output.Position = sizeAt;
            writer.Write(bodySize);
            writer.Flush();
            return output.ToArray();
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
