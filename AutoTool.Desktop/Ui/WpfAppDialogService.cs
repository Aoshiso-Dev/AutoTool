using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AutoTool.Application.Ports;

namespace AutoTool.Desktop.Ui;

/// <summary>
/// 関連機能の共通処理を提供し、呼び出し側の重複実装を減らします。
/// </summary>
public sealed class WpfAppDialogService : IAppDialogService
{
    private static readonly System.Text.RegularExpressions.Regex UrlPattern = new(
        @"https?://[^\s]+",
        System.Text.RegularExpressions.RegexOptions.Compiled
        | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public string? Show(
        string title,
        string message,
        IReadOnlyList<AppDialogAction> actions,
        AppDialogTone tone = AppDialogTone.Info,
        object? owner = null)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(actions);

        if (actions.Count == 0)
        {
            throw new ArgumentException("少なくとも1つのアクションが必要です。", nameof(actions));
        }

        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return actions.First().Id;
        }

        if (app.Dispatcher.CheckAccess())
        {
            return ShowInternal(title, message, actions, tone, owner as Window);
        }

        return app.Dispatcher.Invoke(() => ShowInternal(title, message, actions, tone, owner as Window));
    }

    private static string? ShowInternal(
        string title,
        string message,
        IReadOnlyList<AppDialogAction> actions,
        AppDialogTone tone,
        Window? owner)
    {
        string? selectedActionId = null;

        var dialogWindow = new Window
        {
            Title = title,
            Width = 520,
            MinWidth = 420,
            MaxWidth = 640,
            Height = 300,
            MinHeight = 250,
            SizeToContent = SizeToContent.Manual,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.FromRgb(46, 50, 56)),
            Foreground = new SolidColorBrush(Color.FromRgb(237, 239, 242)),
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false
        };

        var resolvedOwner = owner
            ?? System.Windows.Application.Current?.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive)
            ?? System.Windows.Application.Current?.MainWindow;

        if (resolvedOwner is not null && resolvedOwner != dialogWindow)
        {
            dialogWindow.Owner = resolvedOwner;
        }

        var borderBrush = new SolidColorBrush(Color.FromRgb(86, 92, 101));
        var accentBrush = new SolidColorBrush(GetToneColor(tone));

        var root = new Border
        {
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Color.FromRgb(46, 50, 56)),
            Padding = new Thickness(16)
        };

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(246, 248, 250))
        };
        Grid.SetRow(titleBlock, 0);
        layout.Children.Add(titleBlock);

        var messagePanel = new Border
        {
            Margin = new Thickness(0, 12, 0, 14),
            Padding = new Thickness(12, 10, 12, 10),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = borderBrush,
            Background = new SolidColorBrush(Color.FromRgb(57, 62, 69))
        };
        Grid.SetRow(messagePanel, 1);
        layout.Children.Add(messagePanel);

        var messageLayout = new Grid();
        messageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        messageLayout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        messagePanel.Child = messageLayout;

        var toneIndicator = new Border
        {
            Width = 8,
            Margin = new Thickness(0, 1, 10, 1),
            Background = accentBrush,
            CornerRadius = new CornerRadius(4)
        };
        Grid.SetColumn(toneIndicator, 0);
        messageLayout.Children.Add(toneIndicator);

        var messageScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Grid.SetColumn(messageScroll, 1);
        messageLayout.Children.Add(messageScroll);

        var messageBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 22,
            Foreground = new SolidColorBrush(Color.FromRgb(235, 237, 240))
        };
        PopulateMessageInlines(messageBlock, message);
        messageScroll.Content = messageBlock;

        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(buttonPanel, 2);
        layout.Children.Add(buttonPanel);

        foreach (var action in actions)
        {
            var button = new Button
            {
                Content = action.Label,
                MinWidth = 110,
                Height = 34,
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(12, 0, 12, 0),
                FontWeight = FontWeights.SemiBold,
                Background = action.IsDefault
                    ? new SolidColorBrush(Color.FromRgb(97, 106, 117))
                    : new SolidColorBrush(Color.FromRgb(69, 75, 84)),
                Foreground = new SolidColorBrush(Color.FromRgb(245, 246, 248)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(102, 110, 121)),
                BorderThickness = new Thickness(1),
                IsDefault = action.IsDefault,
                IsCancel = action.IsCancel
            };

            button.Click += (_, _) =>
            {
                selectedActionId = action.Id;
                dialogWindow.Close();
            };

            buttonPanel.Children.Add(button);
        }

        dialogWindow.Content = root;
        root.Child = layout;

        dialogWindow.Closed += (_, _) =>
        {
            if (selectedActionId is not null)
            {
                return;
            }

            selectedActionId = actions.FirstOrDefault(x => x.IsCancel)?.Id;
        };

        dialogWindow.PreviewKeyDown += (_, args) =>
        {
            if (args.Key != System.Windows.Input.Key.Escape)
            {
                return;
            }

            selectedActionId ??= actions.FirstOrDefault(x => x.IsCancel)?.Id;
            dialogWindow.Close();
            args.Handled = true;
        };

        dialogWindow.ShowDialog();
        return selectedActionId;
    }

    private static void PopulateMessageInlines(TextBlock messageBlock, string message)
    {
        messageBlock.Inlines.Clear();

        var lastIndex = 0;
        foreach (System.Text.RegularExpressions.Match match in UrlPattern.Matches(message))
        {
            if (match.Index > lastIndex)
            {
                messageBlock.Inlines.Add(new System.Windows.Documents.Run(message[lastIndex..match.Index]));
            }

            var url = match.Value;
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var hyperlink = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run(url))
                {
                    NavigateUri = uri
                };
                hyperlink.Click += (_, _) => OpenUrl(uri);
                messageBlock.Inlines.Add(hyperlink);
            }
            else
            {
                messageBlock.Inlines.Add(new System.Windows.Documents.Run(url));
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < message.Length)
        {
            messageBlock.Inlines.Add(new System.Windows.Documents.Run(message[lastIndex..]));
        }
    }

    private static void OpenUrl(Uri uri)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch
        {
            // 通知ダイアログからのリンク遷移失敗は無視する。
        }
    }

    private static Color GetToneColor(AppDialogTone tone) => tone switch
    {
        AppDialogTone.Info => Color.FromRgb(112, 147, 255),
        AppDialogTone.Warning => Color.FromRgb(232, 173, 68),
        AppDialogTone.Error => Color.FromRgb(214, 92, 92),
        AppDialogTone.Question => Color.FromRgb(124, 186, 114),
        _ => Color.FromRgb(112, 147, 255)
    };
}
