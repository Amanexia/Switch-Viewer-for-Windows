# Switch Viewer v0.1

汎用UVC/UAC系USBキャプチャボードからSwitchの映像と音声を取り込み、
Windows上で低遅延寄りに表示するための最小構成アプリ。

## 想定環境

- Windows 10/11
- .NET 8 SDK
- 汎用USBキャプチャボード
- キャプチャ映像デバイスがDirectShow/UVCとして認識されること
- キャプチャ音声がWaveIn/UACデバイスとして認識されること

今回確認した映像デバイス名: `USB3.0 Video`

## 起動

PowerShell / コマンドプロンプトでプロジェクトフォルダを開き、

```text
dotnet restore
dotnet run
```

またはReleaseビルド:

```text
dotnet publish -c Release -r win-x64 --self-contained true
```

生成物は `bin\Release\net8.0-windows\win-x64\publish\` に出ます。

## 操作

- F11 / Alt+Enter: フルスクリーン
- Esc: フルスクリーン解除
- Volume: 音量
- Mute: ミュート
- Always on top: 最前面
- Fullscreen: フルスクリーン

## Discord

起動後、Discordの画面共有で `Switch Viewer` のウィンドウを選択すれば、
通話しているときのSwitch画面共有に利用できます。

Discord側で音声共有が必要な場合は、Discordのアプリ音声共有設定も確認してください。

## 注意

汎用キャプチャボードは製品ごとに音声デバイス名・対応解像度・フレームレートが違います。
映像が出ても音声が出ない場合は、Windowsの「サウンド入力デバイス」に
キャプチャボードの音声デバイスが存在するか確認してください。

このv0.1は「まず映して音を出す」ことを優先した試作版です。
