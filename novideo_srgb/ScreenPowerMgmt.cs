using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace novideo_srgb
{
    public enum PowerMgmt
    {
        StandBy,
        Off,
        On
    };

    public class ScreenPowerMgmtEventArgs
    {
        private PowerMgmt _PowerStatus;
        public ScreenPowerMgmtEventArgs(PowerMgmt powerStat)
        {
            this._PowerStatus = powerStat;
        }
        public PowerMgmt PowerStatus
        {
            get { return this._PowerStatus; }
        }
    }

    internal static class NativeMethods
    {
        public static Guid GUID_CONSOLE_DISPLAY_STATE = new Guid(0x6fe69556, 0x704a, 0x47a0, 0x8f, 0x24, 0xc2, 0x8d, 0x93, 0x6f, 0xda, 0x47);
        public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0x00000000;
        public const int WM_POWERBROADCAST = 0x0218;
        public const int PBT_POWERSETTINGCHANGE = 0x8013;

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct POWERBROADCAST_SETTING
        {
            public Guid PowerSetting;
            public uint DataLength;
            public byte Data;
        }

        [DllImport(@"User32", SetLastError = true, EntryPoint = "RegisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid PowerSettingGuid, Int32 Flags);

        [DllImport(@"User32", SetLastError = true, EntryPoint = "UnregisterPowerSettingNotification", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnregisterPowerSettingNotification(IntPtr handle);
    }

    public class ScreenPowerMgmt
    {
        private IntPtr _screenStateNotify = IntPtr.Zero;
        private HwndSource _hwndSource;

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_POWERBROADCAST) {
                if (wParam.ToInt32() == NativeMethods.PBT_POWERSETTINGCHANGE) {
                    var s = (NativeMethods.POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(NativeMethods.POWERBROADCAST_SETTING));
                    if (s.PowerSetting == NativeMethods.GUID_CONSOLE_DISPLAY_STATE) {
                        if (s.Data == 0) {
                            SwitchMonitorOff();
                        }
                        else {
                            SwitchMonitorOn();
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }

        public void Register(IntPtr handle)
        {
            _screenStateNotify = NativeMethods.RegisterPowerSettingNotification(handle, ref NativeMethods.GUID_CONSOLE_DISPLAY_STATE, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource.AddHook(HwndHook);
        }
        
        ~ScreenPowerMgmt()
        {
            if (_screenStateNotify != IntPtr.Zero) {
                _hwndSource.RemoveHook(HwndHook);
                NativeMethods.UnregisterPowerSettingNotification(_screenStateNotify);
            }
        }

        public delegate void ScreenPowerMgmtEventHandler(object sender, ScreenPowerMgmtEventArgs e);
        public event ScreenPowerMgmtEventHandler ScreenPower;
        private void OnScreenPowerMgmtEvent(ScreenPowerMgmtEventArgs args)
        {
            if (this.ScreenPower != null)
                this.ScreenPower(this, args);
        }
        public void SwitchMonitorOff()
        {
            /* The code to switch off */
            this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.Off));
        }
        public void SwitchMonitorOn()
        {
            /* The code to switch on */
            this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.On));
        }
        public void SwitchMonitorStandby()
        {
            /* The code to switch standby */
            this.OnScreenPowerMgmtEvent(new ScreenPowerMgmtEventArgs(PowerMgmt.StandBy));
        }
    }
}
