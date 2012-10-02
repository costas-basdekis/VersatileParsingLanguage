namespace Versatile_Parsing_Language
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
			this.ssMain = new System.Windows.Forms.StatusStrip();
			this.ssMain_Status = new System.Windows.Forms.ToolStripStatusLabel();
			this.ssMain_RunStatus = new System.Windows.Forms.ToolStripStatusLabel();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.groupBox6 = new System.Windows.Forms.GroupBox();
			this.tvGrammar = new System.Windows.Forms.TreeView();
			this.groupBox7 = new System.Windows.Forms.GroupBox();
			this.tbCompiled = new FixedRichTextBox();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.butParseLoad = new System.Windows.Forms.Button();
			this.butParseSave = new System.Windows.Forms.Button();
			this.butAssembly = new System.Windows.Forms.Button();
			this.cbPath = new System.Windows.Forms.ComboBox();
			this.tbParse_Source = new FixedRichTextBox();
			this.lbErrors = new System.Windows.Forms.ListBox();
			this.splitContainer4 = new System.Windows.Forms.SplitContainer();
			this.tabMain = new System.Windows.Forms.TabControl();
			this.tabMain_Parse = new System.Windows.Forms.TabPage();
			this.tabMain_Source = new System.Windows.Forms.TabPage();
			this.butRun_Run = new System.Windows.Forms.Button();
			this.butRun_Save = new System.Windows.Forms.Button();
			this.butRun_Load = new System.Windows.Forms.Button();
			this.cbRun_Path = new System.Windows.Forms.ComboBox();
			this.splitContainer3 = new System.Windows.Forms.SplitContainer();
			this.tbRun_Source = new System.Windows.Forms.TextBox();
			this.cbRun_ShowTree = new System.Windows.Forms.CheckBox();
			this.cbRun_Function = new System.Windows.Forms.ComboBox();
			this.tvRun_RetVal = new System.Windows.Forms.TreeView();
			this.tabMain_Debug = new System.Windows.Forms.TabPage();
			this.splitContainer8 = new System.Windows.Forms.SplitContainer();
			this.splitContainer5 = new System.Windows.Forms.SplitContainer();
			this.splitContainer6 = new System.Windows.Forms.SplitContainer();
			this.tsDebug = new System.Windows.Forms.ToolStrip();
			this.tsDebug_Functions = new System.Windows.Forms.ToolStripComboBox();
			this.tsDebug_Run = new System.Windows.Forms.ToolStripButton();
			this.tsDebug_Pause = new System.Windows.Forms.ToolStripButton();
			this.tsDebug_Stop = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsDebug_StepInto = new System.Windows.Forms.ToolStripButton();
			this.tsDebug_StepOver = new System.Windows.Forms.ToolStripButton();
			this.tsDebug_StepOut = new System.Windows.Forms.ToolStripButton();
			this.splitContainer7 = new System.Windows.Forms.SplitContainer();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.lvDebug_FS = new System.Windows.Forms.ListView();
			this.lbDebug_FS_Functions = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.lvDebug_BS = new System.Windows.Forms.ListView();
			this.lbDebug_BS_Blocks = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.lvDebug_Runnables = new System.Windows.Forms.ListView();
			this.chDebug_Runnables_Runnables = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.ilMain = new System.Windows.Forms.ImageList(this.components);
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.tvDebug_Variables = new System.Windows.Forms.TreeView();
			this.splitContainer9 = new System.Windows.Forms.SplitContainer();
			this.groupBox5 = new System.Windows.Forms.GroupBox();
			this.dgvDebug_Block = new System.Windows.Forms.DataGridView();
			this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.cmsRTF = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cmsRTF_Breakpoint = new System.Windows.Forms.ToolStripMenuItem();
			this.timApplyParseSyntaxColoring = new System.Windows.Forms.Timer(this.components);
			this.butNewWindow = new System.Windows.Forms.Button();
			this.timParseUndoIdleWait = new System.Windows.Forms.Timer(this.components);
			this.timParseUndoMaxWait = new System.Windows.Forms.Timer(this.components);
			this.ssMain.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.groupBox6.SuspendLayout();
			this.groupBox7.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
			this.splitContainer4.Panel1.SuspendLayout();
			this.splitContainer4.Panel2.SuspendLayout();
			this.splitContainer4.SuspendLayout();
			this.tabMain.SuspendLayout();
			this.tabMain_Parse.SuspendLayout();
			this.tabMain_Source.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			this.tabMain_Debug.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer8)).BeginInit();
			this.splitContainer8.Panel1.SuspendLayout();
			this.splitContainer8.Panel2.SuspendLayout();
			this.splitContainer8.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).BeginInit();
			this.splitContainer5.Panel1.SuspendLayout();
			this.splitContainer5.Panel2.SuspendLayout();
			this.splitContainer5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer6)).BeginInit();
			this.splitContainer6.Panel1.SuspendLayout();
			this.splitContainer6.Panel2.SuspendLayout();
			this.splitContainer6.SuspendLayout();
			this.tsDebug.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer7)).BeginInit();
			this.splitContainer7.Panel1.SuspendLayout();
			this.splitContainer7.Panel2.SuspendLayout();
			this.splitContainer7.SuspendLayout();
			this.groupBox1.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.groupBox4.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer9)).BeginInit();
			this.splitContainer9.Panel1.SuspendLayout();
			this.splitContainer9.SuspendLayout();
			this.groupBox5.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dgvDebug_Block)).BeginInit();
			this.cmsRTF.SuspendLayout();
			this.SuspendLayout();
			// 
			// ssMain
			// 
			this.ssMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ssMain_Status,
            this.ssMain_RunStatus});
			this.ssMain.Location = new System.Drawing.Point(0, 526);
			this.ssMain.Name = "ssMain";
			this.ssMain.Size = new System.Drawing.Size(833, 22);
			this.ssMain.TabIndex = 5;
			this.ssMain.Text = "statusStrip1";
			// 
			// ssMain_Status
			// 
			this.ssMain_Status.Name = "ssMain_Status";
			this.ssMain_Status.Size = new System.Drawing.Size(39, 17);
			this.ssMain_Status.Text = "Ready";
			// 
			// ssMain_RunStatus
			// 
			this.ssMain_RunStatus.Name = "ssMain_RunStatus";
			this.ssMain_RunStatus.Size = new System.Drawing.Size(0, 17);
			this.ssMain_RunStatus.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// splitContainer1
			// 
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.groupBox6);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.groupBox7);
			this.splitContainer1.Size = new System.Drawing.Size(210, 494);
			this.splitContainer1.SplitterDistance = 218;
			this.splitContainer1.TabIndex = 11;
			// 
			// groupBox6
			// 
			this.groupBox6.Controls.Add(this.tvGrammar);
			this.groupBox6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox6.Location = new System.Drawing.Point(0, 0);
			this.groupBox6.Name = "groupBox6";
			this.groupBox6.Size = new System.Drawing.Size(210, 218);
			this.groupBox6.TabIndex = 2;
			this.groupBox6.TabStop = false;
			this.groupBox6.Text = "Gramar Tree";
			// 
			// tvGrammar
			// 
			this.tvGrammar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tvGrammar.HideSelection = false;
			this.tvGrammar.Location = new System.Drawing.Point(3, 16);
			this.tvGrammar.Name = "tvGrammar";
			this.tvGrammar.Size = new System.Drawing.Size(204, 199);
			this.tvGrammar.TabIndex = 1;
			this.tvGrammar.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvGrammar_AfterSelect);
			this.tvGrammar.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvGrammar_NodeMouseClick);
			this.tvGrammar.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvGrammar_NodeMouseDoubleClick);
			// 
			// groupBox7
			// 
			this.groupBox7.Controls.Add(this.tbCompiled);
			this.groupBox7.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox7.Location = new System.Drawing.Point(0, 0);
			this.groupBox7.Name = "groupBox7";
			this.groupBox7.Size = new System.Drawing.Size(210, 272);
			this.groupBox7.TabIndex = 1;
			this.groupBox7.TabStop = false;
			this.groupBox7.Text = "Assembly";
			// 
			// tbCompiled
			// 
			this.tbCompiled.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbCompiled.HideSelection = false;
			this.tbCompiled.Location = new System.Drawing.Point(3, 16);
			this.tbCompiled.Name = "tbCompiled";
			this.tbCompiled.ReadOnly = true;
			this.tbCompiled.Size = new System.Drawing.Size(204, 253);
			this.tbCompiled.TabIndex = 0;
			this.tbCompiled.Text = "";
			this.tbCompiled.WordWrap = false;
			this.tbCompiled.SelectionChanged += new System.EventHandler(this.tbCompiled_SelectionChanged);
			this.tbCompiled.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tbCompiled_MouseDown);
			// 
			// splitContainer2
			// 
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer2.Panel1
			// 
			this.splitContainer2.Panel1.Controls.Add(this.butParseLoad);
			this.splitContainer2.Panel1.Controls.Add(this.butParseSave);
			this.splitContainer2.Panel1.Controls.Add(this.butAssembly);
			this.splitContainer2.Panel1.Controls.Add(this.cbPath);
			this.splitContainer2.Panel1.Controls.Add(this.tbParse_Source);
			// 
			// splitContainer2.Panel2
			// 
			this.splitContainer2.Panel2.Controls.Add(this.lbErrors);
			this.splitContainer2.Size = new System.Drawing.Size(605, 494);
			this.splitContainer2.SplitterDistance = 374;
			this.splitContainer2.TabIndex = 13;
			// 
			// butParseLoad
			// 
			this.butParseLoad.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butParseLoad.Location = new System.Drawing.Point(365, 4);
			this.butParseLoad.Name = "butParseLoad";
			this.butParseLoad.Size = new System.Drawing.Size(75, 23);
			this.butParseLoad.TabIndex = 18;
			this.butParseLoad.Text = "Load";
			this.butParseLoad.UseVisualStyleBackColor = true;
			this.butParseLoad.Click += new System.EventHandler(this.butParseLoad_Click);
			// 
			// butParseSave
			// 
			this.butParseSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butParseSave.Location = new System.Drawing.Point(446, 4);
			this.butParseSave.Name = "butParseSave";
			this.butParseSave.Size = new System.Drawing.Size(75, 23);
			this.butParseSave.TabIndex = 19;
			this.butParseSave.Text = "Save";
			this.butParseSave.UseVisualStyleBackColor = true;
			this.butParseSave.Click += new System.EventHandler(this.butParseSave_Click);
			// 
			// butAssembly
			// 
			this.butAssembly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butAssembly.Location = new System.Drawing.Point(527, 4);
			this.butAssembly.Name = "butAssembly";
			this.butAssembly.Size = new System.Drawing.Size(75, 23);
			this.butAssembly.TabIndex = 30;
			this.butAssembly.Text = "Assembly";
			this.butAssembly.UseVisualStyleBackColor = true;
			this.butAssembly.Click += new System.EventHandler(this.butAssembly_Click);
			// 
			// cbPath
			// 
			this.cbPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cbPath.FormattingEnabled = true;
			this.cbPath.Items.AddRange(new object[] {
            "\\html.txt",
            "\\htmlsafe.txt",
            "\\language.txt"});
			this.cbPath.Location = new System.Drawing.Point(1, 4);
			this.cbPath.Name = "cbPath";
			this.cbPath.Size = new System.Drawing.Size(358, 21);
			this.cbPath.TabIndex = 17;
			this.cbPath.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbPath_KeyDown);
			// 
			// tbParse_Source
			// 
			this.tbParse_Source.AcceptsTab = true;
			this.tbParse_Source.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbParse_Source.DetectUrls = false;
			this.tbParse_Source.EnableAutoDragDrop = true;
			this.tbParse_Source.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.tbParse_Source.HideSelection = false;
			this.tbParse_Source.Location = new System.Drawing.Point(1, 31);
			this.tbParse_Source.Name = "tbParse_Source";
			this.tbParse_Source.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.ForcedBoth;
			this.tbParse_Source.ShowSelectionMargin = true;
			this.tbParse_Source.Size = new System.Drawing.Size(602, 340);
			this.tbParse_Source.TabIndex = 16;
			this.tbParse_Source.Text = "";
			this.tbParse_Source.WordWrap = false;
			this.tbParse_Source.SelectionChanged += new System.EventHandler(this.tbParse_Source_SelectionChanged);
			this.tbParse_Source.TextChanged += new System.EventHandler(this.tbParse_Source_TextChanged);
			this.tbParse_Source.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbParse_Source_KeyDown);
			this.tbParse_Source.MouseDown += new System.Windows.Forms.MouseEventHandler(this.tbParse_Source_MouseDown);
			// 
			// lbErrors
			// 
			this.lbErrors.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lbErrors.FormattingEnabled = true;
			this.lbErrors.Location = new System.Drawing.Point(0, 0);
			this.lbErrors.Name = "lbErrors";
			this.lbErrors.Size = new System.Drawing.Size(605, 116);
			this.lbErrors.TabIndex = 9;
			this.lbErrors.SelectedIndexChanged += new System.EventHandler(this.lbErrors_SelectedIndexChanged);
			// 
			// splitContainer4
			// 
			this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer4.Location = new System.Drawing.Point(3, 3);
			this.splitContainer4.Name = "splitContainer4";
			// 
			// splitContainer4.Panel1
			// 
			this.splitContainer4.Panel1.Controls.Add(this.splitContainer2);
			// 
			// splitContainer4.Panel2
			// 
			this.splitContainer4.Panel2.Controls.Add(this.splitContainer1);
			this.splitContainer4.Size = new System.Drawing.Size(819, 494);
			this.splitContainer4.SplitterDistance = 605;
			this.splitContainer4.TabIndex = 31;
			// 
			// tabMain
			// 
			this.tabMain.Controls.Add(this.tabMain_Parse);
			this.tabMain.Controls.Add(this.tabMain_Source);
			this.tabMain.Controls.Add(this.tabMain_Debug);
			this.tabMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabMain.Location = new System.Drawing.Point(0, 0);
			this.tabMain.Name = "tabMain";
			this.tabMain.SelectedIndex = 0;
			this.tabMain.Size = new System.Drawing.Size(833, 526);
			this.tabMain.TabIndex = 16;
			// 
			// tabMain_Parse
			// 
			this.tabMain_Parse.Controls.Add(this.splitContainer4);
			this.tabMain_Parse.Location = new System.Drawing.Point(4, 22);
			this.tabMain_Parse.Name = "tabMain_Parse";
			this.tabMain_Parse.Padding = new System.Windows.Forms.Padding(3);
			this.tabMain_Parse.Size = new System.Drawing.Size(825, 500);
			this.tabMain_Parse.TabIndex = 0;
			this.tabMain_Parse.Text = "Parse source";
			this.tabMain_Parse.UseVisualStyleBackColor = true;
			// 
			// tabMain_Source
			// 
			this.tabMain_Source.Controls.Add(this.butRun_Run);
			this.tabMain_Source.Controls.Add(this.butRun_Save);
			this.tabMain_Source.Controls.Add(this.butRun_Load);
			this.tabMain_Source.Controls.Add(this.cbRun_Path);
			this.tabMain_Source.Controls.Add(this.splitContainer3);
			this.tabMain_Source.Location = new System.Drawing.Point(4, 22);
			this.tabMain_Source.Name = "tabMain_Source";
			this.tabMain_Source.Size = new System.Drawing.Size(825, 500);
			this.tabMain_Source.TabIndex = 2;
			this.tabMain_Source.Text = "Source & Run";
			this.tabMain_Source.UseVisualStyleBackColor = true;
			// 
			// butRun_Run
			// 
			this.butRun_Run.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butRun_Run.Enabled = false;
			this.butRun_Run.Location = new System.Drawing.Point(751, 4);
			this.butRun_Run.Name = "butRun_Run";
			this.butRun_Run.Size = new System.Drawing.Size(75, 23);
			this.butRun_Run.TabIndex = 29;
			this.butRun_Run.Text = "Run";
			this.butRun_Run.UseVisualStyleBackColor = true;
			this.butRun_Run.Click += new System.EventHandler(this.butRun_Run_Click);
			// 
			// butRun_Save
			// 
			this.butRun_Save.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butRun_Save.Location = new System.Drawing.Point(670, 4);
			this.butRun_Save.Name = "butRun_Save";
			this.butRun_Save.Size = new System.Drawing.Size(75, 23);
			this.butRun_Save.TabIndex = 28;
			this.butRun_Save.Text = "Save";
			this.butRun_Save.UseVisualStyleBackColor = true;
			this.butRun_Save.Click += new System.EventHandler(this.butRun_Save_Click);
			// 
			// butRun_Load
			// 
			this.butRun_Load.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butRun_Load.Location = new System.Drawing.Point(589, 4);
			this.butRun_Load.Name = "butRun_Load";
			this.butRun_Load.Size = new System.Drawing.Size(75, 23);
			this.butRun_Load.TabIndex = 27;
			this.butRun_Load.Text = "Load";
			this.butRun_Load.UseVisualStyleBackColor = true;
			this.butRun_Load.Click += new System.EventHandler(this.butRun_Load_Click);
			// 
			// cbRun_Path
			// 
			this.cbRun_Path.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.cbRun_Path.FormattingEnabled = true;
			this.cbRun_Path.Items.AddRange(new object[] {
            "\\html source.txt"});
			this.cbRun_Path.Location = new System.Drawing.Point(1, 4);
			this.cbRun_Path.Name = "cbRun_Path";
			this.cbRun_Path.Size = new System.Drawing.Size(582, 21);
			this.cbRun_Path.TabIndex = 26;
			this.cbRun_Path.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cbRun_Path_KeyDown);
			// 
			// splitContainer3
			// 
			this.splitContainer3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer3.Location = new System.Drawing.Point(4, 31);
			this.splitContainer3.Name = "splitContainer3";
			// 
			// splitContainer3.Panel1
			// 
			this.splitContainer3.Panel1.Controls.Add(this.tbRun_Source);
			// 
			// splitContainer3.Panel2
			// 
			this.splitContainer3.Panel2.Controls.Add(this.cbRun_ShowTree);
			this.splitContainer3.Panel2.Controls.Add(this.cbRun_Function);
			this.splitContainer3.Panel2.Controls.Add(this.tvRun_RetVal);
			this.splitContainer3.Size = new System.Drawing.Size(827, 469);
			this.splitContainer3.SplitterDistance = 554;
			this.splitContainer3.TabIndex = 25;
			// 
			// tbRun_Source
			// 
			this.tbRun_Source.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tbRun_Source.HideSelection = false;
			this.tbRun_Source.Location = new System.Drawing.Point(0, 0);
			this.tbRun_Source.Multiline = true;
			this.tbRun_Source.Name = "tbRun_Source";
			this.tbRun_Source.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.tbRun_Source.Size = new System.Drawing.Size(554, 469);
			this.tbRun_Source.TabIndex = 0;
			this.tbRun_Source.TextChanged += new System.EventHandler(this.tbRun_Source_TextChanged);
			// 
			// cbRun_ShowTree
			// 
			this.cbRun_ShowTree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.cbRun_ShowTree.AutoSize = true;
			this.cbRun_ShowTree.Location = new System.Drawing.Point(174, 37);
			this.cbRun_ShowTree.Name = "cbRun_ShowTree";
			this.cbRun_ShowTree.Size = new System.Drawing.Size(78, 17);
			this.cbRun_ShowTree.TabIndex = 2;
			this.cbRun_ShowTree.Text = "Show Tree";
			this.cbRun_ShowTree.UseVisualStyleBackColor = true;
			this.cbRun_ShowTree.CheckedChanged += new System.EventHandler(this.cbRun_ShowTree_CheckedChanged);
			// 
			// cbRun_Function
			// 
			this.cbRun_Function.Dock = System.Windows.Forms.DockStyle.Top;
			this.cbRun_Function.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cbRun_Function.FormattingEnabled = true;
			this.cbRun_Function.Location = new System.Drawing.Point(0, 0);
			this.cbRun_Function.Name = "cbRun_Function";
			this.cbRun_Function.Size = new System.Drawing.Size(269, 21);
			this.cbRun_Function.TabIndex = 1;
			// 
			// tvRun_RetVal
			// 
			this.tvRun_RetVal.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tvRun_RetVal.Location = new System.Drawing.Point(0, 27);
			this.tvRun_RetVal.Name = "tvRun_RetVal";
			this.tvRun_RetVal.Size = new System.Drawing.Size(264, 442);
			this.tvRun_RetVal.TabIndex = 0;
			this.tvRun_RetVal.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvRun_RetVal_BeforeExpand);
			this.tvRun_RetVal.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvRun_RetVal_AfterSelect);
			this.tvRun_RetVal.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvRun_RetVal_NodeMouseClick);
			// 
			// tabMain_Debug
			// 
			this.tabMain_Debug.Controls.Add(this.splitContainer8);
			this.tabMain_Debug.Location = new System.Drawing.Point(4, 22);
			this.tabMain_Debug.Name = "tabMain_Debug";
			this.tabMain_Debug.Size = new System.Drawing.Size(825, 500);
			this.tabMain_Debug.TabIndex = 3;
			this.tabMain_Debug.Text = "Debug";
			this.tabMain_Debug.UseVisualStyleBackColor = true;
			// 
			// splitContainer8
			// 
			this.splitContainer8.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer8.Location = new System.Drawing.Point(0, 0);
			this.splitContainer8.Name = "splitContainer8";
			this.splitContainer8.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer8.Panel1
			// 
			this.splitContainer8.Panel1.Controls.Add(this.splitContainer5);
			// 
			// splitContainer8.Panel2
			// 
			this.splitContainer8.Panel2.Controls.Add(this.splitContainer9);
			this.splitContainer8.Size = new System.Drawing.Size(825, 500);
			this.splitContainer8.SplitterDistance = 392;
			this.splitContainer8.TabIndex = 1;
			// 
			// splitContainer5
			// 
			this.splitContainer5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer5.Location = new System.Drawing.Point(0, 0);
			this.splitContainer5.Name = "splitContainer5";
			// 
			// splitContainer5.Panel1
			// 
			this.splitContainer5.Panel1.Controls.Add(this.splitContainer6);
			// 
			// splitContainer5.Panel2
			// 
			this.splitContainer5.Panel2.Controls.Add(this.groupBox4);
			this.splitContainer5.Size = new System.Drawing.Size(825, 392);
			this.splitContainer5.SplitterDistance = 506;
			this.splitContainer5.TabIndex = 0;
			// 
			// splitContainer6
			// 
			this.splitContainer6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer6.Location = new System.Drawing.Point(0, 0);
			this.splitContainer6.Name = "splitContainer6";
			this.splitContainer6.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitContainer6.Panel1
			// 
			this.splitContainer6.Panel1.Controls.Add(this.tsDebug);
			this.splitContainer6.Panel1.Controls.Add(this.splitContainer7);
			// 
			// splitContainer6.Panel2
			// 
			this.splitContainer6.Panel2.Controls.Add(this.groupBox3);
			this.splitContainer6.Size = new System.Drawing.Size(506, 392);
			this.splitContainer6.SplitterDistance = 136;
			this.splitContainer6.TabIndex = 0;
			// 
			// tsDebug
			// 
			this.tsDebug.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsDebug_Functions,
            this.tsDebug_Run,
            this.tsDebug_Pause,
            this.tsDebug_Stop,
            this.toolStripSeparator1,
            this.tsDebug_StepInto,
            this.tsDebug_StepOver,
            this.tsDebug_StepOut});
			this.tsDebug.Location = new System.Drawing.Point(0, 0);
			this.tsDebug.Name = "tsDebug";
			this.tsDebug.Size = new System.Drawing.Size(506, 25);
			this.tsDebug.TabIndex = 1;
			// 
			// tsDebug_Functions
			// 
			this.tsDebug_Functions.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.tsDebug_Functions.Name = "tsDebug_Functions";
			this.tsDebug_Functions.Size = new System.Drawing.Size(121, 25);
			// 
			// tsDebug_Run
			// 
			this.tsDebug_Run.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_Run.Enabled = false;
			this.tsDebug_Run.Image = global::Versatile_Parsing_Language.Properties.Resources.run;
			this.tsDebug_Run.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_Run.Name = "tsDebug_Run";
			this.tsDebug_Run.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_Run.Text = "Run";
			this.tsDebug_Run.Click += new System.EventHandler(this.tsDebug_Run_Click);
			// 
			// tsDebug_Pause
			// 
			this.tsDebug_Pause.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_Pause.Enabled = false;
			this.tsDebug_Pause.Image = global::Versatile_Parsing_Language.Properties.Resources.pause;
			this.tsDebug_Pause.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_Pause.Name = "tsDebug_Pause";
			this.tsDebug_Pause.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_Pause.Text = "Pause";
			// 
			// tsDebug_Stop
			// 
			this.tsDebug_Stop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_Stop.Enabled = false;
			this.tsDebug_Stop.Image = global::Versatile_Parsing_Language.Properties.Resources.stop;
			this.tsDebug_Stop.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_Stop.Name = "tsDebug_Stop";
			this.tsDebug_Stop.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_Stop.Text = "Stop";
			this.tsDebug_Stop.Click += new System.EventHandler(this.tsDebug_Stop_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// tsDebug_StepInto
			// 
			this.tsDebug_StepInto.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_StepInto.Enabled = false;
			this.tsDebug_StepInto.Image = global::Versatile_Parsing_Language.Properties.Resources.stepinto;
			this.tsDebug_StepInto.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_StepInto.Name = "tsDebug_StepInto";
			this.tsDebug_StepInto.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_StepInto.Text = "Step Into";
			this.tsDebug_StepInto.Click += new System.EventHandler(this.tsDebug_StepInto_Click);
			// 
			// tsDebug_StepOver
			// 
			this.tsDebug_StepOver.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_StepOver.Enabled = false;
			this.tsDebug_StepOver.Image = global::Versatile_Parsing_Language.Properties.Resources.stepover;
			this.tsDebug_StepOver.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_StepOver.Name = "tsDebug_StepOver";
			this.tsDebug_StepOver.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_StepOver.Text = "Step Over";
			this.tsDebug_StepOver.Click += new System.EventHandler(this.tsDebug_StepOver_Click);
			// 
			// tsDebug_StepOut
			// 
			this.tsDebug_StepOut.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsDebug_StepOut.Enabled = false;
			this.tsDebug_StepOut.Image = global::Versatile_Parsing_Language.Properties.Resources.stepout;
			this.tsDebug_StepOut.ImageTransparentColor = System.Drawing.Color.White;
			this.tsDebug_StepOut.Name = "tsDebug_StepOut";
			this.tsDebug_StepOut.Size = new System.Drawing.Size(23, 22);
			this.tsDebug_StepOut.Text = "Step Out";
			this.tsDebug_StepOut.Click += new System.EventHandler(this.tsDebug_StepOut_Click);
			// 
			// splitContainer7
			// 
			this.splitContainer7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer7.Location = new System.Drawing.Point(0, 28);
			this.splitContainer7.Name = "splitContainer7";
			// 
			// splitContainer7.Panel1
			// 
			this.splitContainer7.Panel1.Controls.Add(this.groupBox1);
			// 
			// splitContainer7.Panel2
			// 
			this.splitContainer7.Panel2.Controls.Add(this.groupBox2);
			this.splitContainer7.Size = new System.Drawing.Size(506, 108);
			this.splitContainer7.SplitterDistance = 251;
			this.splitContainer7.TabIndex = 0;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.lvDebug_FS);
			this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox1.Location = new System.Drawing.Point(0, 0);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(251, 108);
			this.groupBox1.TabIndex = 0;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Function Stack";
			// 
			// lvDebug_FS
			// 
			this.lvDebug_FS.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.lbDebug_FS_Functions});
			this.lvDebug_FS.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvDebug_FS.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.lvDebug_FS.Location = new System.Drawing.Point(3, 16);
			this.lvDebug_FS.MultiSelect = false;
			this.lvDebug_FS.Name = "lvDebug_FS";
			this.lvDebug_FS.Size = new System.Drawing.Size(245, 89);
			this.lvDebug_FS.TabIndex = 0;
			this.lvDebug_FS.UseCompatibleStateImageBehavior = false;
			this.lvDebug_FS.View = System.Windows.Forms.View.Details;
			this.lvDebug_FS.ItemActivate += new System.EventHandler(this.lvDebug_FS_ItemActivate);
			// 
			// lbDebug_FS_Functions
			// 
			this.lbDebug_FS_Functions.Text = "Functions";
			this.lbDebug_FS_Functions.Width = 235;
			// 
			// groupBox2
			// 
			this.groupBox2.Controls.Add(this.lvDebug_BS);
			this.groupBox2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox2.Location = new System.Drawing.Point(0, 0);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(251, 108);
			this.groupBox2.TabIndex = 1;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = "Block Stack";
			// 
			// lvDebug_BS
			// 
			this.lvDebug_BS.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.lbDebug_BS_Blocks});
			this.lvDebug_BS.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvDebug_BS.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.lvDebug_BS.Location = new System.Drawing.Point(3, 16);
			this.lvDebug_BS.MultiSelect = false;
			this.lvDebug_BS.Name = "lvDebug_BS";
			this.lvDebug_BS.Size = new System.Drawing.Size(245, 89);
			this.lvDebug_BS.TabIndex = 0;
			this.lvDebug_BS.UseCompatibleStateImageBehavior = false;
			this.lvDebug_BS.View = System.Windows.Forms.View.Details;
			this.lvDebug_BS.ItemActivate += new System.EventHandler(this.lvDebug_BS_ItemActivate);
			// 
			// lbDebug_BS_Blocks
			// 
			this.lbDebug_BS_Blocks.Text = "Blocks";
			this.lbDebug_BS_Blocks.Width = 239;
			// 
			// groupBox3
			// 
			this.groupBox3.Controls.Add(this.lvDebug_Runnables);
			this.groupBox3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox3.Location = new System.Drawing.Point(0, 0);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.Size = new System.Drawing.Size(506, 252);
			this.groupBox3.TabIndex = 1;
			this.groupBox3.TabStop = false;
			this.groupBox3.Text = "Block";
			// 
			// lvDebug_Runnables
			// 
			this.lvDebug_Runnables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chDebug_Runnables_Runnables});
			this.lvDebug_Runnables.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvDebug_Runnables.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvDebug_Runnables.Location = new System.Drawing.Point(3, 16);
			this.lvDebug_Runnables.Name = "lvDebug_Runnables";
			this.lvDebug_Runnables.Size = new System.Drawing.Size(500, 233);
			this.lvDebug_Runnables.SmallImageList = this.ilMain;
			this.lvDebug_Runnables.TabIndex = 0;
			this.lvDebug_Runnables.UseCompatibleStateImageBehavior = false;
			this.lvDebug_Runnables.View = System.Windows.Forms.View.Details;
			this.lvDebug_Runnables.MouseClick += new System.Windows.Forms.MouseEventHandler(this.lvDebug_Runnables_MouseClick);
			// 
			// chDebug_Runnables_Runnables
			// 
			this.chDebug_Runnables_Runnables.Text = "Runnables";
			this.chDebug_Runnables_Runnables.Width = 333;
			// 
			// ilMain
			// 
			this.ilMain.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilMain.ImageStream")));
			this.ilMain.TransparentColor = System.Drawing.Color.Transparent;
			this.ilMain.Images.SetKeyName(0, "current.png");
			this.ilMain.Images.SetKeyName(1, "onfailgoto.ICO");
			// 
			// groupBox4
			// 
			this.groupBox4.Controls.Add(this.tvDebug_Variables);
			this.groupBox4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox4.Location = new System.Drawing.Point(0, 0);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.Size = new System.Drawing.Size(315, 392);
			this.groupBox4.TabIndex = 0;
			this.groupBox4.TabStop = false;
			this.groupBox4.Text = "Variables";
			// 
			// tvDebug_Variables
			// 
			this.tvDebug_Variables.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tvDebug_Variables.Location = new System.Drawing.Point(3, 16);
			this.tvDebug_Variables.Name = "tvDebug_Variables";
			this.tvDebug_Variables.Size = new System.Drawing.Size(309, 373);
			this.tvDebug_Variables.TabIndex = 0;
			this.tvDebug_Variables.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvRun_RetVal_BeforeExpand);
			// 
			// splitContainer9
			// 
			this.splitContainer9.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer9.Location = new System.Drawing.Point(0, 0);
			this.splitContainer9.Name = "splitContainer9";
			// 
			// splitContainer9.Panel1
			// 
			this.splitContainer9.Panel1.Controls.Add(this.groupBox5);
			this.splitContainer9.Size = new System.Drawing.Size(825, 104);
			this.splitContainer9.SplitterDistance = 358;
			this.splitContainer9.TabIndex = 1;
			// 
			// groupBox5
			// 
			this.groupBox5.Controls.Add(this.dgvDebug_Block);
			this.groupBox5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBox5.Location = new System.Drawing.Point(0, 0);
			this.groupBox5.Name = "groupBox5";
			this.groupBox5.Size = new System.Drawing.Size(358, 104);
			this.groupBox5.TabIndex = 0;
			this.groupBox5.TabStop = false;
			this.groupBox5.Text = "Block";
			// 
			// dgvDebug_Block
			// 
			this.dgvDebug_Block.AllowUserToAddRows = false;
			this.dgvDebug_Block.AllowUserToDeleteRows = false;
			this.dgvDebug_Block.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dgvDebug_Block.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Column1,
            this.Column2});
			this.dgvDebug_Block.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dgvDebug_Block.Location = new System.Drawing.Point(3, 16);
			this.dgvDebug_Block.Name = "dgvDebug_Block";
			this.dgvDebug_Block.ReadOnly = true;
			this.dgvDebug_Block.RowHeadersVisible = false;
			this.dgvDebug_Block.Size = new System.Drawing.Size(352, 85);
			this.dgvDebug_Block.TabIndex = 0;
			// 
			// Column1
			// 
			this.Column1.HeaderText = "Name";
			this.Column1.Name = "Column1";
			this.Column1.ReadOnly = true;
			this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Column2
			// 
			this.Column2.HeaderText = "Value";
			this.Column2.Name = "Column2";
			this.Column2.ReadOnly = true;
			this.Column2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			this.Column2.Width = 250;
			// 
			// cmsRTF
			// 
			this.cmsRTF.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmsRTF_Breakpoint});
			this.cmsRTF.Name = "cmsRTF";
			this.cmsRTF.Size = new System.Drawing.Size(172, 26);
			// 
			// cmsRTF_Breakpoint
			// 
			this.cmsRTF_Breakpoint.Name = "cmsRTF_Breakpoint";
			this.cmsRTF_Breakpoint.Size = new System.Drawing.Size(171, 22);
			this.cmsRTF_Breakpoint.Text = "Toggle Breakpoint";
			this.cmsRTF_Breakpoint.Click += new System.EventHandler(this.cmsRTF_Breakpoint_Click);
			// 
			// timApplyParseSyntaxColoring
			// 
			this.timApplyParseSyntaxColoring.Interval = 250;
			this.timApplyParseSyntaxColoring.Tick += new System.EventHandler(this.timApplyParseSyntaxColoring_Tick);
			// 
			// butNewWindow
			// 
			this.butNewWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.butNewWindow.Location = new System.Drawing.Point(748, 0);
			this.butNewWindow.Name = "butNewWindow";
			this.butNewWindow.Size = new System.Drawing.Size(81, 23);
			this.butNewWindow.TabIndex = 17;
			this.butNewWindow.Text = "New Window";
			this.butNewWindow.UseVisualStyleBackColor = true;
			this.butNewWindow.Click += new System.EventHandler(this.butNewWindow_Click);
			// 
			// timParseUndoIdleWait
			// 
			this.timParseUndoIdleWait.Interval = 1000;
			this.timParseUndoIdleWait.Tick += new System.EventHandler(this.timParseUndoWait_Tick);
			// 
			// timParseUndoMaxWait
			// 
			this.timParseUndoMaxWait.Interval = 5000;
			this.timParseUndoMaxWait.Tick += new System.EventHandler(this.timParseUndoWait_Tick);
			// 
			// frmMain
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(833, 548);
			this.Controls.Add(this.butNewWindow);
			this.Controls.Add(this.tabMain);
			this.Controls.Add(this.ssMain);
			this.KeyPreview = true;
			this.Name = "frmMain";
			this.Text = "Grammar View";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmMain_KeyDown);
			this.ssMain.ResumeLayout(false);
			this.ssMain.PerformLayout();
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.groupBox6.ResumeLayout(false);
			this.groupBox7.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.splitContainer4.Panel1.ResumeLayout(false);
			this.splitContainer4.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
			this.splitContainer4.ResumeLayout(false);
			this.tabMain.ResumeLayout(false);
			this.tabMain_Parse.ResumeLayout(false);
			this.tabMain_Source.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel1.PerformLayout();
			this.splitContainer3.Panel2.ResumeLayout(false);
			this.splitContainer3.Panel2.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
			this.splitContainer3.ResumeLayout(false);
			this.tabMain_Debug.ResumeLayout(false);
			this.splitContainer8.Panel1.ResumeLayout(false);
			this.splitContainer8.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer8)).EndInit();
			this.splitContainer8.ResumeLayout(false);
			this.splitContainer5.Panel1.ResumeLayout(false);
			this.splitContainer5.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer5)).EndInit();
			this.splitContainer5.ResumeLayout(false);
			this.splitContainer6.Panel1.ResumeLayout(false);
			this.splitContainer6.Panel1.PerformLayout();
			this.splitContainer6.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer6)).EndInit();
			this.splitContainer6.ResumeLayout(false);
			this.tsDebug.ResumeLayout(false);
			this.tsDebug.PerformLayout();
			this.splitContainer7.Panel1.ResumeLayout(false);
			this.splitContainer7.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer7)).EndInit();
			this.splitContainer7.ResumeLayout(false);
			this.groupBox1.ResumeLayout(false);
			this.groupBox2.ResumeLayout(false);
			this.groupBox3.ResumeLayout(false);
			this.groupBox4.ResumeLayout(false);
			this.splitContainer9.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer9)).EndInit();
			this.splitContainer9.ResumeLayout(false);
			this.groupBox5.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.dgvDebug_Block)).EndInit();
			this.cmsRTF.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.StatusStrip ssMain;
		private System.Windows.Forms.ToolStripStatusLabel ssMain_Status;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.TreeView tvGrammar;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.ListBox lbErrors;
		private System.Windows.Forms.TabControl tabMain;
		private System.Windows.Forms.TabPage tabMain_Parse;
		private System.Windows.Forms.ComboBox cbPath;
		private FixedRichTextBox tbParse_Source;
		private System.Windows.Forms.Button butParseSave;
		private System.Windows.Forms.Button butParseLoad;
		private System.Windows.Forms.TabPage tabMain_Source;
		private FixedRichTextBox tbCompiled;
		private System.Windows.Forms.Button butRun_Run;
		private System.Windows.Forms.Button butRun_Save;
		private System.Windows.Forms.Button butRun_Load;
		private System.Windows.Forms.ComboBox cbRun_Path;
		private System.Windows.Forms.SplitContainer splitContainer3;
		private System.Windows.Forms.TextBox tbRun_Source;
		private System.Windows.Forms.TreeView tvRun_RetVal;
		private System.Windows.Forms.Button butAssembly;
		private System.Windows.Forms.SplitContainer splitContainer4;
		private System.Windows.Forms.TabPage tabMain_Debug;
		private System.Windows.Forms.SplitContainer splitContainer5;
		private System.Windows.Forms.SplitContainer splitContainer6;
		private System.Windows.Forms.SplitContainer splitContainer7;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.GroupBox groupBox2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.TreeView tvDebug_Variables;
		private System.Windows.Forms.ListView lvDebug_Runnables;
		private System.Windows.Forms.ColumnHeader chDebug_Runnables_Runnables;
		private System.Windows.Forms.ToolStrip tsDebug;
		private System.Windows.Forms.ToolStripButton tsDebug_Run;
		private System.Windows.Forms.ToolStripButton tsDebug_Pause;
		private System.Windows.Forms.ToolStripButton tsDebug_Stop;
		private System.Windows.Forms.ToolStripButton tsDebug_StepInto;
		private System.Windows.Forms.ToolStripButton tsDebug_StepOver;
		private System.Windows.Forms.ToolStripButton tsDebug_StepOut;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ComboBox cbRun_Function;
		private System.Windows.Forms.ToolStripComboBox tsDebug_Functions;
		private System.Windows.Forms.ContextMenuStrip cmsRTF;
		private System.Windows.Forms.ToolStripMenuItem cmsRTF_Breakpoint;
		private System.Windows.Forms.ImageList ilMain;
		private System.Windows.Forms.SplitContainer splitContainer8;
		private System.Windows.Forms.DataGridView dgvDebug_Block;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column1;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
		private System.Windows.Forms.SplitContainer splitContainer9;
		private System.Windows.Forms.GroupBox groupBox5;
		private System.Windows.Forms.GroupBox groupBox6;
		private System.Windows.Forms.GroupBox groupBox7;
		private System.Windows.Forms.Timer timApplyParseSyntaxColoring;
		private System.Windows.Forms.CheckBox cbRun_ShowTree;
		private System.Windows.Forms.ListView lvDebug_FS;
		private System.Windows.Forms.ListView lvDebug_BS;
		private System.Windows.Forms.ColumnHeader lbDebug_FS_Functions;
		private System.Windows.Forms.ColumnHeader lbDebug_BS_Blocks;
		private System.Windows.Forms.Button butNewWindow;
		private System.Windows.Forms.ToolStripStatusLabel ssMain_RunStatus;
		private System.Windows.Forms.Timer timParseUndoIdleWait;
		private System.Windows.Forms.Timer timParseUndoMaxWait;
    }
}

