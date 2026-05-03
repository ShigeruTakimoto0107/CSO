using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PowerShellTerminal
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("使用法: pst.exe <macro_file.psl>");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine("エラー: ファイルが見つかりません: " + filePath);
                return;
            }

            // マクロファイルを読み込み、コメントを除いた最初の有効なコマンドを確認
            Orchestrator orchestrator = new Orchestrator();
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            string firstCommand = orchestrator.GetFirstEffectiveCommand(lines);

            // 管理者権限が必要か判定
            if (firstCommand.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsRunAsAdmin())
                {
                    RestartAsAdmin(filePath);
                    return;
                }
            }

            // メイン処理の実行
            using (PowerShellController ps = new PowerShellController())
            {
                try
                {
                    orchestrator.ExecuteMacro(new List<string>(lines), ps);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー: " + ex.Message);
                }
            }
        }

        static bool IsRunAsAdmin()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RestartAsAdmin(string filePath)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.FileName = Assembly.GetExecutingAssembly().Location;
            proc.Arguments = "\"" + filePath + "\"";
            proc.Verb = "runas"; // 管理者として実行
            proc.UseShellExecute = true;

            try
            {
                Process.Start(proc);
            }
            catch (Exception)
            {
                Console.WriteLine("管理者権限への昇格がキャンセルされました。");
            }
        }
    }
}