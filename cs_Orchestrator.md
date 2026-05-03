using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerShellTerminal
{
    public class Orchestrator
    {
        public void ExecuteFile(string filePath, PowerShellController ps)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            string[] lines = File.ReadAllLines(filePath, Encoding.Default);

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                // コマンド解析 (引用符対応)
                var matches = Regex.Matches(trimmedLine, @"(?<match>""[^""]*""|'[^']*'|[^\s]+)");
                if (matches.Count == 0) continue;

                string command = matches[0].Value.ToLower();
                string arg = matches.Count > 1 ? Unquote(matches[1].Value) : "";

                switch (command)
                {
                    case "sendln":
                        ps.SendLn(arg);
                        break;
                    case "wait":
                        ps.Wait(arg, 30000);
                        break;
                    case "pause":
                        int sec = int.TryParse(arg, out sec) ? sec : 1;
                        System.Threading.Thread.Sleep(sec * 1000);
                        break;
                    default:
                        // 未知のコマンドはそのままPowerShellへ
                        ps.SendLn(trimmedLine);
                        break;
                }
            }
        }

        private string Unquote(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if ((text.StartsWith("\"") && text.EndsWith("\"")) || (text.StartsWith("'") && text.EndsWith("'")))
            {
                return text.Substring(1, text.Length - 2);
            }
            return text;
        }
    }
}