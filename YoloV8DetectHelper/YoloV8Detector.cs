using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System.Runtime.InteropServices;

namespace YoloWinLib;

public sealed class YoloV8Detector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly string _outputName;
    private readonly int _inputSize; // 既定 640

    /// <summary>クラス名ラベル（任意）。COCO80 などをセット。</summary>
    public string[]? Labels { get; init; }

    public YoloV8Detector(string onnxPath, int inputSize = 640, bool useDirectML = false)
    {
        _inputSize = inputSize;

        var resolved = ResolveModelPath(onnxPath);
        if (!File.Exists(resolved))
        {
            var msg = $"ONNX ファイルが見つかりません: '{onnxPath}'\n" +
                      $"試したパス: {string.Join(" | ", CandidatePaths(onnxPath))}\n" +
                      $"実行フォルダ: {AppContext.BaseDirectory} / CWD: {Environment.CurrentDirectory}";
            throw new FileNotFoundException(msg, onnxPath);
        }

        try
        {
            if (useDirectML)
            {
                var so = new SessionOptions();
                try { so.AppendExecutionProvider_DML(); } catch { /* ignore */ }
                _session = new InferenceSession(resolved, so);
            }
            else
            {
                _session = new InferenceSession(resolved);
            }
        }
        catch (DllNotFoundException ex)
        {
            try { _session = new InferenceSession(resolved); }
            catch
            {
                throw new InvalidOperationException(
                    "ONNX ランタイムのネイティブ DLL が見つかりません。DirectML を使う場合は " +
                    "Microsoft.ML.OnnxRuntime.DirectML を参照に追加し、出力フォルダに DLL がコピーされているか確認してください。", ex);
            }
        }
        catch (OnnxRuntimeException ex)
        {
            throw new InvalidOperationException(
                $"ONNX のロードに失敗しました: {resolved}\nモデルの互換性や入力サイズを確認してください。", ex);
        }

        _inputName = _session.InputMetadata.Keys.First();
        _outputName = _session.OutputMetadata.Keys.First();
    }

    /// <summary>
    /// 推論 (BGR入力を明示的にRGBへ変換後、swapRB=false でBlob化)
    /// </summary>
    public List<Detection> Detect(Mat frameBgr, float confTh = 0.25f, float iouTh = 0.45f)
    {
        // 1) Letterbox
        var lb = Letterbox(frameBgr, new OpenCvSharp.Size(_inputSize, _inputSize), 32);

        // 2) 必ず3ch BGRへ揃える (BGRA / GRAY 対応)
        using var bgr3 = EnsureBgr3(lb.Image);

        // 3) 明示的に BGR -> RGB 変換
        using var rgb = new Mat();
        Cv2.CvtColor(bgr3, rgb, ColorConversionCodes.BGR2RGB);

        // 4) Blob作成（既にRGBなので swapRB:false）
        using var blob = CvDnn.BlobFromImage(
            rgb,
            scaleFactor: 1.0 / 255.0,
            size: new OpenCvSharp.Size(_inputSize, _inputSize),
            mean: default,
            swapRB: false,
            crop: false);

        // 5) Tensor詰め替え
        var tensor = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });
        blob.GetArray(out float[] buf);
        buf.CopyTo(tensor.Buffer.Span);

        // 6) 推論
        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };
        using var results = _session.Run(inputs);
        var t = results.First(r => r.Name == _outputName).AsTensor<float>();

        // 7) 後処理
        int origW = frameBgr.Cols, origH = frameBgr.Rows;
        var dets = ParseDetections(t, confTh, lb, origW, origH);
        dets = MathUtil.Nms(dets, iouTh);
        return dets;
    }

    private static Mat EnsureBgr3(Mat src)
    {
        int ch = src.Channels();
        if (ch == 3) return src.Clone();
        var dst = new Mat();
        if (ch == 4) Cv2.CvtColor(src, dst, ColorConversionCodes.BGRA2BGR);
        else if (ch == 1) Cv2.CvtColor(src, dst, ColorConversionCodes.GRAY2BGR);
        else throw new NotSupportedException($"Unsupported channel count: {ch}");
        return dst;
    }

    private static List<Detection> ParseDetections(Tensor<float> tensor, float confTh, LetterboxResult lb, int origW, int origH)
    {
        var dims = tensor.Dimensions;
        var data = tensor.ToArray();
        var dets = new List<Detection>();
        if (dims.Length == 3)
        {
            if (dims[1] < dims[2]) // [1,84,8400]
            {
                int ch = dims[1];
                int anchors = dims[2];
                int classes = ch - 4;
                for (int a = 0; a < anchors; a++)
                {
                    float x = data[0 * anchors + a];
                    float y = data[1 * anchors + a];
                    float w = data[2 * anchors + a];
                    float h = data[3 * anchors + a];
                    int best = -1; float score = 0f;
                    for (int c = 0; c < classes; c++)
                    {
                        float s = data[(4 + c) * anchors + a];
                        if (s > score) { score = s; best = c; }
                    }
                    if (score < confTh) continue;
                    var rect = MathUtil.UndoLetterbox(MathUtil.Xywh2Xyxy(x, y, w, h), lb);
                    rect = MathUtil.ClipRect(rect, origW, origH);
                    if (rect.Width <= 0 || rect.Height <= 0) continue;
                    dets.Add(new(rect, score, best));
                }
            }
            else // [1,8400,84]
            {
                int anchors = dims[1];
                int ch = dims[2];
                int classes = ch - 4;
                for (int a = 0; a < anchors; a++)
                {
                    int baseIdx = a * ch;
                    float x = data[baseIdx + 0];
                    float y = data[baseIdx + 1];
                    float w = data[baseIdx + 2];
                    float h = data[baseIdx + 3];
                    int best = -1; float score = 0f;
                    for (int c = 0; c < classes; c++)
                    {
                        float s = data[baseIdx + 4 + c];
                        if (s > score) { score = s; best = c; }
                    }
                    if (score < confTh) continue;
                    var rect = MathUtil.UndoLetterbox(MathUtil.Xywh2Xyxy(x, y, w, h), lb);
                    rect = MathUtil.ClipRect(rect, origW, origH);
                    if (rect.Width <= 0 || rect.Height <= 0) continue;
                    dets.Add(new(rect, score, best));
                }
            }
        }
        else throw new NotSupportedException("想定外の出力次元です（YOLOv8 Detection）");
        return dets;
    }

    public void Dispose() => _session.Dispose();

    private static string ResolveModelPath(string path)
    {
        foreach (var c in CandidatePaths(path))
            if (File.Exists(c)) return c;
        return path;
    }

    private static IEnumerable<string> CandidatePaths(string path)
    {
        if (Path.IsPathRooted(path)) { yield return path; yield break; }
        yield return Path.GetFullPath(path, Environment.CurrentDirectory);
        yield return Path.GetFullPath(path, AppContext.BaseDirectory);
        var entry = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrEmpty(entry))
        {
            var dir = Path.GetDirectoryName(entry)!;
            yield return Path.GetFullPath(path, dir);
        }
    }

    private static LetterboxResult Letterbox(Mat src, OpenCvSharp.Size newShape, int stride)
    {
        int srcH = src.Rows, srcW = src.Cols;
        float r = Math.Min((float)newShape.Width / srcW, (float)newShape.Height / srcH);
        int newW = (int)Math.Round(srcW * r);
        int newH = (int)Math.Round(srcH * r);
        int dw = newShape.Width - newW;
        int dh = newShape.Height - newH;
        dw = dw % stride; dh = dh % stride;
        dw /= 2; dh /= 2;

        var resized = new Mat();
        Cv2.Resize(src, resized, new OpenCvSharp.Size(newW, newH), 0, 0, InterpolationFlags.Area);
        var outImg = new Mat();
        Cv2.CopyMakeBorder(resized, outImg, dh, newShape.Height - newH - dh, dw, newShape.Width - newW - dw,
            BorderTypes.Constant, new Scalar(114, 114, 114));
        resized.Dispose();
        return new LetterboxResult(outImg, r, dw, dh);
    }
}

public readonly record struct Detection(OpenCvSharp.Rect2f Rect, float Score, int ClassId);
internal readonly record struct LetterboxResult(Mat Image, float Gain, float PadX, float PadY);