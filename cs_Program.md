using System;
using System.IO;
using System.Collections.Generic;

namespace PowerShellTerminal
{
    class Program
    {
        static void Main(string[] args)
        {
            // 管理者権限チェック（Orchestratorからの再起動判定に使用）
            if (args.Length > 0 && args[0].EndsWith(".psl"))
            {
                RunMacro(args[0]);
            }
            else
            {
                Console.WriteLine("PowerShell Terminal (PST) - .pslファイルを指定してください。");
            }
        }

        static void RunMacro(string filePath)
        {
            using (PowerShellController ps = new PowerShellController())
            {
                Orchestrator orchestrator = new Orchestrator();
                
                try
                {
                    // マクロの実行
                    orchestrator.ExecuteFile(filePath, ps);

                    // マクロ終了後の対話モード
                    // PST> プロンプトを廃止し、PowerShellのプロンプトをそのまま活かす
                    while (true)
                    {
                        string input = Console.ReadLine();
                        if (string.IsNullOrEmpty(input)) continue;
                        if (input.ToLower() == "exit") break;

                        ps.SendLn(input);
                        // SendLnの内部でプロンプト ">" を待機し表示するため、
                        // ここで手動のプロンプト出力は行わない
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー: " + ex.Message);
                }
            }
        }
    }
}