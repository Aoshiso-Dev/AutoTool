using System;

namespace AutoTool.ViewModel.Helpers
{
    /// <summary>
    /// Phase 5���S�����ŁF�v���p�e�B�A�N�Z�T�[
    /// MacroPanels�ˑ����폜������������
    /// </summary>
    public class PropertyAccessor<TSource, TValue>
    {
        private readonly Func<TSource?, TValue> _getter;
        private readonly Action<TSource?, TValue> _setter;
        private readonly TValue _defaultValue;

        public PropertyAccessor(
            Func<TSource?, TValue> getter, 
            Action<TSource?, TValue> setter, 
            TValue defaultValue)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
            _defaultValue = defaultValue;
        }

        public TValue GetValue(TSource? source)
        {
            try
            {
                return source != null ? _getter(source) : _defaultValue;
            }
            catch
            {
                return _defaultValue;
            }
        }

        public void SetValue(TSource? source, TValue value)
        {
            try
            {
                if (source != null)
                {
                    _setter(source, value);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PropertyAccessor.SetValue �G���[: {ex.Message}");
            }
        }
    }
}