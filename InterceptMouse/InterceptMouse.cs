/*

 */
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace InterceptMouse
{
	class CruiseEvent {
		public int cruiseDir;
		public IntPtr cruiseHw;
		public int cruisePt;
	}
	
	class InterceptMouse
	{
		
		private static int loopSleep;
		private static int scrollDelta;
		private static LowLevelMouseProc _proc = HookCallback;
		private static IntPtr _hookID = IntPtr.Zero;
		private static BlockingCollection<CruiseEvent> queue = new BlockingCollection<CruiseEvent>();

		public static int Main(string[] args)
		{
			if (args.Length == 0) {
				loopSleep = 10;
				scrollDelta = 15;
			} else if (args.Length == 2) {
				loopSleep = Convert.ToInt16(args[0]);
				scrollDelta = Convert.ToInt16(args[1]);
				
			} else {
				Console.WriteLine("Wrong number of arguments: " + args);
				return 1;
			}
			new Thread(delegate() {
			           	CruiseEvent cruise;
			           	while (true) {
			           		cruise = queue.Take();
			           		if (cruise.cruiseDir != 0) {
			           			Console.WriteLine("cruiseDir: " + cruise.cruiseDir);
			           			if (cruise.cruiseHw != IntPtr.Zero) {
			           				Console.WriteLine("cruiseHw: " + cruise.cruiseHw);
			           				float ticks = 0.0f;
			           				while (queue.Count == 0) {
			           					ticks = Math.Min(ticks + 0.2f, scrollDelta);
			           					int delta = cruise.cruiseDir * (int)ticks;
			           					SendMessage(cruise.cruiseHw, (uint)MouseMessages.WM_MOUSEWHEEL, delta << 16, cruise.cruisePt);
			           					Thread.Sleep(loopSleep);
			           				}
			           			}
			           		}
			           	}
			           }).Start();
			
			_hookID = SetHook(_proc);
			Application.Run();
			UnhookWindowsHookEx(_hookID);
			return 0;
		}

		private static IntPtr SetHook(LowLevelMouseProc proc)
		{
			using (Process curProcess = Process.GetCurrentProcess())
				using (ProcessModule curModule = curProcess.MainModule)
			{
				return SetWindowsHookEx(WH_MOUSE_LL, proc,
				                        GetModuleHandle(curModule.ModuleName), 0);
			}
		}

		private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

		private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			bool handled = false;
			if (nCode >= 0 /*&& MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam*/)
			{
				MouseMessages msgId = (MouseMessages) wParam;
				MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
//				if (msgId == MouseMessages.WM_XBUTTONDOWN || msgId == MouseMessages.WM_XBUTTONUP) {
//					Console.WriteLine(wParam.ToString("X4")
//					                  + " (" + hookStruct.pt.x + ", " + hookStruct.pt.y + ") "
//					                  + hookStruct.mouseData.high.ToString("X4") + " "
//					                  + hookStruct.flags);
//				}
				CruiseEvent cruise = null; 
				if (msgId == MouseMessages.WM_XBUTTONDOWN)
				{
					cruise = new CruiseEvent();
					cruise.cruiseHw = WindowFromPoint(hookStruct.pt);
					cruise.cruisePt = hookStruct.pt.y << 16 | hookStruct.pt.x;
					if (hookStruct.mouseData.high == 1) {
						cruise.cruiseDir = +1;
					}
					else if (hookStruct.mouseData.high == 2) {
						cruise.cruiseDir = -1;
					}
					else {
						cruise.cruiseDir = 0;
					}
					handled = true;
				}
				else if (msgId == MouseMessages.WM_XBUTTONUP) {
					cruise = new CruiseEvent();
					cruise.cruiseDir = 0;
					cruise.cruiseHw = IntPtr.Zero;
					handled = true;
				}
				if (cruise != null) {
					queue.Add(cruise);
				}
			}
			return handled ? (IntPtr) 1 : CallNextHookEx(_hookID, nCode, wParam, lParam);
		}

		private const int WH_MOUSE_LL = 14;

		private enum MouseMessages
		{
			WM_LBUTTONDOWN = 0x0201,
			WM_LBUTTONUP = 0x0202,
			WM_MOUSEMOVE = 0x0200,
			WM_MOUSEWHEEL = 0x020A,
			WM_RBUTTONDOWN = 0x0204,
			WM_RBUTTONUP = 0x0205,
			WM_XBUTTONDOWN = 0x020B,
			WM_XBUTTONUP = 0x020C
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int x;
			public int y;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct DWORD
		{
			[FieldOffset(0)]
			public ushort low;

			[FieldOffset(2)]
			public ushort high;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MSLLHOOKSTRUCT
		{
			public POINT pt;
			public DWORD mouseData;
			public uint flags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern IntPtr WindowFromPoint(POINT pnt);
		
		[DllImport("user32.dll")]
		private static extern bool PostMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

		[DllImport("user32.dll")]
		private static extern bool SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		
	}
}