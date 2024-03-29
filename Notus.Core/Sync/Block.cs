﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Notus.Communication;
using NVG = Notus.Variable.Globals;
namespace Notus.Sync
{
    public class Block
    {
        public static bool Block2(
            List<Notus.Variable.Struct.IpInfo> nodeList,
            System.Action<Notus.Variable.Class.BlockData>? Func_NewBlockIncome = null
        )
        {
            bool waitForOtherNodes = false;
            long smallestBlockRow = long.MaxValue;
            foreach (Variable.Struct.IpInfo? tmpEntry in nodeList)
            {
                if (tmpEntry != null)
                {
                    Notus.Variable.Class.BlockData? nodeLastBlock = Notus.Toolbox.Network.GetLastBlock(tmpEntry, NVG.Settings);
                    if (nodeLastBlock != null)
                    {
                        if (smallestBlockRow > nodeLastBlock.info.rowNo)
                        {
                            smallestBlockRow = nodeLastBlock.info.rowNo;
                        }
                    }
                }
            }
            if (NVG.Settings.LastBlock.info.rowNo > smallestBlockRow)
            {
                //Console.WriteLine("My Node Higher Than Other");
            }
            else
            {
                //Console.WriteLine("My Node Smaller Than Other");
            }
            bool exitForLoop = false;
            int nCount = 0;
            List<bool> nodeControlList = new List<bool>();
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodeControlList.Add(false);
            }
            for (long blockNo = NVG.Settings.LastBlock.info.rowNo; blockNo < (smallestBlockRow + 1) && exitForLoop == false; blockNo++)
            {
                //kontrol edilmemiş olanlar false olarak işaretlenecek
                for (int i = 0; i < nodeList.Count; i++)
                {
                    nodeControlList[i] = false;
                }
                // burada belirtilen sayıda node'u kontrol ederek blok bulacak
                // aşağıdaki verilen 8 sayısı en fazla kontrol edilecek node sayısı
                for (int iCount = 0; iCount < 8; iCount++)
                {
                    if (nodeControlList[nCount] == false)
                    {
                        nodeControlList[nCount] = true;
                        Notus.Variable.Struct.IpInfo? currentNode = nodeList[nCount];
                        if (currentNode != null)
                        {
                            Notus.Variable.Class.BlockData? nodeLastBlock =
                                Notus.Toolbox.Network.GetBlockFromNode(
                                    currentNode,
                                    blockNo,
                                    NVG.Settings
                                );
                            if (nodeLastBlock != null)
                            {
                                waitForOtherNodes = true;
                                if (Func_NewBlockIncome != null)
                                {
                                    Func_NewBlockIncome(nodeLastBlock);
                                }
                            }
                        }
                    }
                    nCount++;
                    if (nodeList.Count == nCount)
                    {
                        nCount = 0;
                    }
                }
            }
            return waitForOtherNodes;
        }


        //burada verilen blok numarasını tüm nodelardan sorgula
        //alınan blok özetlerini kontrol et ve en çok olan özeti kabul et       
        private static Notus.Variable.Class.BlockData? GetValidBlock
        (
            Notus.Globals.Variable.Settings objSettings
        )
        {
            return null;
        }
    }
}
