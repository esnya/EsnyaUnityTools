# EsnyaUnityTools
Common utilities for Unity.

## Requirements
* Unity 2018.4.20f1
* [PostProcessing](https://docs.unity3d.com/Packages/com.unity.postprocessing@2.1/manual/Installation.html) 2.3.0
* (Optional) VRCSDK3 Avatars

## Usage
Open the menu `EsnyaTools`.

## Features
### Animation Replacer
This tool replaces the same Animation or BlendTree in a given AnimatorController.

AnimatorController内の同じアニメーションとBlendTreeをまとめて置き換えるツール。

## Asset Renamer
A tool to rename asset files by pattern in a directory.

ディレクトリ内のアセットのファイル名をパターンマッチで置き換えるツール。

### Crunch All
This tool enables CrunchCompression for all Texture2D in a project. Note that it doesn't stop or return when pressed.

プロジェクト内の全てのTexture2DのCrunch Compressionを有効にするツール。途中で止められないし、戻す機能もないので注意。

### Create BlendTree
Create new asset of BlendTree.

新しいBlendTreeをAssetとして作成する。

### Remove Duplicate Names
A tool for numbering objects with the same name that have the same parent.
 VRCSDK has a similar function, but for when there are too many objects and they get stuck.

同じ親を持つ同名オブジェクトに連番をつけるツール。VRCSDKも同様の機能を持っているが、オブジェクトが多すぎて固まったとき用。

### FBX Animation Converter
Scriptable object that extracts necessary parts from the animation imported from FBX, etc., which is created by Assets/Create/EsnyaTools/FBXAnimationConverter.
Specify the name of ClipPath, the search pattern of ClipPath, and the search pattern of path and property names using regular expressions. In the path and property names, you can refer to the contents of the matched string or group in ClipPath as `$&` and `$1`.
When you click "Generate" button or the source is updated, an animation clip is generated.

FBXなどからインポートされるアニメーションから必要な部分を取り出すScriptable Object。Assets/Create/EsnyaTools/FBXAnimationConverterから作成する。
生成するClipPathの名前、ClipPathの検索パターン、パスとプロパティ名の検索パターンを正規表現で記述する。パスとプロパティ名にはClipPathでマッチした文字列やグループの内容を`$&`、`$1`のように参照できる。
Generateボタンを押すか、インポート元が更新されるとAnimation Clipを生成する。

## Features for VRC Any Versions
### Fix VRCCam
A tool to modify the VRCCam Prehab, which has poor parameters by default.
It allows you to add a PostProcessingLayer, set Background Color, and use physical camera properties.

デフォルトではなんとも言えないVRCCamのPrefabを変更するツール。
PostProcessingLayerを追加したり、Background Colorを設定したり、物理カメラパラメータ設定を使ったりできる。

## Features for VRC AVATAR SDK 3.0
### View Position Visualizer
A tool to display Gizmo in order to adjust the ViewPosition to the correct position, and to adjust the animation while moving the Upright.

ViewPosition を正しい位置に合わせるための Gizmo を表示するツール。Upright を動かしながらアニメーション調整するためのもの。

### ExEquipments
A gimmick generator that uses ExpressionsMenu to equip items, hold them in your hands and pin them to the world. Add the generated menu as a SubMenu and enjoy it now.

ExpressionsMenuを使ってアイテムを装備したり、手に持ったり、ワールドに固定したりするギミックを生成する。出力されたMenuをSubMenuとして追加すればすぐ使える。
