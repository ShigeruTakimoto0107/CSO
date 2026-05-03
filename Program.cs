using System;
using System.IO;
using System.Collections.Generic;

namespace PowerShellTerminal
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("使用法: PST.exe [マクロファイルパス (.psl)]");
                return;
            }

            string filePath = args[0];
            if (!File.Exists(filePath))
            {
                Console.WriteLine("エラー: ファイルが見つかりません: " + filePath);
                return;
            }

            try
            {
                // PowerShellプロセスの起動
                using (PowerShellController ps = new PowerShellController())
                {
                    Orchestrator orchestrator = new Orchestrator();
                    
                    // マクロの実行
                    orchestrator.ExecuteFile(filePath, ps);

                    // マクロ終了後、対話モードへ移行（exitと打つまで終了しない）
                    Console.WriteLine("\n--- マクロ実行完了。対話モードを開始します (exitで終了) ---");
                    while (true)
                    {
                        string input = Console.ReadLine();
                        if (string.IsNullOrEmpty(input)) continue;
                        if (input.Trim().ToLower() == "exit") break;

                        ps.SendLn(input);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("実行エラー: " + ex.Message);
            }
        }
    }
}