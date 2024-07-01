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
    /// 读取本地JSOn数据
    /// </summary>
    internal class FileLoader
    {
        public static async Task<string> LoadText(string relativeFilePath)
        {
            StorageFile file = null;
            if (!NativeHelper.IsAppPackaged)
            {
                //如果时调试状态的话，直接读取本地根目录下的数据
                var sourcePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), relativeFilePath));
                file = await StorageFile.GetFileFromPathAsync(sourcePath);

            }
            else
            {
                //如果时程序打包状态的话需要用如下方式过去程序根目录
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
