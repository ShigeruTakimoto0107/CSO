using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions; // 正規表現のために追加

public class Orchestrator
{
    public void ExecuteFile(string filePath, PowerShellController ps)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
        // UTF-8(BOMあり)ファイルを正しく読み込みます
        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        ExecuteMacro(new List<string>(lines), ps);
    }

    public void ExecuteMacro(List<string> commands, PowerShellController ps)
    {
        foreach (string line in commands)
        {
            string trimmedLine = line.Trim();
            // コメント行（# または ;）と空行をスキップ
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";")) continue;

            // --- 引数解析ロジックの修正 ---
            // 正規表現により、スペース区切りだが引用符内は保持する
            // 1. [^\s"']+ (引用符もスペースもない連続した文字)
            // 2. "([^"]*)" (ダブルクォートで囲まれた中身)
            // 3. '([^']*)' (シングルクォートで囲まれた中身)
            var matches = Regex.Matches(trimmedLine, @"(?<match>[^\s""']+)|""(?<match>[^""]*)""|'(?<match>[^']*)'");
            
            if (matches.Count == 0) continue;

            // 1番目のマッチをコマンド、2番目以降を引数とする
            string cmd = matches[0].Groups["match"].Value.ToLower();
            
            // 引数部分の取得（2番目以降の要素を結合するか、特定のコマンドでは2番目のみを使用）
            string arg = matches.Count > 1 ? matches[1].Groups["match"].Value : string.Empty;

            if (cmd == "sendln")
            {
                // sendln の場合は、第1引数（arg）を送信
                ps.SendLn(arg);
            }
            else if (cmd == "wait")
            {
                // 正規表現の Groups["match"].Value ですでにクォートは外れているためそのまま渡す
                ps.Wait(arg, 30000);
            }
            else if (cmd == "clearbuffer")
            {
                ps.ClearBuffer();
            }
            else if (cmd == "pause")
            {
                int seconds;
                if (!int.TryParse(arg, out seconds))
                {
                    seconds = 1;
                }
                System.Threading.Thread.Sleep(seconds * 1000);
            }
            else
            {
                // 未知のコマンド（PowerShell直接実行）
                // ここは元の行をそのまま送る
                ps.SendLn(trimmedLine);
                ps.Wait(">", 30000);
            }
        }
    }
}