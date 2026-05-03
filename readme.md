# PowerShell Terminal (PST)

PowerShell Terminal (PST) は、Windows PowerShell をバックグラウンドで操作し、TeraTerm マクロ (TTL) のようなスクリプト実行を可能にするオートメーションツールです。

## プロジェクト概要

本プロジェクトは、C# で実装されたコアエンジンにより PowerShell プロセスをリダイレクト制御し、独自のスクリプト形式（.psl）を用いて自動化処理を簡潔に記述することを目指しています。

### 主な特徴

- **Simple is Best**: コンソールに出力されるテキストを「正」とする確実な制御。
- **テラターム互換の操作感**: `sendln`（送信）や `wait`（待機）といった馴染みのあるコマンド体系。
- **PowerShell のフル活用**: Windows 標準の強力なシェル機能をそのまま自動化に利用可能。
- **環境依存の低さ**: .NET Framework 4.0 互換の堅牢な設計。

## スクリプト仕様 (.psl)

スクリプトファイルは 1 行 1 コマンド形式で記述します。拡張子は **.psl** です。

| コマンド | 引数 | 説明 |
| :--- | :--- | :--- |
| **sendln** | `[文字列]` | 指定した文字列を PowerShell へ送信します。 |
| **wait** | `[文字列]` | 指定した文字列が標準出力に出現するまで待機します。 |
| **pause** | `[秒数]` | 指定した秒数だけ実行を停止します。 |
| **admin** | なし | スクリプトを管理者権限で再起動して実行します。 |

- **コメント**: `#` または `;` で始まる行はコメントとして読み飛ばされます。
- **直接実行**: 未知のコマンドはそのまま PowerShell に送信され、プロンプト(`>`)が戻るまで待機します。

## アーキテクチャ

- **Program.cs**: アプリケーションの起動とスクリプトファイルの読み込み管理。
- **PowerShellController.cs**: PowerShell プロセスの制御、非同期バッファ管理、および応答待機ロジック。
- **Orchestrator.cs**: スクリプトの解析（パース）とコマンドの実行。

## 開発環境と制約

- **言語**: C# (.NET Framework 4.0 互換)
- **文字コード**: 日本語 Windows 環境 (Shift-JIS / Encoding.Default)
- **リポジトリ**: [https://github.com/ShigeruTakimoto0107/PST](https://github.com/ShigeruTakimoto0107/PST)

---
*旧プロジェクト名: C-S-O (Console-System-Orchestration)*
