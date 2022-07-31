using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

namespace Notus
{
    public static class IO
    {
        /*
        public static bool AddWatermarkToImage(string sourceName, string destinationPath, string walletKey, Notus.Core.Enum.ProtectionLevel protectionLevel, bool imageIsLight = false)
        {
            float[] values = Notus.Core.Function.ProtectionType(protectionLevel, imageIsLight);
            Font font = new Font("Arial", values[0]);

            try
            {
                using Bitmap bitmap = (Bitmap)System.Drawing.Image.FromFile(sourceName);
                using Graphics graphic = Graphics.FromImage(bitmap);
                using Brush brush = new SolidBrush(Color.FromArgb(System.Convert.ToInt32(values[3]), !imageIsLight ? Color.White : Color.Black));

                SizeF textSize = graphic.MeasureString(walletKey, font);
                graphic.SmoothingMode = SmoothingMode.HighQuality;
                graphic.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                float t = 0, x = 0, y = 0;
                for (int j = 1; j <= 40; j++, y += values[2])
                {
                    x = 0;
                    if (j % 2 == 0)
                        t = (textSize.Width / 2);
                    for (int i = 1; i <= 40; i++, x += values[1])
                    {
                        graphic.DrawString(walletKey, font, brush, new PointF(x + t, y));
                        x += textSize.Width;
                    }
                    t = 0;
                }

                bitmap.Save(destinationPath);
                return true;
            }
            catch { return false; }
        }
     
        */
        public static void ClearBlocks(
            Notus.Variable.Enum.NetworkType networkType,
            Notus.Variable.Enum.NetworkLayer networkLayer
        )
        {
            DirectoryInfo d = new DirectoryInfo(
                Notus.IO.GetFolderName(
                    networkType,
                    networkLayer,
                    Notus.Variable.Constant.StorageFolderName.Block
                )
            );
            FileInfo[] filesList = d.GetFiles("*.zip");
            foreach (FileInfo fileObj in filesList)
            {
                File.Delete(fileObj.FullName);
            }

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
            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Balance));

            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Block));

            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Common));

            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.File));

            CreateDirectory(GetFolderName(networkType, networkLayer, Variable.Constant.StorageFolderName.Node));
        }
    }
}