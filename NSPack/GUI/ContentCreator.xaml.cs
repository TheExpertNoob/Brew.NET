using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.Windows.Interop;
using Microsoft.VisualBasic.FileIO;
using System.Collections.Generic;
using Brew.HacPack;

namespace Brew.NSPack.GUI
{
    public partial class ContentCreator : Page
    {
        public readonly string logopath = HacPack.Utils.TemporaryDirectory + "\\logo.jpg";
		
		private bool isInitializing = true;

        public class SDKInfo
        {
            public string Version { get; set; }
            public string HexValue { get; set; }
            
            public SDKInfo(string version)
            {
                Version = version;
                HexValue = ConvertToHex(version);
            }
            
            private string ConvertToHex(string version)
            {
                var parts = version.Split('.');
                if (parts.Length != 4) return "000C1100"; // default fallback
                
                byte major = byte.Parse(parts[0]);
                byte minor = byte.Parse(parts[1]);
                byte micro = byte.Parse(parts[2]);
                byte revision = byte.Parse(parts[3]);
                
                return $"{major:X2}{minor:X2}{micro:X2}{revision:X2}";
            }
        }

        private Dictionary<int, Dictionary<string, List<string>>> firmwareSDKMap = new Dictionary<int, Dictionary<string, List<string>>>
        {
            {1, new Dictionary<string, List<string>> {
                {"1.0.0", new List<string> {"0.11.29.0", "0.11.30.0"}},
                {"2.0.0", new List<string> {"0.11.29.0", "0.11.30.0", "1.2.0.0", "1.2.2.0", "1.3.0.0", "1.3.2.0"}},
                {"2.1.0", new List<string> {"1.2.3.0", "1.3.2.0"}},
                {"2.2.0", new List<string> {"1.2.3.0", "1.3.2.0"}},
                {"2.3.0", new List<string> {"1.3.2.0"}}
            }},
            {2, new Dictionary<string, List<string>> {
                {"3.0.0", new List<string> {"3.3.0.0", "3.3.1.0", "3.4.0.0"}}
            }},
            {3, new Dictionary<string, List<string>> {
                {"3.0.1", new List<string> {"3.3.0.0", "3.4.0.0"}},
                {"3.0.2", new List<string> {"3.4.0.0", "3.5.1.0"}}
            }},
            {4, new Dictionary<string, List<string>> {
                {"4.0.0", new List<string> {"0.11.29.0", "4.2.0.0", "4.3.0.0", "4.3.1.0", "4.3.3.0", "4.4.0.0"}},
                {"4.0.1", new List<string> {"4.4.0.0"}},
                {"4.1.0", new List<string> {"4.4.0.0"}}
            }},
            {5, new Dictionary<string, List<string>> {
                {"5.0.0", new List<string> {"4.3.1.0", "5.2.0.0", "5.3.0.0"}},
                {"5.0.1", new List<string> {"5.2.0.0", "5.3.0.0"}},
                {"5.0.2", new List<string> {"5.2.0.0", "5.3.0.0"}},
                {"5.1.0", new List<string> {"5.3.0.0", "5.4.110.0"}}
            }},
            {6, new Dictionary<string, List<string>> {
                {"6.0.0", new List<string> {"6.3.0.0", "6.3.1.0", "6.3.2.0", "6.4.0.0"}},
                {"6.0.1", new List<string> {"6.3.4.0", "6.4.0.0"}},
                {"6.1.0", new List<string> {"6.3.1.0", "6.3.5.0", "6.4.0.0"}}
            }},
            {7, new Dictionary<string, List<string>> {
                {"6.2.0", new List<string> {"0.11.29.0", "4.3.1.0", "5.2.0.0", "5.3.0.0", "6.3.0.0", "6.3.1.0", "6.3.2.0", "6.3.5.0", "6.4.0.0"}}
            }},
            {8, new Dictionary<string, List<string>> {
                {"7.0.0", new List<string> {"7.2.1.0", "7.3.0.0"}},
                {"7.0.1", new List<string> {"7.3.0.0"}},
                {"8.0.0", new List<string> {"8.1.0.0", "8.2.99.0"}},
                {"8.0.1", new List<string> {"8.1.0.0", "8.2.0.0"}}
            }},
            {9, new Dictionary<string, List<string>> {
                {"8.1.0", new List<string> {"8.1.0.0", "8.2.0.0"}},
                {"8.1.1", new List<string> {"8.1.0.0", "8.2.0.0", "8.2.99.0"}}
            }},
            {10, new Dictionary<string, List<string>> {
                {"9.0.0", new List<string> {"9.2.2.0", "9.3.0.0"}},
                {"9.0.1", new List<string> {"9.2.2.0", "9.3.0.0"}}
            }},
            {11, new Dictionary<string, List<string>> {
                {"9.1.0", new List<string> {"0.11.29.0", "4.3.1.0", "6.4.0.0", "9.2.2.0", "9.2.3.0", "9.3.0.0"}},
                {"9.2.0", new List<string> {"9.3.0.0"}},
                {"10.0.0", new List<string> {"10.2.0.0", "10.3.0.0", "10.4.0.0"}},
                {"10.0.1", new List<string> {"10.4.0.0"}},
                {"10.0.2", new List<string> {"10.4.0.0"}},
                {"10.0.3", new List<string> {"10.4.0.0"}},
                {"10.0.4", new List<string> {"10.3.0.0", "10.4.0.0"}},
                {"10.1.0", new List<string> {"10.4.0.0", "10.5.2.0"}},
                {"10.1.1", new List<string> {"10.4.0.0", "10.7.99.0"}},
                {"10.2.0", new List<string> {"10.4.0.0", "10.7.1.0", "10.7.99.0"}},
                {"11.0.0", new List<string> {"11.3.0.0", "11.3.3.0", "11.4.0.0"}},
                {"11.0.1", new List<string> {"11.4.0.0"}},
                {"12.0.0", new List<string> {"11.4.3.0", "12.2.0.0", "12.3.0.0"}},
                {"12.0.1", new List<string> {"12.3.0.0"}},
                {"12.0.2", new List<string> {"12.3.0.0"}},
                {"12.0.3", new List<string> {"12.3.0.0"}}
            }},
            {12, new Dictionary<string, List<string>> {
                {"12.1.0", new List<string> {"12.3.0.0", "12.3.2.0"}},
                {"13.0.0", new List<string> {"13.2.1.0", "13.3.0.0"}},
                {"13.1.0", new List<string> {"13.4.0.0"}},
                {"13.2.0", new List<string> {"13.4.0.0", "13.4.1.0"}},
                {"13.2.1", new List<string> {"13.4.0.0"}}
            }},
            {13, new Dictionary<string, List<string>> {
                {"14.0.0", new List<string> {"10.7.1.0", "14.2.0.0", "14.3.0.0"}},
                {"14.1.0", new List<string> {"14.3.0.0"}},
                {"14.1.1", new List<string> {"14.2.0.0", "14.3.0.0"}},
                {"14.1.2", new List<string> {"14.3.0.0"}}
            }},
            {14, new Dictionary<string, List<string>> {
                {"15.0.0", new List<string> {"15.2.1.0", "15.3.0.0"}},
                {"15.0.1", new List<string> {"15.2.1.0", "15.3.0.0"}}
            }},
            {15, new Dictionary<string, List<string>> {
                {"16.0.0", new List<string> {"16.1.2.0", "16.1.3.0", "16.2.0.0"}},
                {"16.0.1", new List<string> {"16.2.0.0"}},
                {"16.0.2", new List<string> {"16.2.0.0"}},
                {"16.0.3", new List<string> {"16.2.0.0"}},
                {"16.1.0", new List<string> {"16.2.0.0", "16.2.2.0", "16.2.3.0"}}
            }},
            {16, new Dictionary<string, List<string>> {
                {"17.0.0", new List<string> {"17.4.0.0", "17.4.2.0", "17.5.0.0"}},
                {"17.0.1", new List<string> {"17.5.0.0"}}
            }},
            {17, new Dictionary<string, List<string>> {
                {"18.0.0", new List<string> {"18.2.0.0", "18.2.2.0", "18.3.0.0"}},
                {"18.0.1", new List<string> {"18.3.0.0"}},
                {"18.1.0", new List<string> {"18.2.2.0", "18.2.3.0", "18.3.0.0"}}
            }},
            {18, new Dictionary<string, List<string>> {
                {"19.0.0", new List<string> {"19.3.0.0"}},
                {"19.0.1", new List<string> {"19.3.0.0"}}
            }},
            {19, new Dictionary<string, List<string>> {
                {"20.0.0", new List<string> {"10.7.1.0", "20.5.100.0", "20.5.3.0", "20.5.4.0"}},
                {"20.0.1", new List<string> {"20.5.4.0"}},
                {"20.1.0", new List<string> {"20.5.4.0", "20.5.110.0"}},
                {"20.1.1", new List<string> {"20.5.4.0"}},
                {"20.1.5", new List<string> {"20.5.4.0", "20.5.110.0"}},
                {"20.2.0", new List<string> {"20.5.110.0", "20.5.112.0", "20.5.4.0"}},
                {"20.3.0", new List<string> {"20.5.4.0", "20.5.112.0"}},
                {"20.4.0", new List<string> {"20.5.110.0", "20.5.112.0", "20.5.4.0"}},
                {"20.5.0", new List<string> {"20.5.4.0"}}
            }},
            {20, new Dictionary<string, List<string>> {
                {"21.0.0", new List<string> {"10.7.1.0", "21.3.0.0", "21.3.1.0", "21.4.0.0"}},
                {"21.0.1", new List<string> {"21.4.0.0"}},
                {"21.1.0", new List<string> {"21.3.0.0", "21.3.1.0", "21.4.0.0"}}
            }}
        };

        public ContentCreator()
        {
            InitializeComponent();
            if(File.Exists(logopath)) File.Delete(logopath);
            Properties.Resources.SampleLogo.Save(logopath, ImageFormat.Jpeg);
            Image_Icon.Source = Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.SampleLogo.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            //InitializeDropdowns();
            isInitializing = false;
        }

        private void InitializeDropdowns()
        {
            PopulateFirmwareVersions(Combo_KeyGen.SelectedIndex + 1);
        }
        
        private void Combo_KeyGen_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;
            
            // Use SelectedIndex instead of parsing Text
            int keyGen = Combo_KeyGen.SelectedIndex + 1; // +1 because index 0 = keygen 1
            PopulateFirmwareVersions(keyGen);
        }
        
        private void Combo_Firmware_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing) return;
            if (Combo_Firmware.SelectedItem == null) return;
            
            string firmware = Combo_Firmware.SelectedItem.ToString();
            int keyGen = Combo_KeyGen.SelectedIndex + 1;
            PopulateSDKVersions(keyGen, firmware);
        }

        private int GetSelectedKeyGen()
        {
            string kgen = Combo_KeyGen.Text;
            if(kgen is "1 (1.0.0 - 2.3.0)") return 1;
            else if(kgen is "2 (3.0.0)") return 2;
            else if(kgen is "3 (3.0.1 - 3.0.2)") return 3;
            else if(kgen is "4 (4.0.0 - 4.1.0)") return 4;
            else if(kgen is "5 (5.0.0 - 5.1.0)") return 5;
            else if(kgen is "6 (6.0.0 - 6.1.0)") return 6;
            else if(kgen is "7 (6.2.0)") return 7;
            else if(kgen is "8 (7.0.0 - 8.0.1)") return 8;
            else if(kgen is "9 (8.1.0)") return 9;
            else if(kgen is "10 (9.0.0 - 9.0.1)") return 10;
            else if(kgen is "11 (9.1.0 - 12.0.3)") return 11;
            else if(kgen is "12 (12.1.0 - 13.2.1)") return 12;
            else if(kgen is "13 (14.0.0 - 14.1.2)") return 13;
            else if(kgen is "14 (15.0.0 - 15.0.1)") return 14;
            else if(kgen is "15 (16.0.0 - 16.1.0)") return 15;
            else if(kgen is "16 (17.0.0 - 17.0.1)") return 16;
            else if(kgen is "17 (18.0.0 - 18.1.0)") return 17;
            else if(kgen is "18 (19.0.0 - 19.0.1)") return 18;
            else if(kgen is "19 (20.0.0 - 20.5.0)") return 19;
            else if(kgen is "20 (21.0.0 - Latest)") return 20;
            return Combo_KeyGen.SelectedIndex + 1;
        }

        private void PopulateFirmwareVersions(int keyGen)
        {
            Combo_Firmware.SelectionChanged -= Combo_Firmware_SelectionChanged;
            
            Combo_Firmware.Items.Clear();
            Combo_SDK.Items.Clear();
            
            if (firmwareSDKMap.ContainsKey(keyGen))
            {
                foreach (var firmware in firmwareSDKMap[keyGen].Keys)
                {
                    Combo_Firmware.Items.Add(firmware);
                }
                
                if (Combo_Firmware.Items.Count > 0)
                {
                    Combo_Firmware.SelectedIndex = Combo_Firmware.Items.Count - 1;
                }
            }
            
            Combo_Firmware.SelectionChanged += Combo_Firmware_SelectionChanged;
            
            // Manually trigger SDK population
            if (Combo_Firmware.SelectedItem != null)
            {
                string firmware = Combo_Firmware.SelectedItem.ToString();
                PopulateSDKVersions(keyGen, firmware);
            }
        }
        
        private void PopulateSDKVersions(int keyGen, string firmware)
        {
            Combo_SDK.Items.Clear();
            
            if (firmwareSDKMap.ContainsKey(keyGen) && firmwareSDKMap[keyGen].ContainsKey(firmware))
            {
                foreach (var sdkVersion in firmwareSDKMap[keyGen][firmware])
                {
                    Combo_SDK.Items.Add(sdkVersion);
                }
                
                if (Combo_SDK.Items.Count > 0)
                {
                    Combo_SDK.SelectedIndex = Combo_SDK.Items.Count - 1;
                }
            }
        }

        private string GetSDKHex()
        {
            if (Combo_SDK.SelectedItem == null)
                return "000C1100"; // default
            
            string sdkVersion = Combo_SDK.SelectedItem.ToString();
            var sdk = new SDKInfo(sdkVersion);
            return sdk.HexValue;
        }

        private string GetSelectedFirmware()
        {
            if (Combo_Firmware.SelectedItem == null)
                return "1.0.0";
            
            return Combo_Firmware.SelectedItem.ToString();
        }

        private void Button_ExeFSBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog exe = new FolderBrowserDialog()
            {
                Description = "Select ExeFS directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = exe.ShowDialog();
            if (res is DialogResult.OK)
            {
                if (!File.Exists(exe.SelectedPath + "\\main"))
                {
                    GUI.Resources.log("ExeFS directory doesn't have a main source file (main)");
                    return;
                }
                if (!File.Exists(exe.SelectedPath + "\\main.npdm"))
                {
                    GUI.Resources.log("ExeFS directory doesn't have a NPDM metadata file (main.npdm)");
                    return;
                }
                Box_ExeFS.Text = exe.SelectedPath;
            }
        }

        private void Button_RomFSBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog rom = new FolderBrowserDialog()
            {
                Description = "Select RomFS directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = rom.ShowDialog();
            if (res is DialogResult.OK) Box_RomFS.Text = rom.SelectedPath;
        }

        private void Button_CustomLogoBrowse_Click(object sender, RoutedEventArgs e)
        {
            GUI.Resources.log("The logo directory must contain just two files: a GIF file and a PNG file.\nThe GIF and the PNG need to be made using Photoshop.\nHorizon reads their metadata, otherwise they won't work.", LogType.Warning);
            FolderBrowserDialog clogo = new FolderBrowserDialog()
            {
                Description = "Select custom logo directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = clogo.ShowDialog();
            if (res is DialogResult.OK) Box_CustomLogo.Text = clogo.SelectedPath;
        }

        private void Button_ImportantHTMLBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog imp = new FolderBrowserDialog()
            {
                Description = "Select legal information's important HTML directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = imp.ShowDialog();
            if (res is DialogResult.OK)
            {
                if (!File.Exists(imp.SelectedPath + "\\index.html"))
                {
                    GUI.Resources.log("Selected HTML documents folder does not have a HTML index file (index.html)");
                    return;
                }
                Box_ImportantHTML.Text = imp.SelectedPath;
            }
        }

        private void Button_IPNoticesHTMLBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog ipn = new FolderBrowserDialog()
            {
                Description = "Select legal information's IPNotices HTML directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = ipn.ShowDialog();
            if (res is DialogResult.OK)
            {
                if (!File.Exists(ipn.SelectedPath + "\\index.html"))
                {
                    GUI.Resources.log("Selected HTML documents folder does not have a HTML index file (index.html)");
                    return;
                }
                Box_IPNoticesHTML.Text = ipn.SelectedPath;
            }
        }

        private void Button_SupportHTMLBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog sup = new FolderBrowserDialog()
            {
                Description = "Select legal information's support HTML directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = sup.ShowDialog();
            if (res is DialogResult.OK)
            {
                if (!File.Exists(sup.SelectedPath + "\\index.html"))
                {
                    GUI.Resources.log("Selected HTML documents folder does not have a HTML index file (index.html)");
                    return;
                }
                Box_SupportHTML.Text = sup.SelectedPath;
            }
        }

        private void Button_OfflineHTMLBrowse_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog off = new FolderBrowserDialog()
            {
                Description = "Select offline HTML directory",
                ShowNewFolderButton = true,
            };
            DialogResult res = off.ShowDialog();
            if (res is DialogResult.OK)
            {
                if (!File.Exists(off.SelectedPath + "\\index.html"))
                {
                    GUI.Resources.log("Selected HTML documents folder does not have a HTML index file (index.html)");
                    return;
                }
                Box_OfflineHTML.Text = off.SelectedPath;
            }
        }

        private void Button_IconBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog icon = new OpenFileDialog()
            {
                Title = "Select icon for the NSP package",
                Filter = "Common image types (*.bmp, *.png, *.jpg, *.jpeg, *.dds, *.tga)|*.bmp;*.png;*.jpg;*.jpeg;*.dds;*.tga",

                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            DialogResult res = icon.ShowDialog();
            if (res is DialogResult.OK)
            {
                Bitmap logo = new Bitmap(icon.FileName);
                if (File.Exists(logopath)) File.Delete(logopath);
                logo.Save(logopath, ImageFormat.Jpeg);
                Image_Icon.Source = Imaging.CreateBitmapSourceFromHBitmap(logo.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
        }

        private void Button_KeySetBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog kset = new OpenFileDialog()
            {
                Title = "Select keyset file",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            };
            DialogResult res = kset.ShowDialog();
            if (res is DialogResult.OK) Box_KeySet.Text = kset.FileName;
        }

        private void Button_BuildProgram_Click(object sender, RoutedEventArgs e)
        {
            string tid = Box_TitleID.Text;
            if(tid.Length != 16)
            {
                GUI.Resources.log("Title ID doesn't have 16 characters.");
                return;
            }
            if(!Regex.IsMatch(tid, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                GUI.Resources.log("Title ID is not a valid hex string.");
                return;
            }
            byte keygen = 20;
			string sdkHex = GetSDKHex();
			string requiredSystemVersion = GetSelectedFirmware();
            string kgen = Combo_KeyGen.Text;
            if(kgen is "1 (1.0.0 - 2.3.0)") keygen = 1;
            else if(kgen is "2 (3.0.0)") keygen = 2;
            else if(kgen is "3 (3.0.1 - 3.0.2)") keygen = 3;
            else if(kgen is "4 (4.0.0 - 4.1.0)") keygen = 4;
            else if(kgen is "5 (5.0.0 - 5.1.0)") keygen = 5;
            else if(kgen is "6 (6.0.0 - 6.1.0)") keygen = 6;
            else if(kgen is "7 (6.2.0)") keygen = 7;
            else if(kgen is "8 (7.0.0 - 8.0.1)") keygen = 8;
            else if(kgen is "9 (8.1.0)") keygen = 9;
            else if(kgen is "10 (9.0.0 - 9.0.1)") keygen = 10;
            else if(kgen is "11 (9.1.0 - 12.0.3)") keygen = 11;
            else if(kgen is "12 (12.1.0 - 13.2.1)") keygen = 12;
            else if(kgen is "13 (14.0.0 - 14.1.2)") keygen = 13;
            else if(kgen is "14 (15.0.0 - 15.0.1)") keygen = 14;
            else if(kgen is "15 (16.0.0 - 16.1.0)") keygen = 15;
            else if(kgen is "16 (17.0.0 - 17.0.1)") keygen = 16;
            else if(kgen is "17 (18.0.0 - 18.1.0)") keygen = 17;
            else if(kgen is "18 (19.0.0 - 19.0.1)") keygen = 18;
            else if(kgen is "19 (20.0.0 - 20.5.0)") keygen = 19;
            else if(kgen is "20 (21.0.0 - Latest)") keygen = 20;
            string ccwd = Utils.Cwd;
            string exefs = null;
            string romfs = null;
            string logo = null;
            if(string.IsNullOrEmpty(Box_ExeFS.Text))
            {
                GUI.Resources.log("ExeFS directory was not selected.");
                return;
            }
            if(!Directory.Exists(Box_ExeFS.Text))
            {
                GUI.Resources.log("ExeFS directory does not exist.");
                return;
            }
            exefs = Box_ExeFS.Text;
            if(!string.IsNullOrEmpty(Box_RomFS.Text))
            {
                if(!Directory.Exists(Box_RomFS.Text))
                {
                    GUI.Resources.log("RomFS directory does not exist.");
                    return;
                }
                romfs = Box_RomFS.Text;
            }
            if(!string.IsNullOrEmpty(Box_CustomLogo.Text))
            {
                if(!Directory.Exists(Box_CustomLogo.Text))
                {
                    GUI.Resources.log("Custom logo directory does not exist.");
                    return;
                }
                if(!File.Exists(Box_CustomLogo.Text + "\\NintendoLogo.png"))
                {
                    GUI.Resources.log("NintendoLogo.png logo file does not exist in custom logo's directory.");
                    return;
                }
                if(!File.Exists(Box_CustomLogo.Text + "\\StartupMovie.gif"))
                {
                    GUI.Resources.log("StartupMovie.giflogo file does not exist in custom logo's directory.");
                    return;
                }
                if(Directory.GetFileSystemEntries(Box_CustomLogo.Text).Length > 2)
                {
                    GUI.Resources.log("There are too many files or directories in custom logo directory.");
                    return;
                }
                logo = Box_CustomLogo.Text;
            }
            if(string.IsNullOrEmpty(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file was not selected. This file is required to build the NSP package.");
                return;
            }
            if(!File.Exists(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file does not exist. This file is required to build the NSP package.");
                return;
            }
            string kset = Box_KeySet.Text;
            NCA program = new NCA(kset);
            program.TitleID = tid;
            program.KeyGeneration = keygen;
			program.SDKVersion = sdkHex;
            program.RequiredSystemVersion = requiredSystemVersion;
            program.Type = NCAType.Program;
            program.ExeFS = exefs;
            program.RomFS = romfs;
            program.Logo = logo;
            FolderBrowserDialog cnca = new FolderBrowserDialog()
            {
                Description = "Select directory to save program NCA content (will create a 'Program' directory)",
                ShowNewFolderButton = true,
            };
            DialogResult res = cnca.ShowDialog();
            if(res is DialogResult.OK)
            {
                string outnca = cnca.SelectedPath + "\\Program";
                if(!Directory.Exists(outnca)) Directory.CreateDirectory(outnca);
                program.generate(outnca);
                if(Directory.Exists(outnca))
                {
                    string[] files = Directory.GetFiles(outnca);
                    if(files.Length <= 0) GUI.Resources.log("The build failed. No NCA was generated.");
                    bool ncafound = false;
                    foreach(string file in files)
                    {
                        if(file.ToLower().EndsWith("nca"))
                        {
                            long size = new FileInfo(file).Length;
                            GUI.Resources.log("The build succeeded: \n" + file + "\nFile size: " + Utils.formattedSize(size), LogType.Information);
                            ncafound = true;
                            break;
                        }
                    }
                    if(!ncafound) GUI.Resources.log("The build failed. No NCA was generated.");
                }
                else GUI.Resources.log("The build failed. The NCA's directory does not exist.");
            }
        }

        private void Button_BuildControl_Click(object sender, RoutedEventArgs e)
        {
            string tid = Box_TitleID.Text;
            if(tid.Length != 16)
            {
                GUI.Resources.log("Title ID doesn't have 16 characters.");
                return;
            }
            if(!Regex.IsMatch(tid, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                GUI.Resources.log("Title ID is not a valid hex string.");
                return;
            }
            byte keygen = 20;
			string sdkHex = GetSDKHex();
			string requiredSystemVersion = GetSelectedFirmware();
            string kgen = Combo_KeyGen.Text;
            if(kgen is "1 (1.0.0 - 2.3.0)") keygen = 1;
            else if(kgen is "2 (3.0.0)") keygen = 2;
            else if(kgen is "3 (3.0.1 - 3.0.2)") keygen = 3;
            else if(kgen is "4 (4.0.0 - 4.1.0)") keygen = 4;
            else if(kgen is "5 (5.0.0 - 5.1.0)") keygen = 5;
            else if(kgen is "6 (6.0.0 - 6.1.0)") keygen = 6;
            else if(kgen is "7 (6.2.0)") keygen = 7;
            else if(kgen is "8 (7.0.0 - 8.0.1)") keygen = 8;
            else if(kgen is "9 (8.1.0)") keygen = 9;
            else if(kgen is "10 (9.0.0 - 9.0.1)") keygen = 10;
            else if(kgen is "11 (9.1.0 - 12.0.3)") keygen = 11;
            else if(kgen is "12 (12.1.0 - 13.2.1)") keygen = 12;
            else if(kgen is "13 (14.0.0 - 14.1.2)") keygen = 13;
            else if(kgen is "14 (15.0.0 - 15.0.1)") keygen = 14;
            else if(kgen is "15 (16.0.0 - 16.1.0)") keygen = 15;
            else if(kgen is "16 (17.0.0 - 17.0.1)") keygen = 16;
            else if(kgen is "17 (18.0.0 - 18.1.0)") keygen = 17;
            else if(kgen is "18 (19.0.0 - 19.0.1)") keygen = 18;
            else if(kgen is "19 (20.0.0 - 20.5.0)") keygen = 19;
            else if(kgen is "20 (21.0.0 - Latest)") keygen = 20;
            string ccwd = Utils.Cwd;
            if(string.IsNullOrEmpty(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file was not selected. This file is required to build the NSP package.");
                return;
            }
            if(!File.Exists(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file does not exist. This file is required to build the NSP package.");
                return;
            }
            if(string.IsNullOrEmpty(Box_Name.Text))
            {
                GUI.Resources.log("No application name was set.");
                return;
            }
            if(string.IsNullOrEmpty(Box_Version.Text))
            {
                GUI.Resources.log("No application version string was set.");
                return;
            }
            if(string.IsNullOrEmpty(Box_ProductCode.Text))
            {
                GUI.Resources.log("No application product code string was set.\nRetail games use 'LA-H-XXXXX' (SMO uses LA-H-AAACA)");
                return;
            }
            string kset = Box_KeySet.Text;
            bool screen = Utils.nullableBoolValue(Check_AllowScreenshots.IsChecked);
            bool video = Utils.nullableBoolValue(Check_AllowVideo.IsChecked);
            byte user = Convert.ToByte(Utils.nullableBoolValue(Check_UserAccount.IsChecked));
            NCA control = new NCA(kset);
            control.TitleID = tid;
            control.KeyGeneration = keygen;
			control.SDKVersion = sdkHex;
            // control.RequiredSystemVersion = requiredSystemVersion;
            control.Type = NCAType.Control;
            NACP controlnacp = new NACP();
            NACPEntry ent = new NACPEntry();
            ent.Name = Box_Name.Text;
            ent.Author = Box_Author.Text;
            controlnacp.Screenshot = screen;
            controlnacp.VideoCapture = video;
            controlnacp.StartupUserAccount = user;
            controlnacp.ProductCode = Box_ProductCode.Text;
            controlnacp.ApplicationId = tid;
            controlnacp.Version = Box_Version.Text;
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            controlnacp.Entries.Add(ent);
            Directory.CreateDirectory(HacPack.Utils.TemporaryDirectory + "\\control");
            controlnacp.generate(HacPack.Utils.TemporaryDirectory + "\\control");
            ControlIcons.generate(logopath, HacPack.Utils.TemporaryDirectory + "\\control");
            control.RomFS = HacPack.Utils.TemporaryDirectory + "\\control";
            FolderBrowserDialog cnca = new FolderBrowserDialog()
            {
                Description = "Select directory to save control NCA content (will create a 'Control' directory)",
                ShowNewFolderButton = true,
            };
            DialogResult res = cnca.ShowDialog();
            if(res is DialogResult.OK)
            {
                string outnca = cnca.SelectedPath + "\\Control";
                if(!Directory.Exists(outnca)) Directory.CreateDirectory(outnca);
                control.generate(outnca);
                if(Directory.Exists(outnca))
                {
                    string[] files = Directory.GetFiles(outnca);
                    if(files.Length <= 0) GUI.Resources.log("The build failed. No NCA was generated.");
                    bool ncafound = false;
                    foreach(string file in files)
                    {
                        if(file.ToLower().EndsWith("nca"))
                        {
                            long size = new FileInfo(file).Length;
                            GUI.Resources.log("The build succeeded: \n" + file + "\nFile size: " + Utils.formattedSize(size), LogType.Information);
                            ncafound = true;
                            Directory.Delete(HacPack.Utils.TemporaryDirectory + "\\control", true);
                            break;
                        }
                    }
                    if(!ncafound) GUI.Resources.log("The build failed. No NCA was generated.");
                }
                else GUI.Resources.log("The build failed. The NCA's directory does not exist.");
            }
        }

        private void Button_BuildLegalInfo_Click(object sender, RoutedEventArgs e)
        {
            string tid = Box_TitleID.Text;
            if(tid.Length != 16)
            {
                GUI.Resources.log("Title ID doesn't have 16 characters.");
                return;
            }
            if(!Regex.IsMatch(tid, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                GUI.Resources.log("Title ID is not a valid hex string.");
                return;
            }
            byte keygen = 20;
			string sdkHex = GetSDKHex();
			string requiredSystemVersion = GetSelectedFirmware();
            string kgen = Combo_KeyGen.Text;
            if(kgen is "1 (1.0.0 - 2.3.0)") keygen = 1;
            else if(kgen is "2 (3.0.0)") keygen = 2;
            else if(kgen is "3 (3.0.1 - 3.0.2)") keygen = 3;
            else if(kgen is "4 (4.0.0 - 4.1.0)") keygen = 4;
            else if(kgen is "5 (5.0.0 - 5.1.0)") keygen = 5;
            else if(kgen is "6 (6.0.0 - 6.1.0)") keygen = 6;
            else if(kgen is "7 (6.2.0)") keygen = 7;
            else if(kgen is "8 (7.0.0 - 8.0.1)") keygen = 8;
            else if(kgen is "9 (8.1.0)") keygen = 9;
            else if(kgen is "10 (9.0.0 - 9.0.1)") keygen = 10;
            else if(kgen is "11 (9.1.0 - 12.0.3)") keygen = 11;
            else if(kgen is "12 (12.1.0 - 13.2.1)") keygen = 12;
            else if(kgen is "13 (14.0.0 - 14.1.2)") keygen = 13;
            else if(kgen is "14 (15.0.0 - 15.0.1)") keygen = 14;
            else if(kgen is "15 (16.0.0 - 16.1.0)") keygen = 15;
            else if(kgen is "16 (17.0.0 - 17.0.1)") keygen = 16;
            else if(kgen is "17 (18.0.0 - 18.1.0)") keygen = 17;
            else if(kgen is "18 (19.0.0 - 19.0.1)") keygen = 18;
            else if(kgen is "19 (20.0.0 - 20.5.0)") keygen = 19;
            else if(kgen is "20 (21.0.0 - Latest)") keygen = 20;
            string ccwd = Utils.Cwd;
            string imp = Box_ImportantHTML.Text;
            string ipn = Box_IPNoticesHTML.Text;
            string sup = Box_SupportHTML.Text;
            bool hasimp = !string.IsNullOrEmpty(imp);
            bool hasipn = !string.IsNullOrEmpty(ipn);
            bool hassup = !string.IsNullOrEmpty(sup);
            bool haslinfo = (hasimp || hasipn || hassup);
            if(haslinfo)
            {
                if(hasimp)
                {
                    if(!Directory.Exists(imp))
                    {
                        GUI.Resources.log("Important HTML directory does not exist.");
                        return;
                    }
                    if(!File.Exists(imp + "\\index.html"))
                    {
                        GUI.Resources.log("Important HTML directory does not have a 'index.html' entry file.");
                        return;
                    }
                    if(Directory.GetFileSystemEntries(imp).Length is 0)
                    {
                        GUI.Resources.log("Important HTML directory is empty.");
                        return;
                    }
                }
                if(hasipn)
                {
                    if(!Directory.Exists(ipn))
                    {
                        GUI.Resources.log("IPNotices HTML directory does not exist.");
                        return;
                    }
                    if(!File.Exists(ipn + "\\index.html"))
                    {
                        GUI.Resources.log("IPNotices HTML directory does not have a 'index.html' entry file.");
                        return;
                    }
                    if(Directory.GetFileSystemEntries(ipn).Length is 0)
                    {
                        GUI.Resources.log("IPNotices HTML directory is empty.");
                        return;
                    }
                }
                if(hassup)
                {
                    if(!Directory.Exists(sup))
                    {
                        GUI.Resources.log("Support HTML directory does not exist.");
                        return;
                    }
                    if(!File.Exists(sup + "\\index.html"))
                    {
                        GUI.Resources.log("Support HTML directory does not have a 'index.html' entry file.");
                        return;
                    }
                    if(Directory.GetFileSystemEntries(sup).Length is 0)
                    {
                        GUI.Resources.log("Support HTML directory is empty.");
                        return;
                    }
                }
            }
            else
            {
                GUI.Resources.log("At least one of the legalinfo HTML directories needs to be set.");
                return;
            }
            if(string.IsNullOrEmpty(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file was not selected. This file is required to build the NSP package.");
                return;
            }
            if(!File.Exists(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file does not exist. This file is required to build the NSP package.");
                return;
            }
            string kset = Box_KeySet.Text;
            NCA linfo = new NCA(kset);
            linfo.TitleID = tid;
            linfo.KeyGeneration = keygen;
			linfo.SDKVersion = sdkHex;
            // linfo.RequiredSystemVersion = requiredSystemVersion;
            linfo.Type = NCAType.LegalInformation;
            if(Directory.Exists(HacPack.Utils.TemporaryDirectory + "\\legalinfo")) Directory.Delete(HacPack.Utils.TemporaryDirectory + "\\legalinfo", true);
            Directory.CreateDirectory(HacPack.Utils.TemporaryDirectory + "\\legalinfo");
            if(hasimp) FileSystem.CopyDirectory(imp, HacPack.Utils.TemporaryDirectory + "\\legalinfo\\important.htdocs");
            if(hasipn) FileSystem.CopyDirectory(ipn, HacPack.Utils.TemporaryDirectory + "\\legalinfo\\ipnotices.htdocs");
            if(hassup) FileSystem.CopyDirectory(sup, HacPack.Utils.TemporaryDirectory + "\\legalinfo\\support.htdocs");
            linfo.RomFS = HacPack.Utils.TemporaryDirectory + "\\legalinfo";
            FolderBrowserDialog cnca = new FolderBrowserDialog()
            {
                Description = "Select directory to save legalinfo NCA content (will create a 'LegalInfo' directory)",
                ShowNewFolderButton = true,
            };
            DialogResult res = cnca.ShowDialog();
            if(res is DialogResult.OK)
            {
                string outnca = cnca.SelectedPath + "\\LegalInfo";
                if(!Directory.Exists(outnca)) Directory.CreateDirectory(outnca);
                linfo.generate(outnca);
                if (Directory.Exists(outnca))
                {
                    string[] files = Directory.GetFiles(outnca);
                    if (files.Length <= 0) GUI.Resources.log("The build failed. No NCA was generated.");
                    bool ncafound = false;
                    foreach (string file in files)
                    {
                        if (file.ToLower().EndsWith("nca"))
                        {
                            long size = new FileInfo(file).Length;
                            GUI.Resources.log("The build succeeded: \n" + file + "\nFile size: " + Utils.formattedSize(size), LogType.Information);
                            ncafound = true;
                            break;
                        }
                    }
                    if (!ncafound) GUI.Resources.log("The build failed. No NCA was generated.");
                }
                else GUI.Resources.log("The build failed. The NCA's directory does not exist.");
            }
        }

        private void Button_BuildOffline_Click(object sender, RoutedEventArgs e)
        {
            string tid = Box_TitleID.Text;
            if(tid.Length != 16)
            {
                GUI.Resources.log("Title ID doesn't have 16 characters.");
                return;
            }
            if(!Regex.IsMatch(tid, @"\A\b[0-9a-fA-F]+\b\Z"))
            {
                GUI.Resources.log("Title ID is not a valid hex string.");
                return;
            }
            byte keygen = 20;
			string sdkHex = GetSDKHex();
			string requiredSystemVersion = GetSelectedFirmware();
            string kgen = Combo_KeyGen.Text;
            if(kgen is "1 (1.0.0 - 2.3.0)") keygen = 1;
            else if(kgen is "2 (3.0.0)") keygen = 2;
            else if(kgen is "3 (3.0.1 - 3.0.2)") keygen = 3;
            else if(kgen is "4 (4.0.0 - 4.1.0)") keygen = 4;
            else if(kgen is "5 (5.0.0 - 5.1.0)") keygen = 5;
            else if(kgen is "6 (6.0.0 - 6.1.0)") keygen = 6;
            else if(kgen is "7 (6.2.0)") keygen = 7;
            else if(kgen is "8 (7.0.0 - 8.0.1)") keygen = 8;
            else if(kgen is "9 (8.1.0)") keygen = 9;
            else if(kgen is "10 (9.0.0 - 9.0.1)") keygen = 10;
            else if(kgen is "11 (9.1.0 - 12.0.3)") keygen = 11;
            else if(kgen is "12 (12.1.0 - 13.2.1)") keygen = 12;
            else if(kgen is "13 (14.0.0 - 14.1.2)") keygen = 13;
            else if(kgen is "14 (15.0.0 - 15.0.1)") keygen = 14;
            else if(kgen is "15 (16.0.0 - 16.1.0)") keygen = 15;
            else if(kgen is "16 (17.0.0 - 17.0.1)") keygen = 16;
            else if(kgen is "17 (18.0.0 - 18.1.0)") keygen = 17;
            else if(kgen is "18 (19.0.0 - 19.0.1)") keygen = 18;
            else if(kgen is "19 (20.0.0 - 20.5.0)") keygen = 19;
            else if(kgen is "20 (21.0.0 - Latest)") keygen = 20;
            string ccwd = Utils.Cwd;
            string off = Box_OfflineHTML.Text;
            if(string.IsNullOrEmpty(off))
            {
                GUI.Resources.log("No offline HTML directory was set.");
                return;
            }
            if (!Directory.Exists(off))
            {
                GUI.Resources.log("Offline HTML directory does not exist.");
                return;
            }
            if(Directory.GetFileSystemEntries(off).Length is 0)
            {
                GUI.Resources.log("Offline HTML directory is empty.");
                return;
            }
            if(!File.Exists(off + "\\index.html"))
            {
                GUI.Resources.log("Offline HTML directory does not have a 'index.html' entry file.");
                return;
            }
            if(string.IsNullOrEmpty(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file was not selected. This file is required to build the NSP package.");
                return;
            }
            if(!File.Exists(Box_KeySet.Text))
            {
                GUI.Resources.log("Keyset file does not exist. This file is required to build the NSP package.");
                return;
            }
            string kset = Box_KeySet.Text;
            NCA offh = new NCA(kset);
            offh.TitleID = tid;
            offh.KeyGeneration = keygen;
			offh.SDKVersion = sdkHex;
            // offh.RequiredSystemVersion = requiredSystemVersion;
            offh.Type = NCAType.OfflineHTML;
            if(Directory.Exists(HacPack.Utils.TemporaryDirectory + "\\offline")) Directory.Delete(HacPack.Utils.TemporaryDirectory + "\\offline", true);
            Directory.CreateDirectory(HacPack.Utils.TemporaryDirectory + "\\offline");
            Directory.CreateDirectory(HacPack.Utils.TemporaryDirectory + "\\offline\\html-document");
            Directory.CreateDirectory(HacPack.Utils.TemporaryDirectory + "\\offline\\html-document\\main.htdocs");
            FileSystem.CopyDirectory(off, HacPack.Utils.TemporaryDirectory + "\\offline\\html-document\\main.htdocs");
            offh.RomFS = HacPack.Utils.TemporaryDirectory + "\\offline";
            FolderBrowserDialog cnca = new FolderBrowserDialog()
            {
                Description = "Select directory to save offline HTML NCA content (will create a 'OfflineHTML' directory)",
                ShowNewFolderButton = true,
            };
            DialogResult res = cnca.ShowDialog();
            if(res is DialogResult.OK)
            {
                string outnca = cnca.SelectedPath + "\\OfflineHTML";
                if(!Directory.Exists(outnca)) Directory.CreateDirectory(outnca);
                offh.generate(outnca);
                if(Directory.Exists(outnca))
                {
                    string[] files = Directory.GetFiles(outnca);
                    if(files.Length <= 0) GUI.Resources.log("The build failed. No NCA was generated.");
                    bool ncafound = false;
                    foreach(string file in files)
                    {
                        if(file.ToLower().EndsWith("nca"))
                        {
                            long size = new FileInfo(file).Length;
                            GUI.Resources.log("The build succeeded: \n" + file + "\nFile size: " + Utils.formattedSize(size), LogType.Information);
                            ncafound = true;
                            break;
                        }
                    }
                    if(!ncafound) GUI.Resources.log("The build failed. No NCA was generated.");
                }
                else GUI.Resources.log("The build failed. The NCA's directory does not exist.");
            }
        }
    }
}
