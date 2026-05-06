using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;

namespace PST
{
    public class PowerShellController : IDisposable
    {
        // --- Win32 API Definitions (Must be at class level) ---
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadConsoleOutputCharacter(IntPtr hConsoleOutput, StringBuilder lpCharacter, uint nLength, Coord dwReadCoord, out uint lpNumberOfCharsRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [StructLayout(LayoutKind.Sequential)]
        struct Coord { public short X; public short Y; }
        // -------------------------------------------------------

        private Process _process;
        private Form _inputForm;
        private TextBox _txtInput;
        private ManualResetEvent _uiReadyEvent = new ManualResetEvent(false);

        public PowerShellController(string initialCommand = "")
        {
            // 1. まずUIブリッジを初期化し、表示を待機する
            InitializeInputBridge();
            _uiReadyEvent.WaitOne(3000); 

            // 2. 画面が出た後にPowerShellプロセスを開始する
            string args = "-NoExit -ExecutionPolicy Bypass";
            if (!string.IsNullOrEmpty(initialCommand))
            {
                args += " -Command \"" + initialCommand.Replace("\"", "\"\"") + "\"";
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = false
            };

            try
            {
                _process = Process.Start(startInfo);
                _process.StandardInput.AutoFlush = true;
            }
            catch (Exception ex)
            {
                throw new Exception("PowerShellの起動に失敗しました: " + ex.Message);
            }
        }

        private void InitializeInputBridge()
        {
            Thread uiThread = new Thread(() =>
            {
                _inputForm = new Form 
                { 
                    Text = "PST Input Bridge - " + (Program.IsAdministrator() ? "Admin" : "User"), 
                    Width = 600, 
                    Height = 400, 
                    TopMost = true 
                };
                
                _txtInput = new TextBox 
                { 
                    Multiline = true, 
                    Dock = DockStyle.Fill, 
                    ScrollBars = ScrollBars.Vertical, 
                    Font = new Font("Consolas", 10),
                    BackColor = Color.Black,
                    ForeColor = Color.LightGreen
                };
                
                _inputForm.Shown += (s, e) => _uiReadyEvent.Set();

                _txtInput.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        string[] lines = _txtInput.Lines;
                        if (lines.Length > 0)
                        {
                            string lastCommand = lines[lines.Length - 1];
                            if (_process != null && !_process.HasExited)
                            {
                                _process.StandardInput.WriteLine(lastCommand);
                                _txtInput.AppendText(Environment.NewLine);
                            }
                        }
                    }
                };

                _inputForm.Controls.Add(_txtInput);
                Application.Run(_inputForm);
            });
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.IsBackground = true;
            uiThread.Start();
        }

        private void UpdateUI(string text)
        {
            if (_txtInput != null && _txtInput.IsHandleCreated)
            {
                _txtInput.Invoke(new MethodInvoker(() => {
                    _txtInput.AppendText(text + Environment.NewLine);
                    _txtInput.SelectionStart = _txtInput.TextLength;
                    _txtInput.ScrollToCaret();
                }));
            }
        }

        public void SendCommand(string command)
        {
            if (_process != null && !_process.HasExited)
            {
                UpdateUI("> " + command);
                _process.StandardInput.WriteLine(command);
            }
        }

        public void Wait(string target)
        {
            IntPtr hConsole = GetStdHandle(-11); // STD_OUTPUT_HANDLE
            StringBuilder sb = new StringBuilder(8192);

            while (_process != null && !_process.HasExited)
            {
                uint read;
                // コンソール画面の先頭から内容を読み取る
                if (ReadConsoleOutputCharacter(hConsole, sb, (uint)sb.Capacity, new Coord { X = 0, Y = 0 }, out read))
                {
                    if (sb.ToString().Contains(target))
                    {
                        break;
                    }
                }
                Thread.Sleep(50);
            }
        }

        public void WaitPrompt()
        {
            Wait(">");
        }

        public void WaitForExit()
        {
            if (_process != null && !_process.HasExited)
            {
                _process.WaitForExit();
            }
        }

        public void Dispose()
        {
            if (_inputForm != null && !_inputForm.IsDisposed)
            {
                _inputForm.Invoke(new MethodInvoker(() => _inputForm.Close()));
            }
            if (_process != null)
            {
                _process.Close();
                _process = null;
            }
        }
    }
}