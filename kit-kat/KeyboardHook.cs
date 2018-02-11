﻿using CefSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kit_kat
{
    public class KeyboardHandler : IKeyboardHandler
    {
        public bool OnPreKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey, ref bool isKeyboardShortcut)
        {
            const int WM_SYSKEYDOWN = 0x104;
            const int WM_KEYDOWN = 0x100;
            const int WM_KEYUP = 0x101;
            const int WM_SYSKEYUP = 0x105;
            const int WM_CHAR = 0x102;
            const int WM_SYSCHAR = 0x106;
            const int VK_TAB = 0x9;
            const int VK_LEFT = 0x25;
            const int VK_UP = 0x26;
            const int VK_RIGHT = 0x27;
            const int VK_DOWN = 0x28;
            isKeyboardShortcut = false;
            if (windowsKeyCode == VK_TAB || windowsKeyCode == VK_LEFT || windowsKeyCode == VK_UP || windowsKeyCode == VK_DOWN || windowsKeyCode == VK_RIGHT)
            {
                return false;
            }
            var control = browserControl as Control;
            var msgType = 0;
            switch (type)
            {
                case KeyType.RawKeyDown:
                    if (isSystemKey)
                    {
                        msgType = WM_SYSKEYDOWN;
                    }
                    else
                    {
                        msgType = WM_KEYDOWN;
                    }
                    break;
                case KeyType.KeyUp:
                    if (isSystemKey)
                    {
                        msgType = WM_SYSKEYUP;
                    }
                    else
                    {
                        msgType = WM_KEYUP;
                    }
                    break;
                case KeyType.Char:
                    if (isSystemKey)
                    {
                        msgType = WM_SYSCHAR;
                    }
                    else
                    {
                        msgType = WM_CHAR;
                    }
                    break;
                default:
                    Trace.Assert(false);
                    break;
            }
            // We have to adapt from CEF's UI thread message loop to our fronting WinForm control here.
            // So, we have to make some calls that Application.Run usually ends up handling for us:
            var state = PreProcessControlState.MessageNotNeeded;
            // We can't use BeginInvoke here, because we need the results for the return value
            // and isKeyboardShortcut. In theory this shouldn't deadlock, because
            // atm this is the only synchronous operation between the two threads.
            control.Invoke(new Action(() =>
            {
                var msg = new Message
                {
                    HWnd = control.Handle,
                    Msg = msgType,
                    WParam = new IntPtr(windowsKeyCode),
                    LParam = new IntPtr(nativeKeyCode)
                };

                // First comes Application.AddMessageFilter related processing:
                // 99.9% of the time in WinForms this doesn't do anything interesting.
                if (Application.FilterMessage(ref msg))
                {
                    state = PreProcessControlState.MessageProcessed;
                }
                else
                {
                    // Next we see if our control (or one of its parents)
                    // wants first crack at the message via several possible Control methods.
                    // This includes things like Mnemonics/Accelerators/Menu Shortcuts/etc...
                    state = control.PreProcessControlMessage(ref msg);
                }
            }));
            if (state == PreProcessControlState.MessageNeeded)
            {
                isKeyboardShortcut = true;
            }
            else if (state == PreProcessControlState.MessageProcessed)
            {
                return true;
            }
            return false;
        }

        public bool OnKeyEvent(IWebBrowser browserControl, IBrowser browser, KeyType type, int windowsKeyCode, int nativeKeyCode, CefEventFlags modifiers, bool isSystemKey)
        {
            if (type == KeyType.KeyUp)
            {
                Keys key = (Keys)windowsKeyCode;
                #region Debug Mode
                if (modifiers == (CefEventFlags.ControlDown | CefEventFlags.ShiftDown) && key == Keys.I)
                {
                    #region If CEF is Ready
                    if (Cef.IsInitialized)
                    {
                        MainUI.ui.ShowDevTools();
                    }
                    #endregion
                    return true;
                }
                #endregion
            }
            return false;
        }
    }
}
