using System;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: cso.exe [filename.csl]");
            return;
        }

        string filePath = args[0];
        using (PowerShellController ps = new PowerShellController())
        {
            try
            {
                // 起動直後の初期メッセージを読み飛ばすための猶予
                Thread.Sleep(1000);
                ps.ClearBuffer();

                Orchestrator orch = new Orchestrator();
                orch.ExecuteFile(filePath, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[CSO ERROR] " + ex.Message);
            }
        }
        Console.WriteLine("\nProcess completed. Press any key to exit...");
        Console.ReadKey();
    }
}