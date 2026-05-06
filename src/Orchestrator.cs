using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PST
{
    public class Orchestrator
    {
        public void Execute(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            if (Array.Exists(lines, l => l.Trim().ToLower() == "admin"))
            {
                if (!Program.IsAdministrator())
                {
                    RestartAsAdmin(filePath);
                    return;
                }
            }

            // PowerShellControllerを起動（ここで画面のPOPアップを待機する）
            using (PowerShellController ps = new PowerShellController(""))
            {
                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("//") || trimmedLine.StartsWith(";"))
                    {
                        continue;
                    }
                    if (trimmedLine.ToLower() == "admin") continue;

                    // コマンドと引数の分離
                    string cmd;
                    string args = "";
                    int firstSpace = trimmedLine.IndexOf(' ');

                    if (firstSpace > 0)
                    {
                        cmd = trimmedLine.Substring(0, firstSpace).ToLower();
                        args = trimmedLine.Substring(firstSpace + 1).Trim();

                        // 引用符 (' または ") で囲まれている場合は中身を取り出す
                        if ((args.StartsWith("'") && args.EndsWith("'")) || (args.StartsWith("\"") && args.EndsWith("\"")))
                        {
                            args = args.Substring(1, args.Length - 2);
                        }
                    }
                    else
                    {
                        cmd = trimmedLine.ToLower();
                    }

                    switch (cmd)
                    {
                        case "wait":
                            ps.Wait(args);
                            break;

                        case "sendln":
                            ps.SendCommand(args);
                            break;

                        case "pause":
                            int seconds;
                            if (int.TryParse(args, out seconds))
                            {
                                System.Threading.Thread.Sleep(seconds * 1000);
                            }
                            break;

                        default:
                            // 未知のコマンドはそのままPowerShellに送信し、プロンプトを待機
                            ps.SendCommand(trimmedLine);
                            ps.WaitPrompt();
                            break;
                    }
                }
                // マクロ終了後、ユーザーが手動操作を終えるまで待機
                ps.WaitForExit();
            }
        }

        private void RestartAsAdmin(string filePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
            startInfo.Arguments = "\"" + filePath + "\"";
            startInfo.Verb = "runas";

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine("管理者権限での再起動に失敗しました: " + ex.Message);
            }
        }
    }
}