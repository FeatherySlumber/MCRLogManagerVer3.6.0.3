# MCRLogManagerVer3.6.0.3

CSVファイルを読込み、表とグラフで表示するWindowsアプリケーション。

# 動作環境
.NET Framework 4.7.2以上

# 使い方
## 開くことのできるファイル
ヘッダー行(文字列)と整数のデータで構成されるCSVファイル。
表の列名にはファイルの","で区切られた最後の文字列が適応されます。
(厳密にいえば(a-z,A-Z)が存在する最後の行。)

## 画面、動作解説
＊状態表示バー
グラフや表の状態を表示します。1番上の黒いバーです。

　Scale:
　グラフの表示倍率を示します。
　初期値は1です。

　GraphOriginRow:
　グラフが表の何行目のデータから描画されているかを示します。
　初期値は0です。

　SelectingRow:
　表の選択している行を示します。
  Canvasの縦線が対応しています
　初期値は-1です。

　Point:
　マウスの位置でグラフ上のX値,Y値を表示します。

＊Canvas
グラフを描画する場所です。上から2番目左側です。
グラフの原点はX:Canvasの左端、Y:Canvasの高さの半分、です。
マウスを動かすと状態表示バーのPointが更新されます。

＊Canvas下のボタン
何行目から表示されているかはGraphOriginRowを参照してください。
　<<
　グラフを表の最初から表示します。

　<
　グラフ表示を左に動かします。

　選択行
　旧バージョンでの"選択行からグラフを描画"です。
　表の選択している行からグラフを表示します。
　選択している行はSelectingRowを参照してください。

　>
　グラフ表示を右に動かします。

　>>
　グラフを表の最後から表示します。
　グラフ表示を左に動かさなければほぼ何も表示していないようにみえます。

＊List
表を表示する場所です。上から3番目右側?

＊FileOpenボタン
ファイルを開きます。

＊グラフ管理
　表のどの列でグラフを描画するか(どの列をグラフのソースデータとするか)をリストボックスで選択します。
　ファイルを開くと選択可能になり、選択肢はListの列名と同じものです。
　選択するとそれぞれのグラフが表示されます。
＊＊列名上で右クリック
　該当グラフを何色で表示するか設定できます。デフォルトでグラフの色は重複しないようにしているはずですが見ずらい場合ここで変更して下さい。スライダーで色の指定が可能です。
　また、該当グラフの表示Y倍率を変更することが可能です。Pointの表示は変化しないことに注意してください。
　決定で変更が適用されます。変更しない場合は閉じるボタンを使用してください。

＊グラフから行の選択
　チェック時にCanvas上でクリックすると表のPoint表示からX行が選択されるようになります。

＊X Y 倍表示
　グラフを何倍で描画するか設定します。数字以外は無効にしたはずですが数字を入力してください。整数と小数の入力が可能です。
　フォーカスを外すとグラフが再描画されます。

＊検索
　列を指定し、その列の条件を満たした行番号を取り出します。
　セレクトボックス(コンボボックス)で列名を選び、テキストボックスに数値を入れ、以上・以下・同値のいずれかを選択します。
　検索すると下のリストに結果が表示されます。

＊詳細検索と設定
　AND検索とOR検索が行えます。
　チェックを付けた条件にAND条件の追加、OR条件の追加、チェックを付けた条件の削除ができます。
　背景色が同じのひと塊がAND、背景色が違う並びはORです。
＊＊背景色を変更
　後述のハイライトの色を変更することができます
　決定で変更が適用されます。変更しない場合は閉じるボタンを使用してください。

＊表に反映
　検索結果のリストに含まれる行がハイライトされます。

＊グラフに反映
　検索結果のリストに含まれる部分のCanvasがハイライトされます。
　チェック時(true)、グラフを動かす操作を行うと自動的にOFF(false)になります。
　■時(null)、常時表示されます。※グラスを動かす度に作りなおしているため遅い可能性があります。

＊Graph Clearボタン
全てのグラフの表示を消します。
