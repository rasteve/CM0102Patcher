﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Data;

namespace CM0102Scout
{
    public class SaveReader : IDisposable
    {
        const int BlockSize = 268;
        const int PlayerSize = 70;
        const int NationSize = 290;
        const int StaffSize = 110;
        const int NameSize = 60;
        const int ClubSize = 581;

        CMCompressedFileStream cfs;
        List<byte[]> FlstBlock = new List<byte[]>();
        Encoding latin1 = Encoding.GetEncoding("ISO-8859-1");
        List<string> firstNames = new List<string>();
        List<string> secondNames = new List<string>();
        Dictionary<int, Staff> staffList = new Dictionary<int, Staff>();
        List<Player> players = new List<Player>();
        Dictionary<int, Nation> nations = new Dictionary<int, Nation>();
        Dictionary<int, Club> clubs = new Dictionary<int, Club>();
        DateTime gameDate;

        public SaveReader(string saveFilename)
        {
            bool isCompressed;
            int blockCount;
            using (var sr = new StreamReader(saveFilename))
            {
                using (var br = new BinaryReader(sr.BaseStream))
                {
                    isCompressed = (br.ReadInt32() == 4);

                    // Skip 4 byes
                    sr.BaseStream.Seek(4, SeekOrigin.Current);

                    // Read blocks
                    blockCount = br.ReadInt32();
                    for (int j = 0; j < blockCount; j++)
                    {
                        byte[] newBlock = new byte[BlockSize];
                        br.Read(newBlock, 0, BlockSize);
                        FlstBlock.Add(newBlock);
                        Console.WriteLine("Added: " + TBlock.GetName(newBlock));
                    }
                }
            }

            cfs = new CMCompressedFileStream(saveFilename, isCompressed);
        }

        public void LoadNames()
        {
            // Load First Names
            var firstNameBlocks = ReadBlocks("first_names.dat", NameSize);
            foreach (var firstNameBlock in firstNameBlocks)
            {
                firstNames.Add(latin1.GetString(firstNameBlock, 0, 50).TrimEnd('\0'));
            }

            // Load Second Names
            var secondNameBlocks = ReadBlocks("second_names.dat", NameSize);
            foreach (var secondNameBlock in secondNameBlocks)
            {
                secondNames.Add(latin1.GetString(secondNameBlock, 0, 50).TrimEnd('\0'));
            }

            // Load Game Date
            var generalBlocks = ReadBlocks("general.dat", 3944 + 8);
            gameDate = CMDate(generalBlocks[0], 3944);
        }

        DateTime CMDate(byte[] data, int position)
        {
            var day = BitConverter.ToInt16(data, position);
            var year = BitConverter.ToInt16(data, position + 2);
            var leapYear = BitConverter.ToInt16(data, position + 4);
            return CMDate(day, year, leapYear);
        }

        DateTime CMDate(short day, short year, int leapYear)
        {
            if (leapYear == 1)
                day += 1;
            else
                day += 2;
            return new DateTime(year, 1, 1).AddDays(day - 1);
        }

        public void LoadPlayers()
        {
            if (firstNames.Count == 0)
                LoadNames();

            // Load staff
            var staffBlocks = ReadBlocks("staff.dat", StaffSize);
            foreach (var staffBlock in staffBlocks)
            {
                var staff = new Staff();
                staff.staffId = BitConverter.ToInt32(staffBlock, 0);
                staff.firstName = BitConverter.ToInt32(staffBlock, 4);
                staff.secondName = BitConverter.ToInt32(staffBlock, 8);
                staff.playerId = BitConverter.ToInt32(staffBlock, StaffSize - (1 + 4 + 4 + 4));
                staff.value = BitConverter.ToInt32(staffBlock, StaffSize - (1 + 4 + 4 + 4 + 11 + 4));
                staff.dob = CMDate(staffBlock, 16);
                staff.yearOfBirth = (short)(2001 - BitConverter.ToInt16(staffBlock, 24));
                staff.nationID = BitConverter.ToInt32(staffBlock, 24 + 2);
                staff.clubID = BitConverter.ToInt32(staffBlock, StaffSize - (1 + 4 + 4 + 4 + 11 + 4 + +4 + 8 + 8 + 1 + 4));
                staffList[staff.playerId] = staff;
            }

            // Load Nations
            var nationBlocks = ReadBlocks("nation.dat", NationSize);
            foreach (var nationBlock in nationBlocks)
            {
                var nation = new Nation();
                nation.nationID = BitConverter.ToInt32(nationBlock, 0);
                nation.name = latin1.GetString(nationBlock, 4, 50).TrimEnd('\0');
                nation.genderName = nationBlock[4 + 51];
                nation.shortName = latin1.GetString(nationBlock, 4 + 51 + 1, 26).TrimEnd('\0');
                nation.threeLetterName = latin1.GetString(nationBlock, 4 + 51 + 1 + 26 + 1, 3).TrimEnd('\0');
                nation.nationality = latin1.GetString(nationBlock, 4 + 51 + 1 + 26 + 1 + 4, 26).TrimEnd('\0');
                nation.continent = BitConverter.ToInt32(nationBlock, 4 + 51 + 1 + 26 + 1 + 4 + 26);
                nation.region = nationBlock[4 + 51 + 1 + 26 + 1 + 4 + 26 + 4];
                nation.actualRegion = nationBlock[4 + 51 + 1 + 26 + 1 + 4 + 26 + 4 + 1];
                nations[nation.nationID] = nation;
            }

            // Load Clubs
            var clubBlocks = ReadBlocks("club.dat", ClubSize);
            foreach (var clubBlock in clubBlocks)
            {
                var club = new Club();
                club.clubID = BitConverter.ToInt32(clubBlock, 0);
                club.name = latin1.GetString(clubBlock, 4, 50).TrimEnd('\0');
                club.genderName = clubBlock[4 + 51];
                club.shortName = latin1.GetString(clubBlock, 4 + 51 + 1, 26).TrimEnd('\0');
                clubs[club.clubID] = club;
            }

            // Load Players
            var playerBlocks = ReadBlocks("player.dat", PlayerSize);
            foreach (var playerBlock in playerBlocks)
            {
                int pos = 0;
                var player = new Player();
                var fields = typeof(Player).GetFields();
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(int))
                    {
                        field.SetValue(player, BitConverter.ToInt32(playerBlock, pos));
                        pos += 4;
                    }
                    else
                    if (field.FieldType == typeof(sbyte))
                    {
                        field.SetValue(player, (sbyte)playerBlock[pos]);
                        pos += 1;
                    }
                    else
                    if (field.FieldType == typeof(byte))
                    {
                        field.SetValue(player, (byte)playerBlock[pos]);
                        pos += 1;
                    }
                    else
                    if (field.FieldType == typeof(short))
                    {
                        field.SetValue(player, BitConverter.ToInt16(playerBlock, pos));
                        pos += 2;
                    }
                }

                players.Add(player);
            }
        }

        public DataTable CreateDataTable(bool instrinsicsOn = true)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Age", typeof(int));
            dataTable.Columns.Add("Club", typeof(string));
            dataTable.Columns.Add("Nationality", typeof(string));
            dataTable.Columns.Add("Position", typeof(string));
            dataTable.Columns.Add("CA", typeof(int));
            dataTable.Columns.Add("PA", typeof(int));
            dataTable.Columns.Add("Value", typeof(int));
            dataTable.Columns.Add("Acceleration", typeof(sbyte));
            dataTable.Columns.Add("Aggression", typeof(sbyte));
            dataTable.Columns.Add("Agility", typeof(sbyte));
            dataTable.Columns.Add("Anticipation", typeof(sbyte));
            dataTable.Columns.Add("Balance", typeof(sbyte));
            dataTable.Columns.Add("Bravery", typeof(sbyte));
            dataTable.Columns.Add("Consistency", typeof(sbyte));
            dataTable.Columns.Add("Corners", typeof(sbyte));
            dataTable.Columns.Add("Crossing", typeof(sbyte));
            dataTable.Columns.Add("Decisions", typeof(sbyte));
            dataTable.Columns.Add("Dirtiness", typeof(sbyte));
            dataTable.Columns.Add("Dribbling", typeof(sbyte));
            dataTable.Columns.Add("Finishing", typeof(sbyte));
            dataTable.Columns.Add("Flair", typeof(sbyte));
            dataTable.Columns.Add("Free Kicks", typeof(sbyte));
            dataTable.Columns.Add("Handling", typeof(sbyte));
            dataTable.Columns.Add("Heading", typeof(sbyte));
            dataTable.Columns.Add("Important Matches", typeof(sbyte));
            dataTable.Columns.Add("Injury Proneness", typeof(sbyte));
            dataTable.Columns.Add("Jumping", typeof(sbyte));
            dataTable.Columns.Add("Leadership", typeof(sbyte));
            dataTable.Columns.Add("Left Foot", typeof(sbyte));
            dataTable.Columns.Add("Long Shots", typeof(sbyte));
            dataTable.Columns.Add("Marking", typeof(sbyte));
            dataTable.Columns.Add("Movement", typeof(sbyte));
            dataTable.Columns.Add("Natural Fitness", typeof(sbyte));
            dataTable.Columns.Add("One On Ones", typeof(sbyte));
            dataTable.Columns.Add("Pace", typeof(sbyte));
            dataTable.Columns.Add("Passing", typeof(sbyte));
            dataTable.Columns.Add("Penalties", typeof(sbyte));
            dataTable.Columns.Add("Positioning", typeof(sbyte));
            dataTable.Columns.Add("Reflexes", typeof(sbyte));
            dataTable.Columns.Add("Right Foot", typeof(sbyte));
            dataTable.Columns.Add("Stamina", typeof(sbyte));
            dataTable.Columns.Add("Strength", typeof(sbyte));
            dataTable.Columns.Add("Tackling", typeof(sbyte));
            dataTable.Columns.Add("Teamwork", typeof(sbyte));
            dataTable.Columns.Add("Technique", typeof(sbyte));
            dataTable.Columns.Add("Throw Ins", typeof(sbyte));
            dataTable.Columns.Add("Versatility", typeof(sbyte));
            dataTable.Columns.Add("Vision", typeof(sbyte));
            dataTable.Columns.Add("Work Rate", typeof(sbyte));
            dataTable.Columns.Add("Player Morale", typeof(byte));

            foreach (var player in players)
            {
                var staff = staffList[player.ID];
                var name = firstNames[staff.firstName] + " " + secondNames[staff.secondName];
                var age = AgeCalc(staff.dob);
                var club = "None";
                if (clubs.ContainsKey(staff.clubID))
                    club = clubs[staff.clubID].shortName;
                var nationality = "Unknown";
                nationality = nations[staff.nationID].nationality;

                dataTable.Rows.Add(name, age, club, nationality, player.ShortPosition(), player.CurrentAbility, player.PotentialAbility, staff.value,
                    player.Acceleration,
                    player.Aggression,
                    player.Agility,
                    instrinsicsOn ? player.Anticipation : player.Convert(player.Anticipation),
                    player.Balance,
                    player.Bravery,
                    player.Consistency,
                    player.Corners,
                    instrinsicsOn ? player.Crossing : player.Convert(player.Crossing, true),
                    instrinsicsOn ? player.Decisions : player.Convert(player.Decisions),
                    player.Dirtiness,
                    instrinsicsOn ? player.Dribbling : player.Convert(player.Dribbling, true),
                    instrinsicsOn ? player.Finishing : player.Convert(player.Finishing, true),
                    player.Flair,
                    player.FreeKicks,
                    instrinsicsOn ? player.Handling : player.Convert(player.Handling, true, true),
                    instrinsicsOn ? player.Heading : player.Convert(player.Heading),
                    player.ImportantMatches,
                    player.InjuryProneness,
                    player.Jumping,
                    player.Leadership,
                    player.LeftFoot,
                    instrinsicsOn ? player.LongShots : player.Convert(player.LongShots),
                    instrinsicsOn ? player.Marking : player.Convert(player.Marking, true),
                    instrinsicsOn ? player.Movement : player.Convert(player.Movement, true),
                    player.NaturalFitness,
                    instrinsicsOn ? player.OneOnOnes : player.Convert(player.OneOnOnes, true, true),
                    player.PlayerPace,
                    instrinsicsOn ? player.Passing : player.Convert(player.Passing),
                    instrinsicsOn ? player.Penalties : player.Convert(player.Penalties),
                    instrinsicsOn ? player.Positioning : player.Convert(player.Positioning),
                    instrinsicsOn ? player.Reflexes : player.Convert(player.Reflexes, true, true),
                    player.RightFoot,
                    player.Stamina,
                    player.Strength,
                    instrinsicsOn ? player.Tackling : player.Convert(player.Tackling),
                    player.Teamwork,
                    player.Technique,
                    instrinsicsOn ? player.ThrowIns : player.Convert(player.ThrowIns, true),
                    player.Versatility,
                    instrinsicsOn ? player.Vision : player.Convert(player.Vision, true),
                    player.WorkRate,
                    player.PlayerMorale
                    );
            }
            return dataTable;
        }

        int AgeCalc(DateTime dob)
        {
            int age = gameDate.Year - dob.Year;
            if (dob > gameDate.AddYears(-age)) age--;
            return age;
        }

        public void Dispose()
        {
            cfs.Dispose();
        }

        List<byte[]> ReadBlocks(string blockName, int blockSize)
        {
            var block = FlstBlock.FirstOrDefault(x => TBlock.GetName(x) == blockName);
            cfs.Seek(TBlock.GetPosition(block), SeekOrigin.Begin);
            var blockCount = TBlock.GetSize(block) / blockSize;

            var blocks = new List<byte[]>();
            for (int j = 0; j < blockCount; j++)
            {
                var blockBytes = new byte[blockSize];
                cfs.Read(blockBytes, blockSize);
                blocks.Add(blockBytes);
            }
            return blocks;
        }
    }
}