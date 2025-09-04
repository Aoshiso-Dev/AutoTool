using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AutoTool.Model.List.Interface;
using AutoTool.ViewModel.Panels;

namespace AutoTool.Converters
{
    /// <summary>
    /// �l�X�g���x���Ɋ�Â��}�[�W���R���o�[�^�[
    /// </summary>
    public class NestLevelToMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                // �l�X�g���x���ɉ������C���f���g
                // ���x��0: �C���f���g�Ȃ�
                // ���x��1: 25px (Loop/If��)
                // ���x��2: 45px (Loop����If�A�܂���If����Loop)
                // ���x��3: 65px (���G�ȓ���q)
                var baseIndent = item.NestLevel * 20; // ��{�C���f���g
                var extraIndent = item.NestLevel > 0 ? 5 : 0; // 1���x���ڈȍ~�͏����]���ɃC���f���g
                var totalIndent = baseIndent + extraIndent;
                
                return new Thickness(totalIndent, 2, 0, 2);
            }
            return new Thickness(0, 2, 0, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �l�X�g���x���Ɋ�Â��\���e�L�X�g�R���o�[�^�[
    /// </summary>
    public class NestLevelToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                var prefix = "";
                
                // �l�X�g���x���ɉ��������o�I�ȕ\��
                for (int i = 0; i < item.NestLevel; i++)
                {
                    if (i == 0)
                        prefix += "������ "; // �ŏ��̃��x���̈́�����
                    else
                        prefix += "��   "; // ���̌�̃��x���̈́��ƃX�y�[�X
                }
                
                var displayName = AutoTool.Model.CommandDefinition.CommandRegistry.DisplayOrder.GetDisplayName(item.ItemType) ?? item.ItemType;
                
                // �I���R�}���h�͓��ʂȕ\�L
                if (item.ItemType == "Loop_End" || item.ItemType == "IF_End")
                {
                    if (item.NestLevel > 0)
                    {
                        // �I���R�}���h�̈́������ŕ\��
                        prefix = "";
                        for (int i = 0; i < item.NestLevel - 1; i++)
                        {
                            prefix += "��   ";
                        }
                        prefix += "������ ";
                    }
                    else
                    {
                        prefix = "������ ";
                    }
                }
                
                return $"{prefix}{displayName}";
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ���s��ԂɊ�Â��w�i�F�R���o�[�^�[
    /// </summary>
    public class ExecutionStateToBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values.Length >= 2 && values[0] is ICommandListItem item)
                {
                    var currentExecutingItem = values[1] as ICommandListItem;
                    
                    // �f�o�b�O���O�o��
                    System.Diagnostics.Debug.WriteLine($"ExecutionStateToBackgroundConverter: Item={item.ItemType}(�s{item.LineNumber}), IsRunning={item.IsRunning}, CurrentExecuting={currentExecutingItem?.ItemType ?? "null"}");
                    
                    // ���ݎ��s���̃A�C�e�����`�F�b�N
                    var isCurrentExecuting = IsCurrentExecutingItem(item, currentExecutingItem);
                    
                    if (item.IsRunning || isCurrentExecuting)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> ���F�n�C���C�g�K�p: {item.ItemType}");
                        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 255, 255, 0)); // ���������F
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"  -> �O���[�A�E�g�K�p: {item.ItemType}");
                        return new SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 128, 128, 128));
                    }
                    
                    // �l�X�g���x���ɉ������w�i�F�i���ǔŁj
                    var nestBrush = item.NestLevel switch
                    {
                        0 => System.Windows.Media.Brushes.Transparent,
                        1 => item.IsInLoop && item.IsInIf 
                             ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 128, 0, 128)) // Loop + If: ��
                             : item.IsInLoop 
                               ? new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 0, 150, 255))   // Loop: ��
                               : new SolidColorBrush(System.Windows.Media.Color.FromArgb(25, 0, 200, 100)),  // If: ��
                        2 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(35, 255, 140, 0)),     // �I�����W
                        3 => new SolidColorBrush(System.Windows.Media.Color.FromArgb(45, 200, 0, 200)),     // �}�[���^
                        _ => new SolidColorBrush(System.Windows.Media.Color.FromArgb(55, 100, 100, 100))    // �O���[
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"  -> �l�X�g���x���w�i�K�p: {item.ItemType}, Level={item.NestLevel}, InLoop={item.IsInLoop}, InIf={item.IsInIf}");
                    return nestBrush;
                }
                
                System.Diagnostics.Debug.WriteLine("ExecutionStateToBackgroundConverter: �l���s���܂���null");
                return System.Windows.Media.Brushes.Transparent;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutionStateToBackgroundConverter �G���[: {ex.Message}");
                return System.Windows.Media.Brushes.Transparent;
            }
        }

        private bool IsCurrentExecutingItem(ICommandListItem item, ICommandListItem? currentExecutingItem)
        {
            if (currentExecutingItem == null) return false;
            
            var result = item.LineNumber == currentExecutingItem.LineNumber;
            System.Diagnostics.Debug.WriteLine($"    IsCurrentExecutingItem: {item.ItemType}(�s{item.LineNumber}) == {currentExecutingItem.ItemType}(�s{currentExecutingItem.LineNumber}) -> {result}");
            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ���s��ԃA�C�R���R���o�[�^�[
    /// </summary>
    public class ExecutionStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ICommandListItem item)
                {
                    if (item.IsRunning)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (���s��)");
                        return "?"; // ���s��
                    }
                    else if (!item.IsEnable)
                    {
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> ? (����)");
                        return "?"; // ����
                    }
                    else
                    {
                        // �R�}���h�^�C�v�ɉ������A�C�R��
                        var icon = item.ItemType switch
                        {
                            "Loop" => "??",
                            "Loop_End" => "??",
                            "Loop_Break" => "?",
                            "IF_ImageExist" => "??",
                            "IF_ImageNotExist" => "??",
                            "IF_ImageExist_AI" => "??",
                            "IF_ImageNotExist_AI" => "??",
                            "IF_Variable" => "??",
                            "IF_End" => "?",
                            "Wait_Image" => "?",
                            "Click_Image" => "??",
                            "Click_Image_AI" => "??",
                            "Hotkey" => "?",
                            "Click" => "??",
                            "Wait" => "?",
                            "Execute" => "??",
                            "SetVariable" => "??",
                            "SetVariable_AI" => "??",
                            "Screenshot" => "??",
                            _ => "??"
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter: {item.ItemType} -> {icon} (�ҋ@)");
                        return icon;
                    }
                }
                return "?";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ExecutionStateToIconConverter �G���[: {ex.Message}");
                return "?";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �v���O���X�\�������R���o�[�^�[
    /// </summary>
    public class ProgressVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is ICommandListItem item)
                {
                    var isVisible = item.IsRunning && item.Progress > 0;
                    System.Diagnostics.Debug.WriteLine($"ProgressVisibilityConverter: {item.ItemType} IsRunning={item.IsRunning}, Progress={item.Progress} -> {(isVisible ? "Visible" : "Collapsed")}");
                    return isVisible ? Visibility.Visible : Visibility.Collapsed;
                }
                return Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ProgressVisibilityConverter �G���[: {ex.Message}");
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �u�[���l��������ւ̕ϊ�
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    /// <summary>
    /// �y�A�����O�\���p�R���o�[�^�[�i���ǔŁj
    /// </summary>
    public class PairLineNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                // Pair�v���p�e�B�𓮓I�Ɏ擾
                var pairProperty = item.GetType().GetProperty("Pair");
                if (pairProperty != null)
                {
                    var pairValue = pairProperty.GetValue(item) as ICommandListItem;
                    if (pairValue != null)
                    {
                        // �J�n�R�}���h�ƏI���R�}���h�ŕ\����ς���
                        if (item.ItemType == "Loop")
                            return $"{item.LineNumber}��{pairValue.LineNumber}"; // Loop�J�n
                        else if (item.ItemType == "Loop_End")
                            return $"{pairValue.LineNumber}��{item.LineNumber}"; // Loop�I��
                        else if (IsIfCommand(item.ItemType))
                            return $"{item.LineNumber}��{pairValue.LineNumber}"; // If�J�n
                        else if (item.ItemType == "IF_End")
                            return $"{pairValue.LineNumber}��{item.LineNumber}"; // If�I��
                        else
                            return $"{item.LineNumber}?{pairValue.LineNumber}"; // ���̑��̃y�A
                    }
                }
                
                // �y�A���Ȃ��ꍇ�̕\��
                if (item.ItemType == "Loop" || IsIfCommand(item.ItemType))
                    return $"{item.LineNumber}��?"; // �J�n�R�}���h�őΉ�����I�����Ȃ�
                else if (item.ItemType == "Loop_End" || item.ItemType == "IF_End")
                    return $"?��{item.LineNumber}"; // �I���R�}���h�őΉ�����J�n���Ȃ�
                else
                    return $"{item.LineNumber}"; // �ʏ�̃R�}���h
            }
            return "";
        }

        private bool IsIfCommand(string itemType) => itemType switch
        {
            "IF_ImageExist" => true,
            "IF_ImageNotExist" => true,
            "IF_ImageExist_AI" => true,
            "IF_ImageNotExist_AI" => true,
            "IF_Variable" => true,
            _ => false
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �R�����g�\���p�R���o�[�^�[
    /// </summary>
    public class CommentDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ICommandListItem item)
            {
                if (!string.IsNullOrEmpty(item.Comment))
                {
                    return $" / {item.Comment}";
                }
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �v���O���X���R���o�[�^�[
    /// </summary>
    public class ProgressPercentageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && 
                values[0] is int current && 
                values[1] is int total && 
                total > 0)
            {
                return (double)current / total * 100;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �u�[���l����t�H���g�E�F�C�g�ւ̕ϊ�
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? FontWeights.Normal : FontWeights.Light;
            }
            return FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ���s��Ԃ���F�ւ̕ϊ��i�C���Łj
    /// </summary>
    public class RunningStateToColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                var isRunning = values[0] is bool running && running;
                var showProgress = values[1] is bool progress && progress;
                
                if (isRunning && showProgress)
                    return Colors.LimeGreen; // ���s��
                else if (isRunning)
                    return Colors.Orange;    // ������
                else
                    return Colors.Gray;      // ��~��
            }
            return Colors.Gray;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ���s��Ԃ���e�L�X�g�ւ̕ϊ�
    /// </summary>
    public class RunningStateToTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2)
            {
                var isRunning = values[0] is bool running && running;
                var showProgress = values[1] is bool progress && progress;
                
                if (isRunning && showProgress)
                    return "���s��";
                else if (isRunning)
                    return "������";
                else
                    return "�ҋ@��";
            }
            return "�ҋ@��";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// �v���O���X�l����X�P�[���l�ւ̕ϊ�
    /// </summary>
    public class ProgressToScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double progress)
            {
                return progress / 100.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}