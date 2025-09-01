using System;
using Microsoft.Extensions.Logging;

namespace AutoTool.Services.Safety
{
    /// <summary>
    /// ���S�ȃI�u�W�F�N�g�쐬���x������w���p�[�N���X
    /// DefaultBinder�G���[��������A�t�H�[���o�b�N�@�\���
    /// </summary>
    public static class SafeActivator
    {
        /// <summary>
        /// ���S�ȃC���X�^���X�쐬
        /// </summary>
        public static T? CreateInstance<T>(ILogger? logger = null) where T : class
        {
            try
            {
                logger?.LogDebug("SafeActivator: {Type} �̍쐬�����s���܂�", typeof(T).Name);
                
                // �p�����[�^�Ȃ��R���X�g���N�^�̑��݊m�F
                var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                {
                    logger?.LogWarning("SafeActivator: {Type} �Ƀp�����[�^�Ȃ��R���X�g���N�^��������܂���", typeof(T).Name);
                    return null;
                }

                // �C���X�^���X�쐬
                var instance = (T)Activator.CreateInstance(typeof(T));
                logger?.LogDebug("SafeActivator: {Type} �̍쐬�ɐ������܂���", typeof(T).Name);
                return instance;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SafeActivator: {Type} �̍쐬�Ɏ��s���܂���", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// �p�����[�^�t���̈��S�ȃC���X�^���X�쐬
        /// </summary>
        public static T? CreateInstance<T>(object[] args, ILogger? logger = null) where T : class
        {
            try
            {
                logger?.LogDebug("SafeActivator: {Type} ���p�����[�^�t���ō쐬�����s���܂�", typeof(T).Name);
                
                // �R���X�g���N�^�̌���
                var constructors = typeof(T).GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();
                    if (parameters.Length == args.Length)
                    {
                        // �p�����[�^�^�C�v�̈�v�m�F
                        bool isMatch = true;
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (args[i] != null && !parameters[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                        {
                            var instance = (T)Activator.CreateInstance(typeof(T), args);
                            logger?.LogDebug("SafeActivator: {Type} �̃p�����[�^�t���쐬�ɐ������܂���", typeof(T).Name);
                            return instance;
                        }
                    }
                }

                logger?.LogWarning("SafeActivator: {Type} �ɓK������R���X�g���N�^��������܂���", typeof(T).Name);
                return null;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "SafeActivator: {Type} �̃p�����[�^�t���쐬�Ɏ��s���܂���", typeof(T).Name);
                return null;
            }
        }

        /// <summary>
        /// �^�̈��S���`�F�b�N
        /// </summary>
        public static bool IsInstantiable<T>() where T : class
        {
            try
            {
                var type = typeof(T);
                
                // ���ۃN���X��C���^�[�t�F�[�X�̃`�F�b�N
                if (type.IsAbstract || type.IsInterface)
                {
                    return false;
                }

                // �p�����[�^�Ȃ��R���X�g���N�^�̑��݊m�F
                var constructor = type.GetConstructor(Type.EmptyTypes);
                return constructor != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// �A�v���P�[�V�����̋N�����\���ǂ������`�F�b�N
        /// </summary>
        public static bool CanActivateApplication()
        {
            try
            {
                // ��{�I�ȋN���`�F�b�N
                // �K�v�ɉ����Ēǉ��̈��S���`�F�b�N������
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// �^�̈��S�ȃC���X�^���X��
        /// </summary>
        public static T? CreateInstance<T>(Type type, params object[] args) where T : class
        {
            try
            {
                return Activator.CreateInstance(type, args) as T;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// �^�̈��S�ȃC���X�^���X���i�W�F�l���b�N�Łj
        /// </summary>
        public static T? CreateInstance<T>(params object[] args) where T : class
        {
            try
            {
                return Activator.CreateInstance<T>();
            }
            catch
            {
                return null;
            }
        }
    }
}