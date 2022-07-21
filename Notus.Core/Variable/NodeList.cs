using System.Collections.Generic;

namespace Notus.Variable
{
    public static class NodeList
    {
        public static readonly Dictionary<Notus.Variable.Enum.NetworkLayer,
                Dictionary<Notus.Variable.Enum.NetworkType, List<Notus.Variable.Struct.IpInfo>>> Main
            =
            new Dictionary<Notus.Variable.Enum.NetworkLayer,
            Dictionary<Notus.Variable.Enum.NetworkType, List<Notus.Variable.Struct.IpInfo>>>()
            {
                {
                    Notus.Variable.Enum.NetworkLayer.Layer1,
                        new Dictionary<Notus.Variable.Enum.NetworkType,List<Notus.Variable.Struct.IpInfo>>(){
                        {
                            Notus.Variable.Enum.NetworkType.DevNet,
                            new List<Notus.Variable.Struct.IpInfo>()
                            {
                                {
                                    new Notus.Variable.Struct.IpInfo()
                                    {
                                        IpAddress="89.252.134.91",
                                        Port=5002
                                    }
                                },
                                {
                                    new Notus.Variable.Struct.IpInfo()
                                    {
                                        IpAddress="89.252.184.151",
                                        Port=5002
                                    }
                                }
                            }
                        }
                    }
                }
            };
    }
}
