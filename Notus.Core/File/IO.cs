using System.IO;

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
            Notus.Variable.Common.ClassSetting objSettings,
            string directoryName,
            string extension
        )
        {
            if (Directory.Exists(Notus.IO.GetFolderName(objSettings.Network, objSettings.Layer,
                directoryName)) == false)
            {
                return new string[] { };
            }
            return Directory.GetFiles(
                Notus.IO.GetFolderName(objSettings.Network, objSettings.Layer, directoryName),"*." + extension
            );
        }
        public static string[] GetZipFiles(Notus.Variable.Enum.NetworkType networkType,Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            return GetFileList(networkType,networkLayer,Notus.Variable.Constant.StorageFolderName.Block,"zip");
        }
        public static string[] GetZipFiles(Notus.Variable.Common.ClassSetting objSettings)
        {
            return GetZipFiles(objSettings.Network, objSettings.Layer);
        }

        public static string GetFolderName(Notus.Variable.Common.ClassSetting objSettings, string folderName)
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
        public static void NodeFolderControl(Variable.Enum.NetworkType networkType, Variable.Enum.NetworkLayer networkLayer)
        {
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.TempBlock));
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Balance));
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Block));
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Common));
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.File));
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Node));
        }
    }
}