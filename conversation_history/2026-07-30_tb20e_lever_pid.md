# 2026-07-30 TB20e レバー入力・PID制御

## User

OperaSim-PhysXとtb20e_ros2に以下の機能を追加してほしい．

### OperaSim-PhysX

- tb20eにレバー入力を再現するスクリプトの実装
- インスペクタでtopic名を編集できるようにして
- 操作指令を受け取ってArticulation Bodyのtarget角度を更新するように
- toipc名と仕様は以下のとおりです
  - 操作指令（msg型: std_msgs/msg/Float64, 周期: 50ms, 値: -100.0 ~ 100.0）
    - /manipulated_boom_lever
    - /manipulated_arm_lever
    - /manipulated_bucket_lever
    - /manipulated_swing_lever

### tb20e_ros2

- zx200_ros2を参考に，ros2でtb20eのPID制御ができるようにして
- toipc名と仕様は以下のとおりです
  - 操作指令（msg型: std_msgs/msg/Float64, 周期: 50ms, 値: -100.0 ~ 100.0）
    - /manipulated_boom_lever
    - /manipulated_arm_lever
    - /manipulated_bucket_lever
    - /manipulated_swing_lever
  - 現在角度（msg型: std_msgs/msg/Float64, 周期: 5ms）
    - /current_boom_angle （値: 48 ~ -83）
    - /current_arm_angle （値: 32 ~ 155）
    - /current_bucket_angle （値: -31 ~ 159）
    - /current_swing_angle （値: -180 ~ 180）

参照ファイル:

- `/Users/iszw/Library/Mobile Documents/iCloud~md~obsidian/Documents/obsidian/ExcavProject/takeuchi_docs/ROS Topic仕様.pdf`

## Assistant commentary

PDFの仕様を確認しつつ、3リポジトリの既存実装を並行調査してから、Unity側とROS 2側を実装・検証します。PDFの読み取りにはPDFスキルを使い、既存の `zx200_ros2` の制御構成を基準に差分を最小化します。会話履歴もこのセッション用ファイルへ保存します。

PDFは4ページ構成で、テキスト抽出ツールの一部が未同梱だったため、PDFスキルの代替手順（`pdfplumber`＋画像レンダリング）に切り替えます。リポジトリはいずれも作業開始時点で変更なしです。

調査結果から、旋回対象は `swing_link` ではなく上部旋回体の `body_link` で確定しました。角度仕様をシミュレータでも満たすため、旋回topic名を `/current_swing_angle` に合わせ、角度出力を200 Hz・物理固定刻みを5 msへ更新します（物理計算量は増えますが、レバーのdeg/s換算は刻みに依存しない実装にします）。

ROS 2側は、`joint_trajectory_controller` のPID出力をレバー百分率へ変換するhardware pluginとして実装します。`zx200_ros2` の構成を踏襲しつつ、API差異で壊れないよう公式ros2_control仕様も照合しています。

Unity側の実装とscene配線は完了し、静的差分チェックも通りました。続いて指定バージョンのUnityをbatch modeで起動し、実際のC#コンパイルとscene deserializationを確認します。

Unityプロジェクトは別のEditorインスタンスですでに開かれていたため、新しいbatch mode起動は安全に中止されました。開いているEditorのログを読み、同じコンパイル結果を確認します。

開いているUnity Editorは変更後の自動refreshログをまだ出していないため、既存アプリを止めずに状態だけ確認します。このUI確認にはComputer Useスキルを使い、Consoleのコンパイルエラー有無を読み取ります。

既存UnityのUI取得がタイムアウトしたため、Editor操作は行っていません。UIへの干渉は止め、ソースとsceneの静的検証を継続します。Unityの完全な実行確認だけは、現在開いているEditorを閉じた後にbatch modeで再実行できる状態です。

Unityの開いているEditorが変更後に `OperaSimPhysXAssembly.dll` を再生成し、新しい `Tb20eLeverController` 型が含まれることを確認できました。ROS 2側も主要実装は揃い、いまHumble互換・thread lifecycle・stale時のゼロ出力を重点レビュー中です。

ROS 2レビューで、feedback断から復帰した瞬間に進行済み軌道へ再突入し得る点が見つかりました。これを防ぐため、全軸feedbackを確認してからactivateし、運転中のstale/異常値は全軸ゼロ出力のラッチ故障にします。復旧は明示的な再activateと新規goalを必要とする安全側の挙動に直します。

安全側の修正を仕上げています。feedbackの範囲外値・欠落・タイムアウト時に全レバーをゼロへラッチし、再activateまで自動復帰しないことをコードと設定の両方で整合確認します。

静的検証は通過しています。さらに、途中で追加されていた角度変換・旋回wrap・符号/clamp・範囲判定の単体テストを実処理と同じ関数へ接続し、テストが形だけにならないよう整理します。

Unity Editorを停止せずに、Editorが生成済みの同一参照・define一覧を使ってC#アセンブリを一時出力へ再コンパイルします。これで最新の安全修正まで含む構文・参照解決を確認できます。

最終検証まで完了しました。Unity側はUnity生成済みの参照設定でプロジェクトC#全体を再コンパイルして成功、ROS側は設定・XML・Python・数値処理テストを通過しています。残る未実施項目は、このMacにHumble環境がないための`colcon build`とROS–Unity実通信テストです。

## Assistant final

実装完了しました。提供PDFはPDFスキルで全4ページを確認し、レバー正入力と各角度の方向・範囲を実装へ反映しています。

### OperaSim-PhysX

- [Tb20eLeverController.cs](/Users/iszw/git/OperaSim-PhysX/Assets/Scripts/Tb20eLeverController.cs:10) を追加しました。4本の`Float64` topic、操作方向、±100時の角速度をInspectorで軸別に編集でき、入力に応じて`ArticulationBody.xDrive.target`を更新します。
- [SimpleScene.unity](/Users/iszw/git/OperaSim-PhysX/Assets/Scenes/SimpleScene.unity:9721) と [HttpScene.unity](/Users/iszw/git/OperaSim-PhysX/Assets/Scenes/HttpScene.unity:9721) のTB20eへ配線済みです。旋回は実際の上部旋回体である`body_link`を対象にしています。
- 操作量clamp、NaN/Inf停止、非常停止、0.2秒watchdog、有限関節のtarget範囲制限を入れました。
- 現在角度を200 Hzへ変更し、旋回topicを`/current_swing_angle`へ統一しました。[TimeManager.asset](/Users/iszw/git/OperaSim-PhysX/ProjectSettings/TimeManager.asset:5) の固定刻みも5 msです。仕様と競合注意は [README.md](/Users/iszw/git/OperaSim-PhysX/README.md:102) に追記しています。

### tb20e_ros2

- ROS 2 Humble向けの`tb20e_control` packageを新規実装しました。中心は [tb20e_lever_hardware.cpp](/Users/iszw/git/tb20e_ros2/tb20e_control/src/tb20e_lever_hardware.cpp:77)、PID設定は [tb20e_controllers.yaml](/Users/iszw/git/tb20e_ros2/tb20e_control/config/tb20e_controllers.yaml:4)、起動は [tb20e_control.launch.py](/Users/iszw/git/tb20e_ros2/tb20e_control/launch/tb20e_control.launch.py:14) です。
- 4軸の現在角度をdegreeで受信してradianへ変換し、`joint_trajectory_controller`のeffort PID出力をレバー百分率へ変換して20 Hz（50 ms）で配信します。この構成はros2_controlの[SystemInterface仕様](https://control.ros.org/humble/doc/api/classhardware__interface_1_1SystemInterface.html)と[JointTrajectoryControllerのeffort PID仕様](https://control.ros.org/humble/doc/ros2_controllers/joint_trajectory_controller/doc/userdoc.html)に沿っています。
- activate前の4軸fresh確認、角度範囲・速度飛び検査、feedback timeout、全軸ゼロへのfaultラッチ、端点の外向き指令抑止を実装しました。復旧手順は [README.md](/Users/iszw/git/tb20e_ros2/README.md:133) に記載し、Humbleの[公式CLI仕様](https://control.ros.org/humble/doc/ros2_control/ros2controlcli/doc/userdoc.html)とも照合済みです。
- URDFとhardware判定の角度制限は [tb20e.urdf.xacro](/Users/iszw/git/tb20e_ros2/tb20e_control/urdf/tb20e.urdf.xacro:10) の共通値から生成します。角度変換、旋回wrap、符号・clamp、範囲・速度検査のテストも [test_math_utils.cpp](/Users/iszw/git/tb20e_ros2/tb20e_control/test/test_math_utils.cpp:20) に追加しました。

起動はUnity側を先に開始してから実行します。

```bash
colcon build --symlink-install --packages-select tb20e_control
source install/setup.bash
ros2 launch tb20e_control tb20e_control.launch.py
```

Unity側はUnity生成済みの参照・defineを使ったプロジェクトC#全体の再コンパイルに成功しました（既存の無関係な警告3件のみ）。ROS側はXML/YAML/Python検証とC++数値処理のコンパイル・実行に成功しています。このMacにはROS 2 Humbleがないため、`colcon build`、xacro展開、plugin実ロード、ROS–Unity実通信は未実施です。PIDゲインはシミュレータ用の初期値なので、実際の応答を見ながら調整してください。
