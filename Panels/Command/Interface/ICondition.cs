using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MacroPanels.Command.Interface
{
    /// <summary>
    /// �����]���̃C���^�[�t�F�[�X
    /// </summary>
    public interface ICondition
    {
        /// <summary>
        /// ������]��
        /// </summary>
        /// <param name="cancellationToken">�L�����Z���[�V�����g�[�N��</param>
        /// <returns>�������^�̏ꍇtrue�A�U�̏ꍇfalse</returns>
        Task<bool> Evaluate(CancellationToken cancellationToken);
    }
}
