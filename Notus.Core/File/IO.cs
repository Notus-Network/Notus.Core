using System.IO;
using NVG = Notus.Variable.Globals;
using DirListConst = Notus.Variable.Constant.StorageFolderName;
namespace Notus
{
    public static class IO
    {
        public static string[] GetFileList(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer,
            string directoryName,
            string extension
        )
        {
            if (Directory.Exists(Notus.IO.GetFolderName(networkType, networkLayer,
                directoryName)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(
                Notus.IO.GetFolderName(networkType,networkLayer,directoryName),"*." + extension
            );
        }
        public static string[] GetFileList(
            Notus.Globals.Variable.Settings objSettings,
            string directoryName,
            string extension
        )
        {
            if (Directory.Exists(Notus.IO.GetFolderName(objSettings,
                directoryName)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(
                Notus.IO.GetFolderName(objSettings, directoryName),"*." + extension
            );
        }
        public static string[] GetZipFiles(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            return GetFileList(networkType,networkLayer, DirListConst.Block,"zip");
        }
        public static string[] GetZipFiles(Notus.Globals.Variable.Settings objSettings)
        {
            return GetZipFiles(objSettings.Network, objSettings.Layer);
        }

        public static string GetFolderName(Notus.Globals.Variable.Settings objSettings, string folderName)
        {
            return GetFolderName(objSettings.Network, objSettings.Layer, folderName);
        }
        public static string GetFolderName(Variable.Enum.NetworkType networkType, Variable.Enum.NetworkLayer networkLayer, string folderName)
        {
            return
                Network.Text.NetworkTypeText(networkType) +
                Path.DirectorySeparatorChar +
                Network.Text.NetworkLayerText(networkLayer) +
                Path.DirectorySeparatorChar +
                folderName +
                Path.DirectorySeparatorChar;
        }
        public static void CreateDirectory(string DirectoryName)
        {
            if (!Directory.Exists(DirectoryName))
                Directory.CreateDirectory(DirectoryName);
        }
        public static void NodeFolderControl()
        {
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.BlockForTgz));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.TempBlock));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.Balance));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.Block));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.Common));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.File));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.Node));
            CreateDirectory(GetFolderName(NVG.Settings, DirListConst.Pool));
        }
    }
}