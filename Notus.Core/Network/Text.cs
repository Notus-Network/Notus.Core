namespace Notus.Network
{
    public static class Text
    {
        public static string NetworkTypeStr(Notus.Variable.Enum.NetworkType networkType)
        {
            return (networkType == Notus.Variable.Enum.NetworkType.MainNet ? "main_" : "test_");
        }
        public static string NetworkTypeText(Notus.Variable.Enum.NetworkType networkType)
        {
            if (networkType == Notus.Variable.Enum.NetworkType.MainNet)
            {
                return "main-net";
            }
            if (networkType == Notus.Variable.Enum.NetworkType.TestNet)
            {
                return "test-net";
            }
            if (networkType == Notus.Variable.Enum.NetworkType.DevNet)
            {
                return "dev-net";
            }
            return "unknown-net";
        }
        public static string NetworkLayerText(Notus.Variable.Enum.NetworkLayer networkLayer)
        {
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer1)
            {
                return "layer-1";
            }
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer2)
            {
                return "layer-2";
            }
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer3)
            {
                return "layer-3";
            }
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer4)
            {
                return "layer-4";
            }
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer5)
            {
                return "layer-5";
            }
            if (networkLayer == Notus.Variable.Enum.NetworkLayer.Layer6)
            {
                return "layer-6";
            }
            return "layer-unknown";
        }
    }
}
