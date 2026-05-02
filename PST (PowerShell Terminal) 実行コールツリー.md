PST (PowerShell Terminal) 実行コールツリー
Plaintext
Main (Program.cs)
│  ※ プログラムの起点。コントローラーとオーケストレーターを初期化
│
└─ Orchestrator.Run()
   │  ※ メインループ。ユーザー入力またはマクロファイルを監視
   │
   ├─ PowerShellController.WritePrompt()
   │     ※ コンソールに現在のカレントディレクトリ（PS C:\...>）を表示
   │
   ├─ [分岐] ユーザーが直接入力した場合
   │  └─ Orchestrator.ProcessCommand(input)
   │
   ├─ [分岐] マクロファイル (.csl) を読み込んだ場合
   │  └─ Orchestrator.ExecuteMacro(filePath)
   │     └─ (ループ) Orchestrator.ParseLine(line)
   │
   └─ Orchestrator.DispatchCommand(command, args)
      │  ※ 解析されたコマンド（sendln, wait等）を識別して各メソッドへ振分
      │
      ├─ [sendln] ── PowerShellController.SendCommand(text)
      │     │  ※ PowerShellプロセスの標準入力(StandardInput)へ書き込み
      │     └─ PowerShellController.Wait("> ", timeout)
      │        ※ 送信後、プロンプトが戻るまで待機（自動同期）
      │
      ├─ [wait] ── PowerShellController.Wait(target, timeout)
      │     │  ※ 指定した文字列がバッファに出現するまでスレッドをブロック
      │     └─ (内部監視) PowerShellController._outputBuffer
      │
      ├─ [pause] ── System.Threading.Thread.Sleep(ms)
      │
      └─ [その他] ── PowerShellController.SendCommand(rawText)
            ※ 未知のコマンドはそのままPowerShellへパススルー
主要コンポーネントの解説
1. Program.cs (Entry Point)
役割: オブジェクトの生成と生存期間の管理。

解説: PowerShellControllerをインスタンス化してプロセスを起動し、それをOrchestratorに渡して制御を開始します。

2. Orchestrator.cs (The Brain)
役割: 命令の解釈（パーサー）と実行管理。

解説:

ExecuteMacro: 行単位でスクリプトを読み込み、コメントを除去して実行可能な形に整えます。

DispatchCommand: 独自の「マクロ言語（PST専用コマンド）」と「生のPowerShellコマンド」を切り分ける司令塔です。

3. PowerShellController.cs (The Heart)
役割: powershell.exe プロセスとの低レイヤ通信。

解説:

非同期読み取り: 標準出力を別スレッドで常時監視し、共有バッファ（_outputBuffer）に蓄積します。

Waitメソッド: このシステムの核心部です。バッファ内を検索し、目的の文字列が見つかるまでメインスレッドを待機させることで、非同期なPowerShell操作を「同期処理（マクロ順次実行）」として扱えるようにしています。

エコー・キラー: 自分が送ったコマンドがそのまま出力として戻ってくる現象（エコーバック）を無視し、純粋な実行結果だけを判定対象にするロジックが含まれています。
