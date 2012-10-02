using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public enum ScrollBarType : uint
{
	SbHorz = 0,
	SbVert = 1
}
public class FixedRichTextBox : RichTextBox
{
	protected int _selection_start, _selection_length;
	protected int _vscroll_position, _hscroll_position;

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
	private const int WM_SETREDRAW = 0x0b;
	[DllImport("User32.dll")]
	private static extern int GetScrollPos(IntPtr hWnd, int nBar);
	private enum Message : uint
	{
		WM_HSCROLL = 0x0114,
		WM_VSCROLL = 0x0115
	}
	private enum ScrollBarCommands : uint
	{
		SB_THUMBPOSITION = 4
	}

	public void BeginUpdate(bool SaveSelectionAndView = true)
	{
		if (SaveSelectionAndView)
		{
			_selection_start = SelectionStart;
			_selection_length = SelectionLength;
			_vscroll_position = GetScrollBarPosition(ScrollBarType.SbVert);
			_hscroll_position = GetScrollBarPosition(ScrollBarType.SbHorz);
		}
		SendMessage(Handle, WM_SETREDRAW, (IntPtr)0, IntPtr.Zero);
	}
	public void EndUpdate(bool LoadSelectionAndView = true)
	{
		if (LoadSelectionAndView)
		{
			Select(_selection_start, _selection_length); 
			SetScrollBarPosition(ScrollBarType.SbVert, _vscroll_position);
			SetScrollBarPosition(ScrollBarType.SbHorz, _hscroll_position);
		}
		SendMessage(Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
		Invalidate();
	}

	public int GetScrollBarPosition(ScrollBarType sbt)
	{
		return GetScrollPos(Handle, (int)sbt) << 16;
	}
	public void SetScrollBarPosition(ScrollBarType sbt, int Value)
	{
		uint wParam = (uint)ScrollBarCommands.SB_THUMBPOSITION | (uint)Value;
		SendMessage(Handle, (int)((sbt == ScrollBarType.SbVert) ? Message.WM_VSCROLL : Message.WM_HSCROLL), new IntPtr(wParam), new IntPtr(0));
	}
	/// <summary>
	/// AutoWordSelection flag is not properly followed
	/// </summary>
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		if (!AutoWordSelection) {
			AutoWordSelection = true;
			AutoWordSelection = false;
		}
	}

	public FixedRichTextBox AppendAndSelect(string newText)
	{
		int ss = Text.Length;
		AppendText(newText);
		SelectionStart = ss;
		SelectionLength = Text.Length - ss;

		return this;
	}
}