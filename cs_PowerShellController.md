using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace PowerShellTerminal
{
    public class PowerShellController : IDisposable
    {
        private Process _process;
        private StringBuilder _outputBuffer = new StringBuilder();
        private AutoResetEvent _outputWaitHandle = new AutoResetEvent(false);

        public PowerShellController()
        {
            _process = new Process();
            _process.StartInfo.FileName = "powershell.exe";
            _process.StartInfo.Arguments = "-NoLogo -NoExit";
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.StandardOutputEncoding = Encoding.Default;

            _process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    lock (_outputBuffer)
                    {
                        _outputBuffer.AppendLine(e.Data);
                        // 画面に出力（ユーザーに見せる）
                        Console.WriteLine(e.Data);
                    }
                    _outputWaitHandle.Set();
                }
            };

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            // 【修正】起動時の初期プロンプトが表示されるまで最大5秒待機する
            // これにより、マクロ先頭の wait '>' が即座に成功するようになる
            InternalWaitPrompt(">", 5000);
        }

        public void SendLn(string command)
        {
            _process.StandardInput.WriteLine(command);
        }

        public bool Wait(string target, int timeoutMilliseconds = 30000)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMilliseconds)
            {
                lock (_outputBuffer)
                {
                    string currentOutput = _outputBuffer.ToString();
                    int index = currentOutput.IndexOf(target);
                    if (index >= 0)
                    {
                        // 見つかった文字列までのバッファを消費（TTLのwait挙動を模倣）
                        _outputBuffer.Remove(0, index + target.Length);
                        return true;
                    }
                }
                // 新しい出力が来るまで待機
                _outputWaitHandle.WaitOne(100);
            }
            return false;
        }

        // 内部用の軽量な待機メソッド
        private void InternalWaitPrompt(string target, int timeout)
        {
            Stopwatch sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout)
            {
                lock (_outputBuffer)
                {
                    if (_outputBuffer.ToString().Contains(target)) return;
                }
                Thread.Sleep(50);
            }
        }

        public void Dispose()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.Kill();
                _process.Dispose();
            }
        }
    }
}