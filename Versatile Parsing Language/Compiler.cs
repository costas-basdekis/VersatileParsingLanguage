using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VPL.Execute;
using ExBlock = VPL.Execute.Block;
using Function = VPL.Execute.Function;

namespace VPL.Compile
{
	/// <summary>
	/// An intermediate structure to hold info about a block
	/// </summary>
	public class Block
	{
		public ExBlock ExecuteBlock = new ExBlock();
		public Runnable[] Items = new Runnable[20];
		public int Count;
		/// <summary>
		/// Index in the parent block
		/// </summary>
		public Runnable ExecuteIndex;
		/// <summary>
		/// Has it been compiled yet?
		/// </summary>
		public bool Compiled;
		/// <summary>
		/// If it is a For loop this is where to go to continue
		/// </summary>
		public Runnable ForContinue;

		public Compiler Compiler;

		public Block(Compiler newCompiler)
		{
			Compiler = newCompiler;
		}

		public void AddItem(Runnable Item)
		{
			if ((Items.Length - Count) < 10)
				IncreaseItemsSize();
			var index = ++Count - 1;
			Items[index] = Item;
			Items[index].ExecutionIndex = index;
			Compiler.OutItem(Item, this);
		}
		public void RemoveItem(Runnable Item)
		{
			for (int i = Item.ExecutionIndex; i < (Count - 1); i++)
			{
				Items[i] = Items[i + 1];
				Items[i].ExecutionIndex = i;
			}
			Count--;
		}
		private void IncreaseItemsSize(int Delta = 20)
		{
			Runnable[] new_items = new Runnable[Items.Length + Delta];
			for (int i = 0; i < Items.Length; i++)
			{
				new_items[i] = Items[i];
				Items[i] = null;
			}
			Items = new_items;
		}
		/// <summary>
		/// Move the items to the Execute.Block
		/// </summary>
		public void Done()
		{
			ExecuteBlock.Items = new Runnable[Count];
			for (var i = 0; i < Count; i++)
			{
				ExecuteBlock.Items[i] = Items[i];
				Items[i] = null;
			}
			Items = null;
		}
	}

	public class Compiler
	{
		internal void ThrowParseError(string Description)
		{
			ThrowParseError(Description, new TextPortion());
		}
		internal void ThrowParseError(string Description, TextPortion TextPortion)
		{
			IDE.ExecutionTree.ParseErrors.Add(new ParseError(Description, TextPortion));
		}

		//Function specific lists
		private readonly Dictionary<string, Block> _BlocksByName = new Dictionary<string, Block>();
		private readonly Dictionary<Parse.Block, Block> _BlocksByParse = new Dictionary<Parse.Block, Block>();
		/// <summary>
		/// Used to update a GoTo target until the target is compiled
		/// </summary>
		private struct GoToBackPatchInfo
		{
			private readonly Parse.Block Target;
			private readonly GoToBlock GoToBlock;

			public GoToBackPatchInfo(Parse.Block newTarget, Parse.Block Parent, GoToBlock newGoToBlock, Compiler Compiler)
			{
				Target = newTarget;
				GoToBlock = newGoToBlock;

				//Find the common ancestor
				var ancestor = Target.FindCommonAncestor(Parent);

				Runnable[] ar;
				if (Target.Parent == null)
				//GoTo:Function
				{
					GoToBlock.ParentBlocksReturn = Parent.Depth;
					GoToBlock.ChildBlocksEnter = 1;

					ar = new Runnable[GoToBlock.ChildBlocksEnter];
					ar[0] = Compiler.Functions[newTarget.Function].Block.Items[0];
				}
				else
				{
					if (ancestor != Target)
					{
						GoToBlock.ParentBlocksReturn = Parent.Depth - ancestor.Depth;
						GoToBlock.ChildBlocksEnter = Target.Depth - ancestor.Depth;
					}
					else
					//GoTo a Parent
					{
						
						GoToBlock.ParentBlocksReturn = Parent.Depth - ancestor.Depth + 1;
						GoToBlock.ChildBlocksEnter = 1;
					}

					//Get the block indices
					ar = new Runnable[GoToBlock.ChildBlocksEnter];
				}

				GoToBlock.BlocksEnter = ar;
			}
			public void BackPatch(Compiler Compiler)
			{
				var ar = GoToBlock.BlocksEnter;

				var bl = Target;
				for (int i = ar.Length - 1 ; i >= 0 ; i--)
				{
					ar[i] = Compiler._BlocksByParse[bl].ExecuteIndex;
					bl = bl.Parent;
				}
			}
		} 
		private readonly List<GoToBackPatchInfo> _GoToBackPatches = new List<GoToBackPatchInfo>();

		/// <summary>
		/// Assembly output
		/// </summary>
		private readonly StringBuilder Output = new StringBuilder(10000);
		private Dictionary<Parse.SyntaxType.eType, Parse.SyntaxType> SyntaxOccurences;
		/// <summary>
		/// Apped a string with highlighting to the assembly output
		/// </summary>
		/// <param name="Text"></param>
		/// <param name="Type"></param>
		private void AppendSyntax(string Text, Parse.SyntaxType.eType Type)
		{
			int ss = Output.Length;
			Output.Append(Text);
			var tp = new TextPortion(ss, Text.Length);
			var so = SyntaxOccurences[Type];
			so.Length += tp.Length;
			var soo = SyntaxOccurences[Type].Occurences;
			if (soo.Count > 0)
			{
				var oc = soo[soo.Count - 1];
				if (oc.End == tp.Begin)
				{
					oc.End = tp.End;
					soo.RemoveAt(soo.Count - 1);
					soo.Add(oc);
				}
				else
					SyntaxOccurences[Type].Occurences.Add(tp);
			}
			else
				SyntaxOccurences[Type].Occurences.Add(tp);
		}
		/// <summary>
		/// Clear the assembly output
		/// </summary>
		internal void OutClear()
		{
			IDE.ClearCompiledSyntaxOccurences();
			Output.Clear();
		}
		/// <summary>
		/// Append a comment to the assembly output
		/// </summary>
		/// <param name="Comment"></param>
		internal void OutComment(string Comment)
		{
			AppendSyntax(new string(' ', _OutIdent * 4) + Comment.Split(new[] { "\r" }, StringSplitOptions.None).Aggregate("", (curent, s) => string.Format("{0}//{1}\r", curent, s)), Parse.SyntaxType.eType.cComment);
		}
		/// <summary>
		/// Append a runnable's info to the assembly output
		/// </summary>
		/// <param name="Item"></param>
		/// <param name="Compiled"></param>
		internal void OutItem(Runnable Item, Block Compiled)
		{
			Item.Annotation.CompiledPosition.Begin = Output.Length;

			var d = Item.GetDescription();
			AppendSyntax(new string(' ', (_OutIdent + 1) * 4) + d.Name, Parse.SyntaxType.eType.Name);
			if (d.Parameters != null)
				for (int i = 0 ; i < d.Parameters.Length ; i++)
				{
					var it = d.Parameters[i];
					if (i == 0)
						Output.Append(" ");
					else
						Output.Append(", ");
					if (it.Name != null)
						AppendSyntax(it.Name + ":", Parse.SyntaxType.eType.ArgName);
					if (it.Value != null)
						AppendSyntax(it.Value, Parse.SyntaxType.eType.ArgValue);
				}
			Output.Append("\r");

			Item.Annotation.CompiledPosition.End = Output.Length;
			Item.Annotation.FirstRunnable = Item;
			Item.Annotation.RunnablesCount = 1;
			Item.Annotation.RunnableParentBlock = Compiled.ExecuteBlock;
			IDE.CompiledAnnotations.Add(Item.Annotation.CompiledPosition, Item.Annotation);
		}
		internal int _OutIdent;

		public IDE IDE;
		private readonly Dictionary<Parse.Function, Function> Functions = new Dictionary<Parse.Function, Function>();

		public void Clear()
		{
			Functions.Clear();
			ClearForFunction();
			OutClear();
		}
		/// <summary>
		/// Clear the structures before parsing a function
		/// </summary>
		private void ClearForFunction()
		{
			_GoToBackPatches.Clear();
			_BlocksByParse.Clear();
			_BlocksByName.Clear();
		}

		public bool Compile()
		{
			Clear();
			if ((IDE == null) || (IDE.GrammarTree == null))
				return false;

			var gt = IDE.GrammarTree;

			if (gt.ParseErrors.Count > 0)
				return false;

			//Functions = IDE.ExecutionTree.Functions;
			SyntaxOccurences = IDE.CompiledSyntaxOccurences;
			IDE.CompiledAnnotations.Clear();
			IDE.ExecutionTree.Clear();

			//Create functions stubs
			foreach (var parse_function in gt.Functions)
			{
				var fun = new Function(parse_function.ArgumentsCount, parse_function.Variables.Count, parse_function.Name);
				Functions.Add(parse_function, fun);
				IDE.ExecutionTree.Functions.Add(parse_function.Name, fun);

				//Create variable names list
				fun.VariablesNames = new Dictionary<int, string>(fun.VariablesAndArgumentsCount);
				foreach (var var in parse_function.Variables)
					fun.VariablesNames[var.Value] = var.Key;
			}

			//Compile functions
			foreach (var fun in gt.Functions)
			{
				ComplileFunction(fun);
				IDE.CompiledAnnotations.Add(fun.Annotation.CompiledPosition, fun.Annotation);
			}

			IDE.AssemblyOutput = Output.ToString();

			IDE.ExecutionTree.MaxFunctionVariables = IDE.GrammarTree.MaxFunctionVariables;

			return true;
		}
		private void ComplileFunction(Parse.Function Function)
		{
			ClearForFunction();

			Functions[Function].Inlined = Function.Inlined;
			
			//Create block stubs
			foreach (var pbl in Function.NamedBlocks.Defined)
				_BlocksByName.Add(pbl.Key, new Block(this));
			//The function block
			_BlocksByName.Add(string.Empty, new Block(this));

			//Anotate Item's commands
			if (Output != null)
				Function.Annotation.CompiledPosition.Begin = Output.Length;

			//Output comment
			_OutIdent = 0;
			OutComment("Function " + Function.Name);

			//Compile the block
			var bl = _BlocksByName[string.Empty];
			CompileBlock(Function.Block, bl);
			//Set function's block
			Functions[Function].Block = bl.ExecuteBlock;

			//Anotate Item's commands
			if (Output != null)
				Function.Annotation.CompiledPosition.End = Output.Length;

			//Backpatch GoTos now that we know every block's execution index
			BackPathGoTos();
		}
		private void BackPathGoTos()
		{
			foreach (var gt in _GoToBackPatches)
				gt.BackPatch(this);
		}
		private void CompileBlock(Parse.Block Item, Block Compiled)
		{
			//Don't try to compile it twice
			if (Compiled.Compiled)
				return;
			_BlocksByParse.Add(Item, Compiled);
			Compiled.Compiled = true;
			Compiled.ExecuteBlock.Name = Item.Name;

			//Assembly output
			_OutIdent++;
			if (Item.Name != null)
				OutComment("#" + Item.Name + "#");
			else
				OutComment("(unnamed)");

			//Compile elements
			if (Item.Type == Parse.Block.eType.For)
			{
				//Just before the loop increment
				Runnable forContinue = new NOP();
				Compiled.ForContinue = forContinue;
				//When the initializer or loop increment fail succeed
				Runnable onInitIncrFail = new ReturnBlock(1, true);
				//First item of loop's body
				Runnable loopFirst;
				
				//Keep track the first and last runnables of current element
				int this_exec_begin, this_exec_end;

				this_exec_begin = Compiled.Count;
				//Compile the initializer
				CompileElement(Item.Elements[0], Item, Compiled);
				this_exec_end = Compiled.Count;
				//Set the on fail
				for (int i = this_exec_begin; i < this_exec_end; i++)
				{
					var it = Compiled.Items[i];
					if (it.OnFailJumpTo == null)
						it.OnFailJumpTo = onInitIncrFail;
				}

				this_exec_begin = Compiled.Count;
				//Compile the loop body
				CompileElement(Item.Elements[2], Item, Compiled);
				loopFirst = Compiled.Items[this_exec_begin];

				this_exec_begin = Compiled.Count;
				//Compile the loop increment
				Compiled.AddItem(forContinue);
				CompileElement(Item.Elements[1], Item, Compiled);
				this_exec_end = Compiled.Count;
				//Set the on fail
				for (int i = this_exec_begin; i < this_exec_end; i++)
				{
					var it = Compiled.Items[i];
					if (it.OnFailJumpTo == null)
						it.OnFailJumpTo = onInitIncrFail;
				}

				//Put the continue command
				var cont = new GoToBlock(0, 1, new[]{loopFirst});
				Compiled.AddItem(cont);

				//Put the on fail
				Compiled.AddItem(onInitIncrFail);
			}
			else
			{
				//Keep track the first and last runnables of current and previous element
				int this_exec_begin, this_exec_end;
				int prev_exec_begin = 0, prev_exec_end = 0;
				Runnable this_exec_first;
				for (int i = 0; i < Item.Elements.Count; i++)
				{
					this_exec_begin = Compiled.Count;
					//Compile the element
					CompileElement(Item.Elements[i], Item, Compiled);
					this_exec_end = Compiled.Count;

					//Put jumps if it is multi (except on the last part)
					if ((Item.Type == Parse.Block.eType.Multi))
					{
						//Succeed
						if (i != (Item.Elements.Count - 1))
							Compiled.AddItem(new ReturnBlock(1, true));
						this_exec_first = Compiled.Items[this_exec_begin];
						//Update elements to jump to next on fail
						if (i != 0)
							for (int j = prev_exec_begin; j < prev_exec_end; j++)
								if (Compiled.Items[j].OnFailJumpTo == null)
									Compiled.Items[j].OnFailJumpTo = this_exec_first;
					}
					prev_exec_begin = this_exec_begin;
					prev_exec_end = this_exec_end;
				}
			}

			//Move elements to Execute.Block
			Compiled.Done();

			_OutIdent--;
		}

		/// <summary>
		/// Create the appropriate element and the appropriate quantifier
		/// </summary>
		/// <param name="Item">The elemen to compile</param>
		/// <param name="Parent">The parent parsed block</param>
		/// <param name="Compiled">The parent compiled block</param>
		private void CompileElement(Parse.Element Item, Parse.Block Parent, Block Compiled)
		{
			//Just create a NOP for a null item
			if (Item == null)
			{
				Compiled.AddItem(new NOP());

				return;
			}

			//Anotate Item's commands
			if (Output != null)
				Item.Annotation.CompiledPosition.Begin = Output.Length;

			_OutIdent++;
			OutComment(Item.GetType().Name + Item.Quantifier);

			//Check for quantifier
			QuantifierBefore qb = null;
			if (!Item.Quantifier.IsDefault())
			{
				if (Item.Quantifier.IsIfAny())
				{
					//Nothing, just add NOP after
				}
				else if (Item.Quantifier.IsAsMany() && !Item.Quantifier.Additive)
				{
					//Nothing, just add a ConditionalJump after
				}
				else if (Item.Quantifier.IsNever())
				{
					//Nothing, add a Fail and a NOP after
				}
				else
				{
					qb = new QuantifierBefore(Item.Quantifier);
					Compiled.AddItem(qb);
				}
			}

			int first_runnable_execution_index = Compiled.Count;

			//Find item's type
			if (Item is Parse.Block)
			{
				Parse.Block pabl = (Parse.Block)Item;
				Block bl;
					
				//New block if it is unnamed, or get the stub
				if (pabl.Name == null)
					bl = new Block(this);
				else
					bl = _BlocksByName[pabl.Name];

				//Compile it
				CompileBlock(pabl, bl);

				//Create a call to the block
				Compiled.AddItem(new CallBlock(bl.ExecuteBlock));

				//Update it's index
				bl.ExecuteIndex = Compiled.Items[first_runnable_execution_index];
			}
			else if (Item is Parse.TextSearch)
				CETextSearch((Parse.TextSearch) Item, Parent, Compiled);
			else if (Item is Parse.Literal)
				CELiterall((Parse.Literal) Item, Compiled);
			else if (Item is Parse.Variable)
				CEVariable((Parse.Variable) Item, Parent, Compiled);
			else if (Item is Parse.ControlFlow)
				CEControlFlow((Parse.ControlFlow) Item, Parent, Compiled);
			else if (Item is Parse.FunctionCall)
				CEFunctionCall((Parse.FunctionCall) Item, Parent, Compiled);
			else if (Item is Parse.BinaryOperator)
				CEBinaryOperator((Parse.BinaryOperator) Item, Parent, Compiled);
			else
			{
				Compiled.AddItem(new NOP());
				ThrowParseError("Unkown type " + Item.GetType().Name, Item.Annotation.SourcePosition);
			}

			int last_runnable_execute_index = Compiled.Count - 1;

			//Check for quantifier
			if (!Item.Quantifier.IsDefault())
			{
				if (Item.Quantifier.IsIfAny())
				{
					//On failure jump here
					var nop = new NOP();
					Compiled.AddItem(nop);

					//Update Runnables
					CESetOnJumpFailTo(Compiled, first_runnable_execution_index, last_runnable_execute_index, nop);
				}
				else if (Item.Quantifier.IsAsMany() && !Item.Quantifier.Additive)
				{
					//On failure jump here
					var cj = new ConditionalJump(Compiled.Items[first_runnable_execution_index]);
					Compiled.AddItem(cj);
					
					//Update Runnables
					CESetOnJumpFailTo(Compiled, first_runnable_execution_index, last_runnable_execute_index, cj);
				}
				else if (Item.Quantifier.IsNever())
				{
					//Fail if the item succeeds
					Compiled.AddItem(new ReturnBlock(0, false));

					//On failure jump here
					var nop = new NOP();
					Compiled.AddItem(nop);

					//Update Runnables
					CESetOnJumpFailTo(Compiled, first_runnable_execution_index, last_runnable_execute_index, nop);
				}
				else
				{
					//On failure jump here
					var qa = new QuantifierAfter(qb);
					Compiled.AddItem(qa);

					//Update Runnables
					CESetOnJumpFailTo(Compiled, first_runnable_execution_index, last_runnable_execute_index, qa);
				}
			}

			//Anotate Item's commands
			if (Output != null)
				Item.Annotation.CompiledPosition.End = Output.Length;

			_OutIdent--;

			Item.Annotation.FirstRunnable = Compiled.Items[first_runnable_execution_index];
			Item.Annotation.RunnablesCount = Compiled.Count - first_runnable_execution_index;
			Item.Annotation.RunnableParentBlock = Compiled.ExecuteBlock;
			for (int i = first_runnable_execution_index ; i < Compiled.Count ; i++)
			{
				var rn = Compiled.Items[i];
				if (rn.Annotation.Element == null)
				{
					rn.Annotation.Element = Item;
					rn.Annotation.SourcePosition = Item.Annotation.SourcePosition;
					rn.Annotation.TreeViewNode = Item.Annotation.TreeViewNode;
				}
			}
			IDE.CompiledAnnotations.Add(Item.Annotation.CompiledPosition, Item.Annotation);
		}
		private void CESetOnJumpFailTo(Block Compiled, int indexFrom, int indexTo, Runnable JumpTarget)
		{
			for (var i = indexFrom; i <= indexTo; i++)
				if (Compiled.Items[i].OnFailJumpTo == null)
					Compiled.Items[i].OnFailJumpTo = JumpTarget;
		}
		private void CETextSearch(Parse.TextSearch Item, Parse.Block Parent, Block Compiled)
		{
			switch (Item.Type) 
			{
			case Parse.TextSearch.eType.Normal:
				//Put pattern at LastResult
				CompileElement(Item.Pattern, Parent, Compiled);
				//Compiled.AddItem(new TextLiteral(Item.Pattern));
				//Match
				Compiled.AddItem(new TextMatch());
				break;
			case Parse.TextSearch.eType.Find:
				//Put pattern at LastResult
				CompileElement(Item.Pattern, Parent, Compiled);
				//Compiled.AddItem(new TextLiteral(Item.Pattern));
				//Match
				Compiled.AddItem(new TextFind());
				break;
			case Parse.TextSearch.eType.FindReverse:
				//Put pattern at LastResult
				CompileElement(Item.Pattern, Parent, Compiled);
				//Compiled.AddItem(new TextLiteral(Item.Pattern));
				//Match
				Compiled.AddItem(new TextFindReverse());
				break;
			case Parse.TextSearch.eType.RegularExpression:
				//Match
				Compiled.AddItem(new TextRegEx(((Parse.Literal) Item.Pattern).Text));
				//Check for getting a capture
				if (Item.Tree.Count > 0)
					Compiled.AddItem(new TextRegExGetItem(Item.Tree[0].Index));
				break;
			default:
				ThrowParseError("Unknown type", Item.Annotation.SourcePosition);
				break;
			}
		}
		private void CELiterall(Parse.Literal Item, Block Compiled)
		{
			switch (Item.Type)
			{
			case Parse.Literal.eType.Number:
				Compiled.AddItem(new NOP());
				ThrowParseError("Number literalls are not allowed", Item.Annotation.SourcePosition);
				break;
			case Parse.Literal.eType.Text:
				Compiled.AddItem(new TextLiteral(Item.Text, Item.AssemblyDisplayText));
				break;
			default:
				Compiled.AddItem(new NOP());
				ThrowParseError("Unknown type", Item.Annotation.SourcePosition);
				break;
			}
		}
		private void CEVariable(Parse.Variable Item, Parse.Block Parent, Block Compiled)
		{
			switch (Item.Type)
			{
			case Parse.Variable.eType.Variable:
				var var_index = Parent.Function.Variables[Item.Name];
				var nfv = new NodeFromVariable(var_index);
				nfv.Name = Item.Name;
				//Get the inital node
				Compiled.AddItem(nfv);
				break;
			case Parse.Variable.eType.RetVal:
				//Get the inital node
				Compiled.AddItem(new NodeFromRetVal());
				break;
			case Parse.Variable.eType.Tree:
				//Get the inital node
				Compiled.AddItem(new NodeFromTree());
				break;
			case Parse.Variable.eType.Skipped:
				//Put Skipped at LastResult
				Compiled.AddItem(new SkippedToResult());
				break;
			case Parse.Variable.eType.Source:
				//Put Source at LastResult
				Compiled.AddItem(new SourceToResult());
				break;
			case Parse.Variable.eType.New:
				//Create a new TreeRoot
				Compiled.AddItem(new NodeFromNew());
				break;
			case Parse.Variable.eType.Setting:
				ThrowParseError("Settings not supported", Item.Annotation.SourcePosition);
				break;
			default:
				ThrowParseError("Unknown type", Item.Annotation.SourcePosition);
				break;
			}
			
			//Get the nodes
			if (Item.Nodes.Count > 0)
			{
				//Clear any temps that are left
				var ptc = new PushTemporariesCount();
				Compiled.AddItem(ptc);

				//Check to see if actualy a temp guard is needed
				bool temp_guard_is_needed = false;

				foreach (var tn in Item.Nodes)
				{
					switch (tn.Type)
					{
					case Parse.TreeNode.eType.Normal:
						if (!(tn.Key is Parse.Literal))
							temp_guard_is_needed = true;
						//Push Result to Temp
						Compiled.AddItem(new ResultToTemp());
						//Get the key
						CompileElement(tn.Key, Parent, Compiled);
						//Get the node
						Compiled.AddItem(new NodeByName(tn.Index, false, tn.Index != -1));
						//Search by Value
						if (tn.Value != null)
						{
							if (!(tn.Value is Parse.Literal))
								temp_guard_is_needed = true;
							//Push Result to Temp
							Compiled.AddItem(new ResultToTemp());
							//Get the value
							CompileElement(tn.Value, Parent, Compiled);
							//Get the node
							Compiled.AddItem(new NodeSiblingByValue(tn.ByValueNext, tn.ByValueIgnoreCase));
						}
						break;
					case Parse.TreeNode.eType.Indexed:
						//Search by Value
						if (tn.Value != null)
						{
							if (!(tn.Value is Parse.Literal))
								temp_guard_is_needed = true;
							//Push Result to Temp
							Compiled.AddItem(new ResultToTemp());
							//Get the value
							CompileElement(tn.Value, Parent, Compiled);
							//Get the node
							Compiled.AddItem(new NodeSiblingByValue(tn.ByValueNext, tn.ByValueIgnoreCase));
						}
						else //Get by index
							//Get the node
							Compiled.AddItem(new NodeSiblingByIndex(tn.Index));
						break;
					case Parse.TreeNode.eType.New:
						if (tn.Key == null)
							//Get a new node
							Compiled.AddItem(new NodeSiblingNew());
						else
						{
							if (!(tn.Key is Parse.Literal))
								temp_guard_is_needed = true;
							//Push Result to Temp
							Compiled.AddItem(new ResultToTemp());
							//Get the key
							CompileElement(tn.Key, Parent, Compiled);
							//Get the node
							Compiled.AddItem(new NodeByName(tn.Index, true, false));
						}
						break;
					case Parse.TreeNode.eType.NotNew:
						if (!(tn.Key is Parse.Literal))
							temp_guard_is_needed = true;
						//Push Result to Temp
						Compiled.AddItem(new ResultToTemp());
						//Get the key
						CompileElement(tn.Key, Parent, Compiled);
						//Get the node
						Compiled.AddItem(new NodeByName(tn.Index, false, true));
						break;
					case Parse.TreeNode.eType.Parent:
						//Get the node
						Compiled.AddItem(new NodeParent());
						break;
					case Parse.TreeNode.eType.Next:
						//Get the node
						Compiled.AddItem(new NodeSiblingByNeighbour(true));
						break;
					case Parse.TreeNode.eType.Previous:
						//Get the node
						Compiled.AddItem(new NodeSiblingByNeighbour(false));
						break;
					case Parse.TreeNode.eType.Value:
						//Get the text
						Compiled.AddItem(new NodeValue());
						break;
					case Parse.TreeNode.eType.Name:
						//Get the name
						Compiled.AddItem(new NodeName());
						break;
					default:
						ThrowParseError("Unknown tree type", Item.Annotation.SourcePosition);
						break;
					}
				}

				if (temp_guard_is_needed)
					//Clear any temps that are left
					Compiled.AddItem(new PopClearTemporaries());
				else
					Compiled.RemoveItem(ptc);
			}
		}
		private void CEControlFlow(Parse.ControlFlow Item, Parse.Block Parent, Block Compiled)
		{
			switch (Item.Type)
			{
			case Parse.ControlFlow.eType.Call:
				//A call to a block
				CompileElement(Item.Target, Parent, Compiled);
				break;
			case Parse.ControlFlow.eType.Fail:
				//Return a number of blocks
				Compiled.AddItem(new ReturnBlock(Parent.Depth - Item.Target.Depth + (Item.Target.Type == Parse.Block.eType.Multi ? 0 : 1), false));
				break;
			case Parse.ControlFlow.eType.Succeed:
				//Return a number of blocks
				Compiled.AddItem(new ReturnBlock(Parent.Depth - Item.Target.Depth + 1, true));
				break;
			case Parse.ControlFlow.eType.GoTo:
				//GoTo block - if the common parent is the target then return it and run it again
				var gtb = new GoToBlock(0, 0, null);
				gtb.TargetName = Item.Target.Name;
				//We will add the elements index after the compilation - we might not know a block's index (it is not compiled yet)
				_GoToBackPatches.Add(new GoToBackPatchInfo(Item.Target, Parent, gtb, this));
				Compiled.AddItem(gtb);
				break;
			case Parse.ControlFlow.eType.Continue:
				//GoTo block
				var cont = new GoToBlock(Parent.Depth - Item.Target.Depth, 1, new []{_BlocksByParse[Item.Target].ForContinue});
				cont.TargetName = Item.Target.Name;
				Compiled.AddItem(cont);
				break;
			default:
				ThrowParseError("Unknown type", Item.Annotation.SourcePosition);
				break;
			}
		}
		private void CEFunctionCall(Parse.FunctionCall Item, Parse.Block Parent, Block Compiled)
		{
			int first_runnable_execution_index = Compiled.Count;

			//Clear any temps that are left
			if (Item.Arguments.Count > 0)
				Compiled.AddItem(new PushTemporariesCount());

			//Get the arguments
			foreach (var el in Item.Arguments)
			{
				//Get the argument
				CompileElement(el, Parent, Compiled);
				//Push in temprary
				Compiled.AddItem(new ResultToTemp());
			}

			//Get the arguments and call the function
			var tar = Item.Target;
			if (!Functions.ContainsKey(tar))
			{
				var fun = new Function(tar.ArgumentsCount, tar.Variables.Count, Item.Name);
				Functions.Add(tar, fun);
				//Create a unique function name (functions from imports may define same named functions
				if (IDE.ExecutionTree.Functions.ContainsKey(tar.Name))
				{
					var i = 1;
					string new_name;
					do
					{
						new_name = string.Format("{0}_{1}", tar.Name, i);
						i++;
					} while (IDE.ExecutionTree.Functions.ContainsKey(new_name));
					IDE.ExecutionTree.Functions.Add(new_name, fun);
				}
				else
					IDE.ExecutionTree.Functions.Add(tar.Name, fun);
				//Compile the function
				ComplileFunction(tar);
				IDE.CompiledAnnotations.Add(tar.Annotation.CompiledPosition, tar.Annotation);
			}
			Compiled.AddItem(new CallFunction(Functions[tar]));

			int last_runnable_execution_index = Compiled.Count - 1;
			
			//Clear any temps that are left
			if (Item.Arguments.Count > 0)
			{
				var ct = new PopClearTemporaries();
				Compiled.AddItem(ct);
				CESetOnJumpFailTo(Compiled, first_runnable_execution_index, last_runnable_execution_index, ct);
			}
		}
		private void CEBinaryOperator(Parse.BinaryOperator Item, Parse.Block Parent, Block Compiled)
		{
			//Get left element
			CompileElement(Item.lhs, Parent, Compiled);
			//If not pipe then push left and get right element
			if (Item.Type != Parse.BinaryOperator.eType.Pipe)
			{
				//Clear any temps that are left
				Compiled.AddItem(new PushTemporariesCount());
				//Push it to temp
				Compiled.AddItem(new ResultToTemp());

				var first_runnable_execute_index = Compiled.Count;
				//Get right element
				CompileElement(Item.rhs, Parent, Compiled);
				var last_runnable_execute_index = Compiled.Count - 1;

				//Clear any temps that are left
				var ct = new PopClearTemporaries();
				Compiled.AddItem(ct);
				CESetOnJumpFailTo(Compiled, first_runnable_execute_index, last_runnable_execute_index, ct);
			}

			//Apply operator
			switch (Item.Type)
			{
			case Parse.BinaryOperator.eType.Equal:
				Compiled.AddItem(new ResultTextEqualTempText(true, Item.IgnoreCase));
				break;
			case Parse.BinaryOperator.eType.NotEqual:
				Compiled.AddItem(new ResultTextEqualTempText(false, Item.IgnoreCase));
				break;
			case Parse.BinaryOperator.eType.ReferenceEqual:
				Compiled.AddItem(new ResultNodeEqualTempNode(true));
				break;
			case Parse.BinaryOperator.eType.ReferenceNotEqual:
				Compiled.AddItem(new ResultNodeEqualTempNode(false));
				break;
			case Parse.BinaryOperator.eType.ReferenceCopy:
				Compiled.AddItem(new ResultNodeToTempVariable());
				break;
			case Parse.BinaryOperator.eType.TextAppend:
				Compiled.AddItem(new ResultTextAppendToTempText());
				break;
			case Parse.BinaryOperator.eType.TreeAppend:
				Compiled.AddItem(new ResultNodeAppendToTempNode());
				break;
			case Parse.BinaryOperator.eType.TildeAppend:
				Compiled.AddItem(new TempTextAppendToResultNode());
				break;
			case Parse.BinaryOperator.eType.Tilde:
				Compiled.AddItem(new TempTextToResultNode());
				break;
			case Parse.BinaryOperator.eType.Pipe:
				//Save Source
				Compiled.AddItem(new SourceAndSkippedToTemp());
				//Change Source
				Compiled.AddItem(new ResultToSource());

				var first_runnable_execute_index = Compiled.Count;

				//Get right element
				CompileElement(Item.rhs, Parent, Compiled);

				var last_runnable_execute_index = Compiled.Count - 1;

				//Get Source back
				var tts = new TempToSourceAndSkipped();
				Compiled.AddItem(tts);
				//Get Source back if anything fails
				CESetOnJumpFailTo(Compiled, first_runnable_execute_index, last_runnable_execute_index, tts);

				////Save Source
				//Compiled.AddItem(new NextToSource());
				////Change source
				//Compiled.AddItem(new ResultToNext());
				////Get right element
				//CompileElement(Item.rhs, Parent, Compiled);
				////Get source back
				//Compiled.AddItem(new SourceToNext());
				break;
			default:
				ThrowParseError("Unknown type", Item.Annotation.SourcePosition);
				break;
			}
		}
	}
}