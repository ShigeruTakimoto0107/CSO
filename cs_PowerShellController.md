using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PowerShellTerminal
{
    public class PowerShellController : IDisposable
    {
        private Process _psProcess;
        private StringBuilder _outputBuffer = new StringBuilder();
        private AutoResetEvent _dataEvent = new AutoResetEvent(false);

        public PowerShellController()
        {
            _psProcess = new Process();
            _psProcess.StartInfo.FileName = "powershell.exe";
            _psProcess.StartInfo.Arguments = "-NoExit -NoLogo";
            _psProcess.StartInfo.UseShellExecute = false;
            _psProcess.StartInfo.RedirectStandardInput = true;
            _psProcess.StartInfo.RedirectStandardOutput = true;
            _psProcess.StartInfo.RedirectStandardError = true;
            _psProcess.StartInfo.CreateNoWindow = true;
            _psProcess.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(932); // Shift-JIS

            _psProcess.OutputDataReceived += (s, e) => {
                if (e.Data != null)
                {
                    lock (_outputBuffer) { _outputBuffer.Append(e.Data + Environment.NewLine); }
                    Console.WriteLine(e.Data); // 画面に出力
                    _dataEvent.Set();
                }
            };

            _psProcess.Start();
            _psProcess.BeginOutputReadLine();
            _psProcess.BeginErrorReadLine();
            
            // 起動直後のわずかな待機（プロンプトが出るのを待つ）
            Thread.Sleep(200); 
        }

        public void SendLn(string command)
        {
            _psProcess.StandardInput.WriteLine(command);
        }

        public bool Wait(string target, int timeoutMs = 30000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                lock (_outputBuffer)
                {
                    string currentOutput = _outputBuffer.ToString();
                    if (currentOutput.Contains(target))
                    {
                        // 見つかったらそこまでのバッファをクリア（次の待機に備える）
                        int index = currentOutput.IndexOf(target);
                        _outputBuffer.Remove(0, index + target.Length);
                        return true;
                    }
                }
                _dataEvent.WaitOne(100);
            }
            return false;
        }

        public void Dispose()
        {
            if (!_psProcess.HasExited)
            {
                _psProcess.Kill();
            }
            _psProcess.Dispose();
        }
    }
}