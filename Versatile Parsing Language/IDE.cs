using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VPL.Compile;
using VPL.Execute;
using VPL.Parse;
using Block = VPL.Execute.Block;
using TreeNode = System.Windows.Forms.TreeNode;

namespace VPL
{
	/// <summary>
	/// The IDE holds information about the GrammarTree, ExecutionTree and AssmeblyOutput
	/// </summary>
	public class IDE
	{
		/// <summary>
		/// The position of an element or a runnable in the source text and the assembly output
		/// </summary>
		public class Annotation
		{
			public Element Element;
			public Runnable FirstRunnable;
			public Block RunnableParentBlock;
			public int RunnablesCount;

			public TreeNode TreeViewNode;
			public ListViewItem ListViewItem;

			public TextPortion SourcePosition, CompiledPosition;

			public IDE IDE;

			public void CopyFrom(Annotation Other)
			{
				Element = Other.Element;
				FirstRunnable = Other.FirstRunnable;
				RunnableParentBlock = Other.RunnableParentBlock;
				RunnablesCount = Other.RunnablesCount;

				TreeViewNode = Other.TreeViewNode;
				ListViewItem = Other.ListViewItem;

				SourcePosition = Other.SourcePosition;
				CompiledPosition = Other.CompiledPosition;

				IDE = Other.IDE;
			}
		}

		public readonly Dictionary<SyntaxType.eType, SyntaxType> ParseSyntaxOccurences = new Dictionary<SyntaxType.eType, SyntaxType>(), CompiledSyntaxOccurences = new Dictionary<SyntaxType.eType, SyntaxType>();
		public readonly TextPortionTree<Annotation> SourceAnnotations = new TextPortionTree<Annotation>(), CompiledAnnotations = new TextPortionTree<Annotation>();

		public FixedRichTextBox SourceRTF, CompiledRTF;
		public Parser Parser = new Parser();
		public Compiler Compiler = new Compiler();

		public object SourceUID;
		public string SourceText, AssemblyOutput;
		public string SourceFileName;

		public GrammarTree GrammarTree = new GrammarTree();
		public ExecutionTree ExecutionTree = new ExecutionTree();

		/// <summary>
		/// Get an imported file from the IDE
		/// </summary>
		/// <param name="RequesterUID">The UID provided by the IDE of the source file that wants to import</param>
		/// <param name="FileName">The filename requested</param>
		/// <param name="newSource">The requested file's contents</param>
		/// <param name="newUID">The requested file's UID</param>
		/// <param name="newFileName">The requested file's file name</param>
		/// <returns>True on success finding and loading the file, false otherwise</returns>
		public delegate bool RequestSourceFile(object RequesterUID, string FileName, out string newSource, out object newUID, out string newFileName);
		public RequestSourceFile GetSourceFile;

		public IDE()
		{
			SyntaxType.InitList(ParseSyntaxOccurences, SyntaxType.eSyntaxType.Parse);
			SyntaxType.InitList(CompiledSyntaxOccurences, SyntaxType.eSyntaxType.Compiled);
			Clear();
		}

		public void ApplyParseSyntaxColoring()
		{
			FixedRichTextBox rtf = SourceRTF;
			if (rtf == null)
				return;
				
			rtf.BeginUpdate();

			//Find the lengthiest type and select it as the default color
			//SyntaxType.eType lengthiest = SyntaxType.eType.Name;
			int max_length = 0;
// ReSharper disable LoopCanBeConvertedToQuery
			foreach (var ist in ParseSyntaxOccurences)
// ReSharper restore LoopCanBeConvertedToQuery
			{
				if (ist.Value.Occurences.Count > max_length)
				{
					max_length = ist.Value.Occurences.Count;
					//lengthiest = ist.Value.Type;
				}
			}

			rtf.SelectAll();
			//if (ParseSyntaxOccurences.ContainsKey(lengthiest))
			//    rtf.SelectionColor = ParseSyntaxOccurences[lengthiest].Color;//Color.Black;
			//else
				rtf.SelectionColor = Color.Black;
			rtf.SelectionBackColor = Color.White;

			//Apply syntax
			foreach (var ist in ParseSyntaxOccurences)
			{
				var st = ist.Value;
				//if (st.Type != lengthiest)
					foreach (var tp in st.Occurences)
					{
						tp.ToRTB(rtf);
// ReSharper disable RedundantCheckBeforeAssignment
						if (rtf.SelectionColor != st.Color)
// ReSharper restore RedundantCheckBeforeAssignment
							rtf.SelectionColor = st.Color;
					}
			}

			//Apply errors
			ApplyErrors(GrammarTree.ParseErrors);

			rtf.EndUpdate();
		}
		public void ApplyCompiledSyntaxColoring()
		{
			FixedRichTextBox rtf = CompiledRTF;
			if (rtf == null)
				return;
				
			rtf.BeginUpdate();

			//Find the lengthiest type and select it as the default color
			SyntaxType.eType lengthiest = SyntaxType.eType.Name;
			int max_length = 0;
			foreach (var ist in CompiledSyntaxOccurences)
			{
				if (ist.Value.Occurences.Count > max_length)
				{
					max_length = ist.Value.Occurences.Count;
					lengthiest = ist.Value.Type;
				}
			}

			//Apply the text
			rtf.Clear();
			if (CompiledSyntaxOccurences.ContainsKey(lengthiest))
				rtf.ForeColor = CompiledSyntaxOccurences[lengthiest].Color;//Color.Black;
			else
				rtf.ForeColor = Color.Black;
			rtf.Text = AssemblyOutput;

			//Apply syntax
			foreach (var ist in CompiledSyntaxOccurences)
			{
				var st = ist.Value;
				if (st.Type != lengthiest)
					foreach (var tp in st.Occurences)
					{
						tp.ToRTB(rtf);
						rtf.SelectionColor = st.Color;
					}
			}

			rtf.EndUpdate();

			//Apply errors
			rtf = SourceRTF;
			if (rtf == null)
				return;
			rtf.BeginUpdate();
			ApplyErrors(ExecutionTree.ParseErrors);
			rtf.EndUpdate();
		}
		public void ApplyErrors(IEnumerable<ParseError> ParseErrors)
		{
			var rtf = SourceRTF;
			if (rtf == null)
				return;

			foreach (var er in ParseErrors)
			{
				er.TextPortion.ToRTB(rtf);
				rtf.SelectionBackColor = Color.LightPink;
			}
		}
		public void ClearSourceSyntaxOccurences()
		{
			foreach (var st in ParseSyntaxOccurences)
			{
				st.Value.Occurences.Clear();
				st.Value.Length = 0;
			}
		}
		public void ClearCompiledSyntaxOccurences()
		{
			foreach (var st in CompiledSyntaxOccurences)
			{
				st.Value.Occurences.Clear();
				st.Value.Length = 0;
			}
		}
		public void Clear()
		{
			GrammarTree.IDE = this;
			ExecutionTree.IDE = this;
			Parser.IDE = this;
			Compiler.IDE = this;

			ClearSourceSyntaxOccurences();
			SourceAnnotations.Clear();
			CompiledAnnotations.Clear();
			Parser.Clear();
			Compiler.Clear();
			GrammarTree.Clear();
			ExecutionTree.Clear();
		}
	}
}