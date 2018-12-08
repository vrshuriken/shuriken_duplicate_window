# shuriken_duplicate_window
Unity Extending the Editor. Easily duplicate game object with the same arrangement.

## できること
規則的に多量のオブジェクトを複製し並べたいときに利用します。
位置は同じ規則の配置にした上ですべて同じ大きさ・向き・名前にします。

## 導入方法
Editor/ShurikenDuplicateWindow.cs
をEditorフォルダごとUnityのAssets直下にコピーしてください。
UnityのMenu > Window に Shuriken Duplicate が追加されます。

## 利用方法
### 1
![1](https://user-images.githubusercontent.com/45710234/49687150-64104000-fb42-11e8-837b-1e6a813ca488.png)  
まず、配置したいオブジェクトを作成します（なんでも良いです、中に入れ子でオブジェクトを配置しても良いです）。  
オブジェクトをいくつかCtrl+Dで自分でコピー(最低でも2個必要)して好きな配置で並べます。  
例えば2列並べます。  
(2列に並べたオブジェクト名がそれぞれ例えば一列目がcubea,cubea (1),cubea (2),cubea (3)で2列目がcubea (4)とするとHierarchy上の並び順もcubea,cubea (1),cubea (2),cubea (3),cubea (4)の順に並んでいる必要があります。)  
### 2
![2](https://user-images.githubusercontent.com/45710234/49687176-b2254380-fb42-11e8-8558-b56159d7d663.png)  
作成したオブジェクトをすべて選択した状態で、  
UnityのMenu > Window > Shuriken Duplicate を選択しウィンドウを開いてください。  
### 3
![3](https://user-images.githubusercontent.com/45710234/49687191-eef13a80-fb42-11e8-8a38-0d94add7c33a.png)  
選択しているオブジェクト含め合計で何個のオブジェクトが必要か「配置数」に入力してください。  
「オブジェクト作成」ボタンを押すと複製されます。  
### 4
![4](https://user-images.githubusercontent.com/45710234/49687194-04fefb00-fb43-11e8-8199-6bd4cb51ac77.png)  
やり直したい場合はCtrl+Zしてください。  

## 設定項目
### 親
このオブジェクトの子としてオブジェクトを複製します。
通常は「Shuriken Duplicate」を開く時に選択していたオブジェクトの親が設定されています。

### 配置数(選択中含め)
「Shuriken Duplicate」を開く時に選択していたオブジェクトを含め合計何個のオブジェクトになるように複製するか。

### 名前(自動連番)
この名前でオブジェクトを複製・リネームします。
末尾に「 (1)」,「 (2)」等の連番が振られます

### Scale
この大きさでオブジェクトを複製・設定変更します。

### Rotate
この向きでオブジェクトを複製・設定変更します。

### オブジェクトの種類
#### SAME
最初の選択オブジェクトを複製します。
#### EMPTY
Emptyオブジェクトで複製します。
#### CUBE
Cubeオブジェクトで複製します。

### 選択オブジェクト更新
チェックを入れると「Shuriken Duplicate」を開く時に選択していたオブジェクトも更新されます。
