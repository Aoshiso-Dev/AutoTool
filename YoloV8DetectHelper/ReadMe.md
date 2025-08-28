// ==============================================
// File: README.md (利用例)
// ==============================================
/*
# YoloWinLib の使い方（他プロジェクトから）


## 1) 参照を追加
- 同一ソリューションなら **Project Reference** を追加（`YoloWinLib.csproj`）。
- もしくは `dotnet pack` で NuGet パッケージ化し、参照。


## 2) サンプルコード
using YoloWinLib;
using OpenCvSharp;


// 初期化（必要なら COCO80 ラベル配列を渡す）
YoloWin.Init("yolov8n.onnx", inputSize: 640, useDirectML: true, labels: Coco80.Labels);


// ウィンドウタイトルから検出して保存
var result = YoloWin.DetectFromWindowTitle("メモ帳", 0.25f, 0.45f, draw: true);
result.SaveAnnotated("annotated.png");


// 既存の Mat / Bitmap からも
// var result2 = YoloWin.DetectFromMat(mat);


foreach (var d in result.Detections)
{
	Console.WriteLine($"cls={d.ClassId} score={d.Score:0.00} rect=({d.Rect.X},{d.Rect.Y},{d.Rect.Width},{d.Rect.Height})");
}


## 3) Coco80 ラベル（任意）
public static class Coco80
{
	public static readonly string[] Labels = new[]
	{
		"person","bicycle","car","motorcycle","airplane","bus","train","truck","boat","traffic light",
		"fire hydrant","stop sign","parking meter","bench","bird","cat","dog","horse","sheep","cow",
		"elephant","bear","zebra","giraffe","backpack","umbrella","handbag","tie","suitcase","frisbee",
		"skis","snowboard","sports ball","kite","baseball bat","baseball glove","skateboard","surfboard","tennis racket","bottle",
		"wine glass","cup","fork","knife","spoon","bowl","banana","apple","sandwich","orange",
		"broccoli","carrot","hot dog","pizza","donut","cake","chair","couch","potted plant","bed",
		"dining table","toilet","tv","laptop","mouse","remote","keyboard","cell phone","microwave","oven",
		"toaster","sink","refrigerator","book","clock","vase","scissors","teddy bear","hair drier","toothbrush"
	};
}
*/