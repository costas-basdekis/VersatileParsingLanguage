using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace VPL
{
	/// <summary>
	/// A repeated execution of a statement
	/// </summary>
	public struct Quantifier
	{
		public bool Additive;
		public int Min, Max, Count;
		public TextPortion SourcePosition;

		public bool IsDefault()
		{
			return (Min == 1) && (Max == 1);
		}
		public bool IsIfAny()
		{
			return (Min == 0) && (Max == 1);
		}
		public bool IsAsMany()
		{
			return (Min == 0) && (Max == -1);
		}
		public bool IsNever()
		{
			return Max == 0;
		}

		public override string ToString()
		{
			string add = Additive ? "+" : "";

			if ((Min == 1) && (Max == 1))
				return add + "";
			else if ((Min == 0) && (Max == 0))
				return add + "(never)";
			else if ((Min == 0) && (Max == 1))
				return add + "(if any)";
			else if ((Min == 0) && (Max == -1))
				return add + "(as many)";
			else if ((Min == 1) && (Max == -1))
				return add + "(at least once)";
			else if (Max < 0)
				return add + "(at least " + Min + " times)";
			else if (Min == Max)
				return add + "(" + Min + ")";
			else
				return add + "(" + Min + ", " + Max + ")";
		}
	}

	/// <summary>
	/// A structure that holds information about a string portion, or a region in a known string
	/// The implicit operators for Match, Capture and Group store the region in the known string, but not the string
	/// </summary>
	public struct TextPortion
	{
		public string Text;
		public int Length
		{
			get { return _Length; }
			set
			{
				_Length = value;
				if (Text != null)
					Debug.Assert((_Begin + _Length) <= Text.Length);
			}
		}
		private int _Begin, _Length;

		public bool IsInit { get { return (_Begin != 0) || (Length != 0); } }
		public bool PortionInPortion(TextPortion Other)
		{
			return (Other._Begin >= _Begin) && (Other.End <= End);
		}

		/// <summary>
		/// Set the begin without changing the end
		/// </summary>
		public int Begin
		{
			get { return _Begin; }
			set
			{
				_Length += _Begin - value;
				_Begin = value;
				if (Text != null)
					Debug.Assert((_Begin + _Length) <= Text.Length);
			}
		}
		/// <summary>
		/// Set the end without changing the begin
		/// </summary>
		public int End
		{
			get { return _Begin + Length; }
			set { Length = value - _Begin; }
		}

		public TextPortion(string newText, int newBegin = 0, int newLength = -1)
		{
			Text = newText;
			_Begin = newBegin;
			if (newLength <= -1)
				_Length = Text.Length;
			else
				_Length = newLength;

			if (Text != null)
				Debug.Assert((_Begin + _Length) <= Text.Length);
		}
		public TextPortion(int newBegin, int newLength)
		{
			Text = null;
			_Begin = newBegin;
			_Length = newLength;
		}
		public TextPortion(TextPortion rhs)
		{
			Text = rhs.Text;
			_Begin = rhs._Begin;
			_Length = rhs.Length;

			if (Text != null)
				Debug.Assert((_Begin + _Length) <= Text.Length);
		}

		public static implicit operator TextPortion(string value)
		{
			return new TextPortion(value);
		}
		public static implicit operator TextPortion(Match value)
		{
			return new TextPortion(value.Index, value.Length);
		}
		public static implicit operator TextPortion(Group value)
		{
			return new TextPortion(value.Index, value.Length);
		}
		public static implicit operator TextPortion(Capture value)
		{
			return new TextPortion(value.Index, value.Length);
		}

		/// <summary>
		/// Try to join two TextPortions if they refer to the same string and are adjacent
		/// </summary>
		public bool TryAppend(TextPortion Other)
		{
			if (Other.Length == 0)
				return true;
			
			if (ReferenceEquals(Text, Other.Text) && (End == Other._Begin))
			{
				End = Other.End;
				return true;
			}

			return false;
		}
		/// <summary>
		/// Joins two TextPortions
		/// </summary>
		public void Append(TextPortion Other)
		{
			if (TryAppend(Other))
				return;

			Text = ToString() + Other.ToString();
			_Begin = 0;
			Length = Text.Length;
		}

		/// <summary>
		/// Checks if Pattern equals the text, but does not change the struct
		/// </summary>
		public bool Compare(TextPortion Pattern, bool IgnoreCase)
		{
			if (_Length != Pattern._Length)
				return false;

			return Match(Pattern, IgnoreCase);
		}
		/// <summary>
		/// Checks if Pattern is at the beginning of the text and updates the struct
		/// </summary>
		public bool Match(TextPortion Pattern, bool IgnoreCase)
		{
			if (Pattern.Length > Length)
				return false;

			if (string.Compare(Text, _Begin, Pattern.Text, Pattern._Begin, Pattern.Length, IgnoreCase) != 0)
				return false;

			MoveCaret(Pattern.Length);

			return true;
		}
		/// <summary>
		/// Checks if Pattern exists in the text and updates the struct
		/// </summary>
		public bool Find(string Pattern)
		{
			if (Pattern.Length > Length)
				return false;

			var new_begin = Text.IndexOf(Pattern, _Begin, Length, StringComparison.Ordinal);
			if (new_begin == -1)
				return false;

			MoveCaret((new_begin - _Begin) + Pattern.Length);

			return true;
		}
		/// <summary>
		/// Checks if Pattern exists in the text and updates the struct using the last occurence
		/// </summary>
		public bool FindReverse(string Pattern)
		{
			if (Pattern.Length > Length)
				return false;

			var new_begin = Text.LastIndexOf(Pattern, _Begin + Length, Length, StringComparison.Ordinal);
			if (new_begin == -1)
				return false;

			MoveCaret((new_begin - _Begin) + Pattern.Length);

			return true;
		}
		/// <summary>
		/// Move the begin and change length accordingly
		/// </summary>
		public void MoveCaret(int Delta)
		{
			_Begin += Delta;
			Length -= Delta;
		}
		/// <summary>
		/// Move the begin and change length accordingly
		/// </summary>
		public void MoveCaretTo(int newBegin)
		{
			Length -= newBegin - _Begin;
			_Begin = newBegin;
		}

		// ReSharper disable UnusedMember.Local
		public void FromRTB(RichTextBox rtb)
		{
			_Begin = rtb.SelectionStart;
			Length = rtb.SelectionLength;
		}
		public void ToRTB(RichTextBox rtb)
		{
			rtb.SelectionStart = _Begin;
			rtb.SelectionLength = Length;
		}
		// ReSharper restore UnusedMember.Local

		public override string ToString()
		{
			if (Text == null)
				return string.Empty;
			else if ((_Begin != 0) || (Length != Text.Length))
				return Text.Substring(_Begin, Length);
			else
				return Text;
		}
		public string TextPosition()
		{
			return string.Format("{0},{1}", _Begin.ToString(), End.ToString());
		}

		public void Clear()
		{
			Text = string.Empty;
			_Begin = 0;
			_Length = 0;
		}
	}
	/// <summary>
	/// A structure to map text areas to objects
	/// </summary>
	public class TextPortionTree<T>
	{
		private T _Tag;
		private TextPortion _TP;
		private readonly List<TextPortionTree<T>> _Children = new List<TextPortionTree<T>>();

		public TextPortionTree()
		{
			
		}
		private TextPortionTree(TextPortion newTP, T newTag)
		{
			_Tag = newTag;
			_TP = newTP;
		}

		private void UpdateTP()
		{
			if (_Children.Count == 0)
				_TP = string.Empty;
			else
			{
				_TP.Begin = _Children[0]._TP.Begin;
				_TP.End = _Children[_Children.Count - 1]._TP.End;
			}
		}
		public bool Add(TextPortion newItem, T newTag)
		{
			TextPortionTree<T> ch = new TextPortionTree<T>(newItem, newTag);

			ch._Tag = newTag;

			_TP = new TextPortion();

			if (!Add(ch))
				return false;

			UpdateTP();

			return true;
		}
		private bool Add(TextPortionTree<T> Item)
		{
			TextPortion tp = Item._TP;

			if (_TP.IsInit && !_TP.PortionInPortion(tp))
				return false;

			for (int i = 0 ; i < _Children.Count ; i++)
			{
				var ch = _Children[i];
				var tpt = ch._TP;
				if (tp.End <= tpt.Begin)
					//New is before ch
				{
					_Children.Insert(i, Item);
					return true;
				}
				else if (tp.Begin >= tpt.End)
					//New is after ch
				{
					//Nothing to do; just for clarity (so to not include in the next else-ifs)
				}
				else if (tpt.PortionInPortion(tp))
					//New is in ch
					return ch.Add(Item);
				else if (tp.PortionInPortion(tpt))
					//New contains ch (and maybe others
				{
					int j;
					for (j = i + 1 ; j < _Children.Count ; j++)
						if (_Children[j]._TP.Begin >= tp.End)
							break;
					Item._Children.AddRange(_Children.GetRange(i, j - i));
					_Children.RemoveRange(i, j - i);
					_Children.Insert(i, Item);
					return true;
				}
				else if ((tp.Begin < tpt.Begin) == (tp.End < tpt.End))
					//They interleave
					return false;
			}

			//New is after the end of the list
			_Children.Add(Item);

			return true;
		}

		public void Clear()
		{
			foreach (var tpt in _Children)
				tpt.Clear();

			_Tag = default(T);
			_Children.Clear();
		}

		public bool Item(int Index, out T result)
		{
			var rv = this;

			while ((rv._TP.Begin < Index) && (Index <= rv._TP.End))
			{
				TextPortionTree<T> newrv = rv._Children.FirstOrDefault(tpt => (tpt._TP.Begin <= Index) && (Index <= tpt._TP.End));

				if (newrv == null)
					break;

				rv = newrv;
			}

			if ((rv._TP.Begin <= Index) && (Index <= rv._TP.End))
				result = rv._Tag;
			else
				result = default(T);

			return (rv._TP.Begin <= Index) && (Index <= rv._TP.End);
		}

		public string ToString(string Ident = "  ")
		{
			string s = string.Format("{0}{1}", Ident, _TP.TextPosition());

			if (_Children.Count > 0)
				s = _Children.Aggregate(s, (current, tpt) => string.Format("{0}{1}", current, ("\n" + "  " + Ident + tpt.ToString(string.Format("  {0}", Ident)))));

			return s;
		}
	}

	public interface IPoolable
	{
		void Init();
	}
	public class PooledBuffer<T> where T : class, IPoolable, new()
	{
		private readonly Stack<T> _buffer;
		private int _new, _reused;
		
		public PooledBuffer(int initialSize = 200)
		{
			_buffer = new Stack<T>(initialSize);
			_new = 0;
			_reused = 0;
		}
		public void Clear()
		{
			_buffer.Clear();
			_new = 0;
			_reused = 0;
		}

		public T New()
		{
			if (_buffer.Count != 0)
			{
				_reused++;
				var it = _buffer.Pop();
				it.Init();
				return it;
			}
			else
			{
				_new++;
				return new T();
			}
		}
		public void Release(T item)
		{
			if (item == null)
				throw new NullReferenceException();
			_buffer.Push(item);
		}

		public int Count()
		{
			return _buffer.Count;
		}
		public float Reusability()
		{
			return (float) _reused / (_reused + _new);
		}
	}

	public class UndoHistory<T>
	{
		private readonly List<T> _history = new List<T>();
		private int _position = 1;
		private int _size = 10;
		public int Size
		{
			get { return _size; }
			set 
			{
				if (value <= 0)
					value = 1;

				var delta_size = value - _size;

				if (delta_size < 0)
				{
					if (_history.Count > -delta_size)
						_history.RemoveRange(0, -delta_size);
					_position -= -delta_size;
					if (_position < 1)
						_position = 1;
				}

				_size = value;
			}
		}
		public int UndoItems
		{
			get { return (_history.Count > 0) ? _position - 1 : 0; }
		}
		public int RedoItems
		{
			get { return (_history.Count > 0) ? _history.Count - _position : 0; }
		}

		public UndoHistory(int newSize = 10)
		{
			_size = newSize;
		}

		public void Change(T value)
		{
			//Remove redo history
			if (_position < _history.Count)
				_history.RemoveRange(_position, _history.Count - _position);

			if (_position < _size)
			{
				if (_history.Count > 0)
					_position++;
				_history.Add(value);
			}
			else
			//Push items back
			{
				for (int i = 0; i < (_position - 1); i++)
					_history[i] = _history[i + 1];

				//Put new item
				_history[_position - 1] = value;
			}
		}
		public T Undo()
		{
			if (_position == 1)
				throw new InvalidOperationException();

			_position--;
			return _history[_position - 1];
		}
		public T Redo()
		{
			if (_position == _history.Count)
				throw new InvalidOperationException();

			_position++;
			return _history[_position - 1];
		}
		public void Clear()
		{
			_history.Clear();
			_position = 1;
		}
	}
}