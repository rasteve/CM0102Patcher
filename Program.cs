﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace CM0102Patcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            /*
            using (StreamReader sr = new StreamReader(@"C:\ChampMan\Notes\2020\2021\Derby County Minus 12 Points.patch"))
            {
                int firstAddr = -1;
                int lastAddr = -1;
                while (true)
                {
                    var line = sr.ReadLine();
                    if (line == null)
                        break;
                    var splits = line.Split(' ');
                    if (splits.Length >= 3)
                    {
                        var addrStr = splits[0].Substring(0, splits[0].Length - 1);
                        var addr = Convert.ToInt32(addrStr, 16);
                        if (addr != lastAddr + 1)
                            firstAddr = addr;
                        lastAddr = addr;
                        var newAddr = (addr - firstAddr) + 0x006DC000 + (0xDE7383 - 0xDE7000);
                        Console.WriteLine("{0}: {1} {2}", newAddr.ToString("X8"), splits[1], splits[2]);
                    }
                }
            }*/

                //CM9798.Test();
                /*
                CM2 cm2 = new CM2();
                cm2.ReadData();
                */
                //CM9798.SavedPlayerCount(@"C:\ChampMan\cm9798\Fresh\Data\CM9798\PLDATA1.S16");

                /*
                HistoryLoader hl = new HistoryLoader();
                hl.Load(@"C:\ChampMan\Championship Manager 0102\TestQuick\Oct2021\Data\index.dat");

                var latvia = hl.nation.First(x => x.Name.ReadString() == "Bolivia");
                foreach (var club in hl.club)
                {
                    if (club.Nation == latvia.ID)
                    {
                        Console.WriteLine("{0} - {1}", club.Name.ReadString(), club.ShortName.ReadString());
                    }
                }*/

                /*
                var serie = hl.club_comp.FirstOrDefault(x => MiscFunctions.GetTextFromBytes(x.Name) == "French Ligue 2");
                int zerorep = 0;
                //foreach (var club in hl.club)
                List<int> removeClubs = new List<int>();
                for (int i = 0; i < hl.club.Count; i++)
                {
                    if (hl.club[i].Division == -1 && hl.club[i].Reputation > 0 && hl.club[i].Reputation <= 500 && hl.club[i].HasLinkedClub == 0)
                        removeClubs.Add(i);
                    /*
                    if (hl.club[i].Reputation <= 1000)
                    {
                        var temp = hl.club[i];
                        temp.Reputation = 1;
                        hl.club[i] = temp;
                    }*/

                //if (club.Reputation > 1000 && club.Division == -1)
                //  Console.WriteLine("Hello");
                /*6
                if (club.Division == serie.ID)
                {
                    Console.WriteLine("{0} ----- {1}  ({2})", MiscFunctions.GetTextFromBytes(club.Name), MiscFunctions.GetTextFromBytes(club.ShortName), club.Reputation);
                }*/
                /*}
                removeClubs.Sort();
                removeClubs.Reverse();
                foreach (var remove in removeClubs)
                    hl.club.RemoveAt(remove);
                */
                //  hl.Save(@"C:\ChampMan\Championship Manager 0102\TestQuick\Oct2021\Data\index.dat", true);

                Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PatcherForm());
        }

        public static bool RunningInMono()
        {
            return (Type.GetType("Mono.Runtime") != null);
        }
    }
}
