using System;
using System.Diagnostics;
using System.Security.Principal;
using System.IO;
using System.Linq;

namespace PowerShellTerminal
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: pst.exe <macro_file.psl>");
                return;
            }

            string filePath = Path.GetFullPath(args[0]);
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Error: File not found - " + filePath);
                return;
            }

            // マクロの「実質的な1行目」が admin かどうかを判定
            string[] lines = File.ReadAllLines(filePath);
            string firstCommand = GetFirstCommand(lines);

            if (firstCommand != null && firstCommand.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsAdmin())
                {
                    RunAsAdmin(filePath);
                    return;
                }
            }

            // メイン処理の実行
            using (var ps = new PowerShellController())
            using (var orchestrator = new Orchestrator())
            {
                orchestrator.ExecuteMacro(lines, ps);
                
                // マクロ終了後に入力待ちにする（対話モード）
                while (true)
                {
                    string input = Console.ReadLine();
                    if (input == null || input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase)) break;
                    ps.SendLn(input);
                }
            }
        }

        static string GetFirstCommand(string[] lines)
        {
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#") || trimmed.StartsWith(";")) continue;
                return trimmed.Split(' ')[0];
            }
            return null;
        }

        static bool IsAdmin()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RunAsAdmin(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            startInfo.Arguments = "\"" + filePath + "\"";
            startInfo.Verb = "runas";
            startInfo.UseShellExecute = true;
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("管理者権限での実行がキャンセルされました: " + ex.Message);
            }
        }
    }
}