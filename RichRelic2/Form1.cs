using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text;
using System.Windows.Forms;
using FMUtils.KeyboardHook;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Diagnostics;
using Tesseract;
using Leptonica;
using System.Runtime.InteropServices;
using Fastenshtein;

namespace RelicRewards {
    public partial class Form1 : Form {
        public class PlatDucats {
            public TextBox plat { get; set; }
            public TextBox ducats { get; set; }

            public PlatDucats(TextBox plat, TextBox ducats) {
                this.plat = plat;
                this.ducats = ducats;
            }
        }

        public Form1() {
            InitializeComponent();
            this.Text = LB_ProgName.Text = "Relic Rewards v0.3";

            // Listen for global KeyDown events
            var KeyboardHook = new Hook("Global Action Hook");
            KeyboardHook.KeyDownEvent += KeyDown;

            // Start form on bottom right
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(0, workingArea.Bottom - Size.Height);

            // Create cache directory for Ducat cache
            Directory.CreateDirectory("cache");

            // Get list of tradable items and update it daily
            Directory.CreateDirectory("items");
            if (!File.Exists("items\\items.json") || File.GetLastWriteTime("items\\items.json") <= DateTime.Now.AddDays(-1)) {
                Debug.WriteLine("New items list obtained");
                using (var client = new WebClient()) {
                    client.DownloadFile(@"https://api.warframe.market/v1/items", "items\\items.json");
                }
            }

            // Create error directory
            Directory.CreateDirectory("error");
            // Create error log
            if (!File.Exists("error\\errorlog.txt")) {
                using (StreamWriter w = File.CreateText("error\\errorlog.txt")) {
                    w.WriteLine("** RICH RELIC ERROR LOG **");
                }
            }
        }

        // Ignore mouse interaction with form to fake an overlay
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            var style = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        public new void KeyDown(KeyboardHookEventArgs e) {
            // Print Screen key was pressed
            if (e.Key == Keys.PrintScreen) {
                LB_KeyPressed.Text = "Print Screen Pressed";

                // Grab image from screen (hopefully Relic rewards) and convert to black and white using a threshold
                PrintScreenThreshold();

                // Process text from image
                try {
                    string dataPath = @"./tessdata";
                    string language = "eng";
                    string inputFile = "rewards.jpg";
                    OcrEngineMode oem = OcrEngineMode.DEFAULT;
                    PageSegmentationMode psm = PageSegmentationMode.SINGLE_LINE;

                    TessBaseAPI tessBaseAPI = new TessBaseAPI();

                    // Initialize tesseract-ocr 
                    if (!tessBaseAPI.Init(dataPath, language, oem)) {
                        throw new Exception("Could not initialize tesseract.");
                    }

                    // Set the Page Segmentation mode
                    tessBaseAPI.SetPageSegMode(psm);

                    // Warframe Relics are only displayed in caps and &
                    tessBaseAPI.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ&");
                    // Value to tweak space recognition. DE is inconsistant :/
                    tessBaseAPI.SetVariable("tosp_min_sane_kn_sp", "3.35");

                    // Set input file
                    Pix pix = tessBaseAPI.SetImage(inputFile);

                    TB_Part1.Text = GetText(tessBaseAPI, GlobalVar.PART1);
                    TB_Part2.Text = GetText(tessBaseAPI, GlobalVar.PART2);
                    TB_Part3.Text = GetText(tessBaseAPI, GlobalVar.PART3);
                    if (GlobalVar.NUMPEOPLE == 4) {
                        TB_Part4.Text = GetText(tessBaseAPI, GlobalVar.PART4);
                    }


                    // Two-lined parts
                    if (TB_Part1.Text.Trim() == "BLUEPRINT" || !TB_Part1.Text.Trim().Contains(" ")) {
                        TB_Part1.Text = GetText(tessBaseAPI, GlobalVar.PART1, 582);
                    }
                    if (TB_Part2.Text.Trim() == "BLUEPRINT" || !TB_Part2.Text.Trim().Contains(" ")) {
                        TB_Part2.Text = GetText(tessBaseAPI, GlobalVar.PART2, 582);
                    }
                    if (TB_Part3.Text.Trim() == "BLUEPRINT" || !TB_Part3.Text.Trim().Contains(" ")) {
                        TB_Part3.Text = GetText(tessBaseAPI, GlobalVar.PART3, 582);
                    }
                    if (TB_Part4.Text.Trim() == "BLUEPRINT" || !TB_Part4.Text.Trim().Contains(" ") && GlobalVar.NUMPEOPLE == 4) {
                        TB_Part4.Text = GetText(tessBaseAPI, GlobalVar.PART4, 582);
                    }

                    tessBaseAPI.Dispose();
                    pix.Dispose();

                    // Grab latest order list for parts
                    // TODO: Proper requests (this hits the API 4-8 times within a second)
                    using (var client = new WebClient()) {
                        GetPriceJson(client, TB_Part1, TB_Plat1);
                        GetPriceJson(client, TB_Part2, TB_Plat2);
                        GetPriceJson(client, TB_Part3, TB_Plat3);
                        if (GlobalVar.NUMPEOPLE == 4) {
                            GetPriceJson(client, TB_Part4, TB_Plat4);
                        }


                        // These get cached
                        GetDucatsJson(client, TB_Part1, TB_Ducats1);
                        GetDucatsJson(client, TB_Part2, TB_Ducats2);
                        GetDucatsJson(client, TB_Part3, TB_Ducats3);
                        if (GlobalVar.NUMPEOPLE == 4) {
                            GetDucatsJson(client, TB_Part4, TB_Ducats4);
                        }

                    }

                    /* Testing a cleaner method
                    // Lazy way of finding the max
                    List<TextBox> primePlat = new List<TextBox>();
                    primePlat.Add(TB_Part1);
                    primePlat.Add(TB_Part2);
                    primePlat.Add(TB_Part3);
                    primePlat.Add(TB_Part4);

                    List<TextBox> primeDuc = new List<TextBox>();
                    primeDuc.Add(TB_Ducats1);
                    primeDuc.Add(TB_Ducats2);
                    primeDuc.Add(TB_Ducats3);
                    primeDuc.Add(TB_Ducats4);



                    // Get most expensive part and display it
                    int maxIndex = 0;
                    int max = Int32.Parse(primePlat[0].Tag.ToString());
                    string finalPick = primePlat[0].Text;
                    for (int i = 1; i < 4; i++) {
                        if (max < Int32.Parse(primePlat[i].Tag.ToString())) {
                            max = Int32.Parse(primePlat[i].Tag.ToString());
                            finalPick = primePlat[i].Text;
                            maxIndex = i;
                        }
                        else if (max == Int32.Parse(primePlat[i].Tag.ToString()) && max != -1) {
                            if (Int32.Parse(primeDuc[maxIndex].Tag.ToString()) > Int32.Parse(primeDuc[i].Tag.ToString())) {
                                finalPick = primePlat[maxIndex].Text;
                            }
                            else {
                                finalPick = primePlat[i].Text;
                            }
                        }
                    }

                    // If max item is less than 10 Plat, sort by Ducats
                    if (max <= 10) {
                        max = Int32.Parse(primeDuc[0].Tag.ToString());
                        for (int i = 1; i < 4; i++) {
                            if (max < Int32.Parse(primeDuc[i].Tag.ToString())) {
                                max = Int32.Parse(primeDuc[i].Tag.ToString());
                                finalPick = primePlat[i].Text;
                            }
                        }
                    }*/

                    List<PlatDucats> platDuc = new List<PlatDucats>();
                    platDuc.Add(new PlatDucats(TB_Part1, TB_Ducats1));
                    platDuc.Add(new PlatDucats(TB_Part2, TB_Ducats2));
                    platDuc.Add(new PlatDucats(TB_Part3, TB_Ducats3));
                    if (GlobalVar.NUMPEOPLE == 4) {
                        platDuc.Add(new PlatDucats(TB_Part4, TB_Ducats4));
                    }

                    /*Debug.WriteLine(TB_Part1.Tag + " " + TB_Ducats1.Tag);
                    Debug.WriteLine(TB_Part2.Tag + " " + TB_Ducats2.Tag);
                    Debug.WriteLine(TB_Part3.Tag + " " + TB_Ducats3.Tag);
                    Debug.WriteLine(TB_Part4.Tag + " " + TB_Ducats4.Tag);
                    */

                    // Sort by plat, then duc
                    platDuc = platDuc.OrderByDescending(o => Int32.Parse(o.plat.Tag.ToString())).ThenByDescending(o => Int32.Parse(o.ducats.Tag.ToString())).ToList();

                    // If max plat is low, sort by the reverse
                    if (Int32.Parse(platDuc[0].plat.Tag.ToString()) < 15) {
                        platDuc = platDuc.OrderByDescending(o => Int32.Parse(o.ducats.Tag.ToString())).ThenByDescending(o => Int32.Parse(o.plat.Tag.ToString())).ToList();
                    }

                    TB_Pick.Text = platDuc[0].plat.Text;
                }
                catch (Exception ex) {
                    LB_KeyPressed.Text = "Error: MAIN";
                    LogError("MAIN: " + ex.Message);
                }
            }
            else if (e.Key == Keys.D3 && e.isAltPressed) {
                Debug.WriteLine("Switched to 3 people");

                LB_Part4.Visible = false;
                TB_Part4.Visible = false;
                TB_Plat4.Visible = false;
                TB_Ducats4.Visible = false;

                GlobalVar.NUMPEOPLE = 3;

                GlobalVar.PART1 = 435;
                GlobalVar.PART2 = 1011;
                GlobalVar.PART3 = 1590;
            }
            else if (e.Key == Keys.D4 && e.isAltPressed) {
                Debug.WriteLine("Switched to 4 people");

                LB_Part4.Visible = true;
                TB_Part4.Visible = true;
                TB_Plat4.Visible = true;
                TB_Ducats4.Visible = true;

                GlobalVar.NUMPEOPLE = 4;

                GlobalVar.PART1 = 150;
                GlobalVar.PART2 = 725;
                GlobalVar.PART3 = 1300;
                GlobalVar.PART4 = 1875;
            }
            else if (e.Key == Keys.Pause) {
                Environment.Exit(0);
            }
        }

        public void PrintScreenThreshold() {
            Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(printscreen as Image);
            graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            //Bitmap printscreen = new Bitmap("test\\threepeople.jpg");

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(printscreen)) {
                var gray_matrix = new float[][] {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0,      0,      0,      1, 0 },
                new float[] { 0,      0,      0,      0, 1 }};

                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(gray_matrix));
                ia.SetThreshold((float)0.33); // Change this threshold as needed
                var rc = new Rectangle(0, 0, printscreen.Width, printscreen.Height);
                gr.DrawImage(printscreen, rc, 0, 0, printscreen.Width, printscreen.Height, GraphicsUnit.Pixel, ia);

                printscreen.Save(@"rewards.jpg", ImageFormat.Jpeg);
            }
        }

        // Grab text from image
        private string GetText(TessBaseAPI tessBaseAPI, int partX, int partY = 615) {
            // Set image location start
            tessBaseAPI.SetRectangle(partX, partY, 545, 25);

            // Recognize image
            tessBaseAPI.Recognize();

            ResultIterator resultIterator = tessBaseAPI.GetIterator();

            // Extract text from result iterator
            StringBuilder stringBuilder = new StringBuilder();
            PageIteratorLevel pageIteratorLevel = PageIteratorLevel.RIL_PARA;
            do {
                stringBuilder.Append(resultIterator.GetUTF8Text(pageIteratorLevel));
            } while (resultIterator.Next(pageIteratorLevel));

            // Fix up string for Warframe.market
            stringBuilder = stringBuilder.Replace("\n", String.Empty);
            string guess = stringBuilder.ToString();

            // Probably not necessary anymore
            /*
            // Predict common words to ignore noise
            // Not sure if necessary with new FindClosestWord function
            int index;
            List<string> predict = new List<string>();

            // Add common word beginnings here
            predict.Add("NEU");

            foreach (string word in predict) {
                index = guess.IndexOf(word);

                if (index > 0) {
                    guess = guess.Substring(0, index);

                    if (word == "NEU") {
                        guess = guess.Substring(0, index);
                        guess += "NEUROPTICS";
                    }
                    break;
                }
            }

            // Remove any stray characters after last word
            // Not sure if necessary with new FindClosestWord function
            List<string> terminators = new List<string>();

            // Add terminating words here
            terminators.Add("BLUEPRINT");
            terminators.Add("RECEIVER");
            terminators.Add("STRING");
            terminators.Add("HEAD");
            terminators.Add("BLADE");
            terminators.Add("BARREL");
            terminators.Add("NEUROPTICS");
            terminators.Add("LIMB");
            terminators.Add("STOCK");
            terminators.Add("GRIP");
            terminators.Add("HILT");

            foreach (string word in terminators) {
                index = guess.IndexOf(word);

                if (index > 0) {
                    guess = guess.Substring(0, index + word.Length);
                    break;
                }
            }

            // OCR is great, isn't it?
            guess = guess.Replace("VV", "W");
            */

            if ((!guess.Contains("CARRIER") && !guess.Contains("WYRM") && !guess.Contains("HELIOS")) &&
                (guess.Contains("SYSTEMS") || guess.Contains("CHASSIS") || guess.Contains("NEUROPTICS"))) {
                guess = guess.Replace(" BLUEPRINT", "");
            }

            // Match whatever result we get to the closest selling item name from Warframe.market
            // We want to ignore "BLUEPRINT" because this indicates that it's a 2-lined item
            if (guess.Trim().Contains(" ") && !guess.Contains("FORMA")) {
                Debug.Write("Old: " + guess);

                guess = FindClosestWord(guess);

                Debug.WriteLine(" | New: " + guess);
            }

            guess = guess.Replace("BAND", "COLLAR BAND").Replace("BUCKLE", "COLLAR BUCKLE").Replace("&", "AND");

            return guess;
        }

        // The Levenshtein distance algorithm is awesome. This basically allows us to quickly compute the distance between words
        // This function is called when a proper json can't be found and tries to find the closest part from all currently known tradable parts
        private string FindClosestWord(string word) {
            Levenshtein lev = new Fastenshtein.Levenshtein(word);
            int minDistance = 9999;
            string potential = "";

            using (StreamReader r = new StreamReader("items\\items.json")) {
                string json = r.ReadToEnd();
                JObject items = (JObject)JsonConvert.DeserializeObject(json);

                foreach (var item in items["payload"]["items"]["en"]) {
                    string currentItem = item["item_name"].ToString().ToUpper();
                    int levenshteinDistance = lev.Distance(currentItem);

                    //Debug.WriteLine((string)item["item_name"] + " | " + levenshteinDistance);

                    if (minDistance > levenshteinDistance) {
                        minDistance = levenshteinDistance;
                        potential = currentItem;
                    }
                }
            }

            // Arbitrary value; needs more testing for the sweetspot
            if (minDistance <= 15) {
                return potential;
            }
            return word;
        }

        // Warframe.market's API sucks. All of this work just to get lowest price of an item from ingame user
        private void FindPlat(Stream partOrder, TextBox part, TextBox partPlat) {
            using (StreamReader r = new StreamReader(partOrder)) {
                string json = r.ReadToEnd();
                JObject order = (JObject)JsonConvert.DeserializeObject(json);

                List<int> plat = new List<int>();
                foreach (var person in order["payload"]["orders"]) {
                    if ((string)person["platform"] == "pc" &&
                        (string)person["order_type"] == "sell" &&
                        (string)person["user"]["status"] == "ingame") {
                        plat.Add((int)person["platinum"]);
                    }
                }

                // Sort ascending order
                plat.Sort();

                // Get an average, because why not?
                if (plat.Count >= 5) {
                    int sum = 0;
                    double average;
                    for (int i = 0; i < 5; i++) {
                        sum += plat[i];
                    }
                    average = Math.Ceiling(sum / 5.0);
                    partPlat.Text = ((int)average).ToString() + " p";
                    part.Tag = average.ToString();
                }
                // Rare item? Alright then...
                else if (plat.Count > 0) {
                    partPlat.Text = plat[0].ToString() + " p";
                    part.Tag = plat[0].ToString();
                }
                // No one is online or Warframe.market doesn't have item
                else {
                    partPlat.Text = "UNKN[PLAT]";
                    part.Tag = -1;
                }
            }
        }

        public void GetPriceJson(WebClient client, TextBox part, TextBox partPlat) {
            try {
                if (!part.Text.Contains("FORMA")) {
                    Stream partOrder = client.OpenRead(@"https://api.warframe.market/v1/items/" + part.Text.ToLower().Replace(" ", "_") + "/orders");
                    FindPlat(partOrder, part, partPlat);
                }
                else {
                    part.Tag = -1;
                    partPlat.Text = "---";
                }
            }
            catch (Exception ex) {
                LB_KeyPressed.Text = "Error " + part.Text;
                part.Tag = -1;
                partPlat.Text = "UNKN[JP]";

                if (part.Text == "") {
                    part.Text = "---";
                }

                LogError(partPlat.Text + " | " + part.Text + ": " + ex.Message);
            }
        }

        private void FindDucats(string partDucats, TextBox ducats, string url) {
            using (StreamReader r = new StreamReader(partDucats)) {
                string json = r.ReadToEnd();
                JObject items = (JObject)JsonConvert.DeserializeObject(json);

                string numDucats = "";
                foreach (var item in items["payload"]["item"]["items_in_set"]) {
                    if ((string)item["url_name"] == url) {
                        numDucats = (string)item["ducats"];
                        break;
                    }
                }

                if (numDucats.Trim() != "") {
                    ducats.Text = numDucats + " duc";
                    ducats.Tag = numDucats;
                }
                else {
                    ducats.Text = "UNKN[DUC]";
                    ducats.Tag = -1;

                    if (File.Exists(string.Format("cache\\{0}.json", url))) {
                        File.Delete(string.Format("cache\\{0}.json", url));
                    }
                }
            }
        }

        public void GetDucatsJson(WebClient client, TextBox part, TextBox ducats) {
            string partUrl = part.Text.ToLower().Replace(" ", "_");
            string partJson = string.Format("cache\\{0}.json", partUrl);
            try {
                if (!part.Text.Contains("FORMA")) {
                    if (!File.Exists(string.Format("cache\\{0}.json", partUrl))) {
                        client.DownloadFile(@"https://api.warframe.market/v1/items/" + partUrl, partJson);
                    }

                    FindDucats(partJson, ducats, partUrl);
                }
                else {
                    ducats.Text = "---";
                    ducats.Tag = -1;
                }
            }
            catch (Exception ex) {
                LB_KeyPressed.Text = "Error " + ducats.Text;
                ducats.Text = "UNKN[JD]";

                if (File.Exists(partJson)) {
                    File.Delete(partJson);
                }

                if (part.Text == "") {
                    part.Text = "---";
                }

                LogError(ducats.Text + " | " + part.Text + ": " + ex.Message);
            }
        }

        private void LogError(string logMessage) {
            // Save copy of rewards capture
            string rewards = Guid.NewGuid().ToString() + ".jpg";

            using (StreamWriter w = File.AppendText("error\\errorlog.txt")) {
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  :{0}", logMessage);
                w.WriteLine("  :Rewards: {0}", rewards);
                w.WriteLine("-------------------------------");
            }
            File.Copy("rewards.jpg", "error\\" + rewards);
        }
    }
}
