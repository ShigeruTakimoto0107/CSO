using System;
using System.IO;
using System.Security.Principal;
using System.Diagnostics;

namespace PST
{
    class Program
    {
        static void Main(string[] args)
        {
            // 引数がない場合は空のコントローラーで起動（手動操作用）
            if (args.Length == 0)
            {
                Console.WriteLine("使用法: PST.exe [マクロファイルのパス]");
                using (PowerShellController ps = new PowerShellController(""))
                {
                    ps.WaitForExit();
                }
                return;
            }

            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine("エラー: ファイルが見つかりません - " + filePath);
                using (PowerShellController ps = new PowerShellController(""))
                {
                    ps.WaitForExit();
                }
                return;
            }

            try
            {
                Orchestrator orchestrator = new Orchestrator();
                orchestrator.Execute(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("実行エラー: " + ex.Message);
            }
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}