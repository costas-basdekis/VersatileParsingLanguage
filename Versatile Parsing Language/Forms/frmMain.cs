using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VPL;
using VPL.Compile;
using VPL.Execute;
using VPL.Parse;
using Block = VPL.Parse.Block;
using TreeNode = System.Windows.Forms.TreeNode;

namespace Versatile_Parsing_Language
{
	public partial class frmMain : Form
	{
		private readonly IDE _ide = new IDE();
		private readonly Parser _parser;
		private readonly Compiler _compiler;
		private GrammarTree _gt;
		private ProgramStack _ps, _run_ps;
		private readonly UndoHistory<TextPortion> _parse_source_undo_history = new UndoHistory<TextPortion>(40);
		private bool _parse_source_undoing;
		private string _run_source = string.Empty;
		private readonly TextPortionTree<IDE.Annotation> _source_tp, _compile_tp;
		private bool tbSource_Editing, tbCompiled_Editing, tbSource_Selecting, tbCompiled_Selecting;
		private IDE.Annotation _selectedAnnotation;

		private void butNewWindow_Click(object sender, EventArgs e)
		{
			(new frmMain()).Show();
		}
		//Form
		public frmMain()
		{
			InitializeComponent();

			//Load files
// ReSharper disable CoVariantArrayConversion
			string s = Properties.Settings.Default.Source_Paths;
			if (!string.IsNullOrEmpty(s))
			{
				cbPath.Items.Clear();
				cbPath.Items.AddRange(s.Split('\n'));
			}
			cbPath.SelectedIndex = 0;
			s = Properties.Settings.Default.Run_Paths;
			if (s != "")
			{
				cbRun_Path.Items.Clear();
				cbRun_Path.Items.AddRange(s.Split('\n'));
			}
			cbRun_Path.SelectedIndex = 0;
// ReSharper restore CoVariantArrayConversion

			_ide.SourceRTF = tbParse_Source;
			_ide.CompiledRTF = tbCompiled;
			_ide.GetSourceFile = GetSourceFile;
			_parser = _ide.Parser;
			_compiler = _ide.Compiler;
			_source_tp = _ide.SourceAnnotations;
			_compile_tp = _ide.CompiledAnnotations;

			tbParse_Source_TextChanged(null, null);
		}
		private void frmMain_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
			case Keys.F5:
				if (tsDebug_Run.Enabled)
					tsDebug_Run_Click(sender, null);
				break;
			case Keys.F8:
				if (tsDebug_StepInto.Enabled)
					tsDebug_StepInto_Click(sender, null);
				break;
			case Keys.F10:
				if (tsDebug_StepOver.Enabled)
					tsDebug_StepOver_Click(sender, null);
				break;
			case Keys.F11:
				if (tsDebug_StepOut.Enabled)
					tsDebug_StepOut_Click(sender, null);
				break;
			default:
				return;
			}

			e.SuppressKeyPress = true;
		}
		private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
		{
			Properties.Settings.Default.Source_Paths = cbPath.Items.Cast<string>().Aggregate(string.Empty, (current, it) => string.Format("{0}\n{1}", current, it)).Substring(1);
			Properties.Settings.Default.Run_Paths = cbRun_Path.Items.Cast<string>().Aggregate(string.Empty, (current, it) => string.Format("{0}\n{1}", current, it)).Substring(1);
			Properties.Settings.Default.Save();
		}

		//Get actual file paths
		string GetFilePath(string Path, string ParentPath = null)
		{
			try
			{
				if (Path.Substring(0, 1) == @"\")
					return System.IO.Path.Combine(ParentPath ?? Environment.CurrentDirectory, Path.Substring(1));
				else
					return Path;
			}
			catch (Exception)
			{
				return null;
			}
		}
		private bool GetSourceFile(object RequesterUID, string FileName, out string newSource, out object newUID, out string newFileName)
		{
			newUID = null;
			if (FileName.IndexOf(':') == -1)
				FileName = "\\" + FileName;
			newFileName = GetFilePath(FileName);

			//Try load the file
			try
			{
				newSource = System.IO.File.ReadAllText(newFileName);
				return true;
			}
			catch (Exception)
			{
				var i1 = FileName.LastIndexOf('.');
				var i2 = FileName.LastIndexOf('\\');

				if ((i1 == -1) || (i1 < i2))
				{
					FileName += ".txt";
					newFileName = GetFilePath(FileName);
					//Try load the file
					try
					{
						newSource = System.IO.File.ReadAllText(newFileName);
						return true;
					}
					catch (Exception)
					{
						newSource = null;
						return false;
					}
				}
				else
				{
					newSource = null;
					return false;
				}
			}

		}

		//Source IO
		private void cbPath_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				butParseLoad_Click(null, null);
				e.SuppressKeyPress = true;
			}
			else if ((e.KeyCode == Keys.Delete) && (e.Shift))
			{
				if (cbPath.SelectedIndex >= 0)
					if (MessageBox.Show("Are you sure to delete this entry?", "Delete entry", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.OK)
					{
						cbPath.Items.RemoveAt(cbPath.SelectedIndex);
						cbPath.Text = string.Empty;
					}
				e.SuppressKeyPress = true;
			}
		}
		private void butParseLoad_Click(object sender, EventArgs e) 
		{
			string s;

			//Try load the file
			try
			{
				s = System.IO.File.ReadAllText(GetFilePath(cbPath.Text));
			}
			catch (Exception)
			{
				MessageBox.Show("Could not load the file");
				return;
			}

			//Add to the list
			if (cbPath.SelectedIndex < 0)
				if (cbPath.Items.Contains(cbPath.Text))
					cbPath.SelectedIndex = cbPath.Items.IndexOf(cbPath.Text);
				else
				{
					cbPath.SelectedIndex = cbPath.Items.Add(cbPath.Text);
					Properties.Settings.Default.Save();
				}

			//Show the text
			_parse_source_undo_history.Clear();
			tbParse_Source.Text = s;
		}
		private void butParseSave_Click(object sender, EventArgs e)
		{
			if (tbParse_Source.Text.Length == 0)
				if (MessageBox.Show("Text is empty. Are you sure?", "Save empty file", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
					return;
			if (MessageBox.Show("Are you sure?", "Save file", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
				return;

			//Add to the list
			if (cbPath.SelectedIndex < 0)
				if (cbPath.Items.Contains(cbPath.Text))
					cbPath.SelectedIndex = cbPath.Items.IndexOf(cbPath.Text);
				else
				{
					cbPath.SelectedIndex = cbPath.Items.Add(cbPath.Text);
					Properties.Settings.Default.Save();
				}

			var re = new Regex(@"(?<!\r)\n(?!\r)");
			System.IO.File.WriteAllText(GetFilePath(cbPath.Text), re.Replace(tbParse_Source.Text, "\r\n"));
		}

		//Populate parse tree
		private void PopulateTreeView()
		{
			tvGrammar.BeginUpdate();
			tvGrammar.Nodes.Clear();

			TreeNode nd;

			foreach (var fn in _gt.Functions)
			{
				nd = tvGrammar.Nodes.Add(fn.Name);
				fn.Annotation.TreeViewNode = nd;
				nd.Tag = fn.Annotation;
				fn.Block.GetReturnType();
				PopulateBlock(nd, fn.Block);
				nd.Text += ":" + fn.Block.ReturnType.ToString();
			}

			if (tvGrammar.Nodes.Count > 0)
				tvGrammar.Nodes[0].EnsureVisible();

			tvGrammar.EndUpdate();
		}
		private void PopulateBlock(TreeNode Node, Block Block)
		{
			if (Block.Elements.Count == 0)
				Node.Text += "<Empty block>";
			else
			{
				TreeNodeCollection nds = Node.Nodes;
	
				foreach (Element el in Block.Elements)
					PopulateElement(el, nds);

				Node.Expand();
			}
		}
		private void PopulateElement(Element Item, TreeNodeCollection Nodes)
		{
			TreeNode nd;

			if (Item == null)
				nd = Nodes.Add("<Nothing>");
			else if (Item is Block)
			{
				Block bl = (Block)Item;

				nd = Nodes.Add((bl.Name != "") ? "#" + bl.Name + "#" : "");
				switch (bl.Type)
				{
				case Block.eType.Multi:
					nd.Text += "[Multi Block]";
					break;
				default:
					nd.Text += "[Block]";
					break;
				}
				PopulateBlock(nd, bl);
			}
			else if (Item is FunctionCall)
			{
				var fc = Item as FunctionCall;
				nd = Nodes.Add("Call " + fc.Name + "(" + ((fc.Arguments.Count > 0) ? fc.Arguments.Aggregate("", (current, e) => current + ("," + e)).Substring(1) : "") + ")");
				foreach (var el in fc.Arguments)
					PopulateElement(el, nd.Nodes);
			}
			else if (Item is TextSearch)
			{
				nd = Nodes.Add("Match '" + ((TextSearch)Item).Pattern + "'");
				TextSearch ts = (TextSearch)Item;
				switch (ts.Type)
				{
				case TextSearch.eType.Find:
					nd.Text += " (Find)";
					break;
				case TextSearch.eType.FindReverse:
					nd.Text += " (Find reverse)";
					break;
				case TextSearch.eType.RegularExpression:
					nd.Text += " (Regular expression)" + ts.Tree.Aggregate("", (current, v) => current + ("." + v));
					break;
				}
			}
			else if (Item is Literal)
			{
				Literal lt = (Literal)Item;

				switch (lt.Type)
				{
				case Literal.eType.Text:
					nd = Nodes.Add("Text literal '" + lt.AssemblyDisplayText + "'");
					break;
				case Literal.eType.Number:
					nd = Nodes.Add("Number literal '" + lt.Number.ToString() + "'");
					break;
				default:
					nd = Nodes.Add("Unknown literal '" + lt + "'");
					break;
				}
			}
			else if (Item is Variable)
			{
				var vr = (Variable)Item;
				string s;

				switch (vr.Type)
				{
				case Variable.eType.Variable:
					s = "%" + vr.Name + "%" + vr.Nodes.Aggregate("", (current, v) => current + ("." + v));
					break;
				case Variable.eType.Tree:
					if (vr.Nodes.Count == 0)
						s = "Tree";
					else
						s = vr.Nodes.Aggregate("", (current, v) => current + ("." + v)).Substring(1);
					break;
				case Variable.eType.RetVal:
					s = "RetVal" + vr.Nodes.Aggregate("", (current, v) => current + ("." + v));
					break;
				case Variable.eType.Setting:
					s = "@" + vr.Name + "@" + vr.Nodes.Aggregate("", (current, v) => current + ("." + v));
					break;
				case Variable.eType.Skipped:
					s = "Skipped";
					break;
				case Variable.eType.Source:
					s = "Source";
					break;
				case Variable.eType.New:
					s = "New";
					break;
				default:
					s = "???" + vr.Nodes.Aggregate("", (current, v) => current + ("." + v));
					break;
				}

				nd = Nodes.Add(s);
				vr.Nodes.ForEach(v => PopulateElement(v.Key, nd.Nodes));
			}
			else if (Item is BinaryOperator)
			{
				var cmp = (BinaryOperator)Item;
				switch (cmp.Type)
				{
				case BinaryOperator.eType.Equal:
					nd = Nodes.Add("Equal");
					break;
				case BinaryOperator.eType.NotEqual:
					nd = Nodes.Add("Not Equal");
					break;
				case BinaryOperator.eType.ReferenceCopy:
					nd = Nodes.Add("Reference Copy");
					break;
				case BinaryOperator.eType.ReferenceEqual:
					nd = Nodes.Add("Reference Equal");
					break;
				case BinaryOperator.eType.ReferenceNotEqual:
					nd = Nodes.Add("Reference Not Equal");
					break;
				case BinaryOperator.eType.TildeAppend:
					nd = Nodes.Add("Tilde Append");
					break;
				case BinaryOperator.eType.Tilde:
					nd = Nodes.Add("Tilde");
					break;
				case BinaryOperator.eType.Pipe:
					nd = Nodes.Add("Pipe");
					break;
				case BinaryOperator.eType.TextAppend:
					nd = Nodes.Add("Text append");
					break;
				case BinaryOperator.eType.TreeAppend:
					nd = Nodes.Add("Tree append");
					break;
				default:
					nd = Nodes.Add("Unknown Binary Operator");
					break;
				}
				PopulateElement(cmp.lhs, nd.Nodes);
				PopulateElement(cmp.rhs, nd.Nodes);
				nd.Expand();
			}
			else if (Item is ControlFlow)
			{
				ControlFlow ret = (ControlFlow)Item;
				nd = Nodes.Add(ret.Type.ToString());
				if (ret.Name != null)
					nd.Text += ":" + ret.Name;
			}
			else
				nd = Nodes.Add("<Undefined>");

			if (Item != null)
			{
				string q = Item.Quantifier.ToString();
				if (q != "") nd.Text += q;

				nd.Text += ":" + Item.ReturnType.ToString();

				Item.Annotation.TreeViewNode = nd;
				nd.Tag = Item.Annotation;
			}
		}

		//Source textbox events
		private void tbParse_Source_TextChanged(object sender, EventArgs e)
		{
			if (tbSource_Editing)
				return;

			timApplyParseSyntaxColoring.Enabled = false;

			ssMain_Status.Text = "Parsing...";

			int duration;

			//Parse
			duration = Environment.TickCount;
			_ide.SourceText = tbParse_Source.Text;
			_parser.Parse();
			_gt = _ide.GrammarTree;
			duration = Environment.TickCount - duration;
			ssMain_Status.Text = "Parse (" + duration + ")";

			//Compile
			duration = Environment.TickCount;
			tbCompiled.BeginUpdate();
			_compiler.Compile();
			tbCompiled.EndUpdate();
			duration = Environment.TickCount - duration;
			ssMain_Status.Text += ", compile (" + duration + ")";

			//Populate functions
			var compile_success = (_gt.ParseErrors.Count == 0) && (_gt.Functions.Count > 0);
			cbRun_Function.Items.Clear();
			tsDebug_Functions.Items.Clear();
			if (compile_success)
			{
				var et = _ide.ExecutionTree;
				foreach (var fun in et.Functions)
					if (fun.Value.ArgumentsCount == 0)
					{
						cbRun_Function.Items.Add(fun.Key);
						tsDebug_Functions.Items.Add(fun.Key);
					}
				if (cbRun_Function.Items.Count > 0)
					cbRun_Function.SelectedIndex = 0;
				if (tsDebug_Functions.Items.Count > 0)
					tsDebug_Functions.SelectedIndex = 0;
			}

			//Enable/disable buttons
			tsDebug_Run.Enabled = compile_success;
			tsDebug_StepInto.Enabled = compile_success;
			tsDebug_StepOver.Enabled = compile_success;
			butRun_Run.Enabled = compile_success;

			//Populate errors
			lbErrors.Items.Clear();
			foreach (ParseError err in _gt.ParseErrors)
				lbErrors.Items.Add(err);
			if (_gt.ParseErrors.Count > 0)
				lbErrors.Items.Add("Recoverable parsing errors occurred");
			else
				lbErrors.Items.Add("Great parsing success");
			foreach (ParseError err in _ide.ExecutionTree.ParseErrors)
				lbErrors.Items.Add(err);
			if (_gt.ParseErrors.Count > 0)
				lbErrors.Items.Add("Recoverable compiling errors occurred");
			else
				lbErrors.Items.Add("Great compiling success");

			//Enable/disable buttons
			butAssembly.Enabled = (_gt.Functions.Count > 0);

			timApplyParseSyntaxColoring.Enabled = true;

			if (!_parse_source_undoing)
			{
				//Start timers so that we don't create an undo action every time a button is pushed
				timParseUndoIdleWait.Enabled = false;
				timParseUndoIdleWait.Enabled = true;
				timParseUndoMaxWait.Enabled = true;
			}
		}
		private void timApplyParseSyntaxColoring_Tick(object sender, EventArgs e)
		{
			timApplyParseSyntaxColoring.Enabled = false;

			int duration;

			//Apply colouring
			duration = Environment.TickCount;
			tbSource_Editing = true;
			_ide.ApplyParseSyntaxColoring();
			tbSource_Editing = false;
			duration = Environment.TickCount - duration;
			ssMain_Status.Text += ", syntax (" + duration + ")";

		}
		private void tbParse_Source_KeyDown(object sender, KeyEventArgs e)
		{
			if ((e.KeyCode == Keys.Z) && (e.Modifiers == Keys.Control))
			{
				//Create an undo action if necessary
				if (timParseUndoIdleWait.Enabled)
					timParseUndoWait_Tick(null, null);
				
				//Aply action if possible
				if (_parse_source_undo_history.UndoItems > 0)
				{
					_parse_source_undoing = true;
					var tp = _parse_source_undo_history.Undo();
					tbParse_Source.Text = tp.Text;
					tp.ToRTB(tbParse_Source);
					_parse_source_undoing = false;
				}
				
				//Don't let the textbox do its own thing
				e.SuppressKeyPress = true;
			}
			else if ((e.KeyCode == Keys.Y) && (e.Modifiers == Keys.Control))
			{
				//Create an undo action if necessary
				if (timParseUndoIdleWait.Enabled)
					timParseUndoWait_Tick(null, null);
				
				//Aply action if possible
				if (_parse_source_undo_history.RedoItems > 0)
				{
					_parse_source_undoing = true;
					var tp = _parse_source_undo_history.Redo();
					tbParse_Source.Text = tp.Text;
					tp.ToRTB(tbParse_Source);
					_parse_source_undoing = false;
				}
				
				//Don't let the textbox do its own thing
				e.SuppressKeyPress = true;
			}
		}
		private void timParseUndoWait_Tick(object sender, EventArgs e)
		{
			//Disable timers
			timParseUndoIdleWait.Enabled = false;
			timParseUndoMaxWait.Enabled = false;

			//Create undo action
			TextPortion tp = tbParse_Source.Text;
			tp.FromRTB(tbParse_Source);
			_parse_source_undo_history.Change(tp);
		}

		//Synchronize textbox, parse tree and assemlby view selection
		private void butAssembly_Click(object sender, EventArgs e)
		{
			if (tbSource_Editing)
				return;
			int total_duration, duration;

			total_duration = Environment.TickCount;
			
			//Get tree
			duration = Environment.TickCount;
			PopulateTreeView();
			duration = Environment.TickCount - duration;
			ssMain_Status.Text = "Got tree (" + duration + "ms)";

			if (_gt.ParseErrors.Count > 0)
				return;

			tbCompiled_Editing = true;

			//Apply colouring
			duration = Environment.TickCount;
			_ide.ApplyCompiledSyntaxColoring();
			duration = Environment.TickCount - duration;
			ssMain_Status.Text += " and assembly (" + duration + "ms)";

			total_duration = Environment.TickCount - total_duration;

			ssMain_Status.Text += " Total: " + total_duration + "ms";

			tbCompiled_Editing = false;
		}
		private void tbParse_Source_SelectionChanged(object sender, EventArgs e)
		{
			if (tbParse_Source.SelectionLength != 0)
				return;

			IDE.Annotation an;

			if (!_source_tp.Item(tbParse_Source.SelectionStart, out an))
				return;

			tbSource_Selecting = true;
			if (an != null)
				tvGrammar.SelectedNode = an.TreeViewNode;
			else
				tvGrammar.SelectedNode = null;
			tbSource_Selecting = false;
		}
		private void tbParse_Source_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (_source_tp.Item(tbParse_Source.GetCharIndexFromPosition(e.Location), out _selectedAnnotation))
				{
					tvGrammar.SelectedNode = _selectedAnnotation.TreeViewNode;
					if (_selectedAnnotation.FirstRunnable != null)
						_selectedAnnotation = _selectedAnnotation.FirstRunnable.Annotation;
					if (_selectedAnnotation.FirstRunnable != null)
						cmsRTF.Show(tbParse_Source, e.X, e.Y);
				}
			}
		}
		private void tvGrammar_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node == null) return;
			if (e.Node.Tag == null) return;

			if (tabMain.SelectedTab == tabMain_Parse)
			{
				if (e.Node.Tag != null)
				{
					IDE.Annotation an = (IDE.Annotation)e.Node.Tag;
					if (an.Element != null)
						an = an.Element.Annotation;
					if ((!tbSource_Selecting) && an.SourcePosition.IsInit)
						an.SourcePosition.ToRTB(tbParse_Source);
					if ((!tbCompiled_Selecting) && an.CompiledPosition.IsInit)
						an.CompiledPosition.ToRTB(tbCompiled);
				}
			}
		}
		private void tvGrammar_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			tvGrammar_AfterSelect(sender, new TreeViewEventArgs(e.Node));
			tbParse_Source.Focus();
		}
		private void tvGrammar_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (e.Node.Tag != null)
				{
					_selectedAnnotation = (IDE.Annotation)e.Node.Tag;
					if (_selectedAnnotation.FirstRunnable != null)
					{
						_selectedAnnotation = _selectedAnnotation.FirstRunnable.Annotation;
						cmsRTF.Show(tvGrammar, e.X, e.Y);
					}
				}
			}
		}
		private void tbCompiled_SelectionChanged(object sender, EventArgs e)
		{
			if (tbCompiled_Editing)
				return;

			if (tbCompiled.SelectionLength != 0)
				return;

			IDE.Annotation an;

			if (!_compile_tp.Item(tbCompiled.SelectionStart, out an))
				return;

			tbCompiled_Selecting = true;
			if (an != null)
				if (an.TreeViewNode != null)
					tvGrammar.SelectedNode = an.TreeViewNode;
				else if (an.Element != null)
					tvGrammar.SelectedNode = an.Element.Annotation.TreeViewNode;
				else
					tvGrammar.SelectedNode = null;
			else
				tvGrammar.SelectedNode = null;
			tbCompiled_Selecting = false;
		}
		private void tbCompiled_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				if (_compile_tp.Item(tbCompiled.GetCharIndexFromPosition(e.Location), out _selectedAnnotation))
				{
					_selectedAnnotation = _selectedAnnotation.FirstRunnable.Annotation;
					cmsRTF.Show(tbCompiled, e.X, e.Y);
				}
			}
		}
		
		//Errors
		private void lbErrors_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (lbErrors.SelectedIndex < 0) return;

			if (!(lbErrors.SelectedItem is ParseError)) return;
			var err = (ParseError)lbErrors.SelectedItem;

			tbParse_Source.Select(err.TextPortion.Begin, err.TextPortion.Length > 0 ? err.TextPortion.Length : 10);
			tbParse_Source.Focus();
		}

		//Run source file IO
		private void cbRun_Path_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				butRun_Load_Click(null, null);
				e.SuppressKeyPress = true;
			}
			else if ((e.KeyCode == Keys.Delete) && (e.Shift))
			{
				if (cbRun_Path.SelectedIndex >= 0)
					if (MessageBox.Show("Are you sure to delete this entry?", "Delete entry", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.OK)
					{
						cbRun_Path.Items.RemoveAt(cbRun_Path.SelectedIndex);
						cbRun_Path.Text = string.Empty;
					}
				e.SuppressKeyPress = true;
			}
		}
		private void tbRun_Source_TextChanged(object sender, EventArgs e)
		{
			_run_source = tbRun_Source.Text;
		}
		private void butRun_Load_Click(object sender, EventArgs e)
		{
			string s;

			//Try load the file
			try
			{
				s = System.IO.File.ReadAllText(GetFilePath(cbRun_Path.Text));
			}
			catch (Exception)
			{
				MessageBox.Show("Could not load the file");
				return;
			}

			//Add to the list
			if (cbRun_Path.SelectedIndex < 0)
				if (cbRun_Path.Items.Contains(cbRun_Path.Text))
					cbRun_Path.SelectedIndex = cbRun_Path.Items.IndexOf(cbRun_Path.Text);
				else
				{
					cbRun_Path.SelectedIndex = cbRun_Path.Items.Add(cbRun_Path.Text);
					Properties.Settings.Default.Save();
				}

			//Show the text
			if (s.IndexOf('\0') != -1)
				tbRun_Source.Text = s.Replace('\0', ' ');
			else
				tbRun_Source.Text = s;// (s.Length > (16 * 1024)) ? s.Substring(0, 16 * 1024) : s;
			_run_source = s;
		}
		private void butRun_Save_Click(object sender, EventArgs e)
		{
			if (tbRun_Source.Text.Length == 0)
				if (MessageBox.Show("Text is empty. Are you sure?", "Save empty file", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
					return;
			if (MessageBox.Show("Are you sure?", "Save file", MessageBoxButtons.OKCancel, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Cancel)
				return;

			System.IO.File.WriteAllText(GetFilePath(cbRun_Path.Text), tbRun_Source.Text);

			//Add to the list
			if (cbRun_Path.SelectedIndex < 0)
				if (cbRun_Path.Items.Contains(cbRun_Path.Text))
					cbRun_Path.SelectedIndex = cbRun_Path.Items.IndexOf(cbRun_Path.Text);
				else
				{
					cbRun_Path.SelectedIndex = cbRun_Path.Items.Add(cbRun_Path.Text);
					Properties.Settings.Default.Save();
				}
		}
		
		//Run
		private void butRun_Run_Click(object sender, EventArgs e)
		{
			_run_ps = new ProgramStack();

			_run_ps.Source = _run_source;
			Buffers.RootBuffer.Clear();
			Buffers.NodeBuffer.Clear();
			Buffers.FunctionStackFrameBuffer.Clear();
			Buffers.BlockStackFrameBuffer.Clear();
			var rv = _run_ps.RunFunction(_ide.ExecutionTree.Functions[cbRun_Function.Text]);
			if (rv)
			{
				ssMain_RunStatus.Text = "Running...";
				int duration = Environment.TickCount;
				rv = _run_ps.RunUntilBreakPoint();
				duration = Environment.TickCount - duration;
				ssMain_RunStatus.Text = string.Format("{0} ({1}ms/{2}ops={3}. Reused: roots {4}%, nodes {5}%, functions {6}%, blocks {7}%)", (rv ? "Success!" : "Failure!"), duration, _run_ps.ExecutionsCount, Math.Round(((double)_run_ps.ExecutionsCount / duration), 0), Math.Round(Buffers.RootBuffer.Reusability() * 100), Math.Round(Buffers.NodeBuffer.Reusability() * 100), Math.Round(Buffers.FunctionStackFrameBuffer.Reusability() * 100), Math.Round(Buffers.BlockStackFrameBuffer.Reusability() * 100));
			}

			cbRun_ShowTree_CheckedChanged(null, null);
		}
		private void cbRun_ShowTree_CheckedChanged(object sender, EventArgs e)
		{
			if (!cbRun_ShowTree.Checked)
				return;
			if (_run_ps == null)
				return;

			tvRun_RetVal.Nodes.Clear();
			PopulateVariableRoot(_run_ps.Tree.TreeRoot, "Tree", tvRun_RetVal.Nodes);
		}
		private void tvRun_RetVal_AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (e.Node == null)
				return;

			TextPortion tp;

			if (e.Node.Tag is TextPortion)
				tp = (TextPortion)e.Node.Tag;
			else if (e.Node.Tag is VPL.Execute.TreeNode)
				tp = ((VPL.Execute.TreeNode) e.Node.Tag).Item;
			else
				return;

			if ((tp.Text == _run_source) || (tp.Text == tbRun_Source.Text))
			{
				tbRun_Source.SelectionStart = tp.Begin;
				tbRun_Source.SelectionLength = tp.Length;
				tbRun_Source.ScrollToCaret();
			}
		}
		private void tvRun_RetVal_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
				Clipboard.SetText(e.Node.Text);
		}

		//Debug buttons
		private void cmsRTF_Breakpoint_Click(object sender, EventArgs e)
		{
			if (_selectedAnnotation.FirstRunnable == null)
				return;
			if (tbSource_Editing)
				return;
			tbSource_Editing = true;

			_selectedAnnotation.FirstRunnable.BreakPoint = !_selectedAnnotation.FirstRunnable.BreakPoint;

			tbCompiled.BeginUpdate();
			_selectedAnnotation.CompiledPosition.ToRTB(tbCompiled);
			if (_selectedAnnotation.FirstRunnable.BreakPoint)
				tbCompiled.SelectionBackColor = Color.Gold;
			else
				tbCompiled.SelectionBackColor = Color.White;
			tbCompiled.EndUpdate();

			if (_selectedAnnotation.FirstRunnable.Annotation.ListViewItem != null)
				if (_selectedAnnotation.FirstRunnable.BreakPoint)
					_selectedAnnotation.FirstRunnable.Annotation.ListViewItem.BackColor = Color.Gold;
				else
					_selectedAnnotation.FirstRunnable.Annotation.ListViewItem.BackColor = Color.White;

			tbSource_Editing = false;
		}
		private void tsDebug_Run_Click(object sender, EventArgs e)
		{
			if (_ps == null)
			{
				_ps = new ProgramStack();
				_ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.Breakpoints;
				_ps.Debugger.DebuggerCallback = Debugger;

				_ps.Source = _run_source;
				if (!_ps.RunFunction(_ide.ExecutionTree.Functions[tsDebug_Functions.Text]))
					return;
			}

			tsDebug_Stop.Enabled = false;
			tsDebug_StepOut.Enabled = false;
			_ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.Breakpoints;
			_ps.RunUntilBreakPoint();
		}
		private void tsDebug_StepInto_Click(object sender, EventArgs e)
		{
			DebugStep(ProgramStack.DebuggerOptions.eBreakOn.StepInto);
			//if (_ps == null)
			//{
			//    _ps = new ProgramStack();
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepInto;
			//    _ps.Debugger.Debugger = Debugger;

			//    _ps.Source = tbRun_Source.Text;
			//    _ps.RunFunction(_ide.ExecutionTree.Functions[tsDebug_Functions.Text]);
			//}
			//else
			//{
			//    tsDebug_Stop.Enabled = true;
			//    tsDebug_StepOut.Enabled = true;
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepInto;
			//    _ps.RunUntilBreakPoint();
			//}
		}
		private void tsDebug_StepOver_Click(object sender, EventArgs e)
		{
			DebugStep(ProgramStack.DebuggerOptions.eBreakOn.StepOver);
			//if (_ps == null)
			//{
			//    _ps = new ProgramStack();
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepOver;
			//    _ps.Debugger.Debugger = Debugger;

			//    _ps.Source = tbRun_Source.Text;
			//    _ps.RunFunction(_ide.ExecutionTree.Functions[tsDebug_Functions.Text]);
			//}
			//else
			//{
			//    tsDebug_Stop.Enabled = true;
			//    tsDebug_StepOut.Enabled = true;
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepOver;
			//    _ps.RunUntilBreakPoint();
			//}
		}
		private void tsDebug_StepOut_Click(object sender, EventArgs e)
		{
			DebugStep(ProgramStack.DebuggerOptions.eBreakOn.StepOut);
			//if (_ps == null)
			//{
			//    _ps = new ProgramStack();
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepOut;
			//    _ps.Debugger.Debugger = Debugger;

			//    _ps.Source = tbRun_Source.Text;
			//    _ps.RunFunction(_ide.ExecutionTree.Functions[tsDebug_Functions.Text]);
			//}

			//{
			//    tsDebug_Stop.Enabled = true;
			//    _ps.Debugger.BreakOn = ProgramStack.DebuggerOptions.eBreakOn.StepOut;
			//    _ps.RunUntilBreakPoint();
			//}
		}
		private void tsDebug_Stop_Click(object sender, EventArgs e)
		{
			_ps.Debugger.DebuggerCallback = null;
			_ps.Stop();
			Debugger(_ps);
		}
		private void lvDebug_Runnables_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Right)
				return;

			var lvi = lvDebug_Runnables.GetItemAt(e.X, e.Y);

			if (lvi == null)
				return;

			_selectedAnnotation = (IDE.Annotation)lvi.Tag;
			cmsRTF.Show(lvDebug_Runnables, e.Location);
		}

		//Debugger views
		private void DebugStep(ProgramStack.DebuggerOptions.eBreakOn BreakOn)
		{
			if (_ps == null)
			{
				_ps = new ProgramStack();
				_ps.Debugger.BreakOn = BreakOn;
				_ps.Debugger.DebuggerCallback = Debugger;

				_ps.Source = tbRun_Source.Text;
				_ps.RunFunction(_ide.ExecutionTree.Functions[tsDebug_Functions.Text]);
			}
			else
			{
				tsDebug_Stop.Enabled = true;
				tsDebug_StepOut.Enabled = true;
				_ps.Debugger.BreakOn = BreakOn;
				_ps.RunUntilBreakPoint();
			}
		}
		private void Debugger(ProgramStack ProgramStack)
		{
			lvDebug_FS.Items.Clear();
			lvDebug_BS.Items.Clear();
			foreach (ListViewItem lvi in lvDebug_Runnables.Items)
				if (lvi.Tag is IDE.Annotation)
					((IDE.Annotation)lvi.Tag).FirstRunnable.Annotation.ListViewItem = null;
			if ((ProgramStack.FunctionStack == null) || (ProgramStack.FunctionStack.BlockStack == null) || (ProgramStack.FunctionStack.BlockStack.Block != lvDebug_Runnables.Tag))
			{
				lvDebug_Runnables.Items.Clear();
				lvDebug_Runnables.Tag = null;
			}
			dgvDebug_Block.Rows.Clear();
			tvDebug_Variables.Nodes.Clear();

			if (ProgramStack.FunctionStack == null)
			{
				tsDebug_Stop.Enabled = false;
				tsDebug_StepOut.Enabled = false;
				if (ProgramStack.Debugger.DebuggerCallback != null)
				{
					PopulateVariableRoot(ProgramStack.Tree.TreeRoot, "Tree", tvDebug_Variables.Nodes);
					MessageBox.Show(ProgramStack.Success ? "Success!" : "Failure!");
				}

				_ps = null;

				return;
			}
			else
			{
				tsDebug_Stop.Enabled = true;
				tsDebug_StepOut.Enabled = true;
			}

			//Show Tree
			PopulateVariableRoot(ProgramStack.Tree.TreeRoot, "Tree", tvDebug_Variables.Nodes);

			//Fill function stack
			FunctionStackFrame fs;
			for (fs = ProgramStack.FunctionStack ; fs != null ; fs = fs.Previous)
				lvDebug_FS.Items.Add(fs.Function.Name).Tag = fs;

			if (ProgramStack.FunctionStack == null)
				return;

			//Fill block stack
			lvDebug_FS_ItemActivate(null, null);
		}
		private void lvDebug_FS_ItemActivate(object sender, EventArgs e)
		{
			lvDebug_BS.Items.Clear();

			var fs = _ps.FunctionStack;
			if (lvDebug_FS.SelectedItems.Count > 0)
				fs = (FunctionStackFrame)lvDebug_FS.SelectedItems[0].Tag;
		
			if (fs == null)
				return;

			BlockStackFrame bs;
			for (bs = fs.BlockStack; bs != null; bs = bs.Previous)
				lvDebug_BS.Items.Add(bs.Block.Name ?? "(anonymous)").Tag = bs;

			if (lvDebug_BS.Items.Count > 0)
				lvDebug_BS.Items[0].Selected = true;
			lvDebug_BS_ItemActivate(null, null);

			tvDebug_Variables.Nodes.Clear();
			//Show Tree
			PopulateVariableRoot(fs.Variables[0].TreeRoot, "Tree", tvDebug_Variables.Nodes);
			//Show RetVal
			PopulateVariableRoot(fs.Variables[1].TreeRoot, "RetVal", tvDebug_Variables.Nodes);
			//Show variables
			for (int i = 2; i < (fs.Function.VariablesAndArgumentsCount + 2); i++)
			{
				var var = fs.Variables[i];
				PopulateVariableRoot(var.TreeRoot, fs.Function.VariablesNames[i - 1], tvDebug_Variables.Nodes);
				if (var != var.TreeRoot.RootNode)
					PVNode(tvDebug_Variables.Nodes[tvDebug_Variables.Nodes.Count - 1], var, true);
			}
		}
		private void lvDebug_BS_ItemActivate(object sender, EventArgs e)
		{
			var fs = _ps.FunctionStack;
			if (lvDebug_FS.SelectedItems.Count > 0)
				fs = (FunctionStackFrame)lvDebug_FS.SelectedItems[0].Tag;

			if (fs == null)
			{
				lvDebug_Runnables.Items.Clear();
				lvDebug_Runnables.Tag = null;
				return;
			}

			var bs = fs.BlockStack;
			if (lvDebug_BS.SelectedItems.Count > 0)
				bs = (BlockStackFrame)lvDebug_BS.SelectedItems[0].Tag;

			if (bs == null)
			{
				lvDebug_Runnables.Items.Clear();
				lvDebug_Runnables.Tag = null;
				return;
			}

			//Fill block info
			dgvDebug_Block.Rows.Add("Success", fs.Success.ToString());
			dgvDebug_Block.Rows.Add("Quantifier", string.Format("{0}:{1}", bs.Quantifier, bs.Quantifier.Count));
			var s = fs.Result.ToString();
			dgvDebug_Block.Rows.Add("Result", string.Format("{0}({1}):{2}", fs.Result.Type.ToString(), s.Length, (s.Length <= 50) ? s : s.Substring(0, 50)));
			s = bs.Source.ToString();
			dgvDebug_Block.Rows.Add("Source", string.Format("({0}):{1}", s.Length, (s.Length <= 50) ? s : s.Substring(0, 50)));
			s = bs.Source/*NextSource*/.ToString();
			dgvDebug_Block.Rows.Add("NextSource", string.Format("({0}):{1}", s.Length, (s.Length <= 50) ? s : s.Substring(0, 50)));
			s = bs.Skipped.ToString();
			dgvDebug_Block.Rows.Add("Skipped", string.Format("{0}:{1}", s.Length, s));
			
			var bl = bs.Block;
			Runnable rn = null;
			if ((bs.NextItem >= 0) && (bs.NextItem < bl.Items.Length))
				rn = bl.Items[bs.NextItem].OnFailJumpTo;
			if (lvDebug_Runnables.Tag != bl)
			{
				lvDebug_Runnables.Tag = null;
				lvDebug_Runnables.Items.Clear();
			}
			for (var i = 0 ; i < bl.Items.Length ; i++)
			{
				var it = bl.Items[i];
				ListViewItem lvi;
				if (bs.Block != lvDebug_Runnables.Tag)
				{
					lvi = lvDebug_Runnables.Items.Add(it.Description.ToString());
					lvi.Tag = it.Annotation;
					it.Annotation.FirstRunnable.Annotation.ListViewItem = lvi;
				}
				else
					lvi = lvDebug_Runnables.Items[i];
				lvi.ImageIndex = (bs.NextItem == i) ? 0 : (((rn != null) && (rn.ExecutionIndex == i)) ? 1 : -1);
				if (lvi.ImageIndex == 0)
					lvi.EnsureVisible();
				lvi.BackColor = it.BreakPoint ? Color.Gold : Color.White;
			}
			lvDebug_Runnables.Tag = bs.Block;
		}

		//Show a variable
		private void PopulateVariableRoot(TreeRoot Root, string nName, TreeNodeCollection nParent)
		{
			var nd = nParent.Add(string.Format("{0}: {1}", ""/*Root.Type.ToString()*/, nName));
			PVNode(nd, Root.RootNode, true);
			//nd.ExpandAll();
			nd.Expand();
		}
		private void PVNode(TreeNode nParent, VPL.Execute.TreeNode Node, bool ShowName)
		{
			nParent.Collapse();
			TreeNode nd;
			if (ShowName)
				nd = nParent.Nodes.Add(string.Format("{0} = {1}", Node.Name, Node.Item.ToString()));
			else
				nd = nParent.Nodes.Add(Node.Item.ToString());
			if (Node._Children != null)
			{
				nd.Tag = Node;
				nd.Nodes.Add("<Enumerating>");
			}
			else
				nd.Tag = Node.Item;
		}
		private void tvRun_RetVal_BeforeExpand(object sender, TreeViewCancelEventArgs e)
		{
			if (e.Node == null)
				return;
			
			if (e.Node.Tag is List<VPL.Execute.TreeNode>)
			{
				var ch = e.Node.Tag as List<VPL.Execute.TreeNode>;
				foreach (var tn in ch)
					PVNode(e.Node, tn, false);
				e.Node.Nodes.RemoveAt(0);

				e.Node.Tag = null;
			}
			else if (e.Node.Tag is VPL.Execute.TreeNode)
			{
				var nch = e.Node.Tag as VPL.Execute.TreeNode;
				foreach (var ch in nch._Children)
					if (ch.Value.Count > 1)
					{
						var ndc = e.Node.Nodes.Add(string.Format("{0} ({1})", ch.Key, ch.Value.Count));
						if (ch.Value.Count > 0)
						{
							ndc.Tag = ch.Value;
							ndc.Nodes.Add("<Enumerating>");
						}
					}
					else
						PVNode(e.Node, ch.Value[0], true);
				e.Node.Nodes.RemoveAt(0);
				if (nch.Parent != null)
				{
					var nd = e.Node.Nodes.Insert(0, "<Parent>");
					nd.Tag = nch.Parent;
					nd.Nodes.Add("<Enumerating>");
				}

				e.Node.Tag = nch.Item;
			}
		}
	}
}