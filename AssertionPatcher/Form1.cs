using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using JRPC_Client;
using XDevkit;

namespace AssertionPatcher
{
    public partial class Form1 : Form {
        private static readonly string GlobalDirectory = Directory.GetCurrentDirectory();
        private static readonly string PatchFileDir = Path.Combine($"{GlobalDirectory}", @"PatchFile.bin");

        private readonly List<uint> Patches = new List<uint>();
        private static IXboxConsole Xbox360;

        public Form1() {
            InitializeComponent();

            if (GlobalDirectory.Contains("System32")) { 
                // Do some simple checks, to ensure that we're not going to write in System32.
                MessageBox.Show("Do not run this from a zip archive, as administrator, or from the search bar.");
                Process.GetCurrentProcess().Kill(); // Kill our current process.
                return;
            }

        }

        public bool ReadPatchFile() {
            Patches?.Clear();  // Clear the patch list.

            // Check if the patch file exists.
            if (!File.Exists(PatchFileDir)) {
                // Notify the user that the patch file does not exit.
                MessageBox.Show("Could not find the patch file.");
                return false; // Just return, we don't want to continue.
            }

            // Read the patch bytes from the patch file.
            byte[] PatchBytes = File.ReadAllBytes(PatchFileDir);

            // Iterate through our patch bytes.
            for (int i = 0; i < (PatchBytes.Length / 4); i++) {
                int PatchIndex = i * 4; // multiply 4 on each loop.

                byte[] TempArray = {
                    PatchBytes[PatchIndex],
                    PatchBytes[PatchIndex + 1],
                    PatchBytes[PatchIndex + 2],
                    PatchBytes[PatchIndex + 3]
                }; // Create a temporary array to store our current bytes

                Array.Reverse(TempArray); // Reverse our bit order.
                // Add our converted patch into the patch list.
                Patches?.Add(BitConverter.ToUInt32(TempArray, 0));
            }

            // Return true, if our patch list is not null, and has values.
            return (Patches != null && Patches.Count() > 0);
        }

        private void button3_Click(object sender, EventArgs e) {
            listBox1.Items.Clear(); // Clear our current list items.
            
            if (ReadPatchFile()) // Read our patch file.
                // Add all of our patches to the display patch list/
                Patches.ForEach(x => listBox1.Items.Add($"{x.ToString("X")}"));
        }

        private void button4_Click(object sender, EventArgs e) {
            try {
                // Try and connect to our Xbox 360 using JRPC.
                if (Xbox360.Connect(out Xbox360)) 
                    // Notify the user that we've successfully connected.
                    MessageBox.Show("Successfully connected to Xbox 360.");
                else
                    // Notify the user that we've failed to connect.
                    MessageBox.Show("Failed to connect to Xbox 360.");

            }
            catch (Exception ex) {
                // Notify the user that we failed to connect, because we hit an exception.
                MessageBox.Show($"Connection failed: {ex.ToString()}");
            }
        }

        private async void button1_Click(object sender, EventArgs e) {
            // Check if our console has been connected at least once.
            if (Xbox360 != null) {
                if (ReadPatchFile()) { // Read the patches from the patch file.   
                    try { // Disable our button, so people do not spam the fuckin thing.
                        button1.Enabled = false;
                        // Create an async task to run our code.
                        await Task.Run(() => {
                            // Iterate through all of our patches, and write a nop to them.
                            Patches.ForEach(x => { if (x != 0) Xbox360.WriteUInt32(x, 0x60000000); });
                            MessageBox.Show("Assertions Patched");
                        });

                        // Renable our button.
                        button1.Enabled = true;

                        // Renable our button, in case of an exception.
                    } catch { button1.Enabled = true; } return; 
                } 

                // Check if we can read the patch file, and have patches.
                MessageBox.Show("There are no patches to apply.");
                return; // No patches, let's return.
            }

            // Notify the user that they need to connect to their console.
            MessageBox.Show("Connect to your console first.");
        }

        public UInt32 ConvertHexValue(string Value) 
            // Parse our UInt32 value as a hexadecimal number.
            => UInt32.Parse(Value.Replace("0x", "").Replace(" ", ""), System.Globalization.NumberStyles.HexNumber);

        private void button2_Click(object sender, EventArgs e) {
            try {
                // Get our converted hex value.
                var HexValue = ConvertHexValue(textBox1.Text);

                // Check if our file doesn't exist.
                if (!File.Exists(PatchFileDir))
                    // Create a new patch file, and close it.
                    File.Create(PatchFileDir).Close();

                ReadPatchFile(); // Read our patch file.

                // Check if we already have this patch.
                if (!Patches.Contains(HexValue))
                    // If not, add the new patch to the patch list.
                    Patches.Add(HexValue);

                // Create our destination patch array.
                byte[] DestPatchArray = new byte[Patches.Count() * 4];

                // Iterate through our patches.
                for (int i = 0; i < Patches.Count(); i++) {
                    int PatchIndex = (i * 4); // Get our current patch index.

                    // Do some bitwise bullshit, and convert our int into the byte array.
                    DestPatchArray[PatchIndex] = (byte)((Patches[i] & 0xFF000000) >> 24);
                    DestPatchArray[PatchIndex + 1] = (byte)((Patches[i] & 0x00FF0000) >> 16);
                    DestPatchArray[PatchIndex + 2] = (byte)((Patches[i] & 0x0000FF00) >> 8);
                    DestPatchArray[PatchIndex + 3] = (byte)(Patches[i] & 0x000000FF);
                }

                // Write our destination bytes to the patch file.
                File.WriteAllBytes(PatchFileDir, DestPatchArray);                
            } catch (Exception ex) {
                // We've hit an exception, notify the user. 
                MessageBox.Show($"{ex.ToString()}");
            }
        }
    }
}
