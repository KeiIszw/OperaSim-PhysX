# OperaSim-PhysX
Simulator on Unity + PhysX communicating with ROS

## 詳細マニュアル
[OperaSim-PhysXマニュアル](https://operasim-physx.readthedocs.io/ja/latest/)

## 概説
- 本シミュレータは自律施工技術開発基盤OPERA（Open Platform for Earth work with Robotics and Autonomy）の一部であり、どなたでも利用可能です
- シミュレータプラットフォームに[Unity](https://unity.com/)、物理エンジンに[Nvidia PhysX](https://www.nvidia.com/ja-jp/drivers/physx/physx-9-19-0218-driver/)を利用しています
- Unityを利用するため、利用者が所属する組織に応じたUnityのライセンスが必要です。詳細は[Unityの公式サイト](https://store.unity.com/ja)をご確認の上、利用登録をしてください。


![OperaSim-PhysX-Drilling01](https://github.com/user-attachments/assets/fce94452-712f-4bd1-aeae-3e53920c1f50)

## インストール方法
### 1. Unity(ver:2022.3.4f1)のインストール

使用しているPCのOSに応じて以下の通りUnityHubをインストールする


- windows 又は Macの場合: [https://unity3d.com/jp/get-unity/download](https://unity3d.com/jp/get-unity/download)
- Linuxの場合(Linux版は動作確認していない):[https://unity3d.com/get-unity/download](https://unity3d.com/get-unity/download)

### 2. Projectファイルの開き方とUnity Editorのダウンロード
- UnityHubを起動し、画面右上の「追加」から`OperaSim_PhysX`(Githubから自身のPCにダウンロードしたもの)選択し、クリックする（初回起動時には数分程度の時間がかかります）。クリックした際に指定のUnity Editorを選択しダウンロードする。

### 3. Sceneファイルの選択
- デモ用のサンプルSceneファイルが`Asset/Scenes/SampleScene.unity`にあるので、これを開く.  

### 4. ROS-TCP-Connectorの設定
- UnityEditorの上部ツールバーからRobotics > ROS Settingを開き"ROS IP Address", "ROS Port"のところにROS側のIPアドレスおよびポート番号(defaultは10000)を入力する
- もしROS2を利用する場合は"Protocol"のところを"ROS1"->"ROS2"へ変更する

![ros_ip_setting](https://user-images.githubusercontent.com/24404939/159395478-46617a2f-b05c-4227-9fc9-d93712dc4b9f.jpg)

### 5. ROSとの連携方法
![ros-unity](https://user-images.githubusercontent.com/24404939/161001271-0f81d211-4c8e-4341-8f9f-86a02e958c4d.jpg)
- 【初回のみ】ROS側で[ROS-TCP-Endpoint](https://github.com/Unity-Technologies/ROS-TCP-Endpoint)パッケージをcloneし、buildとセットアップを行う。
ROS 1 の場合
  ```bash
  $ cd (rosワークスペース)/src
  $ git clone https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
  $ cd ./ROS-TCP-Endpoint/
  $ sudo chmod +x setup.py
  $ ./setup.py
  $ catkin build ros_tcp_endpoint
  $ source ../../devel/setup.bash
  ```
ROS 2 の場合
  ```bash
  $ cd (ros2ワークスペース)/src
  $ git clone -b main-ros2 https://github.com/Unity-Technologies/ROS-TCP-Endpoint.git
  $ cd ./ROS-TCP-Endpoint/
  $ sudo chmod +x setup.py
  $ ./setup.py
  $ cd ../../
  $ colcon build --packages-select ros_tcp_endpoint
  $ . install/setup.bash
  ```
- ROS側でendpoint.launchを実行する
  ```bash
  $ roslaunch ros_tcp_endpoint endpoint.launch
  ```
- Unity Editor上部の実行ボタンをクリックする

![play_icon](https://user-images.githubusercontent.com/24404939/159396113-993ff0b2-d2bb-4567-ac68-0eafc9f524ac.png)
- ROS側で、対応する建機のunity用launch ファイルを起動する
  - 油圧ショベル
  ```bash
  $ roslaunch zx120_unity zx120_standby.launch
  ```
  - クローラダンプ
  ```bash
  $ roslaunch ic120_unity ic120_standby.launch
  ```
  <!--
  - 油圧ショベルとクローラダンプの両方
  ```bash
  $ roslaunch zx120_ic120_standby.launch
  ```
  -->
 #### ROSと連携時の送受信データ
- Cmd (ROS -> Unity) 

| データの内容 | トピック名 | トピック型 | 物理量 | 単位 | 備考 |
| ----  |  ---- | ---- | ---- | ---- | ---- |
| 建機の移動体部に対する対地速度指令値 | /(建機のns)/tracks/cmd_vel | geometry_msgs/Twist | 速度 | [m/s],[rad/s] |  |
| ダンプトラックの荷台の傾斜角指令値 | /(建機のns)/vessel/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のスイング軸の角度指令値 | /(建機のns)/swing/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のブーム軸の角度指令値 | /(建機のns)/boom/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のアーム軸の角度指令値 | /(建機のns)/arm/cmd | std_msgs/Float64 | 角度 | [rad] |  |
| 建機のバケット軸の角度指令値 | /(建機のns)/bucket/cmd | std_msgs/Float64 | 角度 | [rad] |  |
   
- Res（Unity -> ROS）
     
| データの内容 | トピック名 | トピック型 | 物理量 | 単位 | 備考 |
| ----  |  ---- | ---- | ---- | ---- | ---- |
| 建機のベースリンクの座標 | /(建機のns)  /base_link/pose | geometry_msgs/PoseStamped | 位置・姿勢 | 位置:[m]  姿勢:[-] | Unity内のworld座標系に対する座標の真値 |
| 建機のオドメトリ計算結果 | /(建機のns)  /odom | nav_msgs/Odometry | オドメトリ | 位置:[m]  姿勢:[-] | 初期位置を原点として算出している |
| 建機の関節角度・角速度 | /(建機のns)  /joint_states | sensor_msgs/JointState | 角度・角速度 | 角度:[rad]  角速度:[rad/s] | effortについては次節を参照 |

### TB20eのレバー入力制御

`SimpleScene`と`HttpScene`のTB20eには、ROS 2のレバー操作量を受信する`Tb20eLeverController`が設定されています。操作量は`-100.0`から`100.0`に制限され、入力を不感帯・むだ時間・一次遅れに通し、各`ArticulationBody`の`xDrive.targetVelocity`へ適用します。`stiffness=0`として位置サーボと目標角度の積み上げを廃止しました。

| 操作対象 | 操作指令トピック | 型 | 周期 | 値 |
| ---- | ---- | ---- | ---- | ---- |
| ブーム | `/manipulated_boom_lever` | `std_msgs/msg/Float64` | 50 ms | -100.0 ～ 100.0 |
| アーム | `/manipulated_arm_lever` | `std_msgs/msg/Float64` | 50 ms | -100.0 ～ 100.0 |
| バケット | `/manipulated_bucket_lever` | `std_msgs/msg/Float64` | 50 ms | -100.0 ～ 100.0 |
| スイング | `/manipulated_swing_lever` | `std_msgs/msg/Float64` | 50 ms | -100.0 ～ 100.0 |

| 操作対象 | 現在角度トピック | 型 | 周期 | 値 [deg] |
| ---- | ---- | ---- | ---- | ---- |
| ブーム | `/current_boom_angle` | `std_msgs/msg/Float64` | 5 ms | 48 ～ -83 |
| アーム | `/current_arm_angle` | `std_msgs/msg/Float64` | 5 ms | 32 ～ 155 |
| バケット | `/current_bucket_angle` | `std_msgs/msg/Float64` | 5 ms | -31 ～ 159 |
| スイング | `/current_swing_angle` | `std_msgs/msg/Float64` | 5 ms | -180 ～ 180 |

各操作指令のトピック名と以下のモデルパラメータを、TB20eルートの`Tb20eLeverController`から軸ごとに編集できます。

| パラメータ | 意味・初期値 |
| ---- | ---- |
| `fullLeverTargetSpeedDegPerSecond` | 正レバー100%での無負荷速度。既存scene値を継承（SimpleSceneはboom/arm/swing 50、bucket 80 deg/s） |
| `negativeSpeedRatio` | 負レバー側の最大速度倍率。初期値1 |
| `deadbandPercent` | 不感帯。外側を残りのレバー範囲で線形に再スケール。初期値0 |
| `deadTimeSeconds` | レバー入力のむだ時間。0～5秒、初期値0 |
| `responseTimeSeconds` | 流量指令の一次遅れ時定数。初期値0（追加の遅れなし） |
| `velocityResistance` | 速度差に対する駆動トルク係数。`xDrive.damping`に設定。未校正の仮値10000 |

これは実測未校正のレバー→速度・油圧抵抗の近似です。位置フィードバックはROS側だけが担当します。UnityのForce型Driveは速度差に比例するトルクを発生し、sceneの`forceLimit`で飽和します。速度サーボ相当の作用は残りますが、位置目標を追いかける制御はありません。`velocityResistance=0`では抵抗だけでなく駆動力もゼロになります。中立時は速度指令が0となり抵抗で減速しますが、厳密な位置保持・油圧ロックではなく荷重によるドリフトがあり得ます。重力・慣性・接触・関節制限は引き続きPhysXが計算します。追加の`linearDamping`・`angularDamping`・`jointFriction`は対象4軸で0にします。

指令が0.2秒間届かない場合、無効値受信、非常停止、コンポーネント無効化では、速度指令と遅延履歴を即座に0へ戻します（物体の速度を瞬時に0へ書き換える処理ではありません）。通常のレバー中立には設定したむだ時間・一次遅れが適用されます。関節端で位置目標を蓄積しないため、逆レバーには設定した入力応答に従って反応します。

実機ログ取得後は、各軸・正負方向のレバー段階入力で不感帯、定常速度、開始遅延、立ち上がり・停止を同定してください。初期値はTB20eの実測値ではありません。共有ポンプの流量分配、シリンダ姿勢による速度変化、圧力・バルブ非線形性は未モデル化で、質量・慣性・力上限も未校正のため、負荷応答や実機用PIDゲインの妥当性をこのモデルだけで判断できません。

現在角度を5 ms周期で配信するため、`Fixed Timestep`を従来の0.02秒から0.005秒へ変更しています。そのため、物理計算の負荷は従来設定より増加します。

TB20eのbody・boom・armに設定した`ConfigurableIMUPublisher`は、角度feedback用の`Float64`だけを200 Hzで配信し、`sensor_msgs/Imu`の配信は無効にしています。両方を200 Hzで有効にするとROS TCP Connectorの送信キューが飽和し、角度feedbackの欠落や制御停止につながるためです。IMUデータも必要な場合はInspectorの`Publish Imu Message`を有効にし、ネットワーク負荷に応じてIMUまたは角度の配信周期を調整してください。

> **Note**
> `Tb20eLeverController`が有効な対象軸は、`JointPosController`、`FollowJointTrajectoryAction`、`Com3FrontController`、`FrontDriveGainParamSubscriber`の操作対象から除外します。これらの初期化による位置サーボ復活・指令競合を防ぎます。従来の位置制御に戻す場合は、Play停止後にレバーコンポーネントを無効化し、Driveゲインを設定して再起動してください。

### 関節トルクセンサの有効化

各ゲームオブジェクトに設定された`Joint State Publisher`スクリプトの`Enable Joint Effort Sensor`をチェックすることで、joint_statesトピックからeffort値を出力させることができます。

![Enable Joint Effort Sensor](images/enable_joint_effort_sensor.png)

> **Note**
> 関節トルクセンサは実機では利用できないことが多いのでご注意ください。

## パラメータのチューニング方法

### 関節制御パラメータのチューニング

各関節の制御パラメータは、ゲームオブジェクトのXDriveパラメータを変更することで可能です。

![Joint Properties](images/joint_properties.png)

| プロパティ名 | 説明 |
| ----  |  ---- |
| Lower Limit | 関節可動角の下限（単位はdegree）。可動角制限を有効にするには、Motionプロパティを「Limited」に設定してください |
| Upper Limit | 関節可動角の上限（単位はdegree）。可動角制限を有効にするには、Motionプロパティを「Limited」に設定してください |
| Stiffness | 関節の剛性係数。係数の意味は下の式を参照。0の場合はデフォルト値20000を使用します |
| Damping | 関節の減衰係数。係数の意味は下の式を参照。0の場合はデフォルト値10000を使用します |
| Force Limit | 制御中に加えられるトルクの最大値（単位はnewton）。0の場合はデフォルト値10000を使用します |

Stiffness（剛性）とDamping（減衰）の各係数は、下の式に用いられます。

加えられるトルク = 剛性係数 * (駆動位置 - ターゲット位置) - 減衰係数 * (駆動速度 - ターゲット速度)

上記、各パラメータの詳しい説明は、Unityの公式マニュアルも参照ください。

https://docs.unity3d.com/ja/2023.2/Manual/class-ArticulationBody.html#joint-drive-properties

### 関節制御が振動的になった際のシミュレーションパラメータのチューニング

長いリンクのある多関節の重機をシミュレーションする際に、関節制御が振動的になることがあります。
この症状は、以下の調整を行うことで軽減できます。

メニューから `Edit > Project Settings...` を選択し `Physics` 項目を選択します。

![Physics Properties](images/physics_properties.png)

`Default Solver Iterations` プロパティの数値を大きな値に変更してください。

### 粒子シミュレーションの挙動の調整

土砂の粒子シミュレーションのパラメータは、TerrainゲームオブジェクトのSoil Particle Settingで変更できます。

![Soil Particle Setting](images/soil_particle_setting.png)

| プロパティ名 | 説明 |
| ----  |  ---- |
| Enable | 土砂の粒子シミュレーションをオフにしたい時には、このチェックボックスのチェックを外してください。 |
| Particle Visual Radius | 粒子の見た目上の半径を設定します。粒子同士が干渉する半径を設定するには、下のRockPrefabの設定も合わせて調整してください。 |
| Particle Stick Distance | 近くの粒子との間に引力を働かせることで、土砂の粘性を再現できます。引力を発生させる範囲を設定します。 |
| Stick Force | 近くの粒子との間に発生させる引力の強さを設定します。 |

粒子が周囲の粒子と干渉する半径を調整するには、RockPrefabのSphere ColliderのRadius値を変更してください。

![Soil Particle Collision Radius](images/soil_particle_collision_radius.png)
