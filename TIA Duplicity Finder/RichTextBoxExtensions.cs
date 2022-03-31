using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TIA_Duplicity_Finder
{
    public static class RichTextBoxExtensions
    {

        
        public static void AppendText(this RichTextBox box, string text, Color textColor, Color textBackColor)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = textColor;
            box.SelectionBackColor = textBackColor;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
            
        }

        public static void AddContextMenu(this RichTextBox rtb)
        {
            if (rtb.ContextMenuStrip == null)
            {
                ContextMenuStrip cms = new ContextMenuStrip()
                {
                    ShowImageMargin = false
                };


                ToolStripMenuItem tsmiExport = new ToolStripMenuItem("Export");
                tsmiExport.Click += (sender, e) => ExortAction();
                cms.Items.Add(tsmiExport);

                cms.Items.Add(new ToolStripSeparator());


                ToolStripMenuItem tsmiUndo = new ToolStripMenuItem("Undo");
                tsmiUndo.Click += (sender, e) => rtb.Undo();
                cms.Items.Add(tsmiUndo);

                ToolStripMenuItem tsmiRedo = new ToolStripMenuItem("Redo");
                tsmiRedo.Click += (sender, e) => rtb.Redo();
                cms.Items.Add(tsmiRedo);

                cms.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem tsmiCut = new ToolStripMenuItem("Cut");
                tsmiCut.Click += (sender, e) => rtb.Cut();
                cms.Items.Add(tsmiCut);

                ToolStripMenuItem tsmiCopy = new ToolStripMenuItem("Copy");
                tsmiCopy.Click += (sender, e) => rtb.Copy();
                //tsmiCopy.Click += (sender, e) => CopyAction();
                cms.Items.Add(tsmiCopy);

                ToolStripMenuItem tsmiPaste = new ToolStripMenuItem("Paste");
                tsmiPaste.Click += (sender, e) => rtb.Paste();
                cms.Items.Add(tsmiPaste);

                ToolStripMenuItem tsmiDelete = new ToolStripMenuItem("Delete");
                tsmiDelete.Click += (sender, e) => rtb.SelectedText = "";
                cms.Items.Add(tsmiDelete);

                cms.Items.Add(new ToolStripSeparator());

                ToolStripMenuItem tsmiSelectAll = new ToolStripMenuItem("Select All");
                tsmiSelectAll.Click += (sender, e) => rtb.SelectAll();
                cms.Items.Add(tsmiSelectAll);

                cms.Opening += (sender, e) =>
                {
                    tsmiExport.Enabled = rtb.TextLength > 0;
                    tsmiUndo.Enabled = !rtb.ReadOnly && rtb.CanUndo;
                    tsmiRedo.Enabled = !rtb.ReadOnly && rtb.CanRedo;
                    tsmiCut.Enabled = !rtb.ReadOnly && rtb.SelectionLength > 0;
                    tsmiCopy.Enabled = rtb.SelectionLength > 0;
                    tsmiPaste.Enabled = !rtb.ReadOnly && Clipboard.ContainsText();
                    tsmiDelete.Enabled = !rtb.ReadOnly && rtb.SelectionLength > 0;
                    tsmiSelectAll.Enabled = rtb.TextLength > 0 && rtb.SelectionLength < rtb.TextLength;
                };

                rtb.ContextMenuStrip = cms;


                void CopyAction()
                {
                    //Clipboard.SetData(DataFormats.Rtf, rtb.SelectedRtf);
                    Clipboard.SetText(DataFormats.Rtf);
                    //Clipboard.Clear();
                }

                void ExortAction()
                {

                     SaveFileDialog SaveFileDialog1 = new SaveFileDialog();


                    // Set the properties on SaveFileDialog1 so the user is 
                    // prompted to create the file if it doesn't exist 
                    // or overwrite the file if it does exist.
                    SaveFileDialog1.CreatePrompt = true;
                    SaveFileDialog1.OverwritePrompt = true;

                    // Set the file name to myText.txt, set the type filter
                    // to text files, and set the initial directory to the 
                    // MyDocuments folder.
                    SaveFileDialog1.FileName = rtb.Parent.Text;
                    // DefaultExt is only used when "All files" is selected from 
                    // the filter box and no extension is specified by the user.
                    SaveFileDialog1.DefaultExt = "txt";
                    SaveFileDialog1.Filter =
                        "Text files (*.txt)|*.txt|csv files (*.csv)|*.csv";
                    SaveFileDialog1.InitialDirectory =
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                    // Call ShowDialog and check for a return value of DialogResult.OK,
                    // which indicates that the file was saved. 
                    DialogResult result = SaveFileDialog1.ShowDialog();


                    if (result == DialogResult.OK)
                    {
                        
                        using (var w = new StreamWriter(SaveFileDialog1.OpenFile()))
                        {

                            for (int i = 0; i < rtb.Lines.Length ; i++)
                            {
                                var line = rtb.Lines[i];
                                w.WriteLine(line);
                                w.Flush();
                            }
                            
                        }
                    }
                }
            }
        }
    }
}