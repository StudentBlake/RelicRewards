/* 
 * Project: Relic Rewards
 * Description: Automatically get the best value for your relics
 * Created by StudentBlake
*/

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

namespace RelicRewards
{
    public partial class Main : Form
    {
        public class PlatDucats
        {
            public TextBox plat { get; set; }
            public TextBox ducats { get; set; }

            public PlatDucats(TextBox plat, TextBox ducats)
            {
                this.plat = plat;
                this.ducats = ducats;
            }
        }

        public Main()
        {
            InitializeComponent();
            this.Text = LB_ProgName.Text = "Relic Rewards v0.4 ALPHA";

            // Listen for global KeyDown events
            var KeyboardHook = new Hook("Global Action Hook");
            KeyboardHook.KeyDownEvent += KeyDown;

            // Start form on bottom left
            Rectangle workingArea = Screen.GetWorkingArea(this);
            this.Location = new Point(0, workingArea.Bottom - Size.Height);

            // Create cache directory for Ducat cache
            Directory.CreateDirectory("cache");

            // Get list of tradable items and update it daily
            Directory.CreateDirectory("items");
            if (!File.Exists("items\\items.json") || File.GetLastWriteTime("items\\items.json") <= DateTime.Now.AddDays(-1))
            {
                Debug.WriteLine("New items list obtained");
                using (var client = new WebClient())
                {
                    client.DownloadFile(@"https://api.warframe.market/v1/items", "items\\items.json");
                }
            }

            // Create error directory
            Directory.CreateDirectory("error");
            // Create error log
            if (!File.Exists("error\\errorlog.txt"))
            {
                using (StreamWriter w = File.CreateText("error\\errorlog.txt"))
                {
                    w.WriteLine("** RELIC REWARDS ERROR LOG **");
                }
            }
        }

        // Ignore mouse interaction with form to create an overlay
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        const int GWL_EXSTYLE = -20;
        const int WS_EX_LAYERED = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            var style = GetWindowLong(this.Handle, GWL_EXSTYLE);
            SetWindowLong(this.Handle, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
        }

        public new void KeyDown(KeyboardHookEventArgs e)
        {
            // Use Singleton design to store location information
            LocationInfo currentLocation = LocationInfo._instance;

            // Print Screen key was pressed
            if (e.Key == Keys.PrintScreen)
            {
                // Grab image from screen and convert to black and white using a threshold
                PrintScreenThreshold();

                // Process text from image
                try
                {
                    string dataPath = @"./tessdata";
                    string language = "eng";
                    string inputFile = "rewards.jpg";
                    OcrEngineMode oem = OcrEngineMode.DEFAULT;
                    PageSegmentationMode psm = PageSegmentationMode.SINGLE_LINE;

                    TessBaseAPI tessBaseAPI = new TessBaseAPI();

                    // Initialize tesseract-ocr 
                    if (!tessBaseAPI.Init(dataPath, language, oem))
                    {
                        throw new Exception("Could not initialize tesseract.");
                    }

                    // Set the Page Segmentation mode
                    tessBaseAPI.SetPageSegMode(psm);

                    // Warframe Relics are displayed using A-Z, a-z, and &
                    tessBaseAPI.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz&");

                    // Set input file
                    Pix pix = tessBaseAPI.SetImage(inputFile);

                    TB_Part1.Text = GetText(tessBaseAPI, currentLocation.Part1Loc, 580);
                    TB_Part2.Text = GetText(tessBaseAPI, currentLocation.Part2Loc, 580);
                    if (currentLocation.NumPeople >= 3)
                    {
                        TB_Part3.Text = GetText(tessBaseAPI, currentLocation.Part3Loc, 580);
                    }
                    if (currentLocation.NumPeople == 4)
                    {
                        TB_Part4.Text = GetText(tessBaseAPI, currentLocation.Part4Loc, 580);
                    }

                    tessBaseAPI.Dispose();
                    pix.Dispose();

                    // Grab latest order list for parts
                    // TODO: Try multiple threads! If that's too fast, cache prices for a duration
                    using (var client = new WebClient())
                    {
                        GetPriceJson(client, TB_Part1, TB_Plat1);
                        GetPriceJson(client, TB_Part2, TB_Plat2);
                        if (currentLocation.NumPeople >= 3)
                        {
                            GetPriceJson(client, TB_Part3, TB_Plat3);
                        }
                        if (currentLocation.NumPeople == 4)
                        {
                            GetPriceJson(client, TB_Part4, TB_Plat4);
                        }

                        // These get cached
                        GetDucatsJson(client, TB_Part1, TB_Ducats1);
                        GetDucatsJson(client, TB_Part2, TB_Ducats2);
                        if (currentLocation.NumPeople >= 3)
                        {
                            GetDucatsJson(client, TB_Part3, TB_Ducats3);
                        }
                        if (currentLocation.NumPeople == 4)
                        {
                            GetDucatsJson(client, TB_Part4, TB_Ducats4);
                        }

                    }

                    List<PlatDucats> platDuc = new List<PlatDucats>();
                    platDuc.Add(new PlatDucats(TB_Part1, TB_Ducats1));
                    platDuc.Add(new PlatDucats(TB_Part2, TB_Ducats2));
                    if (currentLocation.NumPeople >= 3)
                    {
                        platDuc.Add(new PlatDucats(TB_Part3, TB_Ducats3));
                    }
                    if (currentLocation.NumPeople == 4)
                    {
                        platDuc.Add(new PlatDucats(TB_Part4, TB_Ducats4));
                    }

                    // Sort by Plat, then Ducats
                    platDuc = platDuc.OrderByDescending(o => Int32.Parse(o.plat.Tag.ToString())).ThenByDescending(o => Int32.Parse(o.ducats.Tag.ToString())).ToList();

                    // If max Plat is low, sort by the reverse
                    if (Int32.Parse(platDuc[0].plat.Tag.ToString()) < 15)
                    {
                        platDuc = platDuc.OrderByDescending(o => Int32.Parse(o.ducats.Tag.ToString())).ThenByDescending(o => Int32.Parse(o.plat.Tag.ToString())).ToList();
                    }

                    // Show best option
                    TB_Pick.Text = platDuc[0].plat.Text;

                    // Show current amount of Plat (and Ducats) made in the current session
                    if (Int32.Parse(platDuc[0].plat.Tag.ToString()) != -1)
                    {
                        //GlobalVar.PLAT += Int32.Parse(platDuc[0].plat.Tag.ToString());
                        LB_Plat.Tag = Int32.Parse(LB_Plat.Tag.ToString()) + Int32.Parse(platDuc[0].plat.Tag.ToString());
                    }
                    if (Int32.Parse(platDuc[0].ducats.Tag.ToString()) != -1)
                    {
                        //GlobalVar.DUCATS += Int32.Parse(platDuc[0].ducats.Tag.ToString());
                        LB_Ducs.Tag = Int32.Parse(LB_Ducs.Tag.ToString()) + Int32.Parse(platDuc[0].ducats.Tag.ToString());
                    }

                    LB_Plat.Text = LB_Plat.Tag.ToString() + " p";
                    LB_Ducs.Text = LB_Ducs.Tag.ToString() + " d";
                }
                catch (Exception ex)
                {
                    LogError("KeyDown: " + ex.Message);
                }
            }
            else if (e.Key == Keys.NumPad2)
            {
                Debug.WriteLine("Switched to 2 people");

                ClearAllExceptTotal();
                EnableRow(false, LB_Part3, TB_Part3, TB_Plat3, TB_Ducats3);
                EnableRow(false, LB_Part4, TB_Part4, TB_Plat4, TB_Ducats4);

                currentLocation.NumPeople = 2;

                currentLocation.Part1Loc = 725;
                currentLocation.Part2Loc = 1300;
            }
            else if (e.Key == Keys.NumPad3)
            {
                Debug.WriteLine("Switched to 3 people");

                ClearAllExceptTotal();
                EnableRow(true, LB_Part3, TB_Part3, TB_Plat3, TB_Ducats3);
                EnableRow(false, LB_Part4, TB_Part4, TB_Plat4, TB_Ducats4);

                currentLocation.NumPeople = 3;

                currentLocation.Part1Loc = 435;
                currentLocation.Part2Loc = 1011;
                currentLocation.Part3Loc = 1590;
            }
            else if (e.Key == Keys.NumPad4)
            {
                Debug.WriteLine("Switched to 4 people");

                ClearAllExceptTotal();
                EnableRow(true, LB_Part3, TB_Part3, TB_Plat3, TB_Ducats3);
                EnableRow(true, LB_Part4, TB_Part4, TB_Plat4, TB_Ducats4);

                currentLocation.NumPeople = 4;

                currentLocation.Part1Loc = 638;
                currentLocation.Part2Loc = 961;
                currentLocation.Part3Loc = 1287;
                currentLocation.Part4Loc = 1610;
            }
            else if (e.Key == Keys.Pause)
            {
                Environment.Exit(0);
            }
        }

        public void PrintScreenThreshold()
        {
            //Bitmap printscreen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(printscreen as Image);
            //graphics.CopyFromScreen(0, 0, 0, 0, printscreen.Size);
            Bitmap printscreen = new Bitmap("test\\new2.jpg");

            using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(printscreen))
            {
                var gray_matrix = new float[][] {
                new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                new float[] { 0,      0,      0,      1, 0 },
                new float[] { 0,      0,      0,      0, 1 }};

                var ia = new System.Drawing.Imaging.ImageAttributes();
                ia.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix(gray_matrix));
                ia.SetThreshold((float)0.50); // Change this threshold as needed (old .33)
                var rc = new Rectangle(0, 0, printscreen.Width, printscreen.Height);
                gr.DrawImage(printscreen, rc, 0, 0, printscreen.Width, printscreen.Height, GraphicsUnit.Pixel, ia);

                printscreen.Save(@"rewards.jpg", ImageFormat.Jpeg);
            }
        }

        // Grab text from image
        private string GetText(TessBaseAPI tessBaseAPI, int partX, int partY)
        {
            // Set image location start
            tessBaseAPI.SetRectangle(partX, partY, 311, 33);

            // Recognize image
            tessBaseAPI.Recognize();

            ResultIterator resultIterator = tessBaseAPI.GetIterator();

            // Extract text from result iterator
            StringBuilder stringBuilder = new StringBuilder();
            PageIteratorLevel pageIteratorLevel = PageIteratorLevel.RIL_PARA;
            do
            {
                stringBuilder.Append(resultIterator.GetUTF8Text(pageIteratorLevel));
            } while (resultIterator.Next(pageIteratorLevel));

            // Fix up string for Warframe.market
            stringBuilder = stringBuilder.Replace("\n", String.Empty);
            string guess = stringBuilder.ToString();

            // Changed to use Levenshtein here due to important of having a matching "Blueprint" word
            Levenshtein levBP = new Fastenshtein.Levenshtein("Blueprint");
            int levBPDistance = levBP.DistanceFrom(guess);

            Debug.WriteLine("Distance from Blueprint: " + levBPDistance);

            // If there is a 4-character difference, accept the word is == "Blueprint"
            // Adjust the offset for 2-lined parts
            if (levBPDistance < 4)
            {
                guess = GetText(tessBaseAPI, partX, 550);
            }

            // Match whatever result we get to the closest selling item name from Warframe.market
            // We want to ignore "Blueprint" because this indicates that it's a 2-lined item
            if (guess != "Blueprint" && !guess.Contains("Forma"))
            {
                Debug.Write("\nOld: " + guess);

                guess = FindClosestWord(guess);

                Debug.WriteLine(" | New: " + guess);
            }

            return guess;
        }

        // The Levenshtein distance algorithm is awesome. This basically allows us to quickly compute the distance between words
        // This function is called when a proper json can't be found and tries to find the closest part from all currently known tradable parts
        private string FindClosestWord(string word)
        {
            Levenshtein lev = new Fastenshtein.Levenshtein(word);
            int minDistance = 9999;
            string potential = "";

            using (StreamReader r = new StreamReader("items\\items.json"))
            {
                string json = r.ReadToEnd();
                JObject items = (JObject)JsonConvert.DeserializeObject(json);

                foreach (var item in items["payload"]["items"])
                {
                    string currentItem = item["item_name"].ToString(); //.ToUpper();
                    int levenshteinDistance = lev.DistanceFrom(currentItem);

                    //Debug.WriteLine((string)item["item_name"] + " | " + levenshteinDistance);

                    if (minDistance > levenshteinDistance)
                    {
                        minDistance = levenshteinDistance;
                        potential = currentItem;
                    }
                }
            }

            // Arbitrary value; needs more testing for the sweetspot
            /*if (minDistance <= 15)
            {
                return potential;
            }
            return word;
            */

            return potential;
        }

        // Warframe.market's API forces us to aquire a large json with many orders
        private void FindPlat(Stream partOrder, TextBox part, TextBox partPlat)
        {
            using (StreamReader r = new StreamReader(partOrder))
            {
                string json = r.ReadToEnd();
                JObject order = (JObject)JsonConvert.DeserializeObject(json);

                List<int> plat = new List<int>();
                foreach (var person in order["payload"]["orders"])
                {
                    if ((string)person["platform"] == "pc" &&
                        (string)person["order_type"] == "sell" &&
                        (string)person["user"]["status"] == "ingame")
                    {
                        plat.Add((int)person["platinum"]);
                    }
                }

                // Sort ascending order
                plat.Sort();

                // Get an average from top 5 current lowest sells
                if (plat.Count >= 5)
                {
                    int sum = 0;
                    double average;
                    for (int i = 0; i < 5; i++)
                    {
                        sum += plat[i];
                    }
                    average = Math.Ceiling(sum / 5.0);
                    partPlat.Text = ((int)average).ToString() + " p";
                    part.Tag = average.ToString();
                }
                // Might be late at night or rare item, use lowest top sell
                else if (plat.Count > 0)
                {
                    partPlat.Text = "*" + plat[0].ToString() + " p";
                    part.Tag = plat[0].ToString();
                }
                // No one is online or Warframe.market doesn't have item
                else
                {
                    partPlat.Text = "UNKN[PLAT]";
                    part.Tag = -1;
                }
            }
        }

        public void GetPriceJson(WebClient client, TextBox part, TextBox partPlat)
        {
            try
            {
                if (!part.Text.Contains("Forma"))
                {
                    Stream partOrder = client.OpenRead(@"https://api.warframe.market/v1/items/" + part.Text.ToLower().Replace(" ", "_") + "/orders");
                    FindPlat(partOrder, part, partPlat);
                }
                else
                {
                    part.Tag = -1;
                    partPlat.Text = "---";
                }
            }
            catch (Exception ex)
            {
                partPlat.Text = "UNKN[JP]";
                part.Tag = -1;

                if (part.Text == "")
                {
                    part.Text = "---";
                }

                LogError("GetPriceJson: " + partPlat.Text + " | " + part.Text + ": " + ex.Message);
            }
        }

        private void FindDucats(string partDucats, TextBox ducats, string url)
        {
            using (StreamReader r = new StreamReader(partDucats))
            {
                string json = r.ReadToEnd();
                JObject items = (JObject)JsonConvert.DeserializeObject(json);

                string numDucats = "";
                foreach (var item in items["payload"]["item"]["items_in_set"])
                {
                    if ((string)item["url_name"] == url)
                    {
                        numDucats = (string)item["ducats"];
                        break;
                    }
                }

                if (numDucats.Trim() != "")
                {
                    ducats.Text = numDucats + " d";
                    ducats.Tag = numDucats;
                }
                else
                {
                    ducats.Text = "UNKN[DUC]";
                    ducats.Tag = -1;

                    File.Delete(string.Format("cache\\{0}.json", url));
                }
            }
        }

        public void GetDucatsJson(WebClient client, TextBox part, TextBox ducats)
        {
            string partUrl = part.Text.ToLower().Replace(" ", "_");
            string partJson = string.Format("cache\\{0}.json", partUrl);
            try
            {
                if (!part.Text.Contains("Forma"))
                {
                    if (!File.Exists(string.Format("cache\\{0}.json", partUrl)))
                    {
                        //Debug.WriteLine(@"https://api.warframe.market/v1/items/" + partUrl);
                        client.DownloadFile(@"https://api.warframe.market/v1/items/" + partUrl, partJson);
                    }

                    FindDucats(partJson, ducats, partUrl);
                }
                else
                {
                    ducats.Text = "---";
                    ducats.Tag = -1;
                }
            }
            catch (Exception ex)
            {
                ducats.Text = "UNKN[JD]";
                ducats.Tag = -1;

                File.Delete(partJson);

                if (part.Text == "")
                {
                    part.Text = "---";
                }

                LogError("GetDucatsJson: " + ducats.Text + " | " + part.Text + ": " + ex.Message);
            }
        }

        public void ClearAllExceptTotal()
        {
            TB_Part1.Text = "";
            TB_Plat1.Text = "";
            TB_Ducats1.Text = "";

            TB_Part2.Text = "";
            TB_Plat2.Text = "";
            TB_Ducats2.Text = "";

            TB_Part3.Text = "";
            TB_Plat3.Text = "";
            TB_Ducats3.Text = "";

            TB_Part4.Text = "";
            TB_Plat4.Text = "";
            TB_Ducats4.Text = "";

            //TB_Pick.Text = "";
        }

        public void EnableRow(bool enable, Label LB_Part, TextBox TB_Part, TextBox TB_Plat, TextBox TB_Ducats)
        {
            LB_Part.Visible = enable;
            TB_Part.Visible = enable;
            TB_Plat.Visible = enable;
            TB_Ducats.Visible = enable;
        }

        private void LogError(string logMessage)
        {
            // Save copy of rewards capture
            string rewards = Guid.NewGuid().ToString() + ".jpg";

            using (StreamWriter w = File.AppendText("error\\errorlog.txt"))
            {
                w.Write("\r\nLog Entry : ");
                w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                    DateTime.Now.ToLongDateString());
                w.WriteLine("  +{0}", logMessage);
                w.WriteLine("  +Rewards: {0}", rewards);
                w.WriteLine("-------------------------------");
            }
            File.Copy("rewards.jpg", "error\\" + rewards);
        }
    }
}
