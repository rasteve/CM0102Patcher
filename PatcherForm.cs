﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CM0102Patcher
{
    public partial class PatcherForm : Form
    {
        public PatcherForm()
        {
            InitializeComponent();

            try
            {
                var exeLocation = (string)Registry.GetValue(RegString.GetRegString(), "Location", "");
                if (!string.IsNullOrEmpty(exeLocation))
                {
                    labelFilename.Text = Path.Combine(exeLocation, "cm0102.exe");
                }
                comboBoxGameSpeed.Items.AddRange(new ComboboxItem[]
                {
                    new ComboboxItem("don't modify", 0),
                    new ComboboxItem("x0.5", 20000),
                    new ComboboxItem("default", 10000),
                    new ComboboxItem("x2", 5000),
                    new ComboboxItem("x4", 2500),
                    new ComboboxItem("x8", 1250),
                    new ComboboxItem("x20", 500),
                    new ComboboxItem("x200", 50),
                    new ComboboxItem("Max", 1)
                });
                comboBoxGameSpeed.SelectedIndex = 4;

                // Set selectable leagues
                comboBoxReplacementLeagues.Items.Add("English National League North");
                comboBoxReplacementLeagues.Items.Add("English National League South");
                comboBoxReplacementLeagues.Items.Add("English Southern Premier Central Division");

                // Set Default Start Year to this year if we're past July (else use last year) 
                var currentYear = DateTime.Now.Year;
                if (DateTime.Now.Month < 7)
                    currentYear--;
                numericGameStartYear.Value = currentYear;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "CM0102.exe Files|*.exe|All files (*.*)|*.*";
            ofd.Title = "Select a CM0102.exe file";
            try
            {
                var path = (string)Registry.GetValue(RegString.GetRegString(), "Location", "");
                if (!string.IsNullOrEmpty(path))
                    ofd.InitialDirectory = path;
            }
            catch { }
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                labelFilename.Text = ofd.FileName;
            }
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            try
            {
                // Let's go! - check the file exists and is writeable
                if (string.IsNullOrEmpty(labelFilename.Text))
                {
                    MessageBox.Show("Please select a cm0102.exe file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!File.Exists(labelFilename.Text))
                {
                    MessageBox.Show("Cannot find cm0102.exe file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                try
                {
                    using (var file = File.Open(labelFilename.Text, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to open and/or write to cm0102.exe file", "File Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var dir = Path.GetDirectoryName(labelFilename.Text);
                var dataDir = Path.Combine(dir, "Data");

                // Start the patcher
                Patcher patcher = new Patcher();
                if (!patcher.CheckForV3968(labelFilename.Text))
                {
                    var YesNo = MessageBox.Show("This does not look to be a 3.9.68 exe. Are you sure you wish to continue?", "3.9.68 Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (YesNo == DialogResult.No)
                        return;
                }

                // Initialise the name patcher
                var namePatcher = new NamePatcher(labelFilename.Text, dataDir);

                // Game speed hack
                var speed = (short)(int)(comboBoxGameSpeed.SelectedItem as ComboboxItem).Value;
                if (speed != 0)
                    patcher.SpeedHack(labelFilename.Text, speed);

                // Currency Inflation
                if (numericCurrencyInflation.Value != 0)
                    patcher.CurrencyInflationChanger(labelFilename.Text, (double)numericCurrencyInflation.Value);

                // Year Change
                if (checkBoxChangeStartYear.Checked)
                {
                    // Assume Staff.data is in Data
                    var staffFile = Path.Combine(dataDir, "staff.dat");
                    var indexFile = Path.Combine(dataDir, "index.dat");
                    var playerConfigFile = Path.Combine(dataDir, "player_setup.cfg");
                    var staffCompHistoryFile = Path.Combine(dataDir, "staff_comp_history.dat");
                    var clubCompHistoryFile = Path.Combine(dataDir, "club_comp_history.dat");
                    var staffHistoryFile = Path.Combine(dataDir, "staff_history.dat");
                    var nationCompHistoryFile = Path.Combine(dataDir, "nation_comp_history.dat");
                    try
                    {
                        YearChanger yearChanger = new YearChanger();
                        var currentYear = yearChanger.GetCurrentExeYear(labelFilename.Text);
                        if (currentYear != (int)numericGameStartYear.Value)
                        {
                            if (!File.Exists(staffFile) || !File.Exists(indexFile))
                            {
                                MessageBox.Show("staff.dat or index.dat not found in Data directory. Aborting year change.", "Files Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                            var yesNo = MessageBox.Show("The Start Year Changer updates staff.dat and other files in the Data directory with the correct years as well as the cm0102.exe. Are you happy to proceed?", "CM0102Patcher - Year Changer", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (yesNo == DialogResult.No)
                                return;
                            int yearIncrement = (((int)numericGameStartYear.Value) - currentYear);
                            yearChanger.ApplyYearChangeToExe(labelFilename.Text, (int)numericGameStartYear.Value);
                            yearChanger.UpdateStaff(indexFile, staffFile, yearIncrement);
                            yearChanger.UpdatePlayerConfig(playerConfigFile, yearIncrement);

                            yearChanger.UpdateHistoryFile(staffCompHistoryFile, 0x3a, yearIncrement, 0x8, 0x30);
                            yearChanger.UpdateHistoryFile(clubCompHistoryFile, 0x1a, yearIncrement, 0x8);
                            yearChanger.UpdateHistoryFile(staffHistoryFile, 0x11, yearIncrement, 0x8);

                            yearChanger.UpdateHistoryFile(nationCompHistoryFile, 0x1a, yearIncrement + 1, 0x8);
                        }
                    }
                    catch (Exception ex)
                    {
                        ExceptionMsgBox.Show(ex);
                        return;
                    }
                }

                // Patches
                if (checkBoxEnableColouredAtts.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["colouredattributes"]);
                if (checkBoxIdleSensitivity.Checked)
                {
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["idlesensitivity"]);
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["idlesensitivitytransferscreen"]);
                }
                if (checkBoxHideNonPublicBids.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["hideprivatebids"]);
                if (checkBox7Subs.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["sevensubs"]);
                if (checkBoxShowStarPlayers.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["showstarplayers"]);
                if (checkBoxDisableUnprotectedContracts.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["disableunprotectedcontracts"]);
                if (checkBoxCDRemoval.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["disablecdremove"]);
                if (checkBoxDisableSplashScreen.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["disablesplashscreen"]);
                if (checkBoxAllowCloseWindow.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["allowclosewindow"]);
                if (checkBoxForceLoadAllPlayers.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["forceloadallplayers"]);
                if (checkBoxRegenFixes.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["regenfixes"]);
                if (checkBoxChangeResolution1280s800.Checked)
                {
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["to1280x800"]);
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["tapanispacemaker"]);

                    int newWidth = 1280; // 1680;
                    int newHeight = 800; // 1050;
                    patcher.SetResolution(labelFilename.Text, newWidth, newHeight);

                    // Convert the core gfx
                    RGNConverter.RGN2RGN(Path.Combine(dataDir, "DEFAULT_PIC.RGN"), Path.Combine(dataDir, "bkg1280_800.rgn"), newWidth, newHeight);
                    RGNConverter.RGN2RGN(Path.Combine(dataDir, "match.mbr"), Path.Combine(dataDir, "m800.mbr"), 126, newHeight);
                    RGNConverter.RGN2RGN(Path.Combine(dataDir, "game.mbr"), Path.Combine(dataDir, "g800.mbr"), 126, newHeight);

                    var picturesDir = Path.Combine(dir, "Pictures");

                    if (Directory.Exists(picturesDir))
                    {
                        var yesNo = MessageBox.Show("Do you wish to convert your CM0102 Pictures directory to 1280x800 too?\r\n\r\nIf no, please turn off Background Changes in CM0102's Options else pictures will not appear correctly.\r\n\r\nIf yes, this takes a few moments.", "CM0102Patcher - Resolution Change", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (yesNo == DialogResult.Yes)
                        {
                            var pf = new PictureConvertProgressForm();

                            pf.OnLoadAction = () =>
                            {
                                new Thread(() =>
                                {
                                    int converting = 1;
                                    Thread.CurrentThread.IsBackground = true;

                                    var picFiles = Directory.GetFiles(picturesDir, "*.rgn");
                                    foreach (var picFile in picFiles)
                                    {
                                        pf.SetProgressText(string.Format("Converting {0}/{1} ({2})", converting++, picFiles.Length, Path.GetFileName(picFile)));
                                        pf.SetProgressPercent((int)(((double)(converting - 1) / ((double)picFiles.Length)) * 100.0));
                                        int Width, Height;
                                        RGNConverter.GetImageSize(picFile, out Width, out Height);
                                        if (Width == 800 && Height == 600)
                                        {
                                            RGNConverter.RGN2RGN(picFile, picFile + ".tmp", 1280, 800, 0, 35, 0, 100 - 35);
                                            File.SetAttributes(picFile, FileAttributes.Normal);
                                            File.Delete(picFile);
                                            File.Move(picFile + ".tmp", picFile);
                                        }
                                    }

                                    pf.CloseForm();
                                }).Start();
                            };

                            pf.ShowDialog();
                        }
                    }
                }
                if (checkBoxJobsAbroadBoost.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["jobsabroadboost"]);
                if (checkBoxNewRegenCode.Checked)
                {
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["tapaninewregencode"]);
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["tapanispacemaker"]);
                }
                if (checkBoxUpdateNames.Checked)
                {
                    namePatcher.RunPatch();
                }
                if (checkBoxManageAnyTeam.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["manageanyteam"]);
                if (checkBoxRemove3NonEULimit.Checked)
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["remove3playerlimit"]);
                if (checkBoxReplaceWelshPremier.Checked)
                {
                    switch (comboBoxReplacementLeagues.SelectedIndex)
                    {
                        case 0:
                            namePatcher.PatchWelshWithNorthernLeague();
                            break;
                        case 1:
                            namePatcher.PatchWelshWithSouthernLeague();
                            break;
                        case 2:
                            namePatcher.PatchWelshWithSouthernPremierCentral();
                            break;
                    }
                }
                if (checkBoxRestrictTactics.Checked)
                {
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["restricttactics"]);
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["changegeneraldat"]);
                }
                if (checkBoxMakeExecutablePortable.Checked)
                {
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["changeregistrylocation"]);
                    patcher.ApplyPatch(labelFilename.Text, patcher.patches["memorycheckfix"]);
                }

                // NOCD Crack
                if (checkBoxRemoveCDChecks.Checked)
                {
                    NoCDPatch nocd = new NoCDPatch();
                    var patched = nocd.PatchEXEFile(labelFilename.Text);
                }

                MessageBox.Show("Patched Successfully!", "CM0102 Patcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                ExceptionMsgBox.Show(ex);
            }
        }

        private void checkBoxChangeStartYear_CheckedChanged(object sender, EventArgs e)
        {
            labelGameStartYear.Enabled = numericGameStartYear.Enabled = checkBoxChangeStartYear.Checked;
        }

        private void buttonTools_Click(object sender, EventArgs e)
        {
            Tools tools = new Tools(labelFilename.Text);
            tools.ShowDialog();
        }

        private void ResetControls(Control controlContainer)
        {
            foreach (var control in controlContainer.Controls)
            {
                if (control is CheckBox)
                {
                    (control as CheckBox).Checked = false;
                }

                if (control is GroupBox || control is TabPage || control is TabControl)
                    ResetControls(control as Control);
            }
        }

        private void PatcherForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control &&
                (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                if (e.KeyChar == (char)19) // (S)ecret Mode
                {
                    checkBoxRemoveCDChecks.Checked = checkBoxRemoveCDChecks.Visible = true;
                    e.Handled = true;
                }
                if (e.KeyChar == (char)14) // N - Blank out all fields
                {
                    ResetControls(this);
                    numericCurrencyInflation.Value = 0;
                    comboBoxGameSpeed.SelectedIndex = 0;
                    e.Handled = true;
                }
                if (e.KeyChar == (char)1 && checkBoxRemoveCDChecks.Visible) // A
                {
                    string doubleWarning = labelFilename.Text.Contains("cm0001.exe") ? "" : "\r\n\r\nTHIS DOES NOT LOOK LIKE A CM0001.EXE!!!!!!!!!!\r\nDOUBLE CHECK BEFORE HITTING YES!!\r\n";
                    if (MessageBox.Show(string.Format("This will change exe:\r\n{0}\r\nTo Year: {1}\r\n\r\nARE YOU SURE YOU WANT TO DO THIS?!{2}", labelFilename.Text, (int)numericGameStartYear.Value, doubleWarning), "CM 00/01 Year Changer", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        var yearChanger = new YearChanger();
                        var currentYear = yearChanger.GetCurrentExeYear(labelFilename.Text, 0x0001009F);
                        yearChanger.ApplyYearChangeTo0001Exe(labelFilename.Text, (int)numericGameStartYear.Value);
                                                
                        var dir = Path.GetDirectoryName(labelFilename.Text);
                        var dataDir = Path.Combine(dir, "Data");
                        var staffFile = Path.Combine(dataDir, "staff.dat");
                        var indexFile = Path.Combine(dataDir, "index.dat");
                        var playerConfigFile = Path.Combine(dataDir, "player_setup.cfg");
                        var staffCompHistoryFile = Path.Combine(dataDir, "staff_comp_history.dat");
                        var clubCompHistoryFile = Path.Combine(dataDir, "club_comp_history.dat");
                        var staffHistoryFile = Path.Combine(dataDir, "staff_history.dat");
                        var nationCompHistoryFile = Path.Combine(dataDir, "nation_comp_history.dat");

                        /*
                        // Update Data Too
                        int yearIncrement = (((int)numericGameStartYear.Value) - currentYear);
                        yearChanger.UpdateStaff(indexFile, staffFile, yearIncrement);
                        yearChanger.UpdatePlayerConfig(playerConfigFile, yearIncrement);

                        // Update History
                        yearChanger.UpdateHistoryFile(staffCompHistoryFile, 0x3a, yearIncrement, 0x8, 0x30);
                        yearChanger.UpdateHistoryFile(clubCompHistoryFile, 0x1a, yearIncrement, 0x8);
                        yearChanger.UpdateHistoryFile(staffHistoryFile, 0x11, yearIncrement, 0x8);
                        yearChanger.UpdateHistoryFile(nationCompHistoryFile, 0x1a, yearIncrement + 1, 0x8);
                        */

                        MessageBox.Show("CM0001 Year Changer Patch Applied!", "CM 00/01 Year Changer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    e.Handled = true;
                }
                if (e.KeyChar == (char)2 && checkBoxRemoveCDChecks.Visible) // B
                {
                    var yearChanger = new YearChanger();
                    var dir = Path.GetDirectoryName(labelFilename.Text);
                    var dataDir = Path.Combine(dir, "Data");
                    var nationCompHistoryFile = Path.Combine(dataDir, "nation_comp_history.dat");
                    var clubCompHistoryFile = Path.Combine(dataDir, "club_comp_history.dat");
                    //yearChanger.UpdateHistoryFile(nationCompHistoryFile, 0x1a, +2, 0x8);
                    yearChanger.UpdateHistoryFile(clubCompHistoryFile, 0x1a, 3, 0x8);
                }
                if (e.KeyChar == (char)3 && checkBoxRemoveCDChecks.Visible) // C
                {
                    var nocd = new NoCDPatch();
                    nocd.PatchEXEFile0001FixV2(labelFilename.Text);
                }
            }
        }

        public class ComboboxItem
        {
            public ComboboxItem(string Text, object Value)
            {
                this.Text = Text;
                this.Value = Value;
            }
            public string Text { get; set; }
            public object Value { get; set; }
            public override string ToString()
            {
                return Text;
            }
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("CM0102Patcher by Nick\r\n\r\nAll credit should go to the geniuses that found and shared their code and great patching work:\r\nTapani\r\nJohnLocke\r\nSaturn\r\nxeno\r\nMadScientist\r\nAnd so many others!\r\n\r\nThanks to everyone at www.champman0102.co.uk for keeping the game alive :)", "CM0102Patcher", MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            RestorePoint.Save(labelFilename.Text);
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            RestorePoint.Restore(labelFilename.Text);
        }

        private void checkBoxAddNorthernLeague_CheckedChanged(object sender, EventArgs e)
        {
            comboBoxReplacementLeagues.Enabled = checkBoxReplaceWelshPremier.Checked;
            if (checkBoxReplaceWelshPremier.Checked && comboBoxReplacementLeagues.SelectedIndex == -1)
                comboBoxReplacementLeagues.SelectedIndex = 0;
        }
    }
}
