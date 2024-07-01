using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using App1.Helper;
using System.IO;
using System.Reflection;

namespace App1.Common
{
    /// <summary>
    /// ��ȡ����JSOn����
    /// </summary>
    internal class FileLoader
    {
        public static async Task<string> LoadText(string relativeFilePath)
        {
            StorageFile file = null;
            if (!NativeHelper.IsAppPackaged)
            {
                //���ʱ����״̬�Ļ���ֱ�Ӷ�ȡ���ظ�Ŀ¼�µ�����
                var sourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), relativeFilePath));
                file = await StorageFile.GetFileFromPathAsync(sourcePath);

            }
            else
            {
                //���ʱ������״̬�Ļ���Ҫ�����·�ʽ��ȥ�����Ŀ¼
                Uri sourceUri = new Uri("ms-appx:///" + relativeFilePath);
                file = await StorageFile.GetFileFromApplicationUriAsync(sourceUri);

            }
            return await FileIO.ReadTextAsync(file);
        }

        public static async Task<IList<string>> LoadLines(string relativeFilePath)
        {
            string fileContents = await LoadText(relativeFilePath);
            return fileContents.Split(Environment.NewLine).ToList();
        }
    }
}
