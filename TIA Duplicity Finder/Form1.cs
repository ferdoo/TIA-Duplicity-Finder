using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Drawing;
using System.Text.RegularExpressions;
using TIA_Duplicity_Finder.Properties;


namespace TIA_Duplicity_Finder
{
    public partial class Form1 : Form
    {

        #region Fileds

        private XmlNamespaceManager _ns;

        private XmlDocument _document;
        public XmlDocument Document
        {
            get { return _document; }
            set { _document = value; }
        }

        private XmlNode _rootNode;
        public XmlNode RootNode
        {
            get { return _rootNode; }
            set { _rootNode = value; }
        }

        private XmlNode _node;

        public int keyvalue; // TEST

        public XmlNode Node
        {
            get { return _node; }
            set { _node = value; }
        }


        #endregion


        #region Variables

        //private IDictionary<string, string> instancneDictionary = new Dictionary<string, string>();


        private List<(string Component, string InstanceType, string BlockName)> namesInstancie = new List<(string Component, string InstanceType, string BlockName)>();

        private List<(string Component, string InstanceType, string BlockName)> names = new List<(string Component, string InstanceType, string BlockName)>();

        private string result = "";

        private string resultInstanceColisions = "";

        private string PartUId = "";

        private string ComponentNameValue = "";

        private string TagNameValue = "";

        private List<Colisions> completeListOfColisions = new List<Colisions>();

        private List<Colisions> localVarListOfColisions = new List<Colisions>();

        private List<Colisions> globalVarListOfColisions = new List<Colisions>();

        private List<Colisions> ListOfInstanceColisions = new List<Colisions>();

        private int LocalColisionsCount = 0;

        private int GlobalColisionsCount = 0;

        private int ColisionsCountInstancie = 0;

        private bool SearchInLocalVar = true;

        StringBuilder errorTextBuilder = new StringBuilder();

        private LocalVarFilerForm localVarFilerForm = new LocalVarFilerForm();

        private string VariableFilterRegExpPattern = Settings1.Default.LocalVarFilter;

        #endregion


        public Form1()
        {
            InitializeComponent();

            this.Text = "TIA Duplicity Finder V5 - TIA V15.1, V16";

            tabPage1.Text = "Tagy Kolizie";
            tabPage2.Text = "Tagy duplicitne zapisy";
            tabPage3.Text = "Tagy Vsetky zapisy";
            tabPage4.Text = "Instancie Kolizie";
            tabPage5.Text = "Instancie Vsetky zapisy";

            this.tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.tabControl1.ShowToolTips = true;
           
        }


        private void button1_Click(object sender, EventArgs e)
        {
            names.Clear();
            namesInstancie.Clear();
            completeListOfColisions.Clear();
            localVarListOfColisions.Clear();
            globalVarListOfColisions.Clear();
            ListOfInstanceColisions.Clear();

            richTextBox1.Clear();
            richTextBox2.Clear();
            richTextBox3.Clear();
            richTextBox4.Clear();
            richTextBox5.Clear();
            label1.Text = "Projekt : ";

            checkBox_SearchInLocal.Enabled = false;
            button2.Enabled = false;

            var openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = @"D:\Excel code generator for TIA Portal Openness\xml Export ProjectCheck debug";
            //openFileDialog.InitialDirectory = @"D:\Excel code generator for TIA Portal Openness\xml Export Openes\TEMP\26-E10-90C-367604-001-1330-SPS.ap15_1\20_Station_1320";
            openFileDialog.Filter = "TIA XML (*.xml)|*.xml";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                
                string Path = openFileDialog.FileName;
                string FolderName = System.IO.Path.GetDirectoryName(Path);
                System.IO.DirectoryInfo Dirinfo = new System.IO.DirectoryInfo(FolderName);
                label1.Text = "Projekt : " + Dirinfo.Name + " --- " + openFileDialog.FileNames.Count() + ". suborov.";

                foreach (var file in openFileDialog.FileNames)
                {

                    //richTextBox1.AppendText(file + "\n");
                    //richTextBox1.AppendText(Environment.NewLine);

                    try
                    {
                        ProcessFile(file);
                    }
                    catch (Exception exception)
                    {
                        MessageBox.Show($"Chyba v subore {file} -> tento subor bude vynechany. \n \n" + exception);
                    }
                    
                }


                GetColisions();

                GetColisionsInstancie();

                checkBox_SearchInLocal.Enabled = true;
                button2.Enabled = checkBox_SearchInLocal.Checked;


                if (errorTextBuilder.Length > 0)
                {
                    MessageBox.Show("CHYBA \n" + errorTextBuilder.ToString());
                    
                }


            }
            else
            {
                checkBox_SearchInLocal.Enabled = true;
                button2.Enabled = checkBox_SearchInLocal.Checked;
            }

            MessageBox.Show("Hotovo, spolu " + openFileDialog.FileNames.Count() + " suborov.",
                System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetDirectoryName(openFileDialog.FileName)),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information,
                MessageBoxDefaultButton.Button1);
            
        }

        
        void ProcessFile(string Filename)
        {
            Document = new XmlDocument();

            _ns = new XmlNamespaceManager(Document.NameTable);
            

            //Load Xml File with fileName into memory
            Document.Load(Filename);
            
            //get root node of xml file
            RootNode = Document.DocumentElement;

            
            var AktualFile = System.IO.Path.GetFileNameWithoutExtension(Filename);


            var engineeringVersion = Document.DocumentElement.SelectSingleNode("//Engineering").Attributes["version"].Value;

            if (engineeringVersion == "V15.1")
            {
                _ns.AddNamespace("SI", "http://www.siemens.com/automation/Openness/SW/Interface/v3");
                _ns.AddNamespace("siemensNetworks", "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v3");
            }
            else if (engineeringVersion == "V16")
            {
                _ns.AddNamespace("SI", "http://www.siemens.com/automation/Openness/SW/Interface/v4");
                _ns.AddNamespace("siemensNetworks", "http://www.siemens.com/automation/Openness/SW/NetworkSource/FlgNet/v4");
            }
            else
            {
                errorTextBuilder.AppendLine($"{AktualFile} > Engineering Version : {engineeringVersion} ?");
                return;
            }




            var listOfNetworks = RootNode.SelectNodes("//SW.Blocks.CompileUnit");


            if (listOfNetworks != null)
            {
                foreach (XmlNode network in listOfNetworks)
                {

                    
                    var listOfCallRef = network.SelectNodes(".//siemensNetworks:Call", _ns);

                    var listOfAccess = network.SelectNodes(".//siemensNetworks:Access", _ns);
                    
                    var listOfPart = network.SelectNodes(".//siemensNetworks:Part", _ns);




                    // **********  viacnasobne variable
                    foreach (XmlNode nodeCallref in listOfCallRef)
                    {

                        var nodeCallInfo = nodeCallref.SelectSingleNode(".//siemensNetworks:CallInfo", _ns);
                        var nodeComponent = nodeCallref.SelectSingleNode(".//siemensNetworks:Component", _ns);
                        var nodeInstance = nodeCallref.SelectSingleNode(".//siemensNetworks:Instance", _ns);

                        var Name = "";
                        var BlockType = "";
                        var Component = "";
                        var InstanceType = "";

                        Name = nodeCallInfo.Attributes["Name"].Value;
                        BlockType = nodeCallInfo.Attributes["BlockType"].Value;


                        if (nodeComponent != null)
                            Component = nodeComponent.Attributes["Name"].Value;

                        if (nodeInstance != null)
                            InstanceType = nodeInstance.Attributes["Scope"].Value;


                        //richTextBox5.AppendText($"BlockType: {BlockType}   Name: {Name}   Component: {Component} InstanceType: {InstanceType} \n");
                        //richTextBox5.AppendText(Environment.NewLine);


                        if (Component != "")
                        {
                            namesInstancie.Add((Component, InstanceType, AktualFile));

                        }

                    }

                    




                    // *************** viacnasobne tagy

                    // initializacia novy network
                    PartUId = "";
                    
                    //najdi spulku - zapis zo spulky
                    foreach (XmlNode nodePart in listOfPart)
                    {
                        if (nodePart.Attributes["Name"].Value != null)
                        {
                            if (nodePart.Attributes["Name"].Value == "Coil")
                            {
                                // Spulka najdena a k nej prisluchajuce ID
                                PartUId = nodePart.Attributes["UId"].Value;

                                // Najdi wire ID prisluchajuce spulke
                                var listOfWire = network.SelectNodes(".//siemensNetworks:Wire", _ns);

                                foreach (XmlNode nodeWire in listOfWire)
                                {

                                    var IdentCon = nodeWire.SelectSingleNode(".//siemensNetworks:IdentCon", _ns);
                                    var NameCon = nodeWire.SelectSingleNode(".//siemensNetworks:NameCon", _ns);

                                    if (NameCon.Attributes["UId"].Value != null)
                                    {
                                        if (NameCon.Attributes["Name"].Value == "operand" &&
                                            NameCon.Attributes["UId"].Value == PartUId)
                                        {
                                            if (!string.IsNullOrEmpty(IdentCon.Attributes["UId"].Value))
                                            {
                                                // Wire ID najdene k spulke
                                                var PartUId = IdentCon.Attributes["UId"].Value;


                                                // Najdi meno symbolu k wide ID
                                                foreach (XmlNode nodeAcess in listOfAccess)
                                                {

                                                    if (nodeAcess.Attributes.GetNamedItem("UId") != null)
                                                    {
                                                        var nodeAcessUId = nodeAcess.Attributes["UId"].Value;

                                                        if (nodeAcessUId == PartUId)
                                                        {

                                                            //init
                                                            ComponentNameValue = "";

                                                            // Meno spulky
                                                            var Scope = nodeAcess.Attributes["Scope"].Value;
                                                            var listOfComponentName =
                                                                nodeAcess.SelectNodes(".//siemensNetworks:Component",
                                                                    _ns);

                                                            foreach (XmlNode ComponentName in listOfComponentName)
                                                            {

                                                                if (ComponentName.Attributes.GetNamedItem(
                                                                        "AccessModifier") != null)
                                                                {
                                                                    var ConstantValue = ComponentName
                                                                        .SelectSingleNode(
                                                                            ".//siemensNetworks:ConstantValue", _ns)
                                                                        .InnerText;

                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "[" + ConstantValue + "]";
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "[" + ConstantValue + "]";
                                                                    }
                                                                }
                                                                else if (ComponentName.Attributes.GetNamedItem(
                                                                             "SliceAccessModifier") != null)
                                                                {
                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "." + ComponentName
                                                                                .Attributes["SliceAccessModifier"]
                                                                                .Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value +
                                                                            "." + ComponentName
                                                                                .Attributes["SliceAccessModifier"]
                                                                                .Value;
                                                                    }
                                                                }
                                                                else
                                                                {

                                                                    if (ComponentNameValue == "")
                                                                    {
                                                                        ComponentNameValue =
                                                                            ComponentName.Attributes["Name"].Value;
                                                                    }
                                                                    else
                                                                    {
                                                                        ComponentNameValue = ComponentNameValue + "." +
                                                                            ComponentName.Attributes["Name"].Value;
                                                                    }
                                                                }

                                                            }

                                                            //richTextBox2.AppendText($"Spulka: {ComponentNameValue}  Scope: {Scope} \n");
                                                            //richTextBox2.AppendText(Environment.NewLine);


                                                            if (ComponentNameValue != "")
                                                            {
                                                                //instancneDictionary.Add(Component, Component);

                                                                names.Add((ComponentNameValue, Scope, AktualFile));

                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                    // najdi call - zapis z bloku
                    foreach (XmlNode nodeCallref in listOfCallRef)
                    {

                        var listOfCallParameterOut = nodeCallref.SelectNodes(".//siemensNetworks:Parameter[@Section='Output']", _ns);

                        var actualCallUid = nodeCallref.Attributes["UId"].Value;

                        foreach (XmlNode CallParameterOut in listOfCallParameterOut)
                        {
                            var CallParameterOutName = CallParameterOut.Attributes["Name"].Value;


                            // Najdi wire ID prisluchajuce callu
                            var listOfWire = network.SelectNodes(".//siemensNetworks:Wire", _ns);
                            foreach (XmlNode nodeWire in listOfWire)
                            {

                                var NameCon = nodeWire.SelectSingleNode(".//siemensNetworks:NameCon[@Name='"+ CallParameterOutName + "' and @UId='" + actualCallUid + "']", _ns);

                                
                                if (NameCon != null)
                                {
                                    var IdentCon = nodeWire.SelectSingleNode(".//siemensNetworks:IdentCon", _ns);

                                    if (IdentCon != null)
                                    {
                                        var ConnUid = IdentCon.Attributes["UId"].Value;

                                        var listOfAcess = network.SelectSingleNode(".//siemensNetworks:Access[@UId='" + ConnUid + "']", _ns);

                                        var Scope = network.SelectSingleNode(".//siemensNetworks:Access[@UId='" + ConnUid + "']", _ns).Attributes["Scope"].Value;

                                        var listOfTagNames = listOfAcess.SelectNodes(".//siemensNetworks:Component", _ns);

                                        TagNameValue = "";

                                        foreach (XmlNode listOfTagName in listOfTagNames)
                                        {

                                            if (listOfTagName.Attributes.GetNamedItem("AccessModifier") != null)
                                            {
                                                var ConstantValue = listOfTagName.SelectSingleNode(".//siemensNetworks:ConstantValue", _ns).InnerText;

                                                if (TagNameValue == "")
                                                {
                                                    TagNameValue = listOfTagName.Attributes["Name"].Value + "[" + ConstantValue + "]";
                                                }
                                                else
                                                {
                                                    TagNameValue = TagNameValue + "." + listOfTagName.Attributes["Name"].Value + "[" + ConstantValue + "]";
                                                }
                                            }
                                            else if (listOfTagName.Attributes.GetNamedItem("SliceAccessModifier") != null)
                                            {
                                                if (TagNameValue == "")
                                                {
                                                    TagNameValue = listOfTagName.Attributes["Name"].Value + "." + listOfTagName.Attributes["SliceAccessModifier"].Value;
                                                }
                                                else
                                                {
                                                    TagNameValue = TagNameValue + "." + listOfTagName.Attributes["Name"].Value + "." + listOfTagName.Attributes["SliceAccessModifier"].Value;
                                                }
                                            }
                                            else
                                            {
                                                if (TagNameValue == "")
                                                {
                                                    TagNameValue = listOfTagName.Attributes["Name"].Value;
                                                }
                                                else
                                                {
                                                    TagNameValue = TagNameValue + "." + listOfTagName.Attributes["Name"].Value;
                                                }
                                            }

                                        }

                                        names.Add((TagNameValue, Scope, AktualFile));

                                    }
                                }
                            }
                        }
                    }

                }
            }
        }


        void GetColisions()
        {

            var colisions = names.GroupBy(x => x.Component)
                    .Where(w => w.Count() > 1)
                    .Select(s => s);
            
            
            foreach (var colision in colisions)
            {
                
                result = "";

                foreach (var xx in colision)
                {

                    result = xx.Component + " -> " + xx.InstanceType + " -> " + xx.BlockName;

                    completeListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });


                    if (xx.InstanceType == "GlobalVariable")

                    {
                        globalVarListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });
                    }
                    else
                    {
                        localVarListOfColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });
                    }
                }
            }

            #region Vypis Globalne Kolizie

            result = "";
            GlobalColisionsCount = 0;

            var oldName = "";
            var newName = "";

            RichTextBoxExtensions.AppendText(richTextBox1, " -- Kolizie globalnych variable -- \n", Color.Blue, Color.White);

            foreach (var globalVarListOfColision in globalVarListOfColisions)
            {

                oldName = globalVarListOfColision.Component;
                if (oldName != newName)
                {
                    newName = globalVarListOfColision.Component;
                    GlobalColisionsCount++;
                }


                result = globalVarListOfColision.Component + " -> " + globalVarListOfColision.InstanceType + " -> " + globalVarListOfColision.BlockName;


                if (GlobalColisionsCount % 2 == 1)
                {
                    RichTextBoxExtensions.AppendText(richTextBox1, GlobalColisionsCount + ". " + result, Color.Red, Color.White);
                    richTextBox1.AppendText(Environment.NewLine);
                }
                else
                {
                    RichTextBoxExtensions.AppendText(richTextBox1, GlobalColisionsCount + ". " + result, Color.Red, Color.LightGray);
                    richTextBox1.AppendText(Environment.NewLine);
                }

            }

            if (GlobalColisionsCount == 0)
            {
                RichTextBoxExtensions.AppendText(richTextBox1, "Nenasiel som.", Color.Green, Color.White);
                richTextBox1.AppendText(Environment.NewLine);
            }

            #endregion


            #region Vypis Lokalne Kolizie

            LocalColisionsCount = 0;

            Regex rg = new Regex(VariableFilterRegExpPattern);
            

            if (SearchInLocalVar)
            {
                var localVarColisionsByComponent = localVarListOfColisions.GroupBy(x => x.Component)
                    .Where(w => w.Count() > 1)
                    .Select(s => s);


                result = "";

                oldName = "";
                newName = "";

                RichTextBoxExtensions.AppendText(richTextBox1, " -- Kolizie lokalnych variable -- \n", Color.Blue, Color.White);

                foreach (var localVarColisionByComponent in localVarColisionsByComponent)
                {

                    var localVarColisionsByBlockName = localVarColisionByComponent.GroupBy(x => x.BlockName)
                        .Where(w => w.Count() > 1)
                        .Select(s => s);


                    foreach (var localVarColisionByBlockName in localVarColisionsByBlockName)
                    {

                        foreach (var localVarListOfColision in localVarColisionByBlockName)
                        {


                            if (!rg.IsMatch(localVarListOfColision.Component) || VariableFilterRegExpPattern == "")
                            {
                                oldName = localVarListOfColision.Component;
                                if (oldName != newName)
                                {
                                    newName = localVarListOfColision.Component;
                                    LocalColisionsCount++;
                                }

                                result = localVarListOfColision.Component + " -> " + localVarListOfColision.InstanceType + " -> " + localVarListOfColision.BlockName;


                                if (LocalColisionsCount % 2 == 1)
                                {
                                    RichTextBoxExtensions.AppendText(richTextBox1, LocalColisionsCount + ". " + result, Color.Black, Color.White);
                                    richTextBox1.AppendText(Environment.NewLine);
                                }
                                else
                                {
                                    RichTextBoxExtensions.AppendText(richTextBox1, LocalColisionsCount + ". " + result, Color.Black, Color.LightGray);
                                    richTextBox1.AppendText(Environment.NewLine);
                                }
                            }
                            
                            
                        }
                    }
                }

                if (LocalColisionsCount == 0)
                {
                    if (VariableFilterRegExpPattern != "")
                    {
                        RichTextBoxExtensions.AppendText(richTextBox1, $"Nenasiel som. Filter : {VariableFilterRegExpPattern}", Color.Green, Color.White);
                        richTextBox1.AppendText(Environment.NewLine);
                    }
                    else
                    {
                        RichTextBoxExtensions.AppendText(richTextBox1, "Nenasiel som. ", Color.Green, Color.White);
                        richTextBox1.AppendText(Environment.NewLine);
                    }
                    
                }
                else
                {
                    if (VariableFilterRegExpPattern != "")
                    {
                        RichTextBoxExtensions.AppendText(richTextBox1, $"! Zapnuty Filter : {VariableFilterRegExpPattern}", Color.Black, Color.Orange);
                        richTextBox1.AppendText(Environment.NewLine);
                    }
                }
            }

            #endregion


            #region Vypis Sumar + dvojite zapisy + vsetky zapisy

            if (LocalColisionsCount + GlobalColisionsCount == 0)
            {
                richTextBox1.AppendText(Environment.NewLine);
                RichTextBoxExtensions.AppendText(richTextBox1, $"Hotovo, nenasiel som ziadne kolizie.", Color.Green, Color.White);
            }
            else
            {
                richTextBox1.AppendText(Environment.NewLine);
                RichTextBoxExtensions.AppendText(richTextBox1, $"Hotovo, spolu {LocalColisionsCount + GlobalColisionsCount} duplicit najdenich.", Color.Red, Color.White);
            }



            for (int i = 0; i < completeListOfColisions.Count; i++)
            {
                richTextBox2.AppendText(i + 1 + " - " + completeListOfColisions[i].Component + " - " + completeListOfColisions[i].InstanceType + " - " + completeListOfColisions[i].BlockName + "\n");
            }


            for (int i = 0; i < names.Count; i++)
            {
                richTextBox3.AppendText(i + 1 + ";" + names[i].Component + ";" + names[i].InstanceType + ";" + names[i].BlockName + "\n");
            }

            #endregion


        }


        void GetColisionsInstancie()
        {

            
            var colisionsInstance = namesInstancie.GroupBy(x => x.Component)
                .Where(w => w.Count() > 1)
                .Select(s => s);


            foreach (var colisionInstance in colisionsInstance)
            {

                foreach (var xx in colisionInstance)
                {
                    ListOfInstanceColisions.Add(new Colisions() { Component = xx.Component, InstanceType = xx.InstanceType, BlockName = xx.BlockName });
                }

            }


            resultInstanceColisions = "";
            ColisionsCountInstancie = 0;

            var oldName = "";
            var newName = "";

            RichTextBoxExtensions.AppendText(richTextBox4, " -- Kolizie Instancii variable -- \n", Color.Blue, Color.White);




            foreach (var InstanceColisions in ListOfInstanceColisions)
            {

                oldName = InstanceColisions.Component;
                if (oldName != newName)
                {
                    newName = InstanceColisions.Component;
                    ColisionsCountInstancie++;
                }


                resultInstanceColisions = InstanceColisions.Component + " -> " + InstanceColisions.InstanceType + " -> " + InstanceColisions.BlockName;


                if (ColisionsCountInstancie % 2 == 1)
                {
                    RichTextBoxExtensions.AppendText(richTextBox4, ColisionsCountInstancie + ". " + resultInstanceColisions, Color.Red, Color.White);
                    richTextBox4.AppendText(Environment.NewLine);
                }
                else
                {
                    RichTextBoxExtensions.AppendText(richTextBox4, ColisionsCountInstancie + ". " + resultInstanceColisions, Color.Red, Color.LightGray);
                    richTextBox4.AppendText(Environment.NewLine);
                }

                
                
            }


            if (ColisionsCountInstancie == 0)
            {
                RichTextBoxExtensions.AppendText(richTextBox4, "Nenasiel som.", Color.Green, Color.White);
                richTextBox4.AppendText(Environment.NewLine);
            }
            else
            {
                richTextBox4.AppendText(Environment.NewLine);
                RichTextBoxExtensions.AppendText(richTextBox4, $"Hotovo, spolu {ColisionsCountInstancie} duplicit najdenich.", Color.Red, Color.White);
            }

            

            for (int i = 0; i < namesInstancie.Count; i++)
            {
                richTextBox5.AppendText(i + ";" + namesInstancie[i].Component + ";" + namesInstancie[i].InstanceType + ";" + namesInstancie[i].BlockName + "\n");
            }
        }


        private void checkBox_SearchInLocal_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SearchInLocal.Checked)
            {
                SearchInLocalVar = true;
                button2.Enabled = true;
                Settings1.Default.SearchInLocal = true;
            }
            else
            {
                SearchInLocalVar = false;
                button2.Enabled = false;
                Settings1.Default.SearchInLocal = false;
            }
            
        }
        
        
        private void button2_Click(object sender, EventArgs e)
        {

            if (localVarFilerForm.ShowDialog() == DialogResult.Cancel)
            {
                VariableFilterRegExpPattern = localVarFilerForm.LocalVarFilter;
            }
            
        }


        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl1.TabPages[e.Index];
            //Color col = e.Index == 0 ? Color.Aqua : Color.Yellow;

            Color col = e.Index < 3 ? Color.Aqua : Color.Yellow;

            e.Graphics.FillRectangle(new SolidBrush(col), e.Bounds);

            Rectangle paddedBounds = e.Bounds;
            int yOffset = (e.State == DrawItemState.Selected) ? -2 : 1;
            paddedBounds.Offset(1, yOffset);
            TextRenderer.DrawText(e.Graphics, page.Text, Font, paddedBounds, page.ForeColor);
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            button2.Enabled = checkBox_SearchInLocal.Checked = Settings1.Default.SearchInLocal;
            
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Settings1.Default.Save();
        }
        

        private void richTextBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                richTextBox1.AddContextMenu();
            }
        }


        private void richTextBox3_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                richTextBox3.AddContextMenu();
            }
        }


        private void richTextBox4_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                richTextBox4.AddContextMenu();
            }
        }


        private void richTextBox5_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                richTextBox5.AddContextMenu();
            }
        }
        
    }
}
