using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading; // ← これを追加

namespace PowerShellTerminal
{
    public class Orchestrator
    {
        public void ExecuteFile(string filePath, PowerShellController ps)
        {
            // エンコーディングを環境に合わせて調整（Shift-JIS/Default）
            string[] lines = File.ReadAllLines(filePath, Encoding.Default);
            ExecuteMacro(new List<string>(lines), ps);
        }

        public void ExecuteMacro(List<string> commands, PowerShellController ps)
        {
            foreach (string line in commands)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                    continue;

                // コマンドと引数の解析（正規表現でクォートに対応）
                var matches = Regex.Matches(trimmedLine, @"(?<match>""[^""]*""|'[^']*'|\S+)");
                if (matches.Count == 0) continue;

                string command = matches[0].Value.ToLower();
                List<string> args = new List<string>();
                for (int i = 1; i < matches.Count; i++)
                {
                    args.Add(Unquote(matches[i].Value));
                }

                switch (command)
                {
                    case "wait":
                        if (args.Count > 0)
                        {
                            if (!ps.Wait(args[0]))
                            {
                                Console.WriteLine("Wait Timeout: " + args[0]);
                            }
                        }
                        break;

                    case "sendln":
                        string textToSend = args.Count > 0 ? args[0] : "";
                        ps.SendLn(textToSend);
                        break;

                    case "pause":
                        int sec = 1;
                        // 引数がある場合は数値変換を試みる
                        if (args.Count > 0) int.TryParse(args[0], out sec);
                        Thread.Sleep(sec * 1000);
                        break;

                    default:
                        // 未知のコマンドはそのままPowerShellに送信し、次のプロンプトを待つ
                        ps.SendLn(trimmedLine);
                        ps.Wait(">");
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