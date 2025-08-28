// ==============================================
// File: README.md (���p��)
// ==============================================
/*
# YoloWinLib �̎g�����i���v���W�F�N�g����j


## 1) �Q�Ƃ�ǉ�
- ����\�����[�V�����Ȃ� **Project Reference** ��ǉ��i`YoloWinLib.csproj`�j�B
- �������� `dotnet pack` �� NuGet �p�b�P�[�W�����A�Q�ƁB


## 2) �T���v���R�[�h
using YoloWinLib;
using OpenCvSharp;


// �������i�K�v�Ȃ� COCO80 ���x���z���n���j
YoloWin.Init("yolov8n.onnx", inputSize: 640, useDirectML: true, labels: Coco80.Labels);


// �E�B���h�E�^�C�g�����猟�o���ĕۑ�
var result = YoloWin.DetectFromWindowTitle("������", 0.25f, 0.45f, draw: true);
result.SaveAnnotated("annotated.png");


// ������ Mat / Bitmap �����
// var result2 = YoloWin.DetectFromMat(mat);


foreach (var d in result.Detections)
{
	Console.WriteLine($"cls={d.ClassId} score={d.Score:0.00} rect=({d.Rect.X},{d.Rect.Y},{d.Rect.Width},{d.Rect.Height})");
}


## 3) Coco80 ���x���i�C�Ӂj
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