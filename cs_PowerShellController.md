using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

public class PowerShellController : IDisposable
{
    private readonly Process _psProcess;
    private readonly StringBuilder _outputBuffer = new StringBuilder();
    private readonly object _bufferLock = new object();
    private bool _isDisposing = false;

    public PowerShellController()
    {
        _psProcess = new Process();
        _psProcess.StartInfo.FileName = "powershell.exe";
        _psProcess.StartInfo.Arguments = "-NoLogo -NoExit -ExecutionPolicy Bypass";
        _psProcess.StartInfo.UseShellExecute = false;
        _psProcess.StartInfo.RedirectStandardInput = true;
        _psProcess.StartInfo.RedirectStandardOutput = true;
        _psProcess.StartInfo.RedirectStandardError = true;
        _psProcess.StartInfo.CreateNoWindow = true;

        // 日本語環境のデフォルト(Shift-JIS)で受け取ることで文字化けを解消[cite: 1]
        _psProcess.StartInfo.StandardOutputEncoding = Encoding.Default;

        _psProcess.Start();

        Thread readThread = new Thread(ReadStream);
        readThread.IsBackground = true;
        readThread.Start();
    }

    private void ReadStream()
    {
        while (!_isDisposing)
        {
            try
            {
                if (_psProcess.HasExited) break;
                int ch = _psProcess.StandardOutput.Read();
                if (ch == -1) break;

                char c = (char)ch;
                lock (_bufferLock)
                {
                    _outputBuffer.Append(c);
                    // 親コンソールへそのまま出力
                    Console.Write(c); 
                }
            }
            catch { break; }
        }
    }

    public void SendLn(string command)
    {
        _psProcess.StandardInput.WriteLine(command);
    }

    public void ClearBuffer()
    {
        lock (_bufferLock)
        {
            _outputBuffer.Length = 0;
        }
    }

    public void Wait(string target, int timeoutMs)
    {
        DateTime start = DateTime.Now;
        while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
        {
            lock (_bufferLock)
            {
                string current = _outputBuffer.ToString();
                int index = current.IndexOf(target);
                if (index != -1)
                {
                    // 一致した箇所までを消費
                    _outputBuffer.Remove(0, index + target.Length);
                    return;
                }
            }
            Thread.Sleep(50);
        }
        throw new TimeoutException("Wait timeout: " + target);
    }

    public void Dispose()
    {
        _isDisposing = true;
        if (_psProcess != null)
        {
            try { if (!_psProcess.HasExited) _psProcess.Kill(); } catch { }
            _psProcess.Dispose();
        }
    }
}