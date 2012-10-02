using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;

namespace VPL
{
	/// <summary>
	/// A parse error structure
	/// </summary>
	public struct ParseError
	{
		public string Description;
		public TextPortion TextPortion;

		public ParseError(string newDescription, Match LastMatch, int newLength = 10)
		{
			TextPortion = new TextPortion();
			Description = newDescription;
			if (LastMatch != null)
				TextPortion.Begin = LastMatch.Index + LastMatch.Length;
			else
				TextPortion.Begin = 0;
			TextPortion.Length = newLength;
		}
		public ParseError(string NewDescription, int NewBegin, int NewLength)
		{
			TextPortion = new TextPortion();
			Description = NewDescription;
			TextPortion.Begin = NewBegin;
			TextPortion.Length = NewLength;
		}
		public ParseError(string newDescription, TextPortion NewTextPortion)
		{
			Description = newDescription;
			TextPortion = NewTextPortion;
		}
		public override string ToString()
		{
			return Description;
		}
	}

	namespace Parse
	{
		public class Function
		{
			/// <summary>
			/// Named blocks, labels, and calls to named blocks
			/// </summary>
			public class NamedBlocksClass
			{
				/// <summary>
				/// A list to calls to named blocks that are yet to be defined
				/// </summary>
				public class UndefinedBlockCall
				{
					public readonly List<ControlFlow> Items = new List<ControlFlow>();
					public Block Target;

					//Update the block call now that we found the target
					public void Update()
					{
						foreach (var cf in Items)
						{
							cf.Name = Target.Name;
							cf.Target = Target;
							cf.ReturnType = Target.ReturnType;
							if (cf.Type == ControlFlow.eType.Call)
							{
								if (!Target.CanBeCalled)
									Target.Annotation.IDE.Parser.ThrowParseError("This block cannot be called", cf.Annotation.SourcePosition);
							}
							else if (cf.Type == ControlFlow.eType.GoTo)
							{
								if (Target.GOTOArea != cf.Parent.GOTOArea)
									//There! Not There, There!, There, not There!	//<--Unhelpful references from 'Harvey Birdman, Attorney at Law' hinder the debugging process
									Target.Annotation.IDE.Parser.ThrowParseError("Cannot jump there from here", cf.Annotation.SourcePosition);
							}
						}
						Items.Clear();
					}
				}

				public Dictionary<string, Block> Defined = new Dictionary<string, Block>(StringComparer.OrdinalIgnoreCase);
				public Dictionary<string, UndefinedBlockCall> Undefined = new Dictionary<string, UndefinedBlockCall>(StringComparer.OrdinalIgnoreCase);
			}

			public string Name;
			public int ArgumentsCount;
			public Block Block = new Block();
			public HashSet<string> Aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			public NamedBlocksClass NamedBlocks = new NamedBlocksClass();
			/// <summary>
			/// Variables and arguments
			/// </summary>
			public Dictionary<string, int> Variables = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			/// <summary>
			/// The function access no variables or returns a value and can be inlined by calling only the block
			/// </summary>
			public bool Inlined;

			public readonly IDE.Annotation Annotation = new IDE.Annotation();

			public Function()
			{
				Block.Parent = null;
				Block.Function = this;
				Inlined = true;
			}
			public Function(string newName, int newArgumentsCount) : this()
			{
				Name = newName;
				ArgumentsCount = newArgumentsCount;
			}
		}

		public abstract class Element
		{
			/// <summary>
			/// The return type can be any combination of flags
			/// </summary>
			[Flags]
			public enum eReturnType
			{
				Undefined = 0,
				Nothing = 1,
				Number = 2,	//Not supported by compiler yet
				Text = 4,
				Node = 8,
				Variable = 16,
				_All = Nothing | Number | Text | Node | Variable,
				_Text = Text | Node | Variable,
				_Node = Node | Variable
			}

			/// <summary>
			/// Helper function to check if the element can be converted to the specified type
			/// </summary>
			public bool CanGet(eReturnType Type)
			{
				return (ReturnType != eReturnType.Undefined) && (ReturnType == (ReturnType & Type));
			}

			public eReturnType ReturnType = eReturnType.Undefined;
			public Quantifier Quantifier;
			public abstract eReturnType GetReturnType();

			public readonly IDE.Annotation Annotation = new IDE.Annotation();

			protected Element()
			{
				Annotation.Element = this;
				Quantifier.Min = 1;
				Quantifier.Max = 1;
			}

			//Replace an element in the parent struct with another
			/// <summary>
			/// Information about the parent and the Element's position inside it
			/// </summary>
			protected internal struct ChangeInParentInfo
			{
				public Element Parent;
				public int Index;
			}
			protected internal ChangeInParentInfo _ChangeInParentInfo;
			/// <summary>
			/// Change the Child with Other
			/// </summary>
			protected abstract void ChangeChild(Element Child, Element Other);
			/// <summary>
			/// Replace this in the parent struct with Other. Elements that do not have children should throw an exception
			/// </summary>
			public void ChangeInParent(Element Other)
			{
				if (_ChangeInParentInfo.Parent != null)
					_ChangeInParentInfo.Parent.ChangeChild(this, Other);
			}
			/// <summary>
			/// Update the children's _ChangeParentInfo. Elements that don't have children should not override it
			/// </summary>
			public virtual void UpdateChildrenParentInfo()
			{
				//
			}
		}
		public class Block : Element
		{
			/// <summary>
			/// Normal: succeed when all elements succeed
			/// Multi: succeed when an element succeeds
			/// For: if the first element succeeds repeatedly execute the second and third element alternatively, and succeed when the second fails or the third succeeds. fail when the third fails
			/// </summary>
			public enum eType
			{
				Normal = 0,
				Multi,
				For
			}
			public eType Type = eType.Normal;

			/// <summary>
			/// Function the block resides into
			/// </summary>
			public Function Function;

			protected Block _Parent;
			/// <summary>
			/// Get some structures from parent block
			/// </summary>
			public Block Parent
			{
				get { return _Parent; }
				set
				{
					_Parent = value;
					if (value == null)
					{
						Function = null;
						Depth = 0;
						GOTOArea = this;
					}
					else
					{
						Function = value.Function;
						Depth = value.Depth + 1;
						GOTOArea = value.GOTOArea;
					}

					//Update children
					foreach (var el in Elements)
					{
						var bl = el as Block;
						if (bl != null)
							bl.Parent = this;
					}
				}
			}

			/// <summary>
			/// Depth in the Blocks tree
			/// </summary>
			public int Depth;
			protected int _SucceedsUpToDepth;
			/// <summary>
			/// Denotes that it has a Succeed element that goes up a number of blocks
			/// </summary>
			public int SucceedsUpToDepth
			{
				get { return _SucceedsUpToDepth; }
				set
				{
					_SucceedsUpToDepth = value;
					if ((value >= 0) && (value <= Depth))
						for (var bl = Parent ; (bl != null) && (bl.Depth >= value) ; bl = bl.Parent)
							bl._SucceedsUpToDepth = value;
				}
			}
			/// <summary>
			/// Only inside the same area can a goto exist; typically an element inside a variable and function call designate a new area
			/// </summary>
			public Block GOTOArea;
			/// <summary>
			/// If a child block (of this) does Fail or Succeed on a parent block (of this) then it can't be called
			/// </summary>
			public bool CanBeCalled = true;
			
			public string Name;
			public List<Element> Elements = new List<Element>();

			/// <summary>
			/// Takes the original block's type and elements
			/// </summary>
			public void TransferFrom(Block Other)
			{
				Type = Other.Type;
				Annotation.SourcePosition = Other.Annotation.SourcePosition;

				//Just move the list
				Elements = Other.Elements;
				Other.Elements = new List<Element>();

				//Update parent
				foreach (var el2 in Elements)
					if (el2 is Block)
						((Block)el2).Parent = this;
			}

			/// <summary>
			/// Find a parent named Key
			/// </summary>
			/// <returns>Returns null if it doesn't find one</returns>
			public Block GetParentBlock(string Key)
			{
				Block bl = Parent;

				while ((bl != null) && (StringComparer.OrdinalIgnoreCase.Compare(bl.Name, Key) != 0))
					bl = bl.Parent;

				return null;
			}
			/// <summary>
			/// Find the most recent common parent
			/// </summary>
			public Block FindCommonAncestor(Block Other)
			{
				Block b1, b2;

				//Return nothing if it is irrelevant
				if ((Other == null) || (Function != Other.Function))
					return null;

				//b1 is the deeper block
				if (Depth > Other.Depth)
				{
					b1 = this;
					b2 = Other;
				}
				else
				{
					b1 = Other;
					b2 = this;
				}

				//Get b1 and b2 to be the same depth
				while ((b1 != null) && (b1.Depth > b2.Depth))
					b1 = b1.Parent;

				//It may be null if Function == null and they don't belong to the same tree
				if (b1 == null)
					return null;

				//Find a common ancestor
				while ((b1 != null) && (b2 != null) && (b1 != b2))
				{
					b1 = b1.Parent;
					b2 = b2.Parent;
				}

				//It may be null if Function == null and they don't belong to the same tree
				if ((b1 == null) || (b2 == null))
					return null;

				return b1;
			}

			/// <summary>
			/// To prevent recursion
			/// </summary>
			private bool _GettingReturnType;
			public override eReturnType GetReturnType()
			{
				if (_GettingReturnType)
					return ReturnType;

				_GettingReturnType = true;

				switch (Type)
				{
					//Typically the last element's return type; if the last block succeeds this or a parent block then its return type is ORed
				case eType.Normal:
					ReturnType = eReturnType.Undefined;

					var rt = eReturnType.Nothing;

					foreach (var el in Elements)
					{
						if ((el != null) && (el.GetReturnType() != eReturnType.Undefined))
						{
							var bl = el as Block;
							if ((bl != null) && (bl.SucceedsUpToDepth < bl.Depth))
							{
								if ((bl.ReturnType != eReturnType.Undefined) && (bl.ReturnType != eReturnType.Nothing))
									ReturnType |= bl.ReturnType;
								else
									ReturnType |= rt;
								rt = eReturnType.Undefined;
							}
							else
								rt = el.ReturnType;
						}
					}
					ReturnType |= rt;
					break;
					//An OR of all parts
				case eType.Multi:
					ReturnType = eReturnType.Undefined;
					foreach (var el in Elements)
						if (el != null)
							ReturnType |= el.GetReturnType();
						else
							ReturnType |= eReturnType.Nothing;
					break;
					//Returns nothing
				case eType.For:
					ReturnType = eReturnType.Undefined;
					foreach (Element el in Elements)
					{
						ReturnType = eReturnType.Nothing;

						if (el != null)
							el.GetReturnType();
					}
					break;
				}

				_GettingReturnType = false;

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				var an = Elements[Child._ChangeInParentInfo.Index].Annotation;
				Other.Annotation.SourcePosition = an.SourcePosition;
				Other.Annotation.IDE = an.IDE;
				Elements[Child._ChangeInParentInfo.Index] = Other;
			}
			public override void UpdateChildrenParentInfo()
			{
				for (int i = 0 ; i < Elements.Count ; i++)
				{
					var el = Elements[i];
					if (el != null)
					{
						el._ChangeInParentInfo.Parent = this;
						el._ChangeInParentInfo.Index = i;
						el.UpdateChildrenParentInfo();
					}
				}
			}
		}
		public class FunctionCall : Element
		{
			public string Name;
			public List<Element> Arguments = new List<Element>();
			public Function Target;

			public override eReturnType GetReturnType()
			{
				var parser = Annotation.IDE.Parser;

				foreach (var el in Arguments)
					if (el.GetReturnType() != eReturnType.Undefined)
						if (!el.CanGet(eReturnType._Text))
							parser.ThrowParseError("Argument value must be text, Node or Variable", el.Annotation.SourcePosition);

				if (Target == null)
					ReturnType = eReturnType.Undefined;
				else if (Target.Inlined)
					ReturnType = eReturnType.Nothing;
				else
					ReturnType = eReturnType.Node;

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				var an = Arguments[Child._ChangeInParentInfo.Index].Annotation;
				Other.Annotation.SourcePosition = an.SourcePosition;
				Other.Annotation.IDE = an.IDE;
				Arguments[Child._ChangeInParentInfo.Index] = Other;
			}
			public override void UpdateChildrenParentInfo()
			{
				for (int i = 0 ; i < Arguments.Count ; i++)
				{
					var arg = Arguments[i];
					if (arg != null)
					{
						arg._ChangeInParentInfo.Parent = this;
						arg._ChangeInParentInfo.Index = i;
						arg.UpdateChildrenParentInfo();
					}
				}
			}
		}
		public class TextSearch : Element
		{
			public enum eType
			{
				Normal = 0,
				Find,
				FindReverse,
				RegularExpression
			}

			public eType Type;
			public Element Pattern;
			public List<TreeNode> Tree = new List<TreeNode>();

			public override eReturnType GetReturnType()
			{
				switch (Type)
				{
				case eType.Normal:
				case eType.Find:
				case eType.FindReverse:
					ReturnType = eReturnType.Text;
					break;
				case eType.RegularExpression:
					if (Tree.Count > 0)
						ReturnType = eReturnType.Text;
					else
						ReturnType = eReturnType.Node;
					break;
				}

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				var an = Pattern.Annotation;
				Other.Annotation.SourcePosition = an.SourcePosition;
				Other.Annotation.IDE = an.IDE;
				Pattern = Other;
			}
			public override void UpdateChildrenParentInfo()
			{
				if (Pattern != null)
				{
					Pattern._ChangeInParentInfo.Parent = this;

					Pattern._ChangeInParentInfo.Index = 0;
					Pattern.UpdateChildrenParentInfo();
				}
			}
		}
		public class Literal : Element
		{
			public enum eType
			{
				Text,
				Number
			}

			public eType Type;
			public string Text, AssemblyDisplayText;
			public int Number;

			public Literal(){}
			public Literal(string newText, string newAssemblyDisplayText = null)
			{
				Type = eType.Text;
				Text = newText;
				AssemblyDisplayText = newAssemblyDisplayText ?? Text;
			}

			public override string ToString()
			{
				switch (Type)
				{
				case eType.Text:
					return AssemblyDisplayText;
				case eType.Number:
					return Number.ToString();
				}

				return "<Unknown>";
			}

			public override eReturnType GetReturnType()
			{
				switch (Type)
				{
				case eType.Text:
					ReturnType = eReturnType.Text;
					break;
				case eType.Number:
					ReturnType = eReturnType.Number;
					break;
				default:
					ReturnType = eReturnType.Nothing;
					break;
				}

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				throw new InvalidOperationException();
			}
		}
		public class Variable : Element
		{
			public enum eType
			{
				Variable,
				Tree,
				RetVal,
				Setting,
				Skipped,
				Source,
				New
			}

			public eType Type;
			public String Name;
			public List<TreeNode> Nodes = new List<TreeNode>();

			public override eReturnType GetReturnType()
			{
				var parser = Annotation.IDE.Parser;

				if (((Type == eType.Tree) || (Type == eType.RetVal) || (Type == eType.Variable) || (Type == eType.New)) && (Nodes.Count == 0))
					ReturnType = eReturnType.Variable;
				else if ((Type == eType.Skipped) || (Type == eType.Source) || ((Nodes.Count > 0) && (Nodes[Nodes.Count - 1].Type == TreeNode.eType.Value)))
					ReturnType = eReturnType.Text;
				else
					ReturnType = eReturnType.Node;

				foreach (var tn in Nodes)
				{
					var k = tn.Key;
					if (k != null)
					{
						k.GetReturnType();
						if (!k.CanGet(eReturnType._Text))
							parser.ThrowParseError("Expected an Element convertible to Text", k.Annotation.SourcePosition);
					}
				}

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				var an = Nodes[Child._ChangeInParentInfo.Index].Key.Annotation;
				Other.Annotation.SourcePosition = an.SourcePosition;
				Other.Annotation.IDE = an.IDE;
				Nodes[Child._ChangeInParentInfo.Index].Key = Other;
			}
			public override void UpdateChildrenParentInfo()
			{
				for (int i = 0 ; i < Nodes.Count ; i++)
				{
					var tn = Nodes[i].Key;
					if (tn != null)
					{
						tn._ChangeInParentInfo.Parent = this;
						tn._ChangeInParentInfo.Index = i;
						tn.UpdateChildrenParentInfo();
					}
				}
			}
		}
		public class TreeNode
		{
			public enum eType
			{
				/// <summary>
				/// Get a child by name, or create it
				/// </summary>
				Normal,
				/// <summary>
				/// Get another indexed sibling
				/// </summary>
				Indexed,
				/// <summary>
				/// Get a new child by name
				/// </summary>
				New,
				/// <summary>
				/// Get a child by name and index - it must exist
				/// </summary>
				NotNew,
				/// <summary>
				/// Get the Parent node
				/// </summary>
				Parent,
				/// <summary>
				/// Get the Next sibling
				/// </summary>
				Next,
				/// <summary>
				/// Get the Previous sibling
				/// </summary>
				Previous,
				/// <summary>
				/// Get the text only
				/// </summary>
				Value,
				/// <summary>
				/// Get the name
				/// </summary>
				Name
			}

			public eType Type;
			public Element Key, Value;
			public int Index;
			public bool ByValueNext, ByValueIgnoreCase;

			public TreeNode()
			{
			}
			public TreeNode(Element newKey, eType newType = eType.Normal, int newIndex = 0)
			{
				Key = newKey;
				Type = newType;
				Index = newIndex;
			}

			public override string ToString()
			{
				switch (Type)
				{
				case eType.Indexed:
					return "[" + Key + "|" + Index + "]";
				case eType.New:
					return "[" + Key + "|New]";
				case eType.NotNew:
					return "[" + Key + "|NotNew]";
				case eType.Parent:
					return "Parent";
				case eType.Value:
					return "Value";
				default:
					return "[" + Key + "]";
				}
			}
		}
		public class BinaryOperator : Element
		{
			/// <summary>
			/// All return the rhs result; ReferenceEqual returns it as a Node (even if it is a Variable), NotEqual returns nothing
			/// </summary>
			public enum eType
			{
				Equal = 0,
				NotEqual,
				ReferenceEqual,
				ReferenceNotEqual,
				ReferenceCopy,
				TextAppend,
				TreeAppend,
				TildeAppend,
				Tilde,
				Pipe
			}

			public eType Type;
			public Element lhs, rhs;
			public bool IgnoreCase;

			public override eReturnType GetReturnType()
			{
				var parser = Annotation.IDE.Parser;

				if (lhs != null)
					lhs.GetReturnType();
				if (rhs != null)
					rhs.GetReturnType();

				//Do some type-checking
				switch (Type)
				{
				case eType.Equal:
				case eType.NotEqual:
					if ((lhs != null) && (rhs != null) && (lhs is Literal) && (rhs is Literal))
						parser.ThrowParseError("Should not compare literals!", Annotation.SourcePosition);
					else
					{
						if ((lhs != null) && !lhs.CanGet(eReturnType._Text))
							parser.ThrowParseError("Should be able to return text", lhs.Annotation.SourcePosition);
						if ((rhs != null) && !rhs.CanGet(eReturnType._Text))
							parser.ThrowParseError("Should be able to return text", rhs.Annotation.SourcePosition);
					}
					break;
				case eType.TreeAppend:
					if ((lhs != null) && !lhs.CanGet(eReturnType._Node))
						parser.ThrowParseError("Only variables or nodes can be the left operand of a tree append", lhs.Annotation.SourcePosition);
					if ((rhs != null) && !rhs.CanGet(eReturnType._Node))
						parser.ThrowParseError("Only variables or nodes can be the right operand of a tree append", rhs.Annotation.SourcePosition);
					break;
				case eType.ReferenceCopy:
				case eType.ReferenceEqual:
				case eType.ReferenceNotEqual:
					if ((lhs != null) && (lhs.ReturnType != eReturnType.Variable))
						parser.ThrowParseError("Only variables can be the left operand of a reference copy", lhs.Annotation.SourcePosition);
					if ((rhs != null) && !rhs.CanGet(eReturnType._Node))
						parser.ThrowParseError("Only variables or nodes can be the right operand of a reference copy", rhs.Annotation.SourcePosition);
					break;
				case eType.Pipe:
					if (rhs != null && !(
						(rhs is Block) ||
						(rhs is FunctionCall) ||
						(rhs is TextSearch) ||
						(
						    (rhs is ControlFlow) &&
						    (((ControlFlow) rhs).Type == ControlFlow.eType.Call))))
						parser.ThrowParseError("Must pipe to a Text Search, Function, Block or Block Call", rhs.Annotation.SourcePosition);
					break;
				case eType.Tilde:
				case eType.TildeAppend:
					if ((lhs != null) && !lhs.CanGet(eReturnType._Text))
						parser.ThrowParseError("Should be able to return text", lhs.Annotation.SourcePosition);
					if ((rhs != null) && !rhs.CanGet(eReturnType._Node))
						parser.ThrowParseError("Must tilde to a Node or Variable", rhs.Annotation.SourcePosition);
					break;
				}

				switch (Type)
				{
				case eType.TextAppend:
					ReturnType = eReturnType.Text;
					break;
				case eType.Equal:
				case eType.Pipe:
				case eType.Tilde:
				case eType.TildeAppend:
					if (rhs != null)
						ReturnType = rhs.ReturnType;
					break;
				case eType.NotEqual:
					ReturnType = eReturnType.Nothing;
					break;
				case eType.ReferenceCopy:
				case eType.ReferenceEqual:
				case eType.ReferenceNotEqual:
					ReturnType = eReturnType.Node;
					break;
				}

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				if (Child._ChangeInParentInfo.Index == 0)
				{
					var an = lhs.Annotation;
					Other.Annotation.SourcePosition = an.SourcePosition;
					Other.Annotation.IDE = an.IDE;
					lhs = Other;
				}
				else
				{
					var an = rhs.Annotation;
					Other.Annotation.SourcePosition = an.SourcePosition;
					Other.Annotation.IDE = an.IDE;
					rhs = Other;
				}
			}
			public override void UpdateChildrenParentInfo()
			{
				if (lhs != null)
				{
					lhs._ChangeInParentInfo.Parent = this;

					lhs._ChangeInParentInfo.Index = 0;
					lhs.UpdateChildrenParentInfo();
				}
				if (rhs != null)
				{
					rhs._ChangeInParentInfo.Parent = this;
					rhs._ChangeInParentInfo.Index = 1;
					rhs.UpdateChildrenParentInfo();
				}
			}
		}
		public class ControlFlow : Element
		{
			public enum eType
			{
				Succeed,
				Fail,
				Call,
				GoTo,
				Continue
			}

			public eType Type;
			public string Name;
			public Block Target;
			public Block Parent;

			public override eReturnType GetReturnType()
			{
				if (Type == eType.Call)
					if (Target != null)
						if (Target.ReturnType == eReturnType.Undefined)
							ReturnType = Target.GetReturnType();
						else
							ReturnType = Target.ReturnType;
					else
						ReturnType = eReturnType.Undefined;
				else
					ReturnType = eReturnType.Nothing;

				return ReturnType;
			}

			protected override void ChangeChild(Element Child, Element Other)
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Used to hold info and highlight same-type syntax occurences
		/// </summary>
		public class SyntaxType
		{
			public enum eType
			{
				Normal = 0,
				//Parse
				Keyword,
				SubKeyword,
				Function,
				Call,
				TextMatch,
				Literal,
				Variable,
				Comment,
				SubBlock,
				Tree,
				Opereator,

				//Compiled
				cComment,
				Name,
				ArgName,
				ArgValue
			}
			[Flags]
			public enum eSyntaxType
			{
				Parse = 1,
				Compiled = 2
			}

			public string Name, RegExName;
			public eType Type;
			public List<TextPortion> Occurences = new List<TextPortion>();
			public Color Color = Color.Black;
			public int Length;

			public SyntaxType()
			{
			}
			public SyntaxType(string newName, eType newType, string newRegExName, Color newColor)
			{
				Name = newName;
				RegExName = newRegExName;
				Type = newType;
				Color = newColor;
			}

			private static void AddSyntaxType(Dictionary<eType, SyntaxType> SyntaxOccurences, SyntaxType st)
			{
				if (!SyntaxOccurences.ContainsKey(st.Type))
					SyntaxOccurences.Add(st.Type, st);
			}
			public static void InitList(Dictionary<eType, SyntaxType> SyntaxOccurences, eSyntaxType Type)
			{
				AddSyntaxType(SyntaxOccurences, new SyntaxType("Normal", eType.Normal, "()", Color.Black));
				if ((Type & eSyntaxType.Parse) == eSyntaxType.Parse)
					//Parse
				{
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Keyword", eType.Keyword, "_kw", Color.Blue));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Sub-keyword", eType.SubKeyword, "_skw", Color.SteelBlue));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Function", eType.Function, "_fun", Color.Maroon));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Call", eType.Call, "_call", Color.Indigo));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Comment", eType.Comment, "_com", Color.Green));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Sub-block", eType.SubBlock, "_sb", Color.IndianRed));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Text match", eType.TextMatch, "_tm", Color.Purple));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Literal", eType.Literal, "_lit", Color.PaleVioletRed));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Variable", eType.Variable, "_var", Color.FromArgb(0x0050FF)));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Sub tree", eType.Tree, "_tr", Color.FromArgb(0x00A0FF)));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Operator", eType.Opereator, "_op", Color.SlateBlue));
				}

				if ((Type & eSyntaxType.Compiled) == eSyntaxType.Compiled)
					//Compiled
				{
					AddSyntaxType(SyntaxOccurences, new SyntaxType("cComment", eType.cComment, string.Empty, Color.Green));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("Name", eType.Name, string.Empty, Color.Blue));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("ArgName", eType.ArgName, string.Empty, Color.SteelBlue));
					AddSyntaxType(SyntaxOccurences, new SyntaxType("ArgValue", eType.ArgValue, string.Empty, Color.Maroon));
				}
			}
		}
		public class GrammarTree
		{
			public void ThrowParseError(string Description, TextPortion TextPortion)
			{
				ParseErrors.Add(new ParseError(Description, TextPortion));
			}
			public List<ParseError> ParseErrors = new List<ParseError>();
			public readonly List<Function> Functions = new List<Function>(), ExternalFunctions = new List<Function>();
			public int MaxFunctionVariables;

			public IDE IDE;

			public void Clear()
			{
				ParseErrors.Clear();
				Functions.Clear();
				ExternalFunctions.Clear();
				MaxFunctionVariables = 0;
			}
		}
	}
}