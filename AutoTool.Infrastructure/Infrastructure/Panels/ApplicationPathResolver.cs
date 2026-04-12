using System;
using System.IO;
using System.Reflection;

namespace AutoTool.Panels.Helpers
{
    /// <summary>
    /// �t�@�C���p�X�̑��΃p�X�E��΃p�X�ϊ���s���w���p�[�N���X
    /// </summary>
    public static class ApplicationPathResolver
    {
        /// <summary>
        /// AutoTool.exe������f�B���N�g���̃p�X��擾
        /// </summary>
        public static string GetApplicationDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory;
        }

        /// <summary>
        /// ��΃p�X�𑊑΃p�X�ɕϊ�����
        /// ���΃p�X�ɕϊ��ł��Ȃ��ꍇ�͐�΃p�X��Ԃ�
        /// </summary>
        /// <param name="absolutePath">��΃p�X</param>
        /// <returns>���΃p�X�܂��͐�΃p�X</returns>
        public static string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return absolutePath;

            try
            {
                var appDirectory = GetApplicationDirectory();
                var uri1 = new Uri(appDirectory + Path.DirectorySeparatorChar);
                var uri2 = new Uri(absolutePath);
                
                if (uri1.Scheme != uri2.Scheme)
                {
                    // �X�L�[�����قȂ�ꍇ�i��F�l�b�g���[�N�p�X�j�͐�΃p�X��Ԃ�
                    return absolutePath;
                }

                var relativeUri = uri1.MakeRelativeUri(uri2);
                var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
                
                // �p�X��؂蕶���𐳋K��
                relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                
                return relativePath;
            }
            catch (Exception ex)
            {
                // �ϊ��Ɏ��s�����ꍇ�͐�΃p�X��Ԃ�
                System.Diagnostics.Debug.WriteLine($"���΃p�X�ϊ��Ɏ��s: {ex.Message}");
                return absolutePath;
            }
        }

        /// <summary>
        /// ���΃p�X���΃p�X�ɕϊ�����
        /// ���ɐ�΃p�X�̏ꍇ�͂��̂܂ܕԂ�
        /// </summary>
        /// <param name="relativePath">���΃p�X�܂��͐�΃p�X</param>
        /// <returns>��΃p�X</returns>
        public static string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return relativePath;

            try
            {
                // ���ɐ�΃p�X�̏ꍇ�͂��̂܂ܕԂ�
                if (Path.IsPathRooted(relativePath))
                    return relativePath;

                var appDirectory = GetApplicationDirectory();
                var absolutePath = Path.Combine(appDirectory, relativePath);
                
                // �p�X�𐳋K��
                return Path.GetFullPath(absolutePath);
            }
            catch (Exception ex)
            {
                // �ϊ��Ɏ��s�����ꍇ�͌��̃p�X��Ԃ�
                System.Diagnostics.Debug.WriteLine($"��΃p�X�ϊ��Ɏ��s: {ex.Message}");
                return relativePath;
            }
        }

        /// <summary>
        /// �t�@�C�������݂��邩�`�F�b�N�i���΃p�X�Ή��j
        /// </summary>
        /// <param name="filePath">�t�@�C���p�X�i���΂܂��͐�΁j</param>
        /// <returns>�t�@�C�������݂��邩�ǂ���</returns>
        public static bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            try
            {
                var absolutePath = ToAbsolutePath(filePath);
                return File.Exists(absolutePath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// �f�B���N�g�������݂��邩�`�F�b�N�i���΃p�X�Ή��j
        /// </summary>
        /// <param name="directoryPath">�f�B���N�g���p�X�i���΂܂��͐�΁j</param>
        /// <returns>�f�B���N�g�������݂��邩�ǂ���</returns>
        public static bool DirectoryExists(string directoryPath)
        {
            if (string.IsNullOrEmpty(directoryPath))
                return false;

            try
            {
                var absolutePath = ToAbsolutePath(directoryPath);
                return Directory.Exists(absolutePath);
            }
            catch
            {
                return false;
            }
        }
    }
}

