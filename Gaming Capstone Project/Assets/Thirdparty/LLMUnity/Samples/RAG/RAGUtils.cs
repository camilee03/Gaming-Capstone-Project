using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace LLMUnitySamples
{
    public class RAGUtils
    {
        public static List<string> SplitText(string text, int chunkSize = 300)
        {
            List<string> chunks = new List<string>();
            int start = 0;
            char[] delimiters = ".!;?\n\r".ToCharArray();

            while (start < text.Length)
            {
                int end = Math.Min(start + chunkSize, text.Length);
                if (end < text.Length)
                {
                    int nearestDelimiter = text.IndexOfAny(delimiters, end);
                    if (nearestDelimiter != -1) end = nearestDelimiter + 1;
                }
                chunks.Add(text.Substring(start, end - start).Trim());
                start = end;
            }
            return chunks;
        }

        public static Dictionary<string, List<string>> ReadGutenbergFile(string text)
        {
            Dictionary<string, List<string>> messages = new Dictionary<string, List<string>>();

            void AddMessage(string message, string name)
            {
                if (name == null) return;
                if (!messages.ContainsKey(name)) messages[name] = new List<string>();
                messages[name].Add(message);
            }

            // read the Hamlet play from the Gutenberg file
            string namePattern = "^[A-Z]+$"; // ^ = start, [] = character set, \+ = more than one, $ = end
            Regex nameRegex = new Regex(namePattern);

            string name = null;
            string message = "";
            int numWords = 0;
            int numLines = 0;

            string[] lines = text.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                line = line.Replace("\r", "");
                string lineTrim = line.Trim();

                numWords += line.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; // things split by space or tab
                numLines++;

                if (nameRegex.IsMatch(line)) // has name
                {
                    name = line;
                    AddMessage(message, name);
                    message = "";
                }
                else if (name != null)
                {
                    if (message != "") message += " ";
                    message += line;
                }
            }
            return messages;
        }

        public static List<string> ReadFile(string text)
        {
            List<string> allLines = new List<string>();

            string[] lines = text.Split("\n");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Replace("\r", "").Trim();
                allLines.Add(line);
            }

            return allLines;
        }
    }
}
