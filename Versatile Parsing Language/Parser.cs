using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VPL.Parse;

namespace VPL
{
	/// <summary>
	/// This class is used to prevent from parsing two consecutive binary operators (eg a + = b).
	/// It is added in the parent block to signify that and is not used for any other purpose.
	/// </summary>
	public class BinaryOperatorStub : Element
	{
		public override eReturnType GetReturnType()
		{
			return eReturnType.Undefined;
		}
		protected override void ChangeChild(Element Child, Element Other)
		{
			throw new InvalidOperationException();
		}
		public override void UpdateChildrenParentInfo()
		{
			//
		}
	}

	/// <summary>
	/// Parse a source file into a grammar tree and return syntax highlighting
	/// </summary>
	public class Parser
	{
		/// <summary>
		/// A string augmented with RegEx and position handling
		/// </summary>
		private class Caret
		{
			//Whitespace + // + /**/ comments
			private const string RWS = @"(((\s+)|(?<_com>/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*?(\*+/|$))|(?<_com>//[^\r\n]*[\r\n]*))+)";

			public string Source;
			public int Position;
			public Match LastMatch;

			public Dictionary<SyntaxType.eType, SyntaxType> SyntaxOccurences;

			/// <summary>
			/// Cached regexes
			/// </summary>
			private static readonly Dictionary<string, Regex> CachedRegExes = new Dictionary<string, Regex>();

			/// <summary>
			/// Initialize the caret
			/// </summary>
			/// <param name="newSource">The source. If null it does not change</param>
			/// <param name="newPosition">The initial position in the source</param>
			public void Initialize(string newSource, int newPosition = 0)
			{
				LastMatch = null;
				if (newSource != null)
					Source = newSource;
				Position = newPosition;
			}

			/// <summary>
			/// Match exactly at the current position
			/// </summary>
			public bool Match(string Expression)
			{
				Regex re;
				if (CachedRegExes.ContainsKey(Expression))
					re = CachedRegExes[Expression];
				else
				{
					re = new Regex(string.Format(@"\G(?:{0})", Expression.Replace(@"{ws}", RWS)), RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Singleline | RegexOptions.Compiled);
					CachedRegExes.Add(Expression, re);
				}
				Match mt;

				mt = re.Match(Source, Position);
				if (mt.Success)
				{
					LastMatch = mt;
					Position = LastMatch.Index + LastMatch.Length;
					AddNewSyntaxOccurences();
				}

				return mt.Success;
			}

			// ReSharper disable UnusedMember.Local
			// ReSharper disable UnusedMethodReturnValue.Local
			/// <summary>
			/// Match all expressions. It resets LastMatch and Position to original if anyone fails
			/// </summary>
			public bool All(params string[] Expressions)
			{
				Match m = LastMatch;
				int p = Position;

				if (Expressions.Any(t => !Match(t)))
				{
					LastMatch = m;
					Position = p;
					return false;
				}

				return true;
			}

			/// <summary>
			/// Match as many sub expressions as possible
			/// </summary>
			/// <returns>Returns true if at least one expression is successful</returns>
			public bool AsMany(params string[] Expressions)
			{
				Match m = LastMatch;
				int p = Position;

				for (int i = 0 ; i < Expressions.Length ; i++)
					if (!Match(Expressions[i]))
						if (i == 0)
						{
							LastMatch = m;
							Position = p;
							return false;
						}
						else
							return true;

				return true;
			}

			/// <summary>
			/// Match Whitespace + // + /**/ comments
			/// </summary>
// ReSharper disable MemberCanBePrivate.Local
			public bool WhiteSpace()
// ReSharper restore MemberCanBePrivate.Local
			{
				return Match(RWS);
			}
			// ReSharper restore UnusedMethodReturnValue.Local
			// ReSharper restore UnusedMember.Local

			/// <summary>
			/// Ignore Whitespace + // + /**/ comments
			/// </summary>
			public Caret WS()
			{
				WhiteSpace();

				return this;
			}

			/// <summary>
			/// Annotate the source text with pre-defined syntax highlighting using RegEx group names
			/// </summary>
			private void AddNewSyntaxOccurences()
			{
				if (SyntaxOccurences != null)
					foreach (var so in SyntaxOccurences)
					{
						var crs = LastMatch.Groups[so.Value.RegExName].Captures;
						for (int i = 0 ; i < crs.Count ; i++)
							so.Value.Occurences.Add(crs[i]);
					}
			}
		}
		/// <summary>
		/// A list to calls to functions that are yet to be defined
		/// </summary>
		private class UndefinedFunctionCall
		{
			public readonly List<FunctionCall> Items = new List<FunctionCall>();
			private readonly Parser Parser;

			public UndefinedFunctionCall(Parser newParser)
			{
				Parser = newParser;
			}

			public void Update(Function Target)
			{
				foreach (FunctionCall fc in Items)
				{
					fc.Name = Target.Name;
					fc.Target = Target;
					if (fc.Target.ArgumentsCount != fc.Arguments.Count)
						Parser.ThrowParseError("Function " + fc.Target.Name + " takes " + fc.Target.ArgumentsCount + " arguments, not " + fc.Arguments.Count, fc.Annotation.SourcePosition);
				}
				Items.Clear();
			}
		}
		/// <summary>
		/// Error with description and position from caret's last match
		/// </summary>
		internal void ThrowParseError(string Description)
		{
			ThrowParseError(Description, _caret.LastMatch);
		}
		/// <summary>
		/// Error with description and position from caret's last match, with Length defined
		/// </summary>
		internal void ThrowParseError(string Description, int Length)
		{
			IDE.GrammarTree.ParseErrors.Add(new ParseError(Description, _caret.LastMatch, Length));
		}
		/// <summary>
		/// Error with description and position defined
		/// </summary>
		internal void ThrowParseError(string Description, TextPortion TextPortion)
		{
			IDE.GrammarTree.ThrowParseError(Description, TextPortion);
		}
		private readonly Caret _caret = new Caret();

		/// <summary>
		/// Predefined keywords that cannot be used in naming Functions, Blocks or Variables
		/// </summary>
		private readonly HashSet<string> Keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// Predefined functions
		/// </summary>
		private readonly Dictionary<string, Function> PredefinedFunctions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// Functions defined in the current file
		/// </summary>
		private readonly Dictionary<string, Function> UserFunctions = new Dictionary<string, Function>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// Functions imported by other files
		/// </summary>
		private readonly Dictionary<string, Function> ImportedFunctions = new Dictionary<string, Function>();
		/// <summary>
		/// Function calls not yet resolved due to functions targets not yet defined, or in imported or predefined functions lists (which can be overriden by functions defined in current file)
		/// </summary>
		private readonly Dictionary<string, UndefinedFunctionCall> UndefinedFunctionCalls = new Dictionary<string, UndefinedFunctionCall>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// The list of functions defined in this file to be exposed
		/// </summary>
		private List<Function> Functions;
		/// <summary>
		/// A list of Continue control flows waiting to be given the correct target
		/// </summary>
		private readonly List<ControlFlow>  PendingContinueControlFlows = new List<ControlFlow>();

		/// <summary>
		/// A cache of compiled RegExes to speeden up parsing; the mapped string is the error description for that regex, whith null signifying success
		/// </summary>
		private readonly Dictionary<string, string> CompiledRegExErrors = new Dictionary<string, string>();

		public IDE IDE;

		private TextPortionTree<IDE.Annotation> SourceCodeAnnotations;
		/// <summary>
		/// Add source code annotations automation
		/// </summary>
		private void AddSourceCodeAnnotation(Element Item)
		{
			Item.Annotation.Element = Item;
			Item.Annotation.IDE = IDE;
			if (SourceCodeAnnotations != null)
				SourceCodeAnnotations.Add(Item.Annotation.SourcePosition, Item.Annotation);
		}

		/// <summary>
		/// Initialize Keywords and Predefined Functions lists
		/// </summary>
		public Parser()
		{
			Clear();

			//Reserved keywords
			Keywords.Add("Function");

			//Variable related
			Keywords.Add("Tree");
			Keywords.Add("Skipped");
			Keywords.Add("RetVal");
			Keywords.Add("RV");
			Keywords.Add("Position");

			//Flow control
			Keywords.Add("Succeed");
			Keywords.Add("Fail");
			Keywords.Add("GoTo");
			Keywords.Add("For");
			Keywords.Add("Continue");
			Keywords.Add("Break");

			//Quantifiers
			Keywords.Add("as_many");
			Keywords.Add("if_any");
			Keywords.Add("never");

			//Match/Find
			Keywords.Add("Match");
			Keywords.Add("Find");
			Keywords.Add("F");
			Keywords.Add("FindReverse");
			Keywords.Add("FR");
			Keywords.Add("RegEx");
			Keywords.Add("RE");
			Keywords.Add("FindRegEx");
			Keywords.Add("FRE");

			//Tree
			Keywords.Add("New");
			Keywords.Add("NotNew");
			Keywords.Add("Parent");
			Keywords.Add("Next");
			Keywords.Add("Previous");
			Keywords.Add("Value");
			Keywords.Add("Name");

			//Predefined functions
			PredefinedFunctions.Add("Match", new Function("Match", 1));
			PredefinedFunctions.Add("Find", new Function("Find", 1));
			PredefinedFunctions.Add("FindReverse", new Function("FindReverse", 1));
			//PredefinedFunctions.Add("RegEx", new Function("RegEx", 1));
			PredefinedFunctions.Add("Line", new Function("Line", 0));
			PredefinedFunctions.Add("Paragraph", new Function("Paragraph", 0));
		}
		/// <summary>
		/// Clear structure
		/// </summary>
		public void Clear()
		{
			UserFunctions.Clear();
			ImportedFunctions.Clear();
			UndefinedFunctionCalls.Clear();
		}

		/// <summary>
		/// Parse Source for grammar
		/// </summary>
		/// <returns>
		/// True if the grammar tree can be updated, False if unrecoverable errors occurred
		/// ParseError.Count indicates success or failure
		/// </returns>
		public bool Parse()
		{
			//Clear structures
			Clear();

			if ((IDE == null) || (IDE.GrammarTree == null))
				return false;

			IDE.GrammarTree.Clear();
			Functions = IDE.GrammarTree.Functions;
			SourceCodeAnnotations = IDE.SourceAnnotations;

			IDE.ClearSourceSyntaxOccurences();
			if (SourceCodeAnnotations != null)
				SourceCodeAnnotations.Clear();

			//Prepare caret
			_caret.SyntaxOccurences = IDE.ParseSyntaxOccurences;
			_caret.Initialize(IDE.SourceText);

			//Parse the file
			if (!ParseImports())
				return false;
			if (!ParseFunctions())
				return false;
			ResolveUndefinedFunctionCalls();

			//Infere the return types and do type checking
			foreach (var fun in Functions)
				fun.Block.GetReturnType();

			return true;
		}
		/// <summary>
		/// Parse imports and imported functions' aliases
		/// </summary>
		/// <returns></returns>
		public bool ParseImports()
		{
			Dictionary<string, Dictionary<string, List<string>>> imports = new Dictionary<string, Dictionary<string, List<string>>>();
			HashSet<string> all_aliases = new HashSet<string>();

			//Find all imports
			while (_caret.WS().Match(@"(?<_kw>import)(?!\w)"))
			{
				Element el;
				string file = null;
				Dictionary<string, List<string>> aliases = new Dictionary<string, List<string>>();

				//Get the file name
				el = PELiteral(null);
				if ((el == null) || !(el is TextSearch) || (((TextSearch)el).Type != TextSearch.eType.Normal))
					ThrowParseError("Expected the file name", (el == null) ? _caret.LastMatch : el.Annotation.SourcePosition);
				else
				{
					file = ((Literal)(((TextSearch)el).Pattern)).Text;

					if (imports.ContainsKey(file))
					{
						ThrowParseError("File already imported", el.Annotation.SourcePosition);
						file = null;
					}
				}

				//Get function aliases
				if (_caret.WS().Match(@"(?<_kw>aliases)(?!\w)"))
				{
					if (!_caret.WS().Match(@"\{"))
						ThrowParseError("Expected {");
					else
					{
						while (true)
						{
							//End of aliases
							if (_caret.WS().Match(@"}"))
							{
								_caret.Position = _caret.LastMatch.Index;
								break;
							}
							
							//Get alias(es)
							if (!_caret.WS().Match(@"(?<_fun>\w+)(?!\w){ws}?(\>{ws}?(?<_fun>\w+)(?!\w)({ws}?,{ws}?(?<_fun>\w+)(?!\w))*)?"))
							{
								ThrowParseError("Expected 'Function>Alias1,Alias2' or 'Function>Alias' or 'Function'");
								break;
							}
							else
							{
								var grps = _caret.LastMatch.Groups["_fun"];
								var fun_name = grps.Captures[0].Value;
								List<string> fun_aliases;
								
								//Get aliases list and add if needed
								if (aliases.ContainsKey(fun_name))
									fun_aliases = aliases[fun_name];
								else
								{
									fun_aliases = new List<string>();
									aliases.Add(fun_name, fun_aliases);
								}

								//Check if an alias is already declared, and if not add it
								if (grps.Captures.Count > 1)
								{
									for (int i = 1 ; i < grps.Captures.Count ; i++)
									{
										var c = grps.Captures[i];
										if (all_aliases.Contains(c.Value))
											ThrowParseError(string.Format("Alias '{0}' already declared", c.Value), c);
										else
										{
											fun_aliases.Add(c.Value);
											all_aliases.Add(c.Value);
										}
									}
								}
								//Import without changing the name
								else
								{
									if (all_aliases.Contains(fun_name))
										ThrowParseError(string.Format("Alias '{0}' already declared", fun_name));
									else
									{
										fun_aliases.Add(fun_name);
										all_aliases.Add(fun_name);
									}
								}
							}
						}

						if (aliases.Count == 0)
							ThrowParseError("No aliases specified");

						if (!_caret.WS().Match(@"}"))
							ThrowParseError("Expected }");
					}
				}

				//Successful import
				if (file != null)
					imports.Add(file, aliases);
			}

			//Load imports
			IDE temp_ide = new IDE();
			temp_ide.GetSourceFile = IDE.GetSourceFile;

			//Load and parse the imports
			if (temp_ide.GetSourceFile == null)
			{
				if (imports.Count > 0)
					ThrowParseError("Imports defined but no way to get source files is provided");
			}
			else
				foreach (var im in imports)
				{
					temp_ide.Clear();
					//Get the source file
					if (!temp_ide.GetSourceFile(IDE.SourceUID, im.Key, out temp_ide.SourceText, out temp_ide.SourceUID, out temp_ide.SourceFileName))
						ThrowParseError(string.Format("Could not get source file '{0}' in '{1}'", im.Key, IDE.SourceFileName));
					else
					{
						//Parse it
						if (!temp_ide.Parser.Parse() || (temp_ide.GrammarTree.ParseErrors.Count > 0))
							ThrowParseError(string.Format("Errors encountered parsing source file '{0}'({1}) in '{2}'", im.Key, temp_ide.SourceFileName, IDE.SourceFileName));

						//Add the imported functions
						var als = im.Value;
						var funs = temp_ide.GrammarTree.Functions;

						foreach (var fun in funs)
							//Add with alias
							if (als.ContainsKey(fun.Name))
							{
								foreach (var fal in als[fun.Name])
									ImportedFunctions.Add(fal, fun);
								als.Remove(fun.Name);
							}
							else if (!all_aliases.Contains(fun.Name))
								ImportedFunctions.Add(fun.Name, fun);

						foreach (var al in als)
							ThrowParseError(string.Format("'{0}' not defined in file '{1}'", al.Key, im.Key));

						//Update maximum function variables (+ arguments)
						if (temp_ide.GrammarTree.MaxFunctionVariables > IDE.GrammarTree.MaxFunctionVariables)
							IDE.GrammarTree.MaxFunctionVariables = temp_ide.GrammarTree.MaxFunctionVariables;
					}
			}

			return true;
		}
		/// <summary>
		/// Parse main program body for functions
		/// </summary>
		/// <returns>Returns false on parse failure only</returns>
		private bool ParseFunctions()
		{
			Function fn;

			while (true)
			{
				//Must start with "Function"
				if (!_caret.WS().Match(@"(?<_kw>Function){ws}"))
					if (_caret.Match(@"."))
					{
						_caret.Position--;

						//Try to see if the declaration is missing
						Block bl;
						while (true)
						{
							//See if we can parse some block
							fn = new Function();
							bl = fn.Block;
							if (_caret.Match(@"\{")) 
								bl.Annotation.SourcePosition = _caret.LastMatch;
							else
								bl.Annotation.SourcePosition.Begin = new TextPortion(_caret.LastMatch).End;
							ParseBlock(bl);
							if (bl.Annotation.SourcePosition.Length == 0)
								break;

							if (bl.Annotation.SourcePosition.Length > 0)
								ThrowParseError("Missing function declaration", bl.Annotation.SourcePosition);

							//Try to parse a function
							if (_caret.WS().Match(@"(?<_kw>Function){ws}"))
								break;
							else if (bl.Annotation.SourcePosition.Length != 0)
							{
								ThrowParseError("Expected function declaration");
								//return false;
							}
						}
						if (bl.Annotation.SourcePosition.Length == 0)
							break;
					}
					else
						break;

				fn = new Function();
				fn.Annotation.IDE = IDE;

				//Parse the function
				ParseFunction(fn);

				//Add aliases and update unresolved function calls
				Functions.Add(fn);
				foreach (string fnn in fn.Aliases)
				{
					//It must not be a reserved keyword
					if (Keywords.Contains(fnn))
						ThrowParseError("'" + fnn + "' is a reserved keyword", fn.Annotation.SourcePosition);
					//It's name must be unique
					if (UserFunctions.ContainsKey(fnn))
						ThrowParseError("Function '" + fnn + "' already exists", fn.Annotation.SourcePosition);
					else
						UserFunctions.Add(fnn, fn);
					//Remove from undefined FunctionCalls
					if (UndefinedFunctionCalls.ContainsKey(fnn))
					{
						UndefinedFunctionCalls[fnn].Update(fn);
						UndefinedFunctionCalls.Remove(fnn);
					}
				}
			}

			if (_caret.WS().Match(@"."))
				ThrowParseError("Expected end of file or 'Function'", 10);

			return true;
		}
		/// <summary>
		/// Parse the Source, right after the 'Function' keyword.
		/// </summary>
		private void ParseFunction(Function Item)
		{
			//Find function name and aliases
			do
			{
				//Function name or alias
				if (!_caret.Match(@"(?<_fun>[a-zA-Z]\w*)"))
					if (_caret.Match(@"(?<_fun>\w+)"))
						ThrowParseError("Functions' " + ((Item.Aliases.Count >= 1) ? "alias" : "name") + " must begin with a letter", _caret.LastMatch);
					else
						ThrowParseError("Expected function's " + ((Item.Aliases.Count >= 1) ? "alias" : "name"));


				if (_caret.LastMatch.Groups["_fun"].Captures.Count != 0)
				{
					Group grp = _caret.LastMatch.Groups["_fun"];
					string fname = grp.Value;

					//The first is the name
					if (Item.Aliases.Count == 0)
					{
						Item.Name = fname;
						Item.Block.Name = fname;
						Item.Annotation.SourcePosition = grp;
						Item.Annotation.IDE = IDE;
						if (SourceCodeAnnotations != null)
							SourceCodeAnnotations.Add(Item.Annotation.SourcePosition, Item.Annotation);
					}

					//Everything is an alias
					if (!Item.Aliases.Contains(fname))
						Item.Aliases.Add(fname);
					else
						ThrowParseError("'" + fname + "' already exists as an alias for this function", grp);
				}
			} while (_caret.WS().Match(@",{ws}?"));

			//Parentheses
			if (!_caret.WS().Match(@"\("))
				ThrowParseError("Expected '('", 10);
			//Arguments
			Element el;
			Variable v;
			int prevVarCount;
			do
			{
				prevVarCount = Item.Variables.Count;

				el = ParseElement(Item.Block);
				if (el == null)
					if (Item.Variables.Count == 0)
						break;
					else
						ThrowParseError("Expected an argument name");
				else if (!(el is Variable))
					ThrowParseError("Only variables names can be used", el.Annotation.SourcePosition);
				else
				{
					v = (Variable)el;
					if ((v.Type != Variable.eType.Variable) || (v.Nodes.Count != 0))
						ThrowParseError("Only variable names can be used", el.Annotation.SourcePosition);
					else if (prevVarCount == Item.Variables.Count)
						ThrowParseError("Cannot declare same argument name twice", el.Annotation.SourcePosition);
					Item.ArgumentsCount++;
				}
			} while (_caret.WS().Match(@","));
			//Close Parentheses
			if (!_caret.WS().Match(@"\)"))
				ThrowParseError("Expected ')'", 10);

			//Get the block
			if (!_caret.WS().Match(@"\{"))
				ThrowParseError("Expected function's body", 10);
			PendingContinueControlFlows.Clear();
			ParseBlock(Item.Block);

			//Check for undefined sub block calls
			foreach (var ubc in Item.NamedBlocks.Undefined)
				if (ubc.Value.Target == null)
					foreach (var cf in ubc.Value.Items)
						ThrowParseError("'" + cf.Name + "' block not defined", cf.Annotation.SourcePosition);
				else
					ubc.Value.Update();

			//Check for unmatched Continues
			foreach (var cf in PendingContinueControlFlows)
				if (cf.Target == null)
					ThrowParseError("Not in an For loop", cf.Annotation.SourcePosition);
				else
					ThrowParseError("Target is not a For loop", cf.Annotation.SourcePosition);
			PendingContinueControlFlows.Clear();

			//Update maximum function variables (+ arguments)
			var arg_count = Item.ArgumentsCount + Item.Variables.Count;
			if (arg_count > IDE.GrammarTree.MaxFunctionVariables)
				IDE.GrammarTree.MaxFunctionVariables = arg_count;
		}
		/// <summary>
		/// Resolve undefined function calls by looking in the imported and predefined functions
		/// </summary>
		private void ResolveUndefinedFunctionCalls()
		{
			//Update ChangeParentInfo
			foreach (var fun in Functions)
				fun.Block.UpdateChildrenParentInfo();

			foreach (var ufc in UndefinedFunctionCalls)
				if (ImportedFunctions.ContainsKey(ufc.Key))
					//Look in the Imported Functions
				{
					ufc.Value.Update(ImportedFunctions[ufc.Key]);
					Functions.Add(ImportedFunctions[ufc.Key]);
				}
				else if (PredefinedFunctions.ContainsKey(ufc.Key))
					//Look in the Predefined Functions
				{
					var fn = PredefinedFunctions[ufc.Key];
					foreach (var fc in ufc.Value.Items)
						if (fc.Arguments.Count != fn.ArgumentsCount)
							//The signatures must match
							ThrowParseError("Function " + fn.Name + " takes " + fn.ArgumentsCount + " arguments, not " + fc.Arguments.Count, fc.Annotation.SourcePosition);
						else
							//Update valid calls
						{
							if (fn == PredefinedFunctions["Match"])
							{
								var ts = new TextSearch();
								ts.Pattern = fc.Arguments[0];
								ts.Type = TextSearch.eType.Normal;
								ts.Annotation.CopyFrom(fc.Annotation);
								ts.Quantifier = fc.Quantifier;
								AddSourceCodeAnnotation(ts);
								fc.ChangeInParent(ts);
							}
							else if (fn == PredefinedFunctions["Find"])
							{
								var ts = new TextSearch();
								ts.Pattern = fc.Arguments[0];
								ts.Type = TextSearch.eType.Find;
								ts.Annotation.CopyFrom(fc.Annotation);
								ts.Quantifier = fc.Quantifier;
								AddSourceCodeAnnotation(ts);
								fc.ChangeInParent(ts);
							}
							else if (fn == PredefinedFunctions["FindReverse"])
							{
								var ts = new TextSearch();
								ts.Pattern = fc.Arguments[0];
								ts.Type = TextSearch.eType.FindReverse;
								ts.Annotation.CopyFrom(fc.Annotation);
								ts.Quantifier = fc.Quantifier;
								AddSourceCodeAnnotation(ts);
								fc.ChangeInParent(ts);
							}
							else if (fn == PredefinedFunctions["Line"])
							{
								//Convert to {F"\r\n" Skipped, Source != '' FRE"$" Skipped}
								var blp = new Block();
								blp.Annotation.IDE = IDE;
								blp.Type = Block.eType.Multi;

								var bl = new Block();
								bl.Annotation.IDE = IDE;
								blp.Elements.Add(bl);
								
								var ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("\r\n");
								ts.Type = TextSearch.eType.Find;
								bl.Elements.Add(ts);

								var vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Skipped;
								bl.Elements.Add(vr);

								bl = new Block();
								bl.Annotation.IDE = IDE;
								blp.Elements.Add(bl);

								var op = new BinaryOperator();
								op.Annotation.IDE = IDE;
								op.Type = BinaryOperator.eType.NotEqual;
								bl.Elements.Add(op);

								vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Source;
								op.lhs = vr;

								var lt = new Literal("");
								lt.Annotation.IDE = IDE;
								op.rhs = lt;

								ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("$");
								ts.Type = TextSearch.eType.RegularExpression;
								bl.Elements.Add(ts);

								vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Skipped;
								bl.Elements.Add(vr);

								blp.UpdateChildrenParentInfo();
								blp.Annotation.CopyFrom(fc.Annotation);
								blp.Quantifier = fc.Quantifier;
								AddSourceCodeAnnotation(blp);
								fc.ChangeInParent(blp);
							}
							else if (fn == PredefinedFunctions["Paragraph"])
							{
								//Convert to {"\r\n"* F"\r\n\r\n" Skipped & {"\r\n"* ''}, "\r\n"* Source != '' FRE"$" Skipped}
								var blp = new Block();
								blp.Annotation.IDE = IDE;
								blp.Type = Block.eType.Multi;

								var bl = new Block();
								bl.Annotation.IDE = IDE;
								blp.Elements.Add(bl);

								var ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("\r\n");
								ts.Type = TextSearch.eType.Normal;
								ts.Quantifier.Min = 0;
								ts.Quantifier.Max = -1;
								bl.Elements.Add(ts);

								ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("\r\n\r\n");
								ts.Type = TextSearch.eType.Find;
								bl.Elements.Add(ts);

								var op = new BinaryOperator();
								op.Annotation.IDE = IDE;
								op.Type = BinaryOperator.eType.TextAppend;
								bl.Elements.Add(op);

								var vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Skipped;
								op.lhs = vr;

								var bl2 = new Block();
								bl2.Annotation.IDE = IDE;
								op.rhs = bl2;

								ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("\r\n");
								ts.Type = TextSearch.eType.Normal;
								ts.Quantifier.Min = 0;
								ts.Quantifier.Max = -1;
								bl2.Elements.Add(ts);

								var lt = new Literal("");
								lt.Annotation.IDE = IDE;
								bl2.Elements.Add(lt);

								bl = new Block();
								bl.Annotation.IDE = IDE;
								blp.Elements.Add(bl);

								ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("\r\n");
								ts.Type = TextSearch.eType.Normal;
								ts.Quantifier.Min = 0;
								ts.Quantifier.Max = -1;
								bl.Elements.Add(ts);

								op = new BinaryOperator();
								op.Annotation.IDE = IDE;
								op.Type = BinaryOperator.eType.NotEqual;
								bl.Elements.Add(op);

								vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Source;
								op.lhs = vr;

								lt = new Literal("");
								lt.Annotation.IDE = IDE;
								op.rhs = lt;

								ts = new TextSearch();
								ts.Annotation.IDE = IDE;
								ts.Pattern = new Literal("$");
								ts.Type = TextSearch.eType.RegularExpression;
								bl.Elements.Add(ts);

								vr = new Variable();
								vr.Annotation.IDE = IDE;
								vr.Type = Variable.eType.Skipped;
								bl.Elements.Add(vr);

								blp.UpdateChildrenParentInfo();
								blp.Annotation.CopyFrom(fc.Annotation);
								blp.Quantifier = fc.Quantifier;
								AddSourceCodeAnnotation(blp);
								fc.ChangeInParent(blp);
							}
						}
				}
				else
					//It is not defined
					foreach (var fc in ufc.Value.Items)
						ThrowParseError("'" + fc.Name + "' function not defined", fc.Annotation.SourcePosition);
		}

		/// <summary>
		/// Parse the block, right after the first '{'
		/// </summary>
		private void ParseBlock(Block Item, bool ImplicitMultiPortion = false)
		{
			while (true)
			{
				//A comma
				if (_caret.WS().Match(@","))
				{
					//If this is an implicit multi portion stop here
					if (ImplicitMultiPortion)
					{
						_caret.Position = _caret.LastMatch.Index;
						return;
					}

					PBComma(Item);
				}
				else
				{
					//Try parse an element
					Element el;
					el = ParseElement(Item);

					//An element
					if (el != null)
					{
						//Must be the last one
						if (Item.Elements.Count > 0)
						{
							var cf = Item.Elements[Item.Elements.Count - 1] as ControlFlow;
							if ((cf != null) && (cf.Type != ControlFlow.eType.Call))
								ThrowParseError("Must be the last one in a block", cf.Annotation.SourcePosition);
						}
						//Add element
						Item.Elements.Add(el);
					}
					else if (_caret.WS().Match(@"(?<fun>Function){ws}"))
					{
						ThrowParseError("Expected end of block", _caret.LastMatch.Groups["fun"]);
						_caret.Position = _caret.LastMatch.Index;
						break;
					}
					else if (_caret.Match(@"(\w+|[^\}])"))
						ThrowParseError("Unexpected token '" + _caret.LastMatch.Value + "'", _caret.LastMatch);
					else
						break;
				}
			}

			//If we are an implicit multi portion don't consume the '}'
			if (ImplicitMultiPortion)
			{
				if (Item.Elements.Count > 0)
				{
					Item.Annotation.SourcePosition.Begin = Item.Elements[0].Annotation.SourcePosition.Begin;
					Item.Annotation.SourcePosition.End = Item.Elements[Item.Elements.Count - 1].Annotation.SourcePosition.End;
				}
			}
			else
			{
				if (!_caret.WS().Match(@"\}"))
					if (_caret.WS().Match(@"(?<fun>Function){ws}"))
						_caret.Position = _caret.LastMatch.Index;
					else if (_caret.Match(@"."))
						ThrowParseError("Unexpected identifier", 10);
					else
						ThrowParseError("Unexpected end of file", 10);

				Item.Annotation.SourcePosition.End = _caret.Position;
			}

			AddSourceCodeAnnotation(Item);
		}
		/// <summary>
		/// Parse all portions of a multi block
		/// </summary>
		private void PBComma(Block Item)
		{
			ParseNextPortion:
			//We are already a multi, parse all next portions
			if (Item.Type == Block.eType.Multi)
			{
				while (true)
				{
					//Parse the implicit multi block
					Block bl = new Block();
					bl.Parent = Item;
					ParseBlock(bl, true);

					//Don't add a block with 1 item
					if (bl.Elements.Count == 1)
					{
						var el = bl.Elements[0];
						bl.Elements.Clear();
						//Update the parent if it is a block
						Block bl2 = el as Block;
						if (bl2 != null)
							bl2.Parent = Item;
						//Add the item
						Item.Elements.Add(el);
					}
					else
						//Add the block
						Item.Elements.Add(bl);

					//Got another portion?
					if (!_caret.WS().Match(@",")) break;
				}

				//Update last element's annotation
				if (Item.Elements.Count > 0)
				{
					var el = Item.Elements[Item.Elements.Count - 1];
					if (el != null)
						Item.Annotation.SourcePosition.End = el.Annotation.SourcePosition.End;
				}
			}
				//We now become a multi - prepare first portion
			else
			{
				//We can't allow empty portions
				if (Item.Elements.Count == 0)
					Item.Elements.Add(null);
				else if (Item.Elements.Count > 1)
				{
					//Create a new block
					Block bl = new Block();
					bl.TransferFrom(Item);
					bl.Parent = Item;
					//bl.TextPosition.Start = bl.Elements[0].TextPosition.Start;
					bl.Annotation.SourcePosition.End = bl.Elements[bl.Elements.Count - 1].Annotation.SourcePosition.End;

					//Add the block
					Item.Elements.Add(bl);
				}

				Item.Type = Block.eType.Multi;

				//Parse next portion
				goto ParseNextPortion;
				//return PBComma(Item);
				//Is this more elegant than a goto? Dunno, but 
				//C# doesn't allow non-parent-block jumps, and 
				//this provides the exact same functionality
			}
		}

		/// <summary>
		/// Parse an element and its quantifier, and put it in Item
		/// </summary>
		private Element ParseElement(Block Parent)
		{
			//Go through each of the helper functions to parse an element
			_caret.WS();
			Element el = PENamedBlock(Parent) ??
			       PEFunctionCall(Parent) ??
			       PEKeyword(Parent) ??
			       PEBinaryOperator(Parent) ??
			       PELiteral(Parent) ??
			       PEMemory(Parent) ??
			       PEBlock(Parent);
			//do
			//{
			//    _caret.WhiteSpace();
			//    Item = PENamedBlock(Parent);
			//    if (Item != null) break;
			//    Item = PEFunctionCall(Parent);
			//    if (Item != null) break;
			//    Item = PEKeyword(Parent);
			//    if (Item != null) break;
			//    Item = PEBinaryOperator(Parent);
			//    if (Item != null) break;
			//    Item = PELiteral(Parent);
			//    if (Item != null) break;
			//    Item = PEMemory(Parent);
			//    if (Item != null) break;
			//    Item = PEBlock(Parent);
			//    if (Item != null) break;
			//} while (false);

			if ((el is Literal) || //No quantifier for text literal
			    (el is Variable) || //No quantifier for variables
			    (el is ControlFlow) || //No quantifier for variables
			    (el is BinaryOperator)) //No quantifier for binary operators
			{
				var q = new Quantifier();

				//No quantifier must be specified
				if (ParseQuantifier(ref q))
					if ((el is ControlFlow) && (((ControlFlow)el).Type == ControlFlow.eType.Call))
						el.Quantifier = q;
					else
						ThrowParseError("No quantifier must be specified here");
			}
			else if (el != null)
			{
				ParseQuantifier(ref el.Quantifier);
				if (el.Quantifier.SourcePosition.IsInit)
					el.Annotation.SourcePosition.End = el.Quantifier.SourcePosition.End;
			}

			if (el != null)
				AddSourceCodeAnnotation(el);

			return el;
		}
		/// <summary>
		/// Parse a block, right before the initial '{'
		/// </summary>
		private Element PEBlock(Block Parent)
		{
			if (_caret.WS().Match(@"(?<bl_start>\{)"))
			{
				Block bl = new Block();
				bl.Name = null;

				bl.Annotation.SourcePosition.Begin = _caret.LastMatch.Index;
				bl.Parent = Parent;

				//If it fails fail
				ParseBlock(bl);
				bl.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;

				return bl;
			}
			else
				return null;
		}
		/// <summary>
		/// Parse a named block or a control flow
		/// </summary>
		private Element PENamedBlock(Block Parent)
		{
			string sName;
			bool bNameIsProper;
			TextPortion tp;
			if (PEIdentifier("#", "_sb", out sName, out bNameIsProper, out tp))
			{
				//Emanate errors
				if (sName.Length == 0)
					ThrowParseError("Missing block name", tp);
				else if (!bNameIsProper)
					ThrowParseError("Block name must begin with a letter", tp);

				//If it is a definition
				if (_caret.WS().Match(@"\:"))
				{
					Block bl = new Block();

					bl.Annotation.IDE = IDE;
					bl.Name = sName;
					bl.Parent = Parent;
					bl.Annotation.SourcePosition = tp;

					if (bl.Name.Length != 0)
					{
						var labels = Parent.Function.NamedBlocks;
						//Must be unique
						if ((labels.Defined.ContainsKey(bl.Name)) || (Parent.Function.Aliases.Contains(bl.Name)))
							ThrowParseError("Block name already exists", bl.Annotation.SourcePosition);
							//Update calls to it
						else if (bNameIsProper)
						{
							labels.Defined.Add(bl.Name, bl);
							if (labels.Undefined.ContainsKey(bl.Name))
								labels.Undefined[bl.Name].Target = bl;
						}
					}
					//Must have a block following
					if (!_caret.WS().Match(@"\{"))
						ThrowParseError("Expected block declaration", bl.Annotation.SourcePosition);
					else
					{
						bl.Annotation.SourcePosition.Begin = _caret.LastMatch.Index;
						//If it fails fail
						ParseBlock(bl);
						bl.Annotation.SourcePosition.End = bl.Annotation.SourcePosition.End;
					}

					return bl;
				}
					//It is a call
				else
				{
					var cf = new ControlFlow();

					cf.Type = ControlFlow.eType.Call;
					cf.Name = sName;
					cf.Annotation.SourcePosition = tp;

					if (cf.Name != "")
					{
						var labels = Parent.Function.NamedBlocks;
						//If not defined yet add to watchlist
						if (labels.Defined.ContainsKey(cf.Name))
						{
							cf.Target = labels.Defined[cf.Name];
							if (!cf.Target.CanBeCalled)
								ThrowParseError("This block cannot be called from here; it uses a goto/succeed/fail of a parent block", cf.Annotation.SourcePosition);
						}
						else
						{
							//Add to watchlist
							if (!labels.Undefined.ContainsKey(cf.Name))
								labels.Undefined.Add(cf.Name, new Function.NamedBlocksClass.UndefinedBlockCall());
							labels.Undefined[cf.Name].Items.Add(cf);
						}
					}

					return cf;
				}
			}
			else
				return null;
		}
		/// <summary>
		/// Parse a function call
		/// </summary>
		private Element PEFunctionCall(Block Parent)
		{
			if (_caret.WS().Match(@"(?<_call>\w+)\("))
			{
				var grp = _caret.LastMatch.Groups["_call"];
				string fname = grp.Value;

				FunctionCall fc = new FunctionCall();
				fc.Annotation.SourcePosition.Begin = grp.Index;
				fc.Annotation.SourcePosition.Length = _caret.LastMatch.Index + _caret.LastMatch.Length -
				                                      fc.Annotation.SourcePosition.Begin;

				//Get arguments
				PEFCArguments(Parent, fc.Arguments);
				fc.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;

				//If not defined yet add to watchlist
				UndefinedFunctionCall ufc;
				if (!UserFunctions.ContainsKey(fname))
				{
					//Add to watchlist
					if (UndefinedFunctionCalls.ContainsKey(fname))
						ufc = UndefinedFunctionCalls[fname];
					else
						ufc = new UndefinedFunctionCall(this);

					ufc.Items.Add(fc);
					UndefinedFunctionCalls[fname] = ufc;

					fc.Target = null;
				}
				else
				{
					fc.Target = UserFunctions[fname];
					fname = fc.Target.Name;
					//Arguments count mismatch
					if (fc.Target.ArgumentsCount != fc.Arguments.Count)
						ThrowParseError(
							"Function " + fc.Target.Name + " takes " + fc.Target.ArgumentsCount + " arguments, not " + fc.Arguments.Count +
							" arguments", fc.Annotation.SourcePosition);
				}
				fc.Name = fname;

				return fc;
			}
			else
				return null;
		}
		/// <summary>
		/// Get the arguments passed to a function call
		/// </summary>
		private void PEFCArguments(Block Parent, List<Element> Arguments)
		{
			//Arguments
			Element el;
			do
			{
				el = ParseElement(Parent);
				if (el == null)
					if (Arguments.Count == 0)
						break;
					else
						ThrowParseError("Expected an argument");
				else
				{
					Arguments.Add(el);
					//Make sure blocks can't be goto-ed from outside
					if (el is Block)
						((Block)el).GOTOArea = (Block)el;
					else if (el is Variable)
					{
						var vr = el as Variable;
						foreach (var tn in vr.Nodes)
							if (tn.Key is Block)
								((Block)tn.Key).GOTOArea = (Block)tn.Key;
					}
				}
			} while (_caret.WS().Match(@","));

			//Close Parentheses
			if (!_caret.WS().Match(@"\)"))
				ThrowParseError("Expected ')'", 10);
		}
		/// <summary>
		/// Parse a keyword
		/// </summary>
		private Element PEKeyword(Block Parent)
		{
			if (
				_caret.WS().Match(
					@"(?<_kw>Tree|Parent|New|Next|Previous|Skipped|Source|RetVal|RV|Succeed|Fail|GoTo|For|Continue|Break)(?!\w)"))
			{
				var grp = _caret.LastMatch.Groups["_kw"];
				switch (grp.Value.ToLower())
				{
					//A variable
				case "tree":
				case "parent":
				case "new":
				case "next":
				case "previous":
				case "skipped":
				case "source":
				case "retval":
				case "rv":
					return PEKVariable(Parent, grp);
					//A named block call
				case "succeed":
				case "fail":
				case "goto":
				case "continue":
				case "break":
					return PEKNamedSubBlock(Parent, grp);
				case "for":
					return PEKFor(Parent, grp);
				}
			}

			return null;
		}
		private Element PEKVariable(Block Parent, Group RegExGroup)
		{
			Variable vr = new Variable();

			vr.Annotation.SourcePosition = RegExGroup;
			switch (RegExGroup.Value.ToLower())
			{
			case "tree":
				vr.Type = Variable.eType.Tree;
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
					ThrowParseError("Don't use 'This'.[Tree] but [Tree]", vr.Annotation.SourcePosition);
				}
				break;
			case "parent":
				vr.Type = Variable.eType.Tree;
				vr.Nodes.Add(new TreeNode(null, TreeNode.eType.Parent));
				if (_caret.Match(@"\."))
					PESubTree(Parent, vr.Nodes);
				vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				break;
			case "new":
				vr.Type = Variable.eType.New;
				if (_caret.Match(@"\."))
					PESubTree(Parent, vr.Nodes);
				vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				break;
			case "next":
				vr.Type = Variable.eType.Tree;
				vr.Nodes.Add(new TreeNode(null, TreeNode.eType.Next));
				if (_caret.Match(@"\."))
					PESubTree(Parent, vr.Nodes);
				vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				break;
			case "previous":
				vr.Type = Variable.eType.Tree;
				vr.Nodes.Add(new TreeNode(null, TreeNode.eType.Previous));
				if (_caret.Match(@"\."))
					PESubTree(Parent, vr.Nodes);
				vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				break;
			case "skipped":
				vr.Type = Variable.eType.Skipped;
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
					ThrowParseError("You cannot use sub-tree with 'Skipped'", vr.Annotation.SourcePosition);
				}
				break;
			case "source":
				vr.Type = Variable.eType.Source;
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
					ThrowParseError("You cannot use sub-tree with 'Source'", vr.Annotation.SourcePosition);
				}
				break;
			case "retval":
			case "rv":
				Parent.Function.Inlined = false;
				vr.Type = Variable.eType.RetVal;
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				}
				break;
			}

			return vr;
		}
		private Element PEKNamedSubBlock(Block Parent, Group RegExGroup)
		{
			Block bl;
			var cf = new ControlFlow();
			cf.Parent = Parent;

			//On continue and break
			bool isForCF = false;
			switch (RegExGroup.Value.ToLower())
			{
			case "succeed":
				cf.Type = ControlFlow.eType.Succeed;
				break;
			case "fail":
				cf.Type = ControlFlow.eType.Fail;
				break;
			case "goto":
				cf.Type = ControlFlow.eType.GoTo;
				break;
			case "continue":
				cf.Type = ControlFlow.eType.Continue;
				PendingContinueControlFlows.Add(cf);
				isForCF = true;
				break;
			case "break":
				cf.Type = ControlFlow.eType.Succeed;
				PendingContinueControlFlows.Add(cf);
				isForCF = true;
				break;
			}

			cf.Annotation.SourcePosition = RegExGroup;

			if (_caret.WS().Match(@":"))
			{
				cf.Name = "";
				bool bNIP;
				TextPortion tp;
				if (PEIdentifier("#", "_sb", out cf.Name, out bNIP, out tp))
				{
					//Emanate errors
					if (cf.Name.Length == 0)
						ThrowParseError("Missing block name", tp);

					cf.Annotation.SourcePosition.End = tp.End;

					bl = Parent;

					if (cf.Type == ControlFlow.eType.GoTo)
					{
						//Find block or wait for it to be defined
						if (cf.Name != "")
						{
							var labels = Parent.Function.NamedBlocks;
							if (labels.Defined.ContainsKey(cf.Name))
							{
								cf.Target = labels.Defined[cf.Name];
								if (cf.Target.GOTOArea != Parent.GOTOArea)
									ThrowParseError("Cannot jump there from here", cf.Annotation.SourcePosition);
								else
								{
									//Obviously parent blocks up to the common ancestor can't be called
									bl = Parent.FindCommonAncestor(cf.Target);
									if (bl != null)
									{
										Block b2 = Parent;
										while ((b2 != null) && (b2 != bl))
										{
											b2.CanBeCalled = false;
											b2 = b2.Parent;
										}
									}
								}
							}
							else
							{
								//It's not defined, so remember to check later
								if (!labels.Undefined.ContainsKey(cf.Name))
									labels.Undefined.Add(cf.Name, new Function.NamedBlocksClass.UndefinedBlockCall());
								labels.Undefined[cf.Name].Items.Add(cf);
							}
						}
					}
					else
					{
						//Find parent block
						while ((bl != null) && (StringComparer.OrdinalIgnoreCase.Compare(bl.Name, cf.Name) != 0))
							bl = bl.Parent;

						if (bl == null)
							ThrowParseError("Block '" + cf.Name + "' not a parent block", cf.Annotation.SourcePosition);
						else
						{
							cf.Target = bl;
							//Obviously parent blocks until cf.Target can't be called
							bl = Parent;
							while ((bl != null) && (StringComparer.OrdinalIgnoreCase.Compare(bl.Name, cf.Name) != 0))
							{
								bl.CanBeCalled = false;
								bl = bl.Parent;
							}
						}
					}
				}
				else if (_caret.Match(@"(?<_kw>Function)(?!\w)"))
				{
					if (isForCF)
						ThrowParseError("Must target a For loop", cf.Annotation.SourcePosition);
					else
					{
						bl = Parent;

						while ((bl != null) && (bl.Parent != null))
							bl = bl.Parent;

						if (bl == null)
							ThrowParseError("Expected block name", cf.Annotation.SourcePosition);
						else
						{
							cf.Name = bl.Name;
							cf.Target = bl;

							cf.Annotation.SourcePosition.End = new TextPortion(_caret.LastMatch).End;
						}
					}
				}
				else
					ThrowParseError("Expected block name", cf.Annotation.SourcePosition);
			}
			else
			{
				switch (cf.Type)
				{
				case ControlFlow.eType.GoTo:
					ThrowParseError("Expected block definition", cf.Annotation.SourcePosition);
					break;
				case ControlFlow.eType.Fail:
				case ControlFlow.eType.Succeed:
					if (!isForCF)
						cf.Target = Parent;
					break;
				}
			}

			if (cf.Type == ControlFlow.eType.Succeed)
				if (cf.Target != null)
					Parent.SucceedsUpToDepth = cf.Target.Depth;

			return cf;
		}
		private Element PEKFor(Block Parent, Group RegExGroup)
		{
			ControlFlow cf;

			//Keep a tab on how many new Continue's are added
			int previousPendContCFs = PendingContinueControlFlows.Count;

			var el = ParseElement(Parent);
			var bl = el as Block;

			if (el == null)
				ThrowParseError("Expected block", RegExGroup);
			else if (bl == null)
				ThrowParseError("Expected block", el.Annotation.SourcePosition);
			else
			{
				bl.Annotation.SourcePosition.Begin = RegExGroup.Index;
				AddSourceCodeAnnotation(bl);
				if (bl.Type == Block.eType.For)
					ThrowParseError("Double 'for' declaration", bl.Annotation.SourcePosition);
				else
				{
					if ((bl.Type == Block.eType.Normal) || (bl.Elements.Count != 3))
						ThrowParseError("Must have exactly 3 portions", bl.Annotation.SourcePosition);
					bl.Type = Block.eType.For;
				}

				//Update pending Continues
				for (int i = previousPendContCFs; i < PendingContinueControlFlows.Count; i++)
				{
					cf = PendingContinueControlFlows[i];
					if (cf.Target == null)
					{
						cf.Target = bl;
						PendingContinueControlFlows.RemoveAt(i);
						i--;
					}
					else if (cf.Target == bl)
					{
						PendingContinueControlFlows.RemoveAt(i);
						i--;
					}
				}
			}

			return el;
		}
		/// <summary>
		/// Parse a [SubTree]
		/// </summary>
		private void PESubTree(Block Parent ,List<TreeNode> Tree)
		{
			do
			{
				var tn = new TreeNode();

				if (_caret.Match(@"(?<_kw>Parent)(?!\w)"))
				{
					tn.Type = TreeNode.eType.Parent;
					Tree.Add(tn);
				}
				else if (_caret.Match(@"(?<_kw>Next)(?!\w)"))
				{
					tn.Type = TreeNode.eType.Next;
					Tree.Add(tn);
				}
				else if (_caret.Match(@"(?<_kw>Previous)(?!\w)"))
				{
					tn.Type = TreeNode.eType.Previous;
					Tree.Add(tn);
				}
				else if (_caret.Match(@"(?<_kw>Value)(?!\w)"))
				{
					tn.Type = TreeNode.eType.Value;
					Tree.Add(tn);
				}
				else if (_caret.Match(@"(?<_kw>Name)(?!\w)"))
				{
					tn.Type = TreeNode.eType.Name;
					Tree.Add(tn);
				}
				else if (_caret.Match(@"\["))
				{
					Tree.Add(tn);

					//Get the key (if any)
					tn.Key = null;
					if (_caret.WS().Match(@"(?<_tr>[a-zA-Z]\w*)"))
					{
						string sKey = _caret.LastMatch.Groups["_tr"].Value;
						if (sKey.Length == 0)
							tn.Type = TreeNode.eType.Indexed;
						else
						{
							tn.Key = new Literal(sKey);
							tn.Key.Annotation.SourcePosition = _caret.LastMatch;
							tn.Key.Annotation.SourcePosition.Begin -= 1;
							AddSourceCodeAnnotation(tn.Key);
							tn.Type = TreeNode.eType.Normal;
						}
					}
						//Get an element
					else if (_caret.WS().Match(@":"))
					{
						var el = ParseElement(Parent);

						if (el == null)
							ThrowParseError("Expected [key|index]; key (optional) is a word or : and an element, index (optional) is a number, New or >=,>~,<=,<~ and an element", 10);
						else
						{
							tn.Key = el;

							//Change the GOTOArea because we can't just jump in here
							var bl2 = el as Block;
							if (bl2 != null)
								bl2.GOTOArea = bl2;

						}
					}

					//Get the index (if any)
					if (_caret.WS().Match(@"\|"))
					{
						if (_caret.WS().Match(@"(?<index>-?\d+)|(?<_kw>(Not)?New)"))
						{
							string sIndex = _caret.LastMatch.Groups["index"].Value;
							if (sIndex.Length != 0)
							{
								if (!int.TryParse(sIndex, out tn.Index))
									ThrowParseError("Expected an integer");
								else if ((tn.Index == -1) && (tn.Key != null))
									ThrowParseError("Use NotNew instead", _caret.LastMatch.Groups["index"]);

								if (tn.Key == null)
									tn.Type = TreeNode.eType.Indexed;
								else
									tn.Type = TreeNode.eType.Normal;
							}
							else
								switch (_caret.LastMatch.Groups["_kw"].Value.ToLower())
								{
								case "new":
									tn.Type = TreeNode.eType.New;
									break;
								case "notnew":
									tn.Type = TreeNode.eType.NotNew;
									if (tn.Key == null)
										ThrowParseError("Use -1 instead", _caret.LastMatch);
									break;
								default:
									if (tn.Key == null)
										tn.Type = TreeNode.eType.Indexed;
									else
										tn.Type = TreeNode.eType.Normal;
									tn.Index = -1;
									break;
								}
						}
						else if (_caret.WS().Match(@"(?<dir>\>|\<)(?<case>=|~)"))
						{
							tn.ByValueNext = _caret.LastMatch.Groups["dir"].Value == ">";
							tn.ByValueIgnoreCase = _caret.LastMatch.Groups["case"].Value == "~";

							var el = ParseElement(Parent);

							if (el == null)
								ThrowParseError(
									"Expected [key|index]; key (optional) is a word or : and an element, index (optional) is a number, New or >=,>~,<=,<~ and an element",
									10);
							else
							{
								tn.Value = el;

								//Change the GOTOArea because we can't just jump in here
								var bl2 = el as Block;
								if (bl2 != null)
									bl2.GOTOArea = bl2;
							}

							if (tn.Key == null)
								tn.Type = TreeNode.eType.Indexed;
							else
								tn.Type = TreeNode.eType.Normal;
						}
						else
							ThrowParseError(
								"Expected [key|index]; key (optional) is a word or : and an element, index (optional) is a number, New or >=,>~,<=,<~ and an element",
								10);
					}
					else
					{
						tn.Index = -1;
						if (tn.Key == null)
							ThrowParseError("Expected [key|index]; key (optional) is a word or : and an element, index (optional) is a number, New or >=,>~,<=,<~ and an element", 10);
					}

					if (!_caret.WS().Match(@"(?<cl_br>\])"))
						ThrowParseError("Expected trailing ]", _caret.LastMatch);
				}
				else
					ThrowParseError("Expected [key|index]; key (optional) is a word or : and an element, index (optional) is a number, New or >=,>~,<=,<~ and an element", 10);

				if (_caret.Match(@"\["))
				{
					ThrowParseError("Separate trees with a space or .");
					_caret.Position = _caret.LastMatch.Index;
				}
				else if (!_caret.Match(@"\."))
					break;
			} while (true);

			//Do some type checking
			for (var i = 0 ; i < (Tree.Count - 1) ; i++)
				if (Tree[i].Type == TreeNode.eType.Value)
					ThrowParseError("Only the last item may be Value");
		}
		/// <summary>
		/// Parse a binary operator
		/// </summary>
		private Element PEBinaryOperator(Block Parent)
		{
			if (_caret.WS().Match(@"(?<_op>!==|!=|!~|\+=|===|==|=~|=|~&|~|\>|&)"))
			{
				BinaryOperator bo = new BinaryOperator();
				bo.Annotation.SourcePosition = _caret.LastMatch;

				//Get type
				switch (_caret.LastMatch.Groups["_op"].Value)
				{
				case "!=":
					bo.Type = BinaryOperator.eType.NotEqual;
					bo.IgnoreCase = false;
					break;
				case "!~":
					bo.Type = BinaryOperator.eType.NotEqual;
					bo.IgnoreCase = true;
					break;
				case "+=":
					bo.Type = BinaryOperator.eType.TreeAppend;
					break;
				case "===":
					bo.Type = BinaryOperator.eType.ReferenceEqual;
					break;
				case "!==":
					bo.Type = BinaryOperator.eType.ReferenceNotEqual;
					break;
				case "==":
					bo.Type = BinaryOperator.eType.Equal;
					bo.IgnoreCase = false;
					break;
				case "=~":
					bo.Type = BinaryOperator.eType.Equal;
					bo.IgnoreCase = true;
					break;
				case "~&":
					bo.Type = BinaryOperator.eType.TildeAppend;
					break;
				case "~":
					bo.Type = BinaryOperator.eType.Tilde;
					break;
				case ">":
					bo.Type = BinaryOperator.eType.Pipe;
					break;
				case "&":
					bo.Type = BinaryOperator.eType.TextAppend;
					break;
				case "=":
					bo.Type = BinaryOperator.eType.ReferenceCopy;
					break;
				}

				//Get left hand side operand
				if (Parent.Elements.Count == 0)
					bo.lhs = null;
				else
				{
					//Get last element
					bo.lhs = Parent.Elements[Parent.Elements.Count - 1];
					if (bo.lhs is BinaryOperatorStub)
						ThrowParseError("Do not stack binary operators", bo.Annotation.SourcePosition);
					else
					{
						Parent.Elements.RemoveAt(Parent.Elements.Count - 1);
						bo.Annotation.SourcePosition.Begin = bo.lhs.Annotation.SourcePosition.Begin;
					}
				}
				//Inform that there is already a binary operator parsing
				Parent.Elements.Add(new BinaryOperatorStub());

				//Get right hand side operand
				bo.rhs = ParseElement(Parent);
				if (bo.rhs != null)
					bo.Annotation.SourcePosition.End = bo.rhs.Annotation.SourcePosition.End;

				//Must have two elements
				if ((bo.rhs == null) || (bo.lhs == null))
					ThrowParseError("Must have two elements", bo.Annotation.SourcePosition);

				Parent.Elements.RemoveAt(Parent.Elements.Count - 1);

				return bo;
			}
			else
				return null;
		}
		/// <summary>
		/// Parse a text match/search or text/number literal
		/// </summary>
		private Element PELiteral(Block Parent)
		{
			if (_caret.WS().Match(@"(?<begin>(?<_skw>(?<find>Find|F)|(?<findrev>FindReverse|FR)|(?<regex>RegEx|RE)|(?<fregex>FindRegEx|FRE))?"")"))
			{
				var ts = new TextSearch();
				ts.Annotation.SourcePosition.Begin = _caret.LastMatch.Groups["begin"].Index;

				//Get the type
				bool regex_find = false;
				if (_caret.LastMatch.Groups["find"].Value != "")
					ts.Type = TextSearch.eType.Find;
				else if (_caret.LastMatch.Groups["findrev"].Value != "")
					ts.Type = TextSearch.eType.FindReverse;
				else if (_caret.LastMatch.Groups["regex"].Value != "")
					ts.Type = TextSearch.eType.RegularExpression;
				else if (_caret.LastMatch.Groups["fregex"].Value != "")
				{
					ts.Type = TextSearch.eType.RegularExpression;
					regex_find = true;
				}
				else
					ts.Type = TextSearch.eType.Normal;

				//Escape "" => "
				_caret.Match(@"(?<_tm>(""""|[^""])*)(?<close>""|$)");
				if (_caret.LastMatch.Groups["close"].Length == 0)
					ThrowParseError("Expecting trailing \"", 10);
				string pattern = _caret.LastMatch.Groups["_tm"].Value.Replace(@"""""", @""""), display_pattern = pattern;

				TextPortion tp = _caret.LastMatch.Groups["_tm"];
				tp.Text = _caret.Source;
				ts.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;

				if (ts.Type == TextSearch.eType.RegularExpression)
				{
					//We prepend with \G to enforce to match at current position
					if (!regex_find)
						pattern = string.Format(@"\G(?:{0})", pattern);

					//Try to compile the regex
					string regex_error = null;
					if (CompiledRegExErrors.ContainsKey(pattern))
						regex_error = CompiledRegExErrors[pattern];
					else
						try
						{
							new Regex(pattern);
						}
						catch (Exception ex)
						{
							regex_error = ex.Message;
						}
						finally
						{
							CompiledRegExErrors.Add(pattern, regex_error);
						}

					if (regex_error != null)
						ThrowParseError("Could not parse regular expression: " + regex_error, ts.Annotation.SourcePosition);
				}
				else
					//Escape characters
					pattern = PELEscape(tp, '"');

				ts.Pattern = new Literal(pattern, display_pattern);

				//See if we request only for a sub tree
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, ts.Tree);
					ts.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
					switch (ts.Type)
					{
					case TextSearch.eType.RegularExpression:
						if (ts.Tree.Count != 0)
						{
							var nKey = ts.Tree[0];
							var lKey = nKey.Key as Literal;
							if ((ts.Tree.Count > 1) ||
								(lKey == null) ||
								(lKey.Type != Literal.eType.Text) ||
								(StringComparer.OrdinalIgnoreCase.Compare(lKey.Text, "Group") != 0))
								ThrowParseError("'Group' is the only valid sub tree", ts.Annotation.SourcePosition);
							else if (nKey.Type == TreeNode.eType.New)
								ThrowParseError("No point in creating a new subtree");
						}
						break;
					default:
						ThrowParseError("Only regular expressionys return trees", ts.Annotation.SourcePosition);
						break;
					}
				}

				return ts;
			}
				//A text literal
			else if (_caret.WS().Match(@"'"))
			{
				var tl = new Literal();
				tl.Type = Literal.eType.Text;
				tl.Annotation.SourcePosition.Begin = _caret.LastMatch.Index;

				//Escape '' => '
				_caret.Match(@"(?<_lit>(''|[^'])*)(?<close>'|$)");
				if (_caret.LastMatch.Groups["close"].Length == 0)
					ThrowParseError("Expecting trailing '", 10);
				tl.AssemblyDisplayText = _caret.LastMatch.Groups["_lit"].Value.Replace("''", "'");
				tl.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;

				//Escape characters
				TextPortion tp = _caret.LastMatch.Groups["_lit"];
				tp.Text = _caret.Source;
				tl.Text = PELEscape(tp, '\'');

				return tl;
			}
				//A number literal
			else if (_caret.WS().Match(@"(?<_lit>-?\d+)"))
			{
				var tl = new Literal();
				tl.Type = Literal.eType.Number;
				tl.Annotation.SourcePosition = _caret.LastMatch;

				if (!int.TryParse(_caret.LastMatch.Value, out tl.Number))
					ThrowParseError("Expected an integer");

				return tl;
			}
			else
				return null;
		}
		/// <summary>
		/// Escape characters inside a text literal
		/// </summary>
		private string PELEscape(TextPortion SourcePosition, char EscapeChar)
		{
			StringBuilder sb = new StringBuilder(SourcePosition.Length);

			//Last position in the string
			var old_pos = SourcePosition.Begin;
			//Caret for finding \
			var crt = new Caret();
			crt.Source = SourcePosition.Text;
			string esc_char = EscapeChar.ToString(), esc_chars = esc_char + esc_char;

			for (var pos = SourcePosition.Text.IndexOf('\\', SourcePosition.Begin, SourcePosition.Length); pos >= 0; pos = SourcePosition.Text.IndexOf('\\', pos, SourcePosition.End - pos))
			{
				//Append in-between text
				if (pos != old_pos)
					sb.Append(SourcePosition.Text.Substring(old_pos, pos - old_pos).Replace(esc_chars, esc_char));

				if (pos == SourcePosition.End)
				{
					ThrowParseError("Expected a character to escape", new TextPortion(pos, 1));
					break;
				}

				pos++;

				//End of file
				if (pos >= SourcePosition.End)
					break;

				switch (SourcePosition.Text[pos])
				{
				case '\\':
					sb.Append('\\');
					pos++;
					break;
				case 'r':
				case 'R':
					sb.Append('\r');
					pos++;
					break;
				case 'n':
				case 'N':
					sb.Append('\n');
					pos++;
					break;
				case 't':
				case 'T':
					sb.Append('\t');
					pos++;
					break;
				case 'x':
				case 'X':
					pos++;
					crt.Position = pos;
					if (!crt.Match(@"(?<cap>([0-9a-fA-F]{2})+)\\"))
						ThrowParseError(
							"Expected one or more consecutive 2-digit hexadecimal values, followed by a \\ (eg \\x2A\\, or \\x02A5F2\\)",
							new TextPortion(pos, 1));
					else
					{
						var cap = crt.LastMatch.Groups["cap"].Value;

						for (int i = 0; i < cap.Length; i += 2)
							sb.Append((char) Int16.Parse(cap.Substring(i, 2), NumberStyles.HexNumber));

						pos = crt.Position;
					}
					break;
				case 'u':
				case 'U':
					pos++;
					crt.Position = pos;
					if (!crt.Match(@"(?<cap>([0-9a-fA-F]{4})+)\\"))
						ThrowParseError(
							"Expected one or more consecutive 4-digit hexadecimal values, followed by a \\ (eg \\u002A\\, or \\u000200A510F2\\)",
							new TextPortion(pos, 1));
					else
					{
						var cap = crt.LastMatch.Groups["cap"].Value;

						for (int i = 0; i < cap.Length; i += 4)
							sb.Append((char) Int16.Parse(cap.Substring(i, 4), NumberStyles.HexNumber));

						pos = crt.Position;
					}
					break;
				default:
					ThrowParseError("Unexpected escape character", new TextPortion(pos, 1));
					break;
				}

				old_pos = pos;
			}

			//Append remaining text
			if (old_pos != SourcePosition.End)
				sb.Append(SourcePosition.Text, old_pos, SourcePosition.End - old_pos);

			return sb.ToString();
		}
		/// <summary>
		/// Parse a variable, setting or sub-tree
		/// </summary>
		private Element PEMemory(Block Parent)
		{
			string sName;
			bool bNIP;
			TextPortion tp;

			//A variable
			if (PEIdentifier("%", "_var", out sName, out bNIP, out tp))
			{
				Parent.Function.Inlined = false;
				//Emanate errors
				if (sName.Length == 0)
					ThrowParseError("Missing variable name", tp);
				else if (!bNIP)
					ThrowParseError("Variable name must begin with a letter", tp);

				//Update variable names list
				if (sName.Length != 0)
				{
					var variables = Parent.Function.Variables;
					if (!variables.ContainsKey(sName))
						variables.Add(sName, variables.Count + 1);
				}

				//Create item
				Variable vr = new Variable();
				vr.Type = Variable.eType.Variable;
				vr.Name = sName;
				vr.Annotation.SourcePosition = tp;

				//Is there a sub-tree?
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				}

				return vr;
			}
				//A setting
			else if (PEIdentifier("@", "_var", out sName, out bNIP, out tp))
			{
				//Emanate errors
				if (sName.Length == 0)
					ThrowParseError("Missing setting name", tp);
				else if (!bNIP)
					ThrowParseError("Setting name must begin with a letter", tp);

				//Create item
				Variable vr = new Variable();
				vr.Type = Variable.eType.Setting;
				vr.Nodes.Add(new TreeNode(new Literal(sName)));
				vr.Annotation.SourcePosition = tp;

				//Is there a sub-tree?
				if (_caret.Match(@"\."))
				{
					PESubTree(Parent, vr.Nodes);
					vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;
				}

				return vr;
			}
				//A sub tree
			else if (_caret.WS().Match(@"(?<begin>(?<dot>\.+)?\[)"))
			{
				var vr = new Variable();

				if (_caret.LastMatch.Groups["dot"].Captures.Count != 0)
					ThrowParseError("Don't use a dot before a tree", _caret.LastMatch.Groups["dot"]);

				vr.Annotation.SourcePosition.Begin = _caret.LastMatch.Groups["begin"].Index;
				_caret.Position = vr.Annotation.SourcePosition.Begin;

				vr.Type = Variable.eType.Tree;
				PESubTree(Parent, vr.Nodes);
				vr.Annotation.SourcePosition.End = _caret.LastMatch.Index + _caret.LastMatch.Length;

				return vr;
			}
			else
				return null;
		}
		/// <summary>
		/// Parse an identifier between two Borders (eg %variable%, #block#, etc). Check if it is a proper name, 
		/// and reports an error if the trailing border is absent
		/// </summary>
		/// <param name="Border">The character (or regex) that must be before and after the identifier</param>
		/// <param name="SyntaxName">The syntax highlighting name</param>
		/// <param name="Name">The identifier</param>
		/// <param name="NameIsProper">If the identifier is a proper name</param>
		/// <param name="TextPortion">Syntax TextPosition</param>
		/// <returns>True if it finds the first border, false otherwise</returns>
		private bool PEIdentifier(string Border, string SyntaxName, out string Name, out bool NameIsProper, out TextPortion TextPortion)
		{
			Name = "";
			NameIsProper = false;
			TextPortion = new TextPortion();

			//Must begin with Border
			if (!_caret.WS().Match(@"(?<begin>" + Border + @")"))
				return false;

			TextPortion.Begin = _caret.LastMatch.Groups["begin"].Index;

			//Names must begin with a letter
			NameIsProper = _caret.Match(@"(?<" + SyntaxName + @">[a-zA-Z]\w*)");
			//Should at lest specify a name
			if (!NameIsProper)
				_caret.Match(@"(?<" + SyntaxName + @">\w+)");

			//Get name
			Name = _caret.LastMatch.Groups[SyntaxName].Value;

			//Must have a trailing Border
			if (!_caret.WS().Match(@"(?<end>" + Border + @")"))
				ThrowParseError("Missing trailing " + Border, new TextPortion(TextPortion.Begin, new TextPortion(_caret.LastMatch).End - TextPortion.Begin));

			TextPortion.End = new TextPortion(_caret.LastMatch).End;

			return true;
		}

		/// <summary>
		/// Parse the Quantifier, right after the element, and update the Item.Quantifier
		/// </summary>
		/// <returns>Returns true if a Quantifier is encountered, false otherwise</returns>
		private bool ParseQuantifier(ref Quantifier Quantifier)
		{
			if (_caret.Match(@"(?<_skw>(?<plus>\&)?((?<keyword>if_any|\?|as_many|\*|at_least_once|\+|never|\!)|\((?<min>\d*)((?<comma>,)(?<max>\d*))?\)))"))
			{
				if (_caret.LastMatch.Groups["plus"].Value != "")
					Quantifier.Additive = true;

				switch (_caret.LastMatch.Groups["keyword"].Value.ToLower())
				{
				case "if_any":
				case "?":
					Quantifier.Min = 0;
					Quantifier.Max = 1;
					break;
				case "as_many":
				case "*":
					Quantifier.Min = 0;
					Quantifier.Max = -1;
					break;
				case "at_least_once":
				case "+":
					Quantifier.Min = 1;
					Quantifier.Max = -1;
					break;
				case "never":
				case "!":
					Quantifier.Min = 0;
					Quantifier.Max = 0;
					break;
				default:
					string min = _caret.LastMatch.Groups["min"].Value, max = _caret.LastMatch.Groups["max"].Value;

					//Get minimum
					if (min == "") Quantifier.Min = 0;
					else if (!int.TryParse(min, out Quantifier.Min)) ThrowParseError("Expecting an integer as minimum");

					//Get maximum
					if (max == "")
						if (_caret.LastMatch.Groups["comma"].Value != "")
							Quantifier.Max = -1;
						else
							Quantifier.Max = Quantifier.Min;
					else if (!int.TryParse(max, out Quantifier.Max)) ThrowParseError("Expecting an integer as maximum");

					//At least one must be specified
					if ((min == "") && (max == "")) ThrowParseError("Specify at least minimum or maximum");

					break;
				}

				if (Quantifier.Additive && ((Quantifier.Max == 0) || (Quantifier.Max ==1)))
					ThrowParseError("& is redundant");

				Quantifier.SourcePosition = _caret.LastMatch;

				return true;
			}
			else if (_caret.Match(@"(?<_skw>(?<plus>\&))"))
				ThrowParseError("Did you mean & to be text concatenation? Leave a space for concatanation or specify a quantifier");

			return false;
		}
	}
}