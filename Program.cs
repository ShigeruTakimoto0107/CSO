using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

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
            Orchestrator orchestrator = new Orchestrator();

            // 管理者権限が必要なマクロかチェック
            if (orchestrator.IsAdminRequired(filePath))
            {
                if (!IsRunAsAdmin())
                {
                    RestartAsAdmin(filePath);
                    return;
                }
            }

            // コンストラクタ内でPowerShellプロセスが起動されるため、Init()は不要
            PowerShellController ps = new PowerShellController();
            try
            {
                // マクロファイルの実行
                Console.WriteLine("--- マクロ実行開始 ---");
                orchestrator.ExecuteFile(filePath, ps);
                Console.WriteLine("--- マクロ実行完了 (対話モードに移行します。終了するには 'exit' を入力してください) ---");

                // マクロ終了後、手動入力を受け付けるループ
                while (true)
                {
                    Console.Write("PST> ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrEmpty(input)) continue;

                    if (input.Trim().ToLower() == "exit")
                    {
                        ps.SendLn("exit");
                        break;
                    }

                    ps.SendLn(input);
                    ps.Wait(">", 30000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("エラー: " + ex.Message);
            }
            finally
            {
                // 既存のDisposeメソッドを呼び出してプロセスを安全に終了させる
                ps.Dispose();
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
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Application.ExecutablePath;
            startInfo.Arguments = "\"" + filePath + "\"";
            startInfo.Verb = "runas";

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