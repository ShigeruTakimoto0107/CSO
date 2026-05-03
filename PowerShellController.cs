using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace PowerShellTerminal
{
    public class PowerShellController : IDisposable
    {
        private Process psProcess;
        private StringBuilder outputBuffer = new StringBuilder();
        private readonly object bufferLock = new object();

        public PowerShellController()
        {
            psProcess = new Process();
            psProcess.StartInfo.FileName = "powershell.exe";
            // -NoPromptを付けず、標準のプロンプトを出力させる
            psProcess.StartInfo.Arguments = "-NoExit -ExecutionPolicy Bypass";
            psProcess.StartInfo.UseShellExecute = false;
            psProcess.StartInfo.RedirectStandardInput = true;
            psProcess.StartInfo.RedirectStandardOutput = true;
            psProcess.StartInfo.RedirectStandardError = true;
            psProcess.StartInfo.CreateNoWindow = true;
            psProcess.StartInfo.StandardOutputEncoding = Encoding.Default;

            psProcess.OutputDataReceived += (s, e) => {
                if (e.Data != null)
                {
                    lock (bufferLock)
                    {
                        outputBuffer.AppendLine(e.Data);
                        Console.WriteLine(e.Data);
                    }
                }
            };

            psProcess.Start();
            psProcess.BeginOutputReadLine();
            psProcess.BeginErrorReadLine();

            // 起動直後のプロンプト ">" を待機して初期表示を完了させる
            Wait(">", 5000);
        }

        public void SendLn(string command)
        {
            outputBuffer.Clear(); // 送信前にバッファをクリアして判定精度を高める
            psProcess.StandardInput.WriteLine(command);
            // コマンド送信後、プロンプトが戻るまで待機
            Wait(">", 30000);
        }

        public bool Wait(string target, int timeoutMs)
        {
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                lock (bufferLock)
                {
                    string currentOutput = outputBuffer.ToString();
                    if (currentOutput.Contains(target))
                    {
                        return true;
                    }
                }
                Thread.Sleep(50);
            }
            return false;
        }

        public void Dispose()
        {
            if (psProcess != null && !psProcess.HasExited)
            {
                psProcess.Kill();
                psProcess.Dispose();
            }
        }
    }
}