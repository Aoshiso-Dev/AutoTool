using AutoTool.Panels.Model.List.Interface;

namespace AutoTool.Panels.Model.MacroFactory
{
    /// <summary>
    /// �R�}���h�t�@�N�g���p�̊g�����\�b�h�Q
    /// </summary>
    public static class CommandFactoryExtensions
    {
        /// <summary>
        /// �w�肵���s�ԍ��͈͓�̎q�v�f��擾����
        /// </summary>
        public static IEnumerable<ICommandListItem> GetChildrenBetween(
            this IEnumerable<ICommandListItem> items,
            int startLine,
            int endLine)
        {
            return items.Where(x => x.LineNumber > startLine && x.LineNumber < endLine);
        }

        /// <summary>
        /// �w�肵���l�X�g���x���̗v�f��擾����
        /// </summary>
        public static IEnumerable<ICommandListItem> GetByNestLevel(
            this IEnumerable<ICommandListItem> items,
            int nestLevel)
        {
            return items.Where(x => x.NestLevel == nestLevel);
        }

        /// <summary>
        /// �L���ȗv�f�݂̂�擾����
        /// </summary>
        public static IEnumerable<ICommandListItem> GetEnabled(
            this IEnumerable<ICommandListItem> items)
        {
            return items.Where(x => x.IsEnable);
        }

        /// <summary>
        /// �y�A�֌W�̌���
        /// </summary>
        public static void ValidatePair(this ICommandListItem item, string commandType)
        {
            if (item is IIfItem ifItem && ifItem.Pair == null)
                throw new InvalidOperationException($"{commandType} (�s {item.LineNumber}) �ɑΉ�����EndIf������܂���");

            if (item is ILoopItem loopItem && loopItem.Pair == null)
                throw new InvalidOperationException($"{commandType} (�s {item.LineNumber}) �ɑΉ�����EndLoop������܂���");
        }
    }
}

