﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Codegen {

    public class Program {

        /// <summary>
        /// Path to the emoji.json file.
        /// </summary>
        public string EmojiFile { get; set; } = "../../../../../../emoji.json";

        public string SourceDir { get; set; } = "../../../EmojiOne";

        public static void Main(string[] args) {
            var program = new Program();
            program.Execute();
        }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns></returns>
        public bool Execute() {
            try {
                // load and parse emoji.json
                var file = new FileInfo(EmojiFile);
                Console.WriteLine("Loading " + file.FullName);

                string json = File.ReadAllText(EmojiFile);
                var emojis = JsonConvert.DeserializeObject<Dictionary<string, Emoji>>(json);

                // write regex patternas and dictionaries to partial class
                Directory.CreateDirectory(SourceDir);
                file = new FileInfo(Path.Combine(SourceDir, "EmojiOne.generated.cs"));
                Console.WriteLine("Writing code to " + file.FullName);
                using (StreamWriter sw = new StreamWriter(Path.Combine(SourceDir, "EmojiOne.generated.cs"), false, Encoding.UTF8)) {
                    sw.WriteLine(@"using System.Collections.Generic;");
                    sw.WriteLine();
                    sw.WriteLine(@"namespace EmojiOne {");
                    sw.WriteLine();
                    sw.WriteLine(@"    public static partial class EmojiOne {");
                    sw.WriteLine();
                    var asciis = emojis.Values.Where(x => x.Ascii.Any());
                    sw.Write(@"        private const string ASCII_PATTERN = @""(?<=\s|^)(");
                    for (int i = 0; i < asciis.Count(); i++) {
                        var emoji = asciis.ElementAt(i);
                        for (int j = 0; j < emoji.Ascii.Length; j++) {
                            sw.Write(Regex.Escape(emoji.Ascii[j]));
                            if (j < emoji.Ascii.Length - 1) {
                                sw.Write("|");
                            }
                        }
                        if (i < asciis.Count() - 1) {
                            sw.Write("|");
                        }
                    }
                    sw.WriteLine(@")(?=\s|$|[!,\.])"";");
                    sw.WriteLine();
                    sw.WriteLine(@"        private const string IGNORE_PATTERN = @""<object[^>]*>.*?</object>|<span[^>]*>.*?</span>|<i[^>]*>.*?</i>|<(?:object|embed|svg|img|div|span|p|a)[^>]*>"";");
                    sw.WriteLine();
                    sw.Write(@"        private const string SHORTNAME_PATTERN = @""(");
                    for (int i = 0; i < emojis.Count; i++) {
                        var emoji = emojis.ElementAt(i).Value;
                        if (i > 0) {
                            sw.Write("|");
                        }
                        sw.Write(Regex.Escape(emoji.Shortname));
                        for (int j = 0; j < emoji.ShortnameAlternates.Length; j++) {
                            sw.Write("|");
                            sw.Write(Regex.Escape(emoji.ShortnameAlternates[j]));
                        }
                    }
                    sw.WriteLine(@")"";");
                    sw.WriteLine();
                    sw.Write(@"        private const string UNICODE_PATTERN = @""(");
                    // NOTE: these must be ordered by length of the unicode code point
                    var codepoints = emojis.Values.SelectMany(e => e.CodePoints.BaseAndDefaultMatches).OrderByDescending(cp => cp.Length).ToList();
                    for (int i = 0; i < codepoints.Count; i++) {
                        var cp = codepoints.ElementAt(i);
                        sw.Write(ToSurrogateString(cp));
                        if (i < codepoints.Count - 1) {
                            sw.Write("|");
                        }
                    }
                    sw.WriteLine(@")"";");
                    sw.WriteLine();
                    sw.WriteLine(@"        private static readonly Dictionary<string, string> ASCII_TO_CODEPOINT = new Dictionary<string, string> {");
                    for (int i = 0; i < asciis.Count(); i++) {
                        var emoji = asciis.ElementAt(i);
                        for (int j = 0; j < emoji.Ascii.Length; j++) {
                            sw.Write(@"            [""{0}""] = ""{1}""", emoji.Ascii[j].Replace("\\", "\\\\"), emoji.CodePoints.Output.ToLower());
                            if (j < emoji.Ascii.Length - 1) {
                                sw.WriteLine(",");
                            }
                        }
                        if (i < asciis.Count() - 1) {
                            sw.WriteLine(",");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(@"        };");
                    sw.WriteLine();
                    sw.WriteLine(@"        private static readonly Dictionary<string, string> CODEPOINT_TO_ASCII = new Dictionary<string, string> {");
                    for (int i = 0; i < asciis.Count(); i++) {
                        var emoji = asciis.ElementAt(i);
                        for (int j = 0; j < emoji.CodePoints.BaseAndDefaultMatches.Length; j++) {
                            sw.Write(@"            [""{0}""] = ""{1}""", emoji.CodePoints.BaseAndDefaultMatches[j].ToLower(), emoji.Ascii.First().Replace("\\", "\\\\"));
                            if (j < emoji.CodePoints.BaseAndDefaultMatches.Length - 1) {
                                sw.WriteLine(",");
                            }
                        }
                        if (i < asciis.Count() - 1) {
                            sw.WriteLine(",");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(@"        };");
                    sw.WriteLine();
                    sw.WriteLine(@"        private static readonly Dictionary<string, string> CODEPOINT_TO_SHORTNAME = new Dictionary<string, string> {");
                    for (int i = 0; i < emojis.Count; i++) {
                        var emoji = emojis.ElementAt(i).Value;
                        for (int j = 0; j < emoji.CodePoints.BaseAndDefaultMatches.Length; j++) {
                            sw.Write(@"            [""{0}""] = ""{1}""", emoji.CodePoints.BaseAndDefaultMatches[j].ToLower(), emoji.Shortname);
                            if (j < emoji.CodePoints.BaseAndDefaultMatches.Length - 1) {
                                sw.WriteLine(",");
                            }
                        }
                         if (i < emojis.Count - 1) {
                            sw.WriteLine(",");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(@"        };");
                    sw.WriteLine();
                    sw.WriteLine(@"        private static readonly Dictionary<string, string> SHORTNAME_TO_CODEPOINT = new Dictionary<string, string> {");
                    for (int i = 0; i < emojis.Count; i++) {
                        var emoji = emojis.ElementAt(i).Value;
                        sw.Write(@"            [""{0}""] = ""{1}""", emoji.Shortname, emoji.CodePoints.Output.ToLower());
                        for (int j = 0; j < emoji.ShortnameAlternates.Length; j++) {
                            sw.WriteLine(",");
                            sw.Write(@"            [""{0}""] = ""{1}""", emoji.ShortnameAlternates[j], emoji.CodePoints.Output.ToLower());
                        }
                        if (i < emojis.Count - 1) {
                            sw.WriteLine(",");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(@"        };");
                    sw.WriteLine();
                    sw.WriteLine(@"        private static readonly Dictionary<string, string> SHORTNAME_TO_CATEGORY = new Dictionary<string, string> {");
                    for (int i = 0; i < emojis.Count; i++) {
                        var emoji = emojis.ElementAt(i).Value;
                        sw.Write(@"            [""{0}""] = ""{1}""", emoji.Shortname, emoji.Category);
                        if (i < emojis.Count - 1) {
                            sw.WriteLine(",");
                        }
                    }
                    sw.WriteLine();
                    sw.WriteLine(@"        };");
                    sw.WriteLine();

                    sw.WriteLine(@"    }");
                    sw.WriteLine(@"}");
                }
                Console.WriteLine("Done!");
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Converts a codepoint to unicode surrogate pairs
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        private string ToSurrogateString(string codepoint) {
            var unicode = ToUnicode(codepoint);
            string s2 = "";
            for (int x = 0; x < unicode.Length; x++) {
                s2 += string.Format("\\u{0:X4}", (int)unicode[x]);
            }
            return s2;
        }

        /// <summary>
        /// Converts a unicode code point/code pair to a unicode character
        /// </summary>
        /// <param name="codepoints"></param>
        /// <returns></returns>
        private string ToUnicode(string codepoint) {
            if (codepoint.Contains('-')) {
                var pair = codepoint.Split('-');
                string[] hilos = new string[pair.Length];
                char[] chars = new char[pair.Length];
                for (int i = 0; i < pair.Length; i++) {
                    var part = Convert.ToInt32(pair[i], 16);
                    if (part >= 0x10000 && part <= 0x10FFFF) {
                        var hi = Math.Floor((decimal)(part - 0x10000) / 0x400) + 0xD800;
                        var lo = ((part - 0x10000) % 0x400) + 0xDC00;
                        hilos[i] = new String(new char[] { (char)hi, (char)lo });
                    } else {
                        chars[i] = (char)part;
                    }
                }
                if (hilos.Any(x => x != null)) {
                    return string.Concat(hilos);
                } else {
                    return new String(chars);
                }

            } else {
                var i = Convert.ToInt32(codepoint, 16);
                return char.ConvertFromUtf32(i);
            }
        }


    }


}


