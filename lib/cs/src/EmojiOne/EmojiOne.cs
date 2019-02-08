﻿//  The MIT License (MIT)
//  Copyright (c) 2019 Linus Birgerstam
//    
//  Permission is hereby granted, free of charge, to any person obtaining a copy of
//  this software and associated documentation files (the "Software"), to deal in
//  the Software without restriction, including without limitation the rights to
//  use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
//  of the Software, and to permit persons to whom the Software is furnished to do
//  so, subject to the following conditions:
//    
//  The above copyright notice and this permission notice shall be included in all
//  copies or substantial portions of the Software.
//    
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//  SOFTWARE.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace EmojiOne {

    /// <summary>
    /// Helper class for converting emoji to different formats.
    /// </summary>
    public static partial class EmojiOne {

        /// <summary>
        /// Used only to direct CDN path. This is a 2-digit version (e.g. 3.1). Not recommended for usage below 3.0.
        /// </summary>
        public static string EmojiVersion { get; set; } = "4.0";

        /// <summary>
        /// Used only to direct CDN path for non-sprite PNG usage. Available options are 32, 64, and 128.
        /// </summary>
        public static int EmojiSize { get; set; } = 32;

        /// <summary>
        /// Defaults to .png. Set to .svg when using local premium assests.
        /// </summary>
        public static string FileExtension { get; set; } = ".png";

        /// <summary>
        /// Defaults to CDN (jsdeliver) path. Change this when using local premium assets.
        /// </summary>
        public static string ImagePath { get; set; } = "https://cdn.jsdelivr.net/emojione/assets/" + EmojiVersion + "/png/";

        /// <summary>
        /// Takes an input string containing both native unicode emoji and shortnames, and translates it into emoji images for display.
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <param name="ascii"><c>true</c> to also convert ascii emoji to images.</param>
        /// <param name="unicodeAlt"><c>true</c> to use the unicode char instead of the shortname as the alt attribute (makes copy and pasting the resulting text better).</param>
        /// <param name="svg"><c>true</c> to use output svg markup instead of png</param>
        /// <param name="sprite"><c>true</c> to enable sprite mode instead of individual images.</param>
        /// <param name="size">Emoji size. If <c>null</c>, <see cref="EmojiSize"/> will be used.</param>
        /// <returns>A string with appropriate html for rendering emoji.</returns>
        public static string ToImage(string str, bool ascii = false, bool unicodeAlt = true, bool svg = false, bool sprite = false, int? size = null) {
            // first pass changes unicode characters into emoji markup
            str = UnicodeToImage(str, unicodeAlt, svg, sprite, size);
            // second pass changes any shortnames into emoji markup
            str = ShortnameToImage(str, ascii, unicodeAlt, svg, sprite, size);
            return str;
        }

        /// <summary>
        /// Unifies all emoji to their standard unicode types. 
        /// </summary>
        /// <param name="str">The input string.</param>
        /// <param name="ascii"><c>true</c> to also convert ascii emoji to unicode.</param>
        /// <returns>A string with standardized unicode.</returns>
        public static string UnifyUnicode(string str, bool ascii = false) {
            // transform all unicode into a standard shortname
            str = ToShort(str);
            // then transform the shortnames into unicode
            str = ShortnameToUnicode(str, ascii);
            return str;
        }

        /// <summary>
        /// Converts shortname emojis to unicode, useful for sending emojis back to mobile devices.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="ascii"><c>true</c> to also convert ascii emoji in the inpur string to unicode.</param>
        /// <returns>A string with unicode replacements</returns>
        public static string ShortnameToUnicode(string str, bool ascii = false) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + SHORTNAME_PATTERN, ShortnameToUnicodeCallback, RegexOptions.IgnoreCase);
            }
            if (ascii) {
                str = AsciiToUnicode(str);
            }
            return str;
        }

        /// <summary>
        /// This will replace shortnames with their ascii equivalent, e.g. :wink: -> ;). 
        /// This is useful for systems that don't support unicode or images.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>A string with ascii replacements.</returns>
        public static string ShortnameToAscii(string str) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + SHORTNAME_PATTERN, ShortnameToAsciiCallback, RegexOptions.IgnoreCase);
            }
            return str;
        }

        /// <summary>
        /// Takes input containing emoji shortnames and converts it to emoji images.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="ascii"><c>true</c> to also convert ascii emoji to images.</param>
        /// <param name="unicodeAlt"><c>true</c> to use the unicode char instead of the shortname as the alt attribute (makes copy and pasting the resulting text better).</param>
        /// <param name="svg"><c>true</c> to use output svg markup instead of png</param>
        /// <param name="sprite"><c>true</c> to enable sprite mode instead of individual images.</param>
        /// <param name="size">Emoji size. If <c>null</c>, <see cref="EmojiSize"/> will be used.</param>
        /// <returns>A string with appropriate html for rendering emoji.</returns>
        public static string ShortnameToImage(string str, bool ascii = false, bool unicodeAlt = true, bool svg = false, bool sprite = false, int? size = null) {
            if (ascii) {
                str = AsciiToShortname(str);
            }
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + SHORTNAME_PATTERN, match => ShortnameToImageCallback(match, unicodeAlt, svg, sprite, size), RegexOptions.IgnoreCase);
            }
            return str;
        }

        /// <summary>
        /// Converts unicode emoji to shortnames.
        /// </summary>
        /// <param name="str">The input string</param>
        /// <returns>A string with shortname replacements.</returns>
        public static string ToShort(string str) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + UNICODE_PATTERN, UnicodeToShortnameCallback);
            }
            return str;
        }

        /// <summary>
        /// Takes native unicode emoji input, such as that from your mobile device, and outputs image markup (png or svg).
        /// </summary>
        /// <param name="str">The input string</param>
        /// <param name="unicodeAlt"><c>true</c> to use the unicode char instead of the shortname as the alt attribute (makes copy and pasting the resulting text better).</param>
        /// <param name="svg"><c>true</c> to use output svg markup instead of png</param>
        /// <param name="sprite"><c>true</c> to enable sprite mode instead of individual images.</param>
        /// <param name="size">Emoji size. If <c>null</c>, <see cref="EmojiSize"/> will be used.</param>
        /// <returns>A string with appropriate html for rendering emoji.</returns>
        public static string UnicodeToImage(string str, bool unicodeAlt = true, bool svg = false, bool sprite = false, int? size = null) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + UNICODE_PATTERN, match => UnicodeToImageCallback(match, unicodeAlt, svg, sprite, size));
            }
            return str;
        }

        /// <summary>
        /// Converts ascii emoji to unicode, e.g. :) -> 😄
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string AsciiToUnicode(string str) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + ASCII_PATTERN, AsciiToUnicodeCallback);
            }
            return str;
        }

        /// <summary>
        /// Converts ascii emoji to shortname, e.g. :) -> :smile:
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string AsciiToShortname(string str) {
            if (str != null) {
                str = Regex.Replace(str, IGNORE_PATTERN + "|" + ASCII_PATTERN, AsciiToShortnameCallback);
            }
            return str;
        }

        private static string AsciiToUnicodeCallback(Match match) {
            // check if the emoji exists in our dictionaries
            var ascii = match.Value;
            if (ASCII_TO_CODEPOINT.ContainsKey(ascii)) {
                // convert codepoint to unicode char
                return ToUnicode(ASCII_TO_CODEPOINT[ascii]);
            }
            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string AsciiToShortnameCallback(Match match) {
            // check if the emoji exists in our dictionaries
            var ascii = match.Value;
            if (ASCII_TO_CODEPOINT.ContainsKey(ascii)) {
                var codepoint = ASCII_TO_CODEPOINT[ascii];
                if (CODEPOINT_TO_SHORTNAME.ContainsKey(codepoint)) {
                    return CODEPOINT_TO_SHORTNAME[codepoint];
                }
            }
            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string ShortnameToImageCallback(Match match, bool unicodeAlt, bool svg, bool sprite, int? size) {
            // check if the emoji exists in our dictionaries
            var shortname = match.Value;
            if (SHORTNAME_TO_CODEPOINT.ContainsKey(shortname)) {
                var codepoint = SHORTNAME_TO_CODEPOINT[shortname];
                string alt = unicodeAlt ? ToUnicode(codepoint) : shortname;
                if (svg) {
                    // TODO: inline svg
                    return null;
                    //if (sprite) {
                    //    return string.Format(@"<svg class=""emojione""><description>{0}</description><use xlink:href=""{1}emojione-sprite.svg#emoji-{2}""></use></svg>", unicodeAlt ? alt : shortname, ImagePathSprites, codepoint);
                    //} else {
                    //    return string.Format(@"<object class=""emojione"" data=""{0}{1}.svg"" type=""image/svg+xml"" standby=""{2}"">{2}</object>", ImagePathSvg, codepoint, unicodeAlt ? alt : shortname);
                    //}
                } else {
                    if (sprite) {
                        return string.Format(@"<span class=""emojione emojione-{0}"" title=""{1}"">{2}</span>", codepoint, shortname, unicodeAlt ? alt : shortname);
                    } else {
                        return string.Format(@"<img class=""emojione"" alt=""{0}"" src=""{1}{2}/{3}.png"" />", unicodeAlt ? alt : shortname, ImagePath, size ?? EmojiSize, codepoint);
                    }
                }
            }

            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string ShortnameToAsciiCallback(Match match) {
            // check if the emoji exists in our dictionaries
            var shortname = match.Value;
            if (SHORTNAME_TO_CODEPOINT.ContainsKey(shortname)) {
                var codepoint = SHORTNAME_TO_CODEPOINT[shortname];
                if (CODEPOINT_TO_ASCII.ContainsKey(codepoint)) {
                    return CODEPOINT_TO_ASCII[codepoint];
                }
            }

            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string ShortnameToUnicodeCallback(Match match) {
            // check if the emoji exists in our dictionaries
            var shortname = match.Value;
            if (SHORTNAME_TO_CODEPOINT.ContainsKey(shortname)) {
                // convert codepoint to unicode char
                return ToUnicode(SHORTNAME_TO_CODEPOINT[shortname]);
            }

            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string UnicodeToImageCallback(Match match, bool unicodeAlt, bool svg, bool sprite, int? size) {
            // check if the emoji exists in our dictionaries
            var codepoint = ToCodePoint(match.Groups[1].Value);
            if (CODEPOINT_TO_SHORTNAME.ContainsKey(codepoint)) {
                var shortname = CODEPOINT_TO_SHORTNAME[codepoint];
                string alt = unicodeAlt ? ToUnicode(codepoint) : shortname;
                if (svg) {
                    // TODO: inline svg
                    return null;
                    //if (sprite) {
                    //    return string.Format(@"<svg class=""emojione""><description>{0}</description><use xlink:href=""{1}emojione-sprite.svg#emoji-{2}""></use></svg>", unicodeAlt ? alt : shortname, ImagePathSprites, codepoint);
                    //} else {
                    //    return string.Format(@"<object class=""emojione"" data=""{0}{1}.svg"" type=""image/svg+xml"" standby=""{2}"">{2}</object>", ImagePathSvg, codepoint, unicodeAlt ? alt : shortname);
                    //}
                } else {
                    if (sprite) {
                        return string.Format(@"<span class=""emojione emojione-{0}"" title=""{1}"">{2}</span>", codepoint, shortname, unicodeAlt ? alt : shortname);
                    } else {
                        return string.Format(@"<img class=""emojione"" alt=""{0}"" src=""{1}{2}/{3}.png"" />", unicodeAlt ? alt : shortname, ImagePath, size ?? EmojiSize, codepoint);
                    }
                }
            }

            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        private static string UnicodeToShortnameCallback(Match match) {
            // check if the emoji exists in our dictionaries
            var unicode = match.Groups[1].Value;
            var codepoint = ToCodePoint(unicode);
            if (CODEPOINT_TO_SHORTNAME.ContainsKey(codepoint)) {
                return CODEPOINT_TO_SHORTNAME[codepoint];
            }

            // we didn't find a replacement so just return the entire match
            return match.Value;
        }

        /// <summary>
        /// Convert a unicode character to its code point/code pair
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        internal static string ToCodePoint(string unicode) {
            string codepoint = "";
            for (var i = 0; i < unicode.Length; i += char.IsSurrogatePair(unicode, i) ? 2 : 1) {
                if (i > 0) {
                    codepoint += "-";
                }
                codepoint += string.Format("{0:X4}", char.ConvertToUtf32(unicode, i));
            }
            return codepoint.ToLower();
        }

        /// <summary>
        /// Converts a unicode code point/code pair to a unicode character
        /// </summary>
        /// <param name="codepoints"></param>
        /// <returns></returns>
        internal static string ToUnicode(string codepoint) {
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
