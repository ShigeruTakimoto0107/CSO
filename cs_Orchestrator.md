using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerShellTerminal
{
    public class Orchestrator
    {
        /// <summary>
        /// コメントや空行を飛ばして、実質的な最初のコマンドを返します
        /// </summary>
        public string GetFirstEffectiveCommand(string[] lines)
        {
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                // 空行およびコメント行をスキップ
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                {
                    continue;
                }

                // 最初に見つかった有効な行の、コマンド部分（最初のスペースまで）を返す
                int spaceIndex = trimmed.IndexOf(' ');
                return spaceIndex == -1 ? trimmed : trimmed.Substring(0, spaceIndex);
            }
            return string.Empty;
        }

        public void ExecuteFile(string filePath, PowerShellController ps)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            // UTF-8 (BOMあり/なし両対応) で読み込み
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            ExecuteMacro(new List<string>(lines), ps);
        }

        public void ExecuteMacro(List<string> commands, PowerShellController ps)
        {
            foreach (string line in commands)
            {
                string trimmedLine = line.Trim();

                // コメント行・空行のスキップ
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";"))
                {
                    continue;
                }

                // adminコマンド自体は実行ループ内では無視する
                if (trimmedLine.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // 引数解析（クォート対応）
                List<string> args = ParseArguments(trimmedLine);
                if (args.Count == 0) continue;

                string command = args[0].ToLower();

                if (command == "sendln")
                {
                    string text = args.Count > 1 ? args[1] : "";
                    ps.SendLn(text);
                }
                else if (command == "wait")
                {
                    string target = args.Count > 1 ? args[1] : ">";
                    ps.Wait(target, 30000);
                }
                else if (command == "pause")
                {
                    int seconds = 1;
                    // C# 4.0互換のため、out変数を事前に宣言
                    int parsed;
                    if (args.Count > 1 && int.TryParse(args[1], out parsed))
                    {
                        seconds = parsed;
                    }
                    System.Threading.Thread.Sleep(seconds * 1000);
                }
                else
                {
                    // 未知のコマンドはそのままPowerShellへ送り、プロンプトを待機
                    ps.SendLn(trimmedLine);
                    ps.Wait(">", 30000);
                }
            }
        }

        private List<string> ParseArguments(string line)
        {
            List<string> args = new List<string>();
            // 正規表現でクォート内またはスペース区切りの単語を抽出
            MatchCollection matches = Regex.Matches(line, @"(?<match>""[^""]*""|'[^']*'|[^\s]+)");

            foreach (Match m in matches)
            {
                args.Add(Unquote(m.Groups["match"].Value));
            }
            return args;
        }

        private string Unquote(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if ((text.StartsWith("\"") && text.EndsWith("\"")) || (text.StartsWith("'") && text.EndsWith("'")))
            {
                if (text.Length >= 2)
                {
                    return text.Substring(1, text.Length - 2);
                }
            }
            return text;
        }
    }
}