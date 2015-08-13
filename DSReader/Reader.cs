using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.EnterpriseServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;
using System.IO;

namespace DSReader
{
    // COM interface
    [Guid("E332DF33-8AAF-4BBD-830A-5C3C8E9E5F5F")]
    public interface DSReader_Interface
    {
        long GetID(int timeout = 10);
        string GetIDStr(int timeout = 10);
    }

    [Guid("AB634001-F13D-11d0-A459-004095E1DAEA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInitDone
    {
        void Init([MarshalAs(UnmanagedType.IDispatch)] object pBackConnection);

        void Done();

        void GetInfo([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref object[] pInfo);
    }

    [Guid("6D7DDED5-E94F-43BC-B729-767E9835FA41"),
        ClassInterface(ClassInterfaceType.AutoDual)]
    public class Reader : DSReader_Interface, IInitDone
    {
        private const string deviceName = @"\\.\touchm0";
        private const int idArraySize = 8;
        private const uint ioctlPresenceDetect = 0x226A90;

        private byte[] result = new byte[idArraySize] { 0, 0, 0, 0, 0, 0, 0, 0 };
        private uint outBytes = 0;

        #region Import signatures
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            uint IoControlCode,
            IntPtr InBuffer,
            uint nInBufferSize,
            IntPtr OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            IntPtr Overlapped
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
            string lpFileName,
            [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
            [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
            IntPtr lpSecurityAttributes,
            [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
            [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
            IntPtr hTemplateFile);
        #endregion


        // @timeout -  time in seconds during which network gets polled
        // returns unsigned 64-bit ID of first iButton detected during selected timeframe
        // returns 0 if no iButton device was presented)
        // return -1 if an exception is raised (receptor device not present)
        public long GetID(int timeout = 10)
        {
            try
            {
                // safe Win32 handle to device
                var handle = CreateFile(deviceName,
                                    FileAccess.ReadWrite,
                                    FileShare.ReadWrite,
                                    IntPtr.Zero,
                                    FileMode.Open,
                                    FileAttributes.Normal,
                                    IntPtr.Zero);

                if (handle.IsInvalid)
                {
                    return -1;
                }

                // memalloc for iocontrol return byte[]
                // all iButton ID's are now 8 bytes long, so idArraySize = 8
                IntPtr res = Marshal.AllocHGlobal(idArraySize);

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // enforcing internal timeout
                while (sw.Elapsed <= TimeSpan.FromSeconds(timeout))
                {
                    // continuously sending ioctl to device, waiting for success
                    bool success = DeviceIoControl(handle,
                                                ioctlPresenceDetect,
                                                IntPtr.Zero,
                                                0,
                                                res,
                                                (uint)idArraySize,
                                                ref outBytes,
                                                IntPtr.Zero);
                    if (success)
                    {
                        // device was found, getting data from unmanaged memory, freeing & returning
                        Marshal.Copy(res, result, 0, idArraySize);
                        Marshal.FreeHGlobal(res);

                        return BitConverter.ToInt64(result, 0);
                    }

                    Thread.Sleep(10);
                }

                // no iButton detected before timeout
                return 0;
            }
            catch (Exception)
            {
                // well, something's wrong
                return -1;
            }
            
        }

        public string GetIDStr(int timeout = 10)
        {
            try
            {
                var a = GetID(timeout);

                switch (a)
                {
                    case 0:
                        return "NOT_FOUND";
                    case -1:
                        return "DRIVER_ERROR";
                    default:
                        return a.ToString("X");    
                }
            }
            catch (Exception)
            {
                // we shouldn't really land here, but just in case
                return "GENERAL_ERROR";
            }

        }

        public Reader()
        {
        }

        public void Init([MarshalAs(UnmanagedType.IDispatch)][In] object pBackConnection)
        {
            //return S_OK;
        }

        public void Done()
        {
            //return S_OK;
        }

        public void GetInfo([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_VARIANT)] ref object[] pInfo)
        {
            pInfo[0] = 2000;

            //return S_OK;
        }

    }
}
