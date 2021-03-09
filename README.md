# Esnya Unity Tools
Common utilities for Unity.

[日本語はこちら](#Japanese)

## Requirements
* Unity 2018.4.20f1
* [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@1.5/manual/index.html) 1.5.3

### Optional
* [PostProcessing](https://docs.unity3d.com/Packages/com.unity.postprocessing@2.1/manual/Installation.html) 3.1.0
* VRCSDK3 Avatars

## Components
### Credit Generator
Update a text of the specified TextMesh Pro Component with licenses file found in the project. It's recommended that you set the tag to `EditorOnly`.

## Assets
The menu `Assets/EsnyaTools` will be added.

### FBX Animation Converter
Scriptable object that extracts necessary parts from the animation imported from FBX, etc., which is created by Assets/Create/EsnyaTools/FBXAnimationConverter.
Specify the name of ClipPath, the search pattern of ClipPath, and the search pattern of path and property names using regular expressions. In the path and property names, you can refer to the contents of the matched string or group in ClipPath as `$&` and `$1`.
When you click "Generate" button or the source is updated, an animation clip is generated.

## Tools
The menu `EsnyaTools` will be added. Some of the tools will also be added to the context menu of the relevant Components.
### Animation Replacer
This tool replaces the same Animation or BlendTree in a given AnimatorController.

### Asset Renamer
A tool to rename asset files by pattern in a directory.

### Crunch All
This tool enables CrunchCompression for all Texture2D in a project. Note that it doesn't stop or return when pressed.

### Create BlendTree
Create new asset of BlendTree.

### Remove Duplicate Names
A tool for numbering objects with the same name that have the same parent.
 VRCSDK has a similar function, but for when there are too many objects and they get stuck.

### Fix VRCCam (VRCSDK2, VRCSDK3-AVATAR, VRCSDK3-WORLD)
A tool to modify the VRCCam Prehab, which has poor parameters by default.
It allows you to add a PostProcessingLayer, set Background Color, and use physical camera properties.

### View Position Visualizer (VRCSDK3-AVATAR)
A tool to display Gizmo in order to adjust the ViewPosition to the correct position, and to adjust the animation while moving the Upright.

### ExEquipments (VRCSDK3-AVATAR)
A gimmick generator that uses ExpressionsMenu to equip items, hold them in your hands and pin them to the world. Add the generated menu as a SubMenu and enjoy it now.

----
----

# Japanese
Unity用の汎用ツール集。

## Components
Scene中のGameObject に `Add Component` で追加。

### Credit Generator
Project中のライセンスファイルを検索してTextMesh Proのテキストを更新するコンポーネント。Runtimeには不要なのでTagを`EditarOnly`にすることを推奨。

## Assets
メニューかProjectウィンドウのコンテキストメニューに `EsnyaTools` が追加されます。

### FBX Animation Converter
FBXなどからインポートされるアニメーションから必要な部分を取り出すScriptable Object。`Assets/Create/EsnyaTools/FBXAnimationConverter`から作成する。
生成するClipPathの名前、ClipPathの検索パターン、パスとプロパティ名の検索パターンを正規表現で記述する。パスとプロパティ名にはClipPathでマッチした文字列やグループの内容を`$&`、`$1`のように参照できる。
Generateボタンを押すか、インポート元が更新されるとAnimation Clipを生成する。

## Tools
メニューに `EsnyaTools` が追加されます。関係するコンポーネントのコンテキストメニューにも追加されていることがあります。

### Animation Replacer
AnimatorController内の同じアニメーションとBlendTreeをまとめて置き換えるツール。

### Asset Renamer
ディレクトリ内のアセットのファイル名をパターンマッチで置き換えるツール。

### Crunch All
プロジェクト内の全てのTexture2DのCrunch Compressionを有効にするツール。途中で止められないし、戻す機能もないので注意。

### Create BlendTree
新しいBlendTreeをAssetとして作成する。

### Remove Duplicate Names
同じ親を持つ同名オブジェクトに連番をつけるツール。VRCSDKも同様の機能を持っているが、オブジェクトが多すぎて固まったとき用。

### VRCCamPatcher (VRCSDK2, VRCSDK3)
デフォルトではなんとも言えないVRCCamのPrefabを変更するツール。
PostProcessingLayerを追加したり、Background Colorを設定したり、物理カメラパラメータ設定を使ったりできる。

### View Position Visualizer (VRCSDK3-AVATAR)
ViewPosition を正しい位置に合わせるための Gizmo を表示するツール。Upright を動かしながらアニメーション調整するためのもの。

### ExEquipments (VRCSDK3-AVATAR)
ExpressionsMenuを使ってアイテムを装備したり、手に持ったり、ワールドに固定したりするギミックを生成する。SubMenu用のMenuを出力する。
