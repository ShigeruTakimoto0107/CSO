using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

            // 最初のスペースでコマンドと引数を分割
            int spaceIndex = trimmedLine.IndexOf(' ');
            string cmd = spaceIndex == -1 ? trimmedLine.ToLower() : trimmedLine.Substring(0, spaceIndex).ToLower();
            string arg = spaceIndex == -1 ? string.Empty : trimmedLine.Substring(spaceIndex + 1).Trim();

            if (cmd == "sendln")
            {
                // 引数をそのまま送信し、PowerShell側でクォートを解釈させます
                ps.SendLn(arg);
            }
            else if (cmd == "wait")
            {
                // 待機文字列を判定する際は、念のためクォートを外して比較します
                ps.Wait(Unquote(arg), 30000);
            }
            else if (cmd == "clearbuffer")
            {
                ps.ClearBuffer();
            }
            else if (cmd == "pause")
            {
                // 指定秒数待機。数値変換に失敗した場合は1秒として処理
                int seconds;
                if (!int.TryParse(arg, out seconds))
                {
                    seconds = 1;
                }
                System.Threading.Thread.Sleep(seconds * 1000);
            }
            else
            {
                // 未知のコマンドはそのままPowerShellへ送り、プロンプトを待ちます
                ps.SendLn(trimmedLine);
                ps.Wait(">", 30000);
            }
        }
    }

    private string Unquote(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if ((text.StartsWith("\"") && text.EndsWith("\"")) ||
            (text.StartsWith("'") && text.EndsWith("'")))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }
}