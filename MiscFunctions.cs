﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace CM0102Patcher
{
    public static class MiscFunctions
    {
        static Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");

        public static string GetTextFromBytes(byte[] bytes, bool useExactSize = false)
        {
            var ret = "";
            if (bytes != null)
            {
                int length = useExactSize ? bytes.Length : Array.IndexOf(bytes, (byte)0);
                ret = latin1.GetString(bytes, 0, length);
            }
            return ret;
        }

        public static byte[] GetBytesFromText(string text, int byteArraySize)
        {
            var bytes = new byte[byteArraySize];
            Array.Copy(latin1.GetBytes(text), bytes, text.Length);
            return bytes;
        }

        static Dictionary<string, string> foreign_characters = new Dictionary<string, string>
        {
            { "äæǽ", "ae" },
            { "öœ", "oe" },
            { "ü", "ue" },
            { "Ä", "Ae" },
            { "Ü", "Ue" },
            { "Ö", "Oe" },
            { "ÀÁÂÃÄÅǺĀĂĄǍΑΆẢẠẦẪẨẬẰẮẴẲẶА", "A" },
            { "àáâãåǻāăąǎªαάảạầấẫẩậằắẵẳặа", "a" },
            { "Б", "B" },
            { "б", "b" },
            { "ÇĆĈĊČ", "C" },
            { "çćĉċč", "c" },
            { "Д", "D" },
            { "д", "d" },
            { "ÐĎĐΔ", "Dj" },
            { "ðďđδ", "dj" },
            { "ÈÉÊËĒĔĖĘĚΕΈẼẺẸỀẾỄỂỆЕЭ", "E" },
            { "èéêëēĕėęěέεẽẻẹềếễểệеэ", "e" },
            { "Ф", "F" },
            { "ф", "f" },
            { "ĜĞĠĢΓГҐ", "G" },
            { "ĝğġģγгґ", "g" },
            { "ĤĦ", "H" },
            { "ĥħ", "h" },
            { "ÌÍÎÏĨĪĬǏĮİΗΉΊΙΪỈỊИЫ", "I" },
            { "ìíîïĩīĭǐįıηήίιϊỉịиыї", "i" },
            { "Ĵ", "J" },
            { "ĵ", "j" },
            { "ĶΚК", "K" },
            { "ķκк", "k" },
            { "ĹĻĽĿŁΛЛ", "L" },
            { "ĺļľŀłλл", "l" },
            { "М", "M" },
            { "м", "m" },
            { "ÑŃŅŇΝН", "N" },
            { "ñńņňŉνн", "n" },
            { "ÒÓÔÕŌŎǑŐƠØǾΟΌΩΏỎỌỒỐỖỔỘỜỚỠỞỢО", "O" },
            { "òóôõōŏǒőơøǿºοόωώỏọồốỗổộờớỡởợо", "o" },
            { "П", "P" },
            { "п", "p" },
            { "ŔŖŘΡР", "R" },
            { "ŕŗřρр", "r" },
            { "ŚŜŞȘŠΣС", "S" },
            { "śŝşșšſσςс", "s" },
            { "ȚŢŤŦτТ", "T" },
            { "țţťŧт", "t" },
            { "ÙÚÛŨŪŬŮŰŲƯǓǕǗǙǛŨỦỤỪỨỮỬỰУ", "U" },
            { "ùúûũūŭůűųưǔǖǘǚǜυύϋủụừứữửựу", "u" },
            { "ÝŸŶΥΎΫỲỸỶỴЙ", "Y" },
            { "ýÿŷỳỹỷỵй", "y" },
            { "В", "V" },
            { "в", "v" },
            { "Ŵ", "W" },
            { "ŵ", "w" },
            { "ŹŻŽΖЗ", "Z" },
            { "źżžζз", "z" },
            { "ÆǼ", "AE" },
            { "ß", "ss" },
            { "Ĳ", "IJ" },
            { "ĳ", "ij" },
            { "Œ", "OE" },
            { "ƒ", "f" },
            { "ξ", "ks" },
            { "π", "p" },
            { "β", "v" },
            { "μ", "m" },
            { "ψ", "ps" },
            { "Ё", "Yo" },
            { "ё", "yo" },
            { "Є", "Ye" },
            { "є", "ye" },
            { "Ї", "Yi" },
            { "Ж", "Zh" },
            { "ж", "zh" },
            { "Х", "Kh" },
            { "х", "kh" },
            { "Ц", "Ts" },
            { "ц", "ts" },
            { "Ч", "Ch" },
            { "ч", "ch" },
            { "Ш", "Sh" },
            { "ш", "sh" },
            { "Щ", "Shch" },
            { "щ", "shch" },
            { "ЪъЬь", "" },
            { "Ю", "Yu" },
            { "ю", "yu" },
            { "Я", "Ya" },
            { "я", "ya" },
            {"\u05de", "th"}
        };

        public static char RemoveDiacritics(this char c)
        {
            foreach (KeyValuePair<string, string> entry in foreign_characters)
            {
                if (entry.Key.IndexOf(c) != -1)
                {
                    return entry.Value[0];
                }
            }
            return c;
        }

        public static string RemoveDiacritics(this string s)
        {
            string text = "";

            if (string.IsNullOrEmpty(s))
                return text;

            if (s[0] == 0xde)
                s = "Th" + s.Substring(1);

            foreach (char c in s)
            {
                int len = text.Length;

                if (c == (char)5)
                    Console.WriteLine();

                foreach (KeyValuePair<string, string> entry in foreign_characters)
                {
                    if (entry.Key.IndexOf(c) != -1)
                    {
                        text += entry.Value;
                        break;
                    }
                }

                if (len == text.Length)
                {
                    text += c;
                }
            }
            return text;
        }

        public static string ReadString(this byte[] bytes)
        {
            return MiscFunctions.GetTextFromBytes(bytes);
        }

        public static bool StartsWithIgnoreBlank(this string s, string s2)
        {
            if (String.IsNullOrEmpty(s) || String.IsNullOrEmpty(s2))
                return false;
            return s.ToLower().StartsWith(s2.ToLower());
        }

        public static void RemoveAccentsFromNameFile(string fileName)
        {
            var names = ReadFile<TNames>(fileName);
            for (int i = 0; i < names.Count; i++)
            {
                var name = GetTextFromBytes(names[i].Name);
                name = name.RemoveDiacritics();
                Array.Copy(ASCIIEncoding.ASCII.GetBytes(name), names[i].Name, name.Length);
            }
            SaveFile(fileName, names);
        }

        public static List<T> ReadFile<T>(string fileName, int seekTo = 0, int count = 0)
        {
            List<T> ret = new List<T>();

            using (var fin = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var br = new BinaryReader(fin))
            {
                fin.Seek(seekTo, SeekOrigin.Begin);
                int objSize = Marshal.SizeOf(typeof(T));

                int counter = 0;
                while (true)
                {
                    var bytes = br.ReadBytes(objSize);
                    if (count != 0 && counter == count)
                        break;
                    if (bytes == null || bytes.Length != objSize)
                        break;
                    var ptrObj = Marshal.AllocHGlobal(objSize);
                    Marshal.Copy(bytes, 0, ptrObj, objSize);
                    var obj = (T)Marshal.PtrToStructure(ptrObj, typeof(T));
                    ret.Add(obj);
                    Marshal.FreeHGlobal(ptrObj);
                    counter++;
                }
            }

            return ret;
        }

        public static void SaveFile<T>(string fileName, List<T> data, int seekTo = 0, bool truncateFirst = false)
        {
            using (var fout = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var bw = new BinaryWriter(fout))
            {
                int objSize = Marshal.SizeOf(typeof(T));
                if (truncateFirst)
                    fout.SetLength(seekTo);
                fout.Seek(seekTo, SeekOrigin.Begin);

                foreach (var obj in data)
                {
                    byte[] arr = new byte[objSize];
                    IntPtr ptr = Marshal.AllocHGlobal(objSize);
                    Marshal.StructureToPtr(obj, ptr, true);
                    Marshal.Copy(ptr, arr, 0, objSize);
                    Marshal.FreeHGlobal(ptr);
                    bw.Write(arr);
                }
            }
        }

        public static ZipStorer OpenZip(string zipFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(zipFileName));
            return ZipStorer.Open(assembly.GetManifestResourceStream(resourceName), FileAccess.Read);
        }

        public static bool CompareByteArrays(byte[] array1, byte[] array2)
        {
            bool bEqual = false;
            if (array1.Length == array2.Length)
            {
                int i = 0;
                while ((i < array1.Length) && (array1[i] == array2[i]))
                {
                    i += 1;
                }
                if (i == array1.Length)
                {
                    bEqual = true;
                }
            }
            return bEqual;
        }
    }
}
