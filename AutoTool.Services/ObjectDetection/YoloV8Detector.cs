using System.IO;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using OpenCvSharp.Dnn;

namespace AutoTool.Services.ObjectDetection;

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
        if (frameBgr == null || frameBgr.Empty())
            throw new ArgumentException("frameBgr is empty");

        // 1) Letterbox
        var lb = Letterbox(frameBgr, new Size(_inputSize, _inputSize), 32);

        // 2) チャネル正規化
        using var bgr3 = EnsureBgr3(lb.Image);

        // 3) BGR -> RGB
        using var rgb = new Mat();
        Cv2.CvtColor(bgr3, rgb, ColorConversionCodes.BGR2RGB);

        // 4) Blob
        using var blob = CvDnn.BlobFromImage(
            rgb,
            scaleFactor: 1f / 255f,
            size: new Size(_inputSize, _inputSize),
            mean: default,
            swapRB: false,
            crop: false);

        if (blob.Empty())
            throw new InvalidOperationException("BlobFromImage returned empty Mat.");

        // Blob shape 調査
        // OpenCV の 4D blob: N,C,H,W
        int dims = blob.Dims; // 4 期待
        var shape = Enumerable.Range(0, dims).Select(i => blob.Size(i)).ToArray(); // [1,3,H,W] 期待
        long expected = 1L * shape.Aggregate(1, (a, b) => a * b);

        // 5) Mat -> float[]
        float[] inputData = new float[expected];

        if (!blob.IsContinuous())
        {
            // 念のため連続化
            using var continuous = blob.Clone();
            CopyMatToArray(continuous, inputData);
        }
        else
        {
            CopyMatToArray(blob, inputData);
        }

        // 値域ログ
        float min = inputData.Min();
        float max = inputData.Max();
        System.Diagnostics.Debug.WriteLine($"[YOLO] blob shape={string.Join("x", shape)} len={inputData.Length} range=({min:F3},{max:F3})");

        // 6) Tensor 構築 (NCHW)
        var tensor = new DenseTensor<float>(new[] { shape[0], shape[1], shape[2], shape[3] });
        inputData.CopyTo(tensor.Buffer.Span);

        var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, tensor) };
        using var results = _session.Run(inputs);

        var output = results.First(r => r.Name == _outputName).AsTensor<float>();
        var outDims = output.Dimensions.ToArray();
        System.Diagnostics.Debug.WriteLine($"[YOLO] output dims={string.Join("x", outDims)} len={output.Length}");

        // 7) 後処理
        int origW = frameBgr.Cols, origH = frameBgr.Rows;
        var dets = ParseDetections(output, confTh, lb, origW, origH);
        dets = MathUtil.Nms(dets, iouTh);
        System.Diagnostics.Debug.WriteLine($"[YOLO] det count={dets.Count}");

        return dets;
    }

    private static unsafe void CopyMatToArray(Mat m, float[] dst)
    {
        if (m.Type() != MatType.CV_32F)
            throw new ArgumentException($"Blob MatType must be CV_32F but was {m.Type()}");
        if (dst.Length == 0) return;

        // Mat のデータは NCHW 連続領域
        Marshal.Copy(m.Data, dst, 0, dst.Length);
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

    private static List<Detection> ParseDetections(
        Tensor<float> tensor,
        float confTh,
        LetterboxResult lb,
        int origW,
        int origH)
    {
        var dims = tensor.Dimensions; // [1,C,Anchors] or [1,Anchors,C]
        if (dims.Length != 3)
            throw new NotSupportedException("想定外の出力次元");

        var flat = tensor.ToArray();
        bool channelFirst = dims[1] < dims[2]; // [1,C,A]
        int anchors = channelFirst ? dims[2] : dims[1];
        int ch = channelFirst ? dims[1] : dims[2];

        // 想定: ch = 4 + numClasses (objectness 無) = 7
        if (ch < 5)
            throw new NotSupportedException($"想定外チャンネル数 ch={ch}");

        int classesStart = 4;
        int numClasses = ch - classesStart;
        bool hasObj = false; // 今回 end2end: objectness なし

        // クラス値が既に [0,1] か判定 (最大値 <=1 かつ 最小値 >=0 なら生値使用)
        bool classAlreadyProb;
        {
            double cMin = double.PositiveInfinity, cMax = double.NegativeInfinity;
            if (channelFirst)
            {
                for (int c = classesStart; c < ch; c++)
                {
                    int baseIdx = c * anchors;
                    for (int a = 0; a < anchors; a++)
                    {
                        float v = flat[baseIdx + a];
                        if (v < cMin) cMin = v;
                        if (v > cMax) cMax = v;
                    }
                }
            }
            else
            {
                for (int a = 0; a < anchors; a++)
                {
                    int baseIdx = a * ch;
                    for (int c = classesStart; c < ch; c++)
                    {
                        float v = flat[baseIdx + c];
                        if (v < cMin) cMin = v;
                        if (v > cMax) cMax = v;
                    }
                }
            }
            classAlreadyProb = cMin >= 0.0 && cMax <= 1.0;
        }

        static float Sigmoid(float x) => 1f / (1f + MathF.Exp(-x));

        var dets = new List<Detection>(256);

        void AddDet(float cx, float cy, float w, float h, float score, int cls)
        {
            // Letterbox Undo
            float x0 = (cx - w / 2f - lb.PadX) / lb.Gain;
            float y0 = (cy - h / 2f - lb.PadY) / lb.Gain;
            float ww = w / lb.Gain;
            float hh = h / lb.Gain;

            var rect = new Rect2f(x0, y0, ww, hh);
            rect = MathUtil.ClipRect(rect, origW, origH);
            if (rect.Width < 2 || rect.Height < 2) return;
            dets.Add(new(rect, score, cls));
        }

        if (channelFirst)
        {
            var xCh = flat.AsSpan(0 * anchors, anchors);
            var yCh = flat.AsSpan(1 * anchors, anchors);
            var wCh = flat.AsSpan(2 * anchors, anchors);
            var hCh = flat.AsSpan(3 * anchors, anchors);

            for (int a = 0; a < anchors; a++)
            {
                float cx = xCh[a];
                float cy = yCh[a];
                float ww = wCh[a];
                float hh = hCh[a];

                // (既に入力スケールの x,y,w,h の想定) → 正規化でなさそうなのでそのまま

                float obj = hasObj ? 1f /*未対応*/ : 1f;

                int bestCls = -1;
                float bestScore = 0f;

                for (int c = 0; c < numClasses; c++)
                {
                    float raw = flat[(classesStart + c) * anchors + a];
                    float p = classAlreadyProb ? raw : Sigmoid(raw);
                    float s = p * obj;
                    if (s > bestScore)
                    {
                        bestScore = s;
                        bestCls = c;
                    }
                }

                if (bestCls >= 0 && bestScore >= confTh)
                    AddDet(cx, cy, ww, hh, bestScore, bestCls);
            }
        }
        else
        {
            for (int a = 0; a < anchors; a++)
            {
                int baseIdx = a * ch;
                float cx = flat[baseIdx + 0];
                float cy = flat[baseIdx + 1];
                float ww = flat[baseIdx + 2];
                float hh = flat[baseIdx + 3];
                float obj = hasObj ? 1f : 1f;

                int bestCls = -1;
                float bestScore = 0f;

                for (int c = 0; c < numClasses; c++)
                {
                    float raw = flat[baseIdx + classesStart + c];
                    float p = classAlreadyProb ? raw : Sigmoid(raw);
                    float s = p * obj;
                    if (s > bestScore)
                    {
                        bestScore = s;
                        bestCls = c;
                    }
                }

                if (bestCls >= 0 && bestScore >= confTh)
                    AddDet(cx, cy, ww, hh, bestScore, bestCls);
            }
        }

        System.Diagnostics.Debug.WriteLine($"[YOLO] parsed det(before NMS)={dets.Count}");
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

/// <summary>検出結果</summary>
public readonly record struct Detection(OpenCvSharp.Rect2f Rect, float Score, int ClassId);

/// <summary>Letterbox処理結果（内部使用）</summary>
internal readonly record struct LetterboxResult(Mat Image, float Gain, float PadX, float PadY);