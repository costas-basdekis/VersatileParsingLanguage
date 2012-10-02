using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace VPL.Execute
{
	public class PoolableTreeNodeSiblings : List<TreeNode>, IPoolable
	{
		public void Init()
		{
			Clear();
		}
	}
	public class PoolableTreeNodeChildren : Dictionary<string, PoolableTreeNodeSiblings>, IPoolable
	{
		public PoolableTreeNodeChildren():base(StringComparer.OrdinalIgnoreCase)
		{}
		public void Init()
		{
			Clear();
		}
	}
	/// <summary>
	/// Buffers
	/// </summary>
	internal static class Buffers
	{
		internal static readonly PooledBuffer<TreeRoot> RootBuffer = new PooledBuffer<TreeRoot>();
		internal static readonly PooledBuffer<TreeNode> NodeBuffer = new PooledBuffer<TreeNode>();
		internal readonly static PooledBuffer<FunctionStackFrame> FunctionStackFrameBuffer = new PooledBuffer<FunctionStackFrame>();
		internal readonly static PooledBuffer<BlockStackFrame>  BlockStackFrameBuffer = new PooledBuffer<BlockStackFrame>();
		internal readonly static PooledBuffer<PoolableTreeNodeSiblings> TreeNodeSiblings = new PooledBuffer<PoolableTreeNodeSiblings>();
		internal readonly static PooledBuffer<PoolableTreeNodeChildren>  TreeNodeChildren = new PooledBuffer<PoolableTreeNodeChildren>();
	}
	/// <summary>
	/// A variable tree node
	/// </summary>
	public class TreeNode : IPoolable
	{
		public TextPortion Item;
		public string Name = string.Empty;
		public int Index = -1;
		internal PoolableTreeNodeChildren _Children;
		private PoolableTreeNodeSiblings Siblings;
		public TreeNode Parent;
		public TreeRoot TreeRoot;

		/// <summary>
		/// Easy initialize a node
		/// </summary>
		public TreeNode SetData(TreeNode newParent, TreeRoot newRoot, string newName, PoolableTreeNodeSiblings newSiblings)
		{
			Parent = newParent;
			TreeRoot = newRoot;
			Siblings = newSiblings;
			Name = newName;

			return this;
		}
		/// <summary>
		/// Free all pooled objects
		/// </summary>
		public void Clear()
		{
			Init();
			Buffers.NodeBuffer.Release(this);
		}
		/// <summary>
		/// Required by IPoolable
		/// </summary>
		public void Init()
		{
			Parent = null;
			TreeRoot = null;

			//Clear children
			if (_Children != null)
			{
				foreach (var l in _Children)
				{
					foreach (var tn in l.Value)
						tn.Clear();
					l.Value.Clear();
					Buffers.TreeNodeSiblings.Release(l.Value);
				}
				_Children.Clear();
				Buffers.TreeNodeChildren.Release(_Children);
				_Children = null;
			}

			Name = string.Empty;
			Siblings = null;
			Item.Clear();
			Index = -2;
		}

		private static TreeNode _Item(PoolableTreeNodeSiblings SiblingsList, int nIndex, bool New, TreeNode Parent, TreeRoot Root, string Key)
		{
			//Create a new one
			if (New)
			{
				TreeNode tn = Buffers.NodeBuffer.New();
				tn.SetData(Parent, Root, Key, SiblingsList);
				tn.Index = SiblingsList.Count;
				SiblingsList.Add(tn);

				return tn;
			}
				//Return an existing one
			else
			{
				if (nIndex >= 0)
				{
					if (nIndex >= SiblingsList.Count)
						return null;
					else
						return SiblingsList[nIndex];
				}
				else
				{
					if (nIndex < -SiblingsList.Count)
						return null;
					else
						return SiblingsList[SiblingsList.Count + nIndex];
				}
			}
		}
		/// <summary>
		/// Get a named and indexed item from the list
		/// </summary>
		/// <param name="Key">The name of the item</param>
		/// <param name="ChildIndex">It's zero based index. -1 gets the last one, -2 gets the second from the end etc</param>
		/// <param name="New">If true then a new one is created</param>
		/// <param name="MustExist">If true then null is returned if there is no item named Key, instead of creating one</param>
		/// <returns>Returns null if the index does not exist</returns>
		public TreeNode GetChild(string Key, int ChildIndex, bool New, bool MustExist)
		{
			PoolableTreeNodeSiblings siblings_list;

			//Get a new children list if we want a new one
			if (_Children == null)
				if (New || !MustExist)
					_Children = Buffers.TreeNodeChildren.New();
				else
					return null;

			if (_Children.ContainsKey(Key))
				siblings_list = _Children[Key];
			else
			{
				if (MustExist)
					return null;
				else
				{
					siblings_list = Buffers.TreeNodeSiblings.New();
					_Children.Add(Key, siblings_list);
					New = true;
				}
			}

			return _Item(siblings_list, ChildIndex, New, this, TreeRoot, Key);
		}
		/// <summary>
		/// Get an indexed sibling
		/// </summary>
		public TreeNode Sibling(int nIndex, bool New)
		{
			return _Item(Siblings, nIndex, New, Parent, TreeRoot, Name);
		}
	}
	/// <summary>
	/// A variable tree root
	/// </summary>
	public class TreeRoot : IPoolable
	{
		public enum eType
		{
			Tree,
			RetVal,
			Variable
		}
		public eType Type;
		public TreeNode RootNode;
		private readonly PoolableTreeNodeSiblings _Top_List = Buffers.TreeNodeSiblings.New();

		/// <summary>
		/// Bookkeeping; when the _Usage is zero the resources are freed to the buffer 
		/// </summary>
		private int _Usage;
		/// <summary>
		/// Used for bookkeeping
		/// </summary>
		public void AddReference()
		{
			_Usage++;
		}
		/// <summary>
		/// Used for bookkeeping
		/// </summary>
		public void RemoveReference()
		{
			_Usage--;
			if (_Usage <= 0)
			{
				if (RootNode != null)
				{
					RootNode.Clear();
					RootNode = null;
				}
				Buffers.RootBuffer.Release(this);
			}
		}
		/// <summary>
		/// Bookkeeping; when the Usage is zero the resources are freed to the buffer 
		/// </summary>
		public int Usage
		{
			get { return _Usage; }
			set
			{
				_Usage = value;
				if (_Usage <= 0)
					RootNode.Clear();
			}
		}

		public TreeRoot()
		{
			Init();
		}
		public void Init()
		{
			RootNode = Buffers.NodeBuffer.New();
			Debug.Assert(RootNode.Index < 0);
			_Top_List.Clear();
			RootNode.SetData(null, this, string.Empty, _Top_List);
			RootNode.Index = 0;
			_Top_List.Add(RootNode);
			_Usage = 0;
		}
	}

	/// <summary>
	/// The result of an operation
	/// </summary>
	public struct Result
	{
		public enum eType
		{
			Nothing = 0,
			TextPortion,
			Node,
			RegEx
		}

		public eType Type;

		public TextPortion tpText;
		public TreeNode Node;
		public int Variable;
		public Match RegEx;

		/// <summary>
		/// Remove Node references if any
		/// </summary>
		public void RemoveNodeReference()
		{
			if (Type == eType.Node)
			{
				Node.TreeRoot.RemoveReference();
				Node = null;
				Type = eType.Nothing;
			}
		}

		public static implicit operator TextPortion(Result r)
		{
			switch (r.Type)
			{
			case eType.TextPortion:
			case eType.RegEx:
				return r.tpText;
			case eType.Node:
				return r.Node.Item;
			default:
				//return string.Empty;
				throw new InvalidOperationException();
			}
		}
		public static implicit operator TreeNode(Result r)
		{
			if (r.Type != eType.Node)
				throw new InvalidCastException();
			return r.Node;
		}
		public static implicit operator Result(TextPortion tp)
		{
			Result r = new Result();

			r.tpText = tp;
			r.Type = eType.TextPortion;

			return r;
		}
		public static implicit operator Result(Match m)
		{
			Result r = new Result();

			r.RegEx = m;
			r.Type = eType.RegEx;

			return r;
		}
		public static implicit operator Result(string s)
		{
			Result r = new Result();

			r.tpText = s;
			r.Type = eType.TextPortion;

			return r;
		}
		public static implicit operator Result(TreeNode tn)
		{
			Result r = new Result();

			if (tn == null)
				r.Type = eType.Nothing;
			else
			{
				r.Type = eType.Node;
				r.Node = tn;
			}

			return r;
		}

		public override string ToString()
		{
			try
			{
				return ((TextPortion) this).ToString();
			}
			catch (Exception)
			{
				return string.Empty;
			}
		}

		public void Clear()
		{
			Node = null;
			RegEx = null;
			tpText.Text = null;
			Type = eType.Nothing;
		}
	}

	/// <summary>
	/// The stack of a program
	/// </summary>
	public class ProgramStack
	{
		/// <summary>
		/// The top of the function stack
		/// </summary>
		public FunctionStackFrame FunctionStack;
		/// <summary>
		/// The size of the function stack
		/// </summary>
		public int StackSize;

		/// <summary>
		/// Temporaries stack
		/// </summary>
		public Stack<Result> Temporaries = new Stack<Result>(100);
		public Stack<int> TemporariesCount = new Stack<int>(100);

		public TreeNode RetVal, Tree;
		public bool Success;
		public TextPortion Source;

		/// <summary>
		/// A callback to the debugger
		/// </summary>
		public delegate void DebuggerMethod(ProgramStack ProgramStack);
		public struct DebuggerOptions
		{
			public DebuggerMethod DebuggerCallback;

			public enum eBreakOn
			{
				/// <summary>
				/// Never break
				/// </summary>
				Never,
				/// <summary>
				/// Break only when a breakpoint is encountered
				/// </summary>
				Breakpoints,
				/// <summary>
				/// Run only one element
				/// </summary>
				StepInto,
				/// <summary>
				/// Break when the next element in the block is about to be run (it also breaks if the block is returned)
				/// </summary>
				StepOver,
				/// <summary>
				/// Break when the block is returned
				/// </summary>
				StepOut
			}
			public eBreakOn BreakOn;

			/// <summary>
			/// Break if condition is met
			/// </summary>
			public bool Break(ProgramStack ProgramStack)
			{
				if (DebuggerCallback == null)
					return false;

				if (ProgramStack.FunctionStack == null)
				{
					DebuggerCallback(ProgramStack);
					return true;
				}

				bool retval = false;

				switch (BreakOn)
				{
				case eBreakOn.Never:
					return false;
				case eBreakOn.StepInto:
					retval = true;
					break;
				case eBreakOn.StepOver:
					retval = (ProgramStack.DeltaBlockStacks <= 0) && (ProgramStack.FunctionStack.BlockStack != null);
					break;
				case eBreakOn.StepOut:
					retval = (ProgramStack.DeltaBlockStacks < 0) &&
					         !((ProgramStack.FunctionStack.Previous == null) && (ProgramStack.FunctionStack.BlockStack == null));
					break;
				}

				if (!retval)
					//Check to see if we have hit a breakpoint
				{
					var fs = ProgramStack.FunctionStack;
					var bs = fs.BlockStack;
					if (bs != null)
					{
						if (bs.NextItem >= 0)
						{
							var bl = bs.Block;
							if (bs.NextItem < bl.Items.Length)
								retval = bl.Items[bs.NextItem].BreakPoint;
						}
					}
				}

				//Call the debugger
				if (retval)
				{
					DebuggerCallback(ProgramStack);
					ProgramStack.DeltaBlockStacks = 0;
				}

				return retval;
			}
		}
		public DebuggerOptions Debugger;
		/// <summary>
		/// How many additinal blocks have we called since last DeltaBlockStacks = 0. Negative indicates that the block has been unloaded.
		/// Used to know when to break when Step Over or Step Out is issued
		/// </summary>
		public int DeltaBlockStacks;

		public int ExecutionsCount;

		/// <summary>
		/// Load the function and don't execute any code
		/// </summary>
		public bool RunFunction(Function Function)
		{
			//Clear any previous stacks left
			while (FunctionStack != null)
			{
				ReturnBlock.StaticRun(this, FunctionStack.StackSize, false);
				ReturnFunction.StaticRun(this);
			}

			//Tree
			var tr = Buffers.RootBuffer.New();
			tr.AddReference();
			Tree = tr.RootNode;

			ExecutionsCount = 0;

			//Load function
			Success = CallFunction.StaticRun(this, Function);
			if (Success)
// ReSharper disable PossibleNullReferenceException
				FunctionStack.BlockStack.GetNextItem(this);
// ReSharper restore PossibleNullReferenceException

			DeltaBlockStacks = 0;

			Debugger.Break(this);

			return Success;
		}
		/// <summary>
		/// Execute commands until a breakpoint is hit
		/// </summary>
		/// <returns>The success of the last executed runnable</returns>
		public bool RunUntilBreakPoint()
		{
			//Run items
			while (FunctionStack != null)
			{
				var fs = FunctionStack;
				var bs = fs.BlockStack;

				if (bs == null)
					//End of function
					ReturnFunction.StaticRun(this);
				else if (bs.NextItem >= bs.Block.Items.Length)
					//End of block
					fs.Success = ReturnBlock.StaticRun(this, 1, fs.Success);
				else
					//Run the item
				{
					fs.Success = bs.Block.Items[bs.NextItem].Run(this);
					ExecutionsCount++;
				}

				//Get next item
				if ((FunctionStack != null) && (FunctionStack.BlockStack != null))
				{
					bs = FunctionStack.BlockStack;
					bs.GetNextItem(this);
				}

				//Break if appropriate
				if (Debugger.DebuggerCallback != null)
					if (Debugger.Break(this))
						break;
			}

			return Success;
		}
		/// <summary>
		/// Stop any execution
		/// </summary>
		public void Stop()
		{
			while (FunctionStack != null)
			{
				ReturnBlock.StaticRun(this, FunctionStack.StackSize, false);
				ReturnFunction.StaticRun(this);
			}

			//Inform debugger that we finished
			if (Debugger.DebuggerCallback != null)
				Debugger.DebuggerCallback(this);
		}
	}
	/// <summary>
	/// The stack frame of an executing function
	/// </summary>
	public class FunctionStackFrame : IPoolable
	{
		/// <summary>
		/// The stack part
		/// </summary>
		public FunctionStackFrame Previous;
		public const int MaxStackSize = 1500;

		public Function Function;

		/// <summary>
		/// Tree (Variables[0]), RetVal (Variables[1]) and variables and arguments
		/// </summary>
		public TreeNode[] Variables;

		public Result Result;
		public bool Success;
		public TextPortion Source;

		/// <summary>
		/// The block stack, pointing to the current block stack item
		/// </summary>
		public BlockStackFrame BlockStack;
		/// <summary>
		/// The size of the block stack
		/// </summary>
		public int StackSize;

		public void Init()
		{
			Previous = null;
			Result.Clear();
			BlockStack = null;
		}
	}
	/// <summary>
	/// The stack of an executing block
	/// </summary>
	public class BlockStackFrame : IPoolable
	{
		/// <summary>
		/// When a function has no variables or arguments then we call the block directly
		/// </summary>
		public bool InlinedFunctionCall;

		/// <summary>
		/// The corresponding Block
		/// </summary>
		public Block Block;
		/// <summary>
		/// The Block's next item to run, the PC
		/// </summary>
		public int NextItem;

		/// <summary>
		/// The current Block item's quantifier
		/// </summary>
		public Quantifier Quantifier;

		/// <summary>
		/// The stack part
		/// </summary>
		public BlockStackFrame Previous;
		public const int MaxStackSize = 1000;

		/// <summary>
		/// Temporaries size before excecuting this block (to remove un-Poped items)
		/// </summary>
		public int TemporariesStackSize;
		/// <summary>
		/// Block source (the one passed when it loaded) and next source (the one to pass to the next runnable)
		/// </summary>
		public TextPortion Source;//, NextSource;
		/// <summary>
		/// Keep track of whether NextSource is Source to save back when exiting the block successfully
		/// </summary>
		//public bool NextIsSource;
		/// <summary>
		/// The text skipped by the last text operation
		/// </summary>
		public TextPortion Skipped;

		/// <summary>
		/// Change the PC to what should be the next Runnable to be executed
		/// </summary>
		public void GetNextItem(ProgramStack ProgramStack)
		{
			if (ProgramStack.FunctionStack.Success)
				NextItem++;
			else if ((NextItem >= 0) && (NextItem < Block.Items.Length))
			{
				var OnFailJumpTo = Block.Items[NextItem].OnFailJumpTo;

				if (OnFailJumpTo != null)
					NextItem = OnFailJumpTo.ExecutionIndex;
				else
					NextItem = Block.Items.Length;
			}
		}

		public void Init()
		{
			Block = null;
			Previous = null;
			Source = string.Empty;
			//NextSource = string.Empty;
			Skipped.Text = null;
			TemporariesStackSize = 0;
			InlinedFunctionCall = false;
		}
		public void Clear(ProgramStack ProgramStack)
		{
			//Remove un-poped items
			if (ProgramStack != null)
				while (ProgramStack.Temporaries.Count > TemporariesStackSize)
					ProgramStack.Temporaries.Pop().RemoveNodeReference();

			Init();
		}
	}

	/// <summary>
	/// A name with parameters to display in the debugging windows
	/// </summary>
	public struct Description
	{
		public struct Parameter
		{
			public string Name, Value;

			public Parameter(string newName, string newValue)
			{
				Name = newName;
				Value = newValue;
			}
		}

		public string Name;
		public Parameter[] Parameters;

		public override string ToString()
		{
			var s = new StringBuilder(100);

			s.Append(Name);
			if (Parameters != null)
				for (int i = 0; i < Parameters.Length; i++)
				{
					var it = Parameters[i];
					if (i == 0)
						s.Append(" ");
					else
						s.Append(", ");
					if (it.Name != null)
						s.Append(it.Name + ":");
					if (it.Value != null)
						s.Append(it.Value);
				}

			return s.ToString();
		}
	}

	/// <summary>
	/// Abstract base class that is an "assembly" instruction
	/// </summary>
	public abstract class Runnable
	{
		/// <summary>
		/// Position in the block array
		/// </summary>
		public int ExecutionIndex;

		/// <summary>
		/// Used for quantifier
		/// </summary>
		public Runnable OnFailJumpTo;

		public bool BreakPoint;

		public readonly IDE.Annotation Annotation = new IDE.Annotation();

		/// <summary>
		/// Run the item
		/// </summary>
		public abstract bool Run(ProgramStack ProgramStack);

		public Description Description;
		public virtual Description GetDescription()
		{
			Description.Name = GetType().Name;

			return Description;
		}
	}

	public class Function
	{
		/// <summary>
		/// The amount of arguments
		/// </summary>
		public int ArgumentsCount;
		/// <summary>
		/// The amount of variables and arguments
		/// </summary>
		public int VariablesAndArgumentsCount;
		/// <summary>
		/// If the function access no variables or returns a value then we can just call the block
		/// </summary>
		public bool Inlined;
		/// <summary>
		/// The arguments and variables names for debugging purposes
		/// </summary>
		public Dictionary<int, string> VariablesNames;
		public Block Block;
		public string Name;

		public Function(int newArgumentsCount, int newVariablesAndArgumentsCount, string newName)
		{
			ArgumentsCount = newArgumentsCount;
			VariablesAndArgumentsCount = newVariablesAndArgumentsCount;
			Name = newName;
		}
	}
	public class CallFunction : Runnable
	{
		public Function Function;

		public CallFunction(Function newFunction)
		{
			Function = newFunction;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack, Function);
		}
		public static bool StaticRun(ProgramStack ProgramStack, Function Function)
		{
			var fs = ProgramStack.FunctionStack;

			TreeRoot tr;

			//Just call the block and return
			if (Function.Inlined && (fs != null))
			{

				//Load the block
				var block_load_success = CallBlock.StaticRun(ProgramStack, Function.Block);

				//We might have exceeded the MaxBlockStackSize
				if (block_load_success)
				{
					//Push the tree
					ProgramStack.Temporaries.Push(fs.Variables[0]);
					//Mark the block stack as inlined
					fs.BlockStack.InlinedFunctionCall = true;
				}

				return block_load_success;
			}

			//We must observe MaxStackSize
			if (ProgramStack.StackSize >= FunctionStackFrame.MaxStackSize)
				return false;

			//Create the new stack and initialize it
			var nfs = Buffers.FunctionStackFrameBuffer.New();
			nfs.Function = Function;
			nfs.Previous = fs;
			//Source
			if (nfs.Previous != null)
				nfs.Source = nfs.Previous.BlockStack.Source;//NextSource;
			else
				nfs.Source = ProgramStack.Source;
			if ((nfs.Variables == null) || (nfs.Variables.Length < (Function.VariablesAndArgumentsCount + 2)))
				nfs.Variables = new TreeNode[Function.VariablesAndArgumentsCount + 2];
			//The tree
			if (fs == null)
				nfs.Variables[0] = ProgramStack.Tree;
			else
				nfs.Variables[0] = fs.Variables[0];
			nfs.Variables[0].TreeRoot.AddReference();
			//The RetVal
			tr = Buffers.RootBuffer.New();
			tr.AddReference();
			nfs.Variables[1] = tr.RootNode;
			//The arguments
			for (var i = Function.ArgumentsCount + 1; i > 1; i--)
			{
				var r = ProgramStack.Temporaries.Pop();
				//Pass the node or convert the result as neccessary
				switch (r.Type)
				{
				case Result.eType.Node:
					nfs.Variables[i] = r.Node;
					nfs.Variables[i].TreeRoot.AddReference();
					break;
				case Result.eType.Nothing:
				case Result.eType.TextPortion:
					tr = Buffers.RootBuffer.New();
					tr.AddReference();
					nfs.Variables[i] = tr.RootNode;
					nfs.Variables[i].Name = Function.VariablesNames[i - 1];
					nfs.Variables[i].Item = r;
					break;
				case Result.eType.RegEx:
					tr = Buffers.RootBuffer.New();
					tr.AddReference();
					if (!TextRegExGetTree.StaticRun(ProgramStack, r, tr.RootNode))
						return false;
					nfs.Variables[i] = tr.RootNode;
					break;
				default:
					throw new InvalidOperationException();
				}
				r.RemoveNodeReference();
			}
			//The variables
			for (var i = 1; i <= (Function.VariablesAndArgumentsCount - Function.ArgumentsCount); i++)
			{
				tr = Buffers.RootBuffer.New();
				tr.AddReference();
				nfs.Variables[1 + Function.ArgumentsCount + i] = tr.RootNode;
				nfs.Variables[1 + Function.ArgumentsCount + i].Name = Function.VariablesNames[Function.ArgumentsCount + i];
			}

			//Update the stack
			ProgramStack.FunctionStack = nfs;
			ProgramStack.StackSize++;
			nfs.Success = true;

			if (ProgramStack.DeltaBlockStacks >= 0)
				ProgramStack.DeltaBlockStacks++;

			//Call the first block
			CallBlock.StaticRun(ProgramStack, Function.Block);

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter(null, Function.Name)};
			return Description;
		}
	}
	public class ReturnFunction : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			StaticRun(ProgramStack);

			return true;
		}
		public static void StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			fs.Result.RemoveNodeReference();

			//Return Source
			if (fs.Success)
				if (fs.Previous != null)
					fs.Previous.BlockStack.Source = fs.Source;//NextSource = fs.Source;
				else
					ProgramStack.Source = fs.Source;

			//Return RetVal
			var pfs = fs.Previous;
			if (pfs != null)
			{
				pfs.Success = fs.Success;
				//Transfer RetVal
				if (fs.Success)
				{
					pfs.Result.RemoveNodeReference();
					pfs.Result = fs.Variables[1];
					fs.Variables[1].TreeRoot.AddReference();
				}
			}
			else
			{
				ProgramStack.Success = fs.Success;
				//Transfer RetVal
				if (fs.Success)
				{
					if (ProgramStack.RetVal != null)
						ProgramStack.RetVal.TreeRoot.RemoveReference();
					ProgramStack.RetVal = fs.Variables[1];
					fs.Variables[1].TreeRoot.AddReference();
				}
			}

			//Clear variables
			for (int index = 0; index < (fs.Function.VariablesAndArgumentsCount + 2) ; index++)
				if (fs.Variables[index] != null)
				{
					fs.Variables[index].TreeRoot.RemoveReference();
					fs.Variables[index] = null;
				}

			//Update stack
			ProgramStack.FunctionStack = pfs;
			ProgramStack.StackSize--;

			ProgramStack.DeltaBlockStacks--;

			Buffers.FunctionStackFrameBuffer.Release(fs);
		}
	}

	public class Block
	{
		public Runnable[] Items;

		/// <summary>
		/// Used for assembly view
		/// </summary>
		public string Name;
	}
	public class CallBlock : Runnable
	{
		public Block Block;

		public CallBlock(Block newBlock)
		{
			Block = newBlock;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack, Block);
		}
		public static bool StaticRun(ProgramStack ProgramStack, Block Block)
		{
			var fs = ProgramStack.FunctionStack;

			//We must observe MaxStackSize
			if (fs.StackSize >= BlockStackFrame.MaxStackSize)
				return false;

			//Create BlockStack
			var bs = Buffers.BlockStackFrameBuffer.New();
			//Initialize
			bs.Previous = fs.BlockStack;
			bs.Block = Block;
			bs.NextItem = -1;
			bs.TemporariesStackSize = ProgramStack.Temporaries.Count;
			//Source
			if (bs.Previous != null)
				bs.Source = bs.Previous.Source;//NextSource;
			else
				bs.Source = fs.Source;
			//bs.NextSource = bs.Source;
			//bs.NextIsSource = true;
			//Update stack
			fs.BlockStack = bs;
			fs.StackSize++;

			if (ProgramStack.DeltaBlockStacks >= 0)
				ProgramStack.DeltaBlockStacks++;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			if (Block != null)
				Description.Parameters = new[]{new Description.Parameter(null, Block.Name)};

			return Description;
		}
	}
	public class ReturnBlock : Runnable
	{
		public int ParentBlocks;
		public bool Success;

		public ReturnBlock(int newParentBlocks, bool newSuccess)
		{
			ParentBlocks = newParentBlocks;
			Success = newSuccess;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack, ParentBlocks, Success);
		}
		public static bool StaticRun(ProgramStack ProgramStack, int ParentBlocks, bool Success)
		{
			var fs = ProgramStack.FunctionStack;
			BlockStackFrame bs;

			//Update stack
			for (int i = 0; i < ParentBlocks; i++)
			{
				bs = fs.BlockStack;
				//Update stack
				fs.BlockStack = bs.Previous;
				//Update source
				if (Success)
				{
					//if (bs.NextIsSource)
					//    bs.Source = bs.NextSource;
					if (bs.Previous != null)
						bs.Previous.Source = bs.Source;//NextSource = bs.Source;
					else
						fs.Source = bs.Source;
				}

				//Pop the original Tree
				if (bs.InlinedFunctionCall)
					fs.Variables[0] = ProgramStack.Temporaries.Pop();

				bs.Clear(ProgramStack);
				Buffers.BlockStackFrameBuffer.Release(bs);
			}
			fs.StackSize -= ParentBlocks;

			ProgramStack.DeltaBlockStacks -= ParentBlocks;

			return Success;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("ParentBlocks", ParentBlocks.ToString()),
			                         	new Description.Parameter("Success", Success.ToString())
			                         };
			return Description;
		}
	}
	public class GoToBlock : Runnable
	{
		public int ParentBlocksReturn, ChildBlocksEnter;
		public Runnable[] BlocksEnter;
		public string TargetName;

		public GoToBlock(int newParentBlocksReturn, int newChildBlocksEnter, Runnable[] newBlocksEnter)
		{
			ParentBlocksReturn = newParentBlocksReturn;
			ChildBlocksEnter = newChildBlocksEnter;
			BlocksEnter = newBlocksEnter;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			//Return parent blocks
			ReturnBlock.StaticRun(ProgramStack, ParentBlocksReturn, true);

			//Enter child blocks
			BlockStackFrame bs;
			for (int i = 0; i < (ChildBlocksEnter - 1); i++)
			{
				//Get index
				bs = fs.BlockStack;
				var ii = BlocksEnter[i];
				//Set PC
				bs.NextItem = ii.ExecutionIndex - 1;
				//Call block
				while (fs.BlockStack == bs)
					if (!bs.Block.Items[ii.ExecutionIndex].Run(ProgramStack))
						return false;
			}
			//Set PC on last
			bs = fs.BlockStack;
			bs.NextItem = BlocksEnter[ChildBlocksEnter - 1].ExecutionIndex - 1;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("Target", TargetName),
			                         	new Description.Parameter("ParentBlocksReturn", ParentBlocksReturn.ToString()),
			                         	new Description.Parameter("ChildBlocksEnter", ChildBlocksEnter.ToString())
			                         };

			return Description;
		}
	}

	public class QuantifierBefore : Runnable
	{
		public Quantifier Quantifier;

		public QuantifierBefore(Quantifier newQuantifier)
		{
			Quantifier = newQuantifier;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			bs.Quantifier = Quantifier;
			bs.Quantifier.Count = 0;

			//Put an empty string in the temp
			if (bs.Quantifier.Additive)
			{
				TextLiteral.StaticRun(ProgramStack, string.Empty);
				ResultToTemp.StaticRun(ProgramStack);
			}

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("Min", Quantifier.Min.ToString()),
			                         	new Description.Parameter("Max", Quantifier.Max.ToString()),
			                         	new Description.Parameter("Additive", Quantifier.Additive.ToString())
			                         };

			return Description;
		}
	}
	public class QuantifierAfter : Runnable
	{
		public QuantifierBefore QuantifierBefore;

		public QuantifierAfter(QuantifierBefore newQuantifierBefore)
		{
			QuantifierBefore = newQuantifierBefore;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			if (fs.Success)
			{
				//Append the result to temp
				if (bs.Quantifier.Additive)
				{
					ResultTextAppendToTempText.StaticRun(ProgramStack);
					ResultToTemp.StaticRun(ProgramStack);
				}

				bs.Quantifier.Count++;

				if ((bs.Quantifier.Count < bs.Quantifier.Max) || (bs.Quantifier.Max == -1))
				{
					//Re-do the item
					bs.NextItem = QuantifierBefore.ExecutionIndex;
					return true;
				}
			}

			//The loop is finished; get the success value
			var success = ((bs.Quantifier.Count >= bs.Quantifier.Min) &&
			               ((bs.Quantifier.Count <= bs.Quantifier.Max) || (bs.Quantifier.Max == -1)));

			//Get or discard the result
			if (bs.Quantifier.Additive)
				if (success)
					TempToResult.StaticRun(ProgramStack);
				else
					TempDiscard.StaticRun(ProgramStack);

			//Finish
			return success;
		}
	}
	public class ConditionalJump : Runnable
	{
		public Runnable JumpTarget;

		public ConditionalJump(Runnable newJumpTarget)
		{
			JumpTarget = newJumpTarget;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			if (fs.Success)
				bs.NextItem = JumpTarget.ExecutionIndex - 1;

			return true;
		}
	}

	public class NOP : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return true;
		}
	}
	public class NothingToResult : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result.Clear();
			return true;
		}
	}

	/*
	public class ResultToNext : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			bs.NextSource = fs.Result;
			bs.NextIsSource = false;
			return true;
		}
	}
	public class SourceToNext : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			bs.NextSource = bs.Source;
			bs.NextIsSource = true;
			return true;
		}
	}
	public class NextToSource : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			bs.Source = bs.NextSource;
			return true;
		}
	}
	*/

	public class SourceToResult : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			//Update result
			fs.Result.RemoveNodeReference();
			//if (bs.NextIsSource)
			//    fs.Result = bs.NextSource;
			//else
				fs.Result = bs.Source;
			return true;
		}
	}
	public class ResultToSource : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			bs.Source = fs.Result;

			return true;
		}
	}
	public class SkippedToResult : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = bs.Skipped;
			return true;
		}
	}

	public class ResultToTemp : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack);
		}
		public static bool StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			if (fs.Result.Type == Result.eType.Node)
				fs.Result.Node.TreeRoot.AddReference();
			ProgramStack.Temporaries.Push(fs.Result);
			return true;
		}
	}
	public class TempToResult : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack);
		}
		public static bool StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var tmp = ProgramStack.Temporaries;

			//Update result
			fs.Result = tmp.Pop();
			return true;
		}
	}
	public class TempDiscard : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			StaticRun(ProgramStack);

			return true;
		}
		public static void StaticRun(ProgramStack ProgramStack)
		{
			ProgramStack.Temporaries.Pop();
		}
	}

	public class SourceAndSkippedToTemp : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack);
		}
		public static bool StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;
			var tmp = ProgramStack.Temporaries;

			tmp.Push(bs.Source);
			tmp.Push(bs.Skipped);

			return true;
		}
	}
	public class TempToSourceAndSkipped : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack);
		}
		public static bool StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;
			var tmp = ProgramStack.Temporaries;

			bs.Skipped = tmp.Pop();
			bs.Source = tmp.Pop();

			//Don't change the success
			return fs.Success;
		}
	}

	public class PushTemporariesCount : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			ProgramStack.TemporariesCount.Push(ProgramStack.Temporaries.Count);

			return true;
		}
	}
	public class PopClearTemporaries : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var tc = ProgramStack.TemporariesCount.Pop();

			while (tc > ProgramStack.Temporaries.Count)
				ProgramStack.Temporaries.Pop();

			return ProgramStack.FunctionStack.Success;
		}
	}

	public class TempTextToResultNode : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			//Update result
			fs.Result.Node.Item = r;
			r.RemoveNodeReference();

			return true;
		}
	}
	public class TempTextAppendToResultNode : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			TextPortion tp = r;
			fs.Result.Node.Item.Append(tp);

			r.RemoveNodeReference();

			return true;
		}
	}
	public class ResultNodeToTempVariable : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			//Make sure we have a node
			if (fs.Result.Type == Result.eType.RegEx)
			{
				TreeRoot tr = Buffers.RootBuffer.New();
				tr.AddReference();
				if (!TextRegExGetTree.StaticRun(ProgramStack, fs.Result, tr.RootNode))
					return false;
				fs.Result = tr.RootNode;
			}

			//Update result
			fs.Variables[r.Variable].TreeRoot.RemoveReference();
			fs.Variables[r.Variable] = fs.Result;
			fs.Variables[r.Variable].TreeRoot.AddReference();

			return true;
		}
	}

	public class ResultTextAppendToTempText : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack);
		}
		public static bool StaticRun(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var tmp = ProgramStack.Temporaries;

			var r = tmp.Pop();

			TextPortion rtp = r, tp = fs.Result;
			rtp.Append(tp);

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = rtp;
			r.RemoveNodeReference();

			return true;
		}
	}
	public class ResultTextEqualTempText : Runnable
	{
		public bool Equal, IgnoreCase;

		public ResultTextEqualTempText(bool newEqual, bool newIgnoreCase)
		{
			Equal = newEqual;
			IgnoreCase = newIgnoreCase;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			TextPortion tp = fs.Result, rtp = r;

			r.RemoveNodeReference();

			//If not of equal length then obviously different
			if (rtp.Length != tp.Length)
				return !Equal;

			//Both refer to the same text
			if ((rtp.Text == tp.Text) && (rtp.Begin == tp.Begin))
				return Equal;

			//Do string comparison
			return rtp.Match(tp, IgnoreCase) == Equal;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("Equal", Equal.ToString()),
			                         	new Description.Parameter("IgnoreCase", IgnoreCase.ToString())
			                         };
			return Description;
		}
	}
	public class ResultNodeEqualTempNode : Runnable
	{
		public bool Equal;

		public ResultNodeEqualTempNode(bool newEqual)
		{
			Equal = newEqual;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			var success = ((r.Type == Result.eType.Node) && (fs.Result.Type == Result.eType.Node) && (r.Node == fs.Result.Node)) == Equal;

			r.RemoveNodeReference();

			return success;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("Reference Equal", Equal.ToString())
			                         };
			return Description;
		}
	}

	public class TextLiteral : Runnable
	{
		public string Value, AssemblyDisplayValue;

		public TextLiteral(string newValue, string newAssemblyDisplayValue = null)
		{
			Value = newValue;
			AssemblyDisplayValue = newAssemblyDisplayValue ?? Value;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack, Value);
		}
		public static bool StaticRun(ProgramStack ProgramStack, string Value)
		{
			var fs = ProgramStack.FunctionStack;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = Value;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[] { new Description.Parameter(null, AssemblyDisplayValue) };

			return Description;
		}
	}
	public class TextMatch : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			TextPortion tp = bs.Source/*NextSource*/, rtp = fs.Result;

			fs.Result.RemoveNodeReference();
			fs.Result = rtp;

			//Update next
			if (!bs.Source/*NextSource*/.Match(fs.Result.ToString(), true))
				return false;
			//Update skipped
			bs.Skipped = string.Empty;
			//Update result
			tp.Length = fs.Result.tpText.Length;
			fs.Result = tp;

			return true;
		}
	}
	public class TextFind : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			int original_begin = bs.Source/*NextSource*/.Begin;
			TextPortion rtp = fs.Result;
			var pattern = rtp.ToString();

			fs.Result.RemoveNodeReference();

			//Update next
			if (!bs.Source/*NextSource*/.Find(pattern))
				return false;
			//Update skipped
			bs.Skipped = new TextPortion(bs.Source.Text, original_begin, bs.Source.Begin - pattern.Length - original_begin);
			//if (bs.NextIsSource)
			//{
			//    bs.Skipped.Text = bs.NextSource.Text;
			//    bs.Skipped.Begin = original_begin;
			//    bs.Skipped.End = bs.NextSource.Begin - pattern.Length;
			//}
			//Update result
			fs.Result.tpText.Text = bs.Source/*NextSource*/.Text;
			fs.Result.tpText.Begin = bs.Source/*NextSource*/.Begin - pattern.Length;
			fs.Result.tpText.Length = pattern.Length;

			return true;
		}
	}
	public class TextFindReverse : Runnable
	{
		public string Pattern;
		public bool UseLastText, ExportResult, ExportSkipped, AppendResult;

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			int original_begin = bs.Source/*NextSource*/.Begin;
			TextPortion rtp = fs.Result;
			var pattern = rtp.ToString();

			fs.Result.RemoveNodeReference();

			//Update next
			if (!bs.Source/*NextSource*/.FindReverse(pattern))
				return false;
			//Update skipped
			//if (bs.NextIsSource)
			{
				bs.Skipped.Text = bs.Source/*NextSource*/.Text;
				bs.Skipped.Begin = original_begin;
				bs.Skipped.End = bs.Source/*NextSource*/.Begin - pattern.Length;
			}
			//Update result
			fs.Result.tpText.Text = bs.Source/*NextSource*/.Text;
			fs.Result.tpText.Begin = bs.Source/*NextSource*/.Begin - pattern.Length;
			fs.Result.tpText.Length = pattern.Length;

			return true;
		}
	}
	public class TextRegEx : Runnable
	{
		public Regex Pattern;

		public TextRegEx(string newPattern)
		{
			Pattern = new Regex(newPattern, RegexOptions.Compiled | RegexOptions.Singleline);
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var bs = fs.BlockStack;

			TextPortion tp = bs.Source/*NextSource*/;
			Match m = Pattern.Match(tp.Text, tp.Begin, tp.Length);

			//Update next
			if (!m.Success)
				return false;
			bs.Source/*NextSource*/.MoveCaretTo(m.Index + m.Length);
			//Update skipped
			//if (bs.NextIsSource)
				if (m.Index != tp.Begin)
				{
					tp.Length = m.Index - tp.Begin;
					bs.Skipped = tp;
				}
				else
					bs.Skipped = string.Empty;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = m;
			fs.Result.tpText = new TextPortion(bs.Source/*NextSource*/.Text, m.Index, m.Length);

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter(null, Pattern.ToString())};

			return Description;
		}
	}
	public class TextRegExGetItem : Runnable
	{
		public int Index;

		public TextRegExGetItem(int newIndex)
		{
			Index = newIndex;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;
			var grps = fs.Result.RegEx.Groups;

			//TextPortion tp;
			Capture c;

			//Check for proper boundaries
			if (Index >= 0)
				if (Index >= grps.Count)
					return false;
				else
					c = grps[Index];
			else if (-Index > grps.Count)
				return false;
			else
				c = grps[grps.Count + Index];

			fs.Result = new TextPortion(fs.Result.tpText.Text, c.Index, c.Length);
			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter("Index", Index.ToString())};

			return Description;
		}
	}
	public class TextRegExGetTree : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			//Create root
			TreeRoot tr = Buffers.RootBuffer.New();

			if (!StaticRun(ProgramStack, fs.Result, tr.RootNode))
				return false;

			//Update result
			fs.Result = tr.RootNode;
			tr.AddReference();

			return true;
		}
		public static bool StaticRun(ProgramStack ProgramStack, Result Result, TreeNode Target)
		{
			var grps = Result.RegEx.Groups;
			var source = Result.tpText.Text;

			Target.Name = "RegEx";
			Target.Item = Result.tpText;

			//Create nodes
			foreach (Group grp in grps)
				Target.GetChild("Group", 0, true, false).Item = new TextPortion(source, grp.Index, grp.Length);

			return true;
		}
	}

	public class NodeFromTree : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return NodeFromVariable.StaticRun(ProgramStack, 0 - 1);
		}
	}
	public class NodeFromRetVal : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			return NodeFromVariable.StaticRun(ProgramStack, 1 - 1);
		}
	}
	public class NodeFromVariable : Runnable
	{
		public int Index;
		public string Name;

		public NodeFromVariable(int newIndex)
		{
			Index = newIndex;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			return StaticRun(ProgramStack, Index);
		}
		public static bool StaticRun(ProgramStack ProgramStack, int Index)
		{
			var fs = ProgramStack.FunctionStack;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = fs.Variables[Index + 1];
			fs.Result.Variable = Index + 1;
			fs.Result.Node.TreeRoot.AddReference();

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter(null, Name)};

			return Description;
		}
	}
	public class NodeFromNew : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var tr = new TreeRoot();
			tr.AddReference();

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = tr.RootNode;

			return true;
		}
	}

	public class ResultNodeAppendToTempNode : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();

			/*//Make a new child with same name and text
			r = r.Node.GetChild(fs.Result.Node.Name, 0, true, false);
			r.Node.Item = fs.Result.Node.Item;*/

			//Copy structure
			CopyTree(fs.Result.Node, r.Node);

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = r;

			return true;
		}

		private void CopyTree(TreeNode Source, TreeNode Target)
		{
			var dict_source = Source._Children;

			if (dict_source != null)
				foreach (var kv in dict_source)
				{
					var lst_source = kv.Value;
					var key = kv.Key;
					foreach (var tn_source in lst_source)
					{
						var tn_dest = Target.GetChild(key, 0, true, false);
						tn_dest.Item = tn_source.Item;
						CopyTree(tn_source, tn_dest);
					}
				}
		}
	}

	public class NodeByName : Runnable
	{
		public int Index;
		public bool New, MustExist;

		public NodeByName(int newIndex, bool newNew, bool newMustExist)
		{
			Index = newIndex;
			New = newNew;
			MustExist = newMustExist;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();
			TextPortion tp = fs.Result;

			fs.Result.RemoveNodeReference();

			var tn = r.Node.GetChild(tp.ToString(), Index, New, MustExist);

			if (tn == null)
			{
				r.RemoveNodeReference();
				return false;
			}

			//Update result
			fs.Result = tn;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{
			                         	new Description.Parameter("Index", Index.ToString()), new Description.Parameter("New", New.ToString()),
			                         	new Description.Parameter("MustExist", MustExist.ToString())
			                         };

			return Description;
		}
	}
	public class NodeSiblingByIndex : Runnable
	{
		public int Index;

		public NodeSiblingByIndex(int newIndex)
		{
			Index = newIndex;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var tn = fs.Result.Node.Sibling(Index, false);

			if (tn == null)
				return false;

			//Update result
			fs.Result = tn;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter("Index", Index.ToString())};

			return Description;
		}
	}
	public class NodeSiblingNew : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			//Update result
			fs.Result = fs.Result.Node.Sibling(0, true);

			return true;
		}
	}
	public class NodeParent : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var tn = fs.Result.Node.Parent;

			if (tn == null)
				return false;

			//Update result
			fs.Result = tn;

			return true;
		}
	}
	public class NodeSiblingByNeighbour : Runnable
	{
		public bool Next;

		public NodeSiblingByNeighbour(bool newNext)
		{
			Next = newNext;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var tn = fs.Result.Node;
			//We must do this check because negative Index gets the item from the end
			if (!Next && (tn.Index == 0))
				return false;
			tn = tn.Sibling(tn.Index + (Next ? 1 : -1), false);

			if (tn == null)
				return false;

			//Update result
			fs.Result = tn;

			return true;
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter("Next", Next.ToString())};

			return Description;
		}
	}
	public class NodeSiblingByValue : Runnable
	{
		public bool Next;
		public bool IgnoreCase;

		public NodeSiblingByValue(bool newNext, bool newIgnoreCase)
		{
			IgnoreCase = newIgnoreCase;
			Next = newNext;
		}

		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var r = ProgramStack.Temporaries.Pop();
			TextPortion tp = fs.Result;
			fs.Result.RemoveNodeReference();

			TreeNode tn = r.Node;

			while (true)
			{
				//If it matches then succeed
				if (tp.Compare(tn.Item, IgnoreCase))
				{
					fs.Result = tn;
					return true;
				}

				//If there is no next then fail
				//We must do this check because negative Index gets the item from the end
				if (!Next && (tn.Index == 0))
				{
					r.RemoveNodeReference();
					return false;
				}
				tn = tn.Sibling(tn.Index + (Next ? 1 : -1), false);
				if (tn == null)
				{
					r.RemoveNodeReference();
					return false;
				}
			}
		}

		public override Description GetDescription()
		{
			Description.Name = GetType().Name;
			Description.Parameters = new[]{new Description.Parameter("Next", Next.ToString()), new Description.Parameter("IgnoreCase", IgnoreCase.ToString())};

			return Description;
		}
	}

	public class NodeValue : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var tp = fs.Result.Node.Item;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = tp;

			return true;
		}
	}
	public class NodeName : Runnable
	{
		public override bool Run(ProgramStack ProgramStack)
		{
			var fs = ProgramStack.FunctionStack;

			var str = fs.Result.Node.Name;

			//Update result
			fs.Result.RemoveNodeReference();
			fs.Result = str;

			return true;
		}
	}

	public class ExecutionTree
	{
		public void ThrowParseError(string Description, TextPortion TextPortion)
		{
			ParseErrors.Add(new ParseError(Description, TextPortion));
		}

		public readonly List<ParseError> ParseErrors = new List<ParseError>();
		public readonly Dictionary<string, Function> Functions = new Dictionary<string, Function>();
		public int MaxFunctionVariables;

		public IDE IDE;

		public void Clear()
		{
			ParseErrors.Clear();
			Functions.Clear();
			MaxFunctionVariables = 0;
		}
	}
}