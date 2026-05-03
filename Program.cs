using System;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: pst.exe [filename.csl]");
            return;
        }

        string macroPath = args[0];
        if (!File.Exists(macroPath))
        {
            Console.WriteLine("[ERROR] File not found: " + macroPath);
            return;
        }

        PowerShellController ps = null;
        try
        {
            ps = new PowerShellController();
            Orchestrator engine = new Orchestrator();

            // PowerShellの起動を少し待機してからバッファをクリア
            //System.Threading.Thread.Sleep(1000);
            ps.ClearBuffer();

            // マクロの実行
            // 内部で1行目が Admin の場合は再起動がかかり、このプロセスは終了する
            engine.ExecuteFile(macroPath, ps);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FATAL ERROR] " + ex.Message);
        }
        finally
        {
            if (ps != null)
            {
                ps.Dispose();
            }
        }

        Console.WriteLine("[PST] Finished. Press any key to exit...");
        Console.ReadKey();
    }
}