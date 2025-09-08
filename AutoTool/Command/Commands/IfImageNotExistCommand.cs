using AutoTool.Command.Base;
using AutoTool.Command.Definition;
using AutoTool.Services.ImageProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.ComponentModel;

namespace AutoTool.Command.Commands
{
    [AutoToolCommand(nameof(IfImageNotExistCommand), typeof(IfImageNotExistCommand))]
    public class IfImageNotExistCommand : IfCommand
    {
        [Category("基本設定"), DisplayName("画像ファイル")]
        public string ImagePath { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("閾値")]
        public double Threshold { get; set; } = 0.8;

        [Category("ウィンドウ"), DisplayName("ウィンドウタイトル")]
        public string WindowTitle { get; set; } = string.Empty;

        [Category("ウィンドウ"), DisplayName("ウィンドウクラス名")]
        public string WindowClassName { get; set; } = string.Empty;

        [Category("詳細設定"), DisplayName("検索色")]
        public System.Windows.Media.Color? SearchColor { get; set; } = null;

        private readonly IImageProcessingService? _imageProcessingService;

        public IfImageNotExistCommand(IAutoToolCommand? parent = null, IServiceProvider? serviceProvider = null)
            : base(parent, serviceProvider)
        {
            Description = "画像非存在確認";
            _imageProcessingService = GetService<IImageProcessingService>();
        }

        protected override void ValidateFiles()
        {
            if (!string.IsNullOrEmpty(ImagePath))
            {
                _logger?.LogDebug("[ValidateFiles] ImagePath検証開始: {ImagePath}", ImagePath);
                ValidateFileExists(ImagePath, "画像ファイル");
                _logger?.LogDebug("[ValidateFiles] ImagePath検証成功: {ImagePath}", ImagePath);
            }
        }

        protected override async Task<bool> EvaluateConditionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(ImagePath)) 
            {
                LogMessage("画像パスが設定されていません");
                return false;
            }

            if (_imageProcessingService == null)
            {
                LogMessage("画像処理サービスが利用できません");
                return false;
            }

            var resolvedImagePath = ResolvePath(ImagePath);
            _logger?.LogDebug("[EvaluateConditionAsync] IfImageNotExist 解決されたImagePath: {OriginalPath} -> {ResolvedPath}", ImagePath, resolvedImagePath);

            LogMessage($"画像の非存在を確認中: {Path.GetFileName(resolvedImagePath)}");

            System.Windows.Point? point = null;

            if (SearchColor.HasValue)
            {
                var searchColorDrawing = System.Drawing.Color.FromArgb(
                    SearchColor.Value.A,
                    SearchColor.Value.R,
                    SearchColor.Value.G,
                    SearchColor.Value.B);

                point = await _imageProcessingService.SearchImageWithColorFilterAsync(
                    resolvedImagePath, searchColorDrawing, Threshold, WindowTitle, WindowClassName, cancellationToken);
            }
            else if (!string.IsNullOrEmpty(WindowTitle))
            {
                point = await _imageProcessingService.SearchImageInWindowAsync(
                    resolvedImagePath, WindowTitle, WindowClassName, Threshold, cancellationToken);
            }
            else
            {
                point = await _imageProcessingService.SearchImageOnScreenAsync(
                    resolvedImagePath, Threshold, cancellationToken);
            }

            if (point == null)
            {
                LogMessage("画像が見つかりませんでした（条件: 真）");
                return true;
            }

            LogMessage($"画像が見つかりました（条件: 偽）: ({point.Value.X}, {point.Value.Y})");
            return false;
        }
    }
}
