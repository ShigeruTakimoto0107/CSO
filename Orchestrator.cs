using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;

namespace PowerShellTerminal
{
    public class Orchestrator : IDisposable
    {
        public void ExecuteMacro(string[] lines, PowerShellController ps)
        {
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";")) continue;

                // 引数のパース（引用符対応）
                List<string> args = ParseArgs(trimmedLine);
                if (args.Count == 0) continue;

                string command = args[0].ToLower();

                switch (command)
                {
                    case "admin":
                        // Program.cs側で処理済みのためスキップ
                        break;

                    case "sendln":
                        if (args.Count > 1) ps.SendLn(args[1]);
                        break;

                    case "wait":
                        if (args.Count > 1) ps.Wait(args[1]);
                        break;

                    default:
                        // 未知のコマンドはそのまま送信してプロンプトを待つ（TeraTerm互換動作）
                        ps.SendLn(trimmedLine);
                        ps.Wait(">", 5000); 
                        break;
                }
            }
        }

        private List<string> ParseArgs(string input)
        {
            var args = new List<string>();
            // 引用符内を保持しつつスペースで分割する正規表現
            var regex = new Regex(@"(""(.*?)""|'(.*?)'|(\S+))");
            foreach (Match match in regex.Matches(input))
            {
                string val = match.Value;
                // 外側の引用符を外す
                if ((val.StartsWith("\"") && val.EndsWith("\"")) || (val.StartsWith("'") && val.EndsWith("'")))
                {
                    val = val.Substring(1, val.Length - 2);
                }
                args.Add(val);
            }
            return args;
        }

        public void Dispose() { }
    }
}