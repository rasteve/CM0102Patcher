﻿using CM0102Patcher.Scouter;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CM0102Patcher
{
    public partial class Tools : Form
    {
        string exeFile;

        public Tools(string exeFile)
        {
            this.exeFile = exeFile;
            InitializeComponent();
        }

        private void buttonApplyPatchfile_Click(object sender, EventArgs e)
        {
            try
            {
                bool unApply = false;
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    MessageBox.Show("Going to Unapply this patchfile!!!", "Unapply Patchfile", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    unApply = true;
                }
                var ofd = new OpenFileDialog();
                ofd.Filter = "CM0102.exe Patch|*.patch|Text Files|*.txt|All files (*.*)|*.*";
                ofd.Title = "Select a CM0102 .patch file";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Logger.Log(exeFile, "{2} External Patchfile {0} to {1}", ofd.FileName, exeFile, unApply ? "Unapplying" : "Applying");
                    Patcher patcher = new Patcher();
                    var patch = patcher.LoadPatchFile(ofd.FileName);
                    if (unApply)
                    {
                        patcher.UnApplyPatch(exeFile, patch);
                        MessageBox.Show("Patch Unapplied successfully!", "Patch Unapplied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        if (patcher.ApplyPatch(exeFile, patch))
                            MessageBox.Show("Patch applied successfully!", "Patch Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionMsgBox.Show(ex);
            }
        }

        private void buttonOffsetCalculator_Click(object sender, EventArgs e)
        {
            var oc = new OffsetCalculator();
            oc.ShowDialog();
        }

        private bool EECHackSave(string saveFile)
        {
            int blockCount;
            int blockPos;
            using (var sr = new CM0102Scout.SaveReader(saveFile))
            {
                if (sr.IsCompressed)
                {
                    MessageBox.Show("EEC Hack only works on uncompressed save files!", "EEC Hack", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                blockPos = sr.GetBlockPos("nation.dat", CM0102Scout.SaveReader.NationSize, out blockCount);
            }
            return EECHack(saveFile, blockPos, blockCount);
        }

        private bool EECHack(string nationFile, int seekTo = 0, int blockCount = 0)
        {
            int block = 0;
            using (var file = File.Open(nationFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (var bw = new BinaryWriter(file))
                {
                    using (var br = new BinaryReader(file))
                    {
                        file.Seek(seekTo, SeekOrigin.Begin);
                        while (true)
                        {
                            file.Seek(0x7f, SeekOrigin.Current);
                            var eec = br.ReadByte();
                            if (eec > 3)
                            {
                                MessageBox.Show("Weird EEC byte read! Exiting...");
                                return false;
                            }
                            if (eec == 0x01 || eec == 0x00)
                            {
                                file.Seek(-1, SeekOrigin.Current);
                                bw.Write((byte)2);
                            }
                            file.Seek(-(0x7f + 1), SeekOrigin.Current);
                            file.Seek(0x122, SeekOrigin.Current);
                            block++;
                            if (blockCount != 0 && block == blockCount)
                                break;
                            if (file.Position + 0x122 >= file.Length)
                                break;
                        }
                    }
                }
            }
            return true;
        }

        private void buttonEECPatcher_Click(object sender, EventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog();
                ofd.Filter = "CM0102 nation.dat file|nation.dat|Uncompressed Saves (*.sav)|*.sav|All files (*.*)|*.*";
                ofd.Title = "Select a CM0102 nation.dat or uncompressed save game file";
                try
                {
                    if (!string.IsNullOrEmpty(RegString.GetRegString()))
                    {
                        var path = (string)Registry.GetValue(RegString.GetRegString(), "Location", "");
                        if (!string.IsNullOrEmpty(path))
                            ofd.InitialDirectory = Path.Combine(path, "Data");
                    }
                }
                catch { }

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var yesNo = MessageBox.Show("This will make all countries EEC members removing the need for work permits. Continue?", "EEC Patcher", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (yesNo == DialogResult.Yes)
                    {
                        bool success;
                        if (Path.GetExtension(ofd.FileName).ToLower() == ".sav")
                            success = EECHackSave(ofd.FileName);
                        else
                            success = EECHack(ofd.FileName);
                        if (success)
                            MessageBox.Show("EEC Hack applied successfully!", "EEC Hack Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionMsgBox.Show(ex);
            }
        }

        private void buttonRefereePatcher_Click(object sender, EventArgs e)
        {
            var rpf = new RefereePatcherForm();
            rpf.ShowDialog();
        }

        private void buttonSaveScouter_Click(object sender, EventArgs e)
        {
            var sg = new ScoutGrid();
            sg.ShowDialog();
        }

        private void buttonRGNImageConverter_Click(object sender, EventArgs e)
        {
            var imgConverter = new ImageConverterForm();
            imgConverter.ShowDialog();
        }

        private void buttonApplyMiscPatch_Click(object sender, EventArgs e)
        {
            Logger.Log(exeFile, "Opening up Misc Patches");
            MessageBox.Show("This will provide a list of ALL current miscellaneous patches.\r\nDO NOT APPLY ANY OF THESE UNLESS YOU KNOW WHAT YOU ARE DOING!!\r\nYou will most likely break your exe by applying these!!\r\nBest to do a Save so you can Restore afterwards!!\r\n\r\nDO NOT ASK FOR SUPPORT ON APPLYING THESE. YOU ARE ON YOUR OWN ON THIS!! :)", "WARNING!!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            new MiscPatches(exeFile).ShowDialog();
        }

        private void buttonRemoveStadiumLimits_Click(object sender, EventArgs e)
        {
            try
            {
                var ofd = new OpenFileDialog();
                ofd.Filter = "CM0102 stadium.dat file|stadium.dat|All files (*.*)|*.*";
                ofd.Title = "Select a CM0102 stadium.dat file";
                try
                {
                    if (!string.IsNullOrEmpty(RegString.GetRegString()))
                    {
                        var path = (string)Registry.GetValue(RegString.GetRegString(), "Location", "");
                        if (!string.IsNullOrEmpty(path))
                            ofd.InitialDirectory = Path.Combine(path, "Data");
                    }
                }
                catch { }

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Stadium.RemoveExpansionLimits(ofd.FileName);
                    MessageBox.Show("Stadium Expansion Limits Removed!", "Stadium Expansion Limits", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                ExceptionMsgBox.Show(ex);
            }
        }

        private void buttonSaveChanger_Click(object sender, EventArgs e)
        {
            var scf = new SaveChangerForm();
            scf.ShowDialog();
        }

        private void buttonFixtureScheduler_Click2(object sender, EventArgs e)
        {
            try
            {
                using (var fileLock = File.Open(exeFile, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    var yearChanger = new YearChanger();
                    int currentYear = yearChanger.GetCurrentExeYear(exeFile);
                    var fixtureScheduler = new FixtureScheduler(exeFile, currentYear);
                    fixtureScheduler.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                ExceptionMsgBox.Show(ex);
            }
        }

        private void buttonHistoryEditor_Click(object sender, EventArgs e)
        {
            HistoryEditorForm hef = new HistoryEditorForm();
            try
            {
                Logger.Log(exeFile, "Opening up History Editor");
                var indexFile = Path.Combine(Path.Combine(Path.GetDirectoryName(exeFile), "Data"), "index.dat");
                if (File.Exists(indexFile))
                    hef.IndexFile = indexFile;
            }
            catch { }
            hef.ShowDialog();
        }

        private void buttonPlayerTransfer_Click(object sender, EventArgs e)
        {
            PlayerTransferForm ptf = new PlayerTransferForm();
            ptf.ShowDialog();
        }

        private void buttonReviewExeLog_Click(object sender, EventArgs e)
        {
            ReviewExeLog rel = new ReviewExeLog(exeFile);
            rel.ShowDialog();
        }

        private void buttonGoHome_Click(object sender, EventArgs e)
        {
            var ghf = new GoHomeForm();
            ghf.ShowDialog();
        }
    }
}