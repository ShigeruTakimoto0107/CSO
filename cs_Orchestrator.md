using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Security.Principal;

public class Orchestrator
{
    public void ExecuteFile(string filePath, PowerShellController ps)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);

        // UTF-8(BOMあり/なし)を考慮して読み込み
        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        List<string> commands = new List<string>(lines);

        // --- 管理者昇格チェック (Step: Admin対応) ---
        if (commands.Count > 0 && commands[0].Trim().Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            if (!IsRunAsAdmin())
            {
                Console.WriteLine("[INFO] Admin directive detected. Restarting as Administrator...");
                RestartAsAdmin(filePath);
                return; // 現在のプロセス（非管理者）を終了
            }
            // 既に管理者の場合は、1行目の "Admin" をスキップして継続
            commands.RemoveAt(0);
        }
        // --------------------------------------------

        ExecuteMacro(commands, ps);
    }

    public void ExecuteMacro(List<string> commands, PowerShellController ps)
    {
        foreach (string line in commands)
        {
            string trimmedLine = line.Trim();

            // コメント行（# または ;）と空行をスキップ
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";")) continue;

            // クォート対応の字句解析 (Regex)
            // コマンドと引数を分離（例: wait "Success Message"）
            MatchCollection matches = Regex.Matches(trimmedLine, @"(?<match>""[^""]*""|'[^']*'|[^\s]+)");
            if (matches.Count == 0) continue;

            string cmd = matches[0].Value.ToLower();
            string arg = matches.Count > 1 ? Unquote(matches[1].Value) : string.Empty;

            if (cmd == "sendln")
            {
                ps.SendLn(arg);
            }
            else if (cmd == "wait")
            {
                ps.Wait(arg, 30000);
            }
            else if (cmd == "clearbuffer")
            {
                ps.ClearBuffer();
            }
            else if (cmd == "pause")
            {
                int seconds;
                if (!int.TryParse(arg, out seconds)) seconds = 1;
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
        if ((text.StartsWith("\"") && text.EndsWith("\"")) || (text.StartsWith("'") && text.EndsWith("'")))
        {
            return text.Substring(1, text.Length - 2);
        }
        return text;
    }

    // 現在のプロセスが管理者権限で実行されているか確認
    private bool IsRunAsAdmin()
    {
        WindowsIdentity id = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(id);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    // 自分自身を管理者権限で再起動
    private void RestartAsAdmin(string filePath)
    {
        ProcessStartInfo psi = new ProcessStartInfo();
        psi.FileName = Process.GetCurrentProcess().MainModule.FileName;
        psi.Arguments = "\"" + filePath + "\""; // 実行中のマクロパスを引数に渡す
        psi.Verb = "runas"; // 管理者として実行
        psi.UseShellExecute = true; // Verb="runas" のために必須

        try
        {
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERROR] Failed to elevate: " + ex.Message);
        }
    }
}