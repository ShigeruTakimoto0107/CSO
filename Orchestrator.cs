using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerShellTerminal
{
    public class Orchestrator
    {
        public bool IsAdminRequired(string filePath)
        {
            if (!File.Exists(filePath)) return false;
            string[] lines = File.ReadAllLines(filePath, Encoding.Default);
            string firstCommand = GetFirstEffectiveCommand(lines);
            return firstCommand.ToLower() == "admin";
        }

        private string GetFirstEffectiveCommand(string[] lines)
        {
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                    continue;

                int spaceIndex = trimmed.IndexOf(' ');
                return spaceIndex == -1 ? trimmed : trimmed.Substring(0, spaceIndex);
            }
            return string.Empty;
        }

        public void ExecuteFile(string filePath, PowerShellController ps)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
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

                // adminコマンド自体は実行ループではスキップ
                if (trimmedLine.ToLower() == "admin") continue;

                List<string> args = ParseLine(trimmedLine);
                string command = args[0].ToLower();

                if (command == "sendln")
                {
                    if (args.Count > 1) ps.SendLn(args[1]);
                }
                else if (command == "wait")
                {
                    if (args.Count > 1) ps.Wait(args[1], 30000);
                }
                else if (command == "pause")
                {
                    int seconds = 1;
                    if (args.Count > 1 && int.TryParse(args[1], out seconds))
                    {
                        System.Threading.Thread.Sleep(seconds * 1000);
                    }
                }
                else
                {
                    // 未知のコマンドはそのままPowerShellへ送り、プロンプトを待機
                    ps.SendLn(trimmedLine);
                    ps.Wait(">", 30000);
                }
            }
        }

        private List<string> ParseLine(string line)
        {
            List<string> args = new List<string>();
            // 引用符対応のパースロジック
            MatchCollection matches = Regex.Matches(line, @"(?<match>[^\s""']+|""(?<inner>[^""]*)""|'(?<inner>[^']*)')");
            
            foreach (Match m in matches)
            {
                if (m.Groups["inner"].Success)
                    args.Add(m.Groups["inner"].Value);
                else
                    args.Add(m.Groups["match"].Value);
            }

            // 引数が1つ（コマンドのみ）の場合のフォールバック
            if (args.Count == 0) args.Add(line);
            
            return args;
        }
    }
}