using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.monitor;

namespace DSReader
{
    [ComImport]
    [Guid("00020400-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDispatch
    {
        [PreserveSig]
        int GetTypeInfoCount(out int Count);

        [PreserveSig]
        int GetTypeInfo
        (
          [MarshalAs(UnmanagedType.U4)] int iTInfo,
          [MarshalAs(UnmanagedType.U4)] int lcid,
          out System.Runtime.InteropServices.ComTypes.ITypeInfo typeInfo
        );

        [PreserveSig]
        int GetIDsOfNames
        (
          ref Guid riid,
          [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr)]
    string[] rgsNames,
          int cNames,
          int lcid,
          [MarshalAs(UnmanagedType.LPArray)] int[] rgDispId
        );

        [PreserveSig]
        int Invoke
        (
          int dispIdMember,
          ref Guid riid,
          uint lcid,
          ushort wFlags,
          ref System.Runtime.InteropServices.ComTypes.DISPPARAMS pDispParams,
          out object pVarResult,
          ref System.Runtime.InteropServices.ComTypes.EXCEPINFO pExcepInfo,
          out UInt32 pArgErr
        );
    }


    // COM interface
    [Guid("E332DF33-8AAF-4BBD-830A-5C3C8E9E5F5F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface DSReader_Interface
    {
        [DispId(1)]
        long GetID(int timeout = 10);
    }

    [Guid("E29CEEE7-EDA9-48A3-8045-D8735B16761C")]
    public interface IInitDone
    {
        [DispId(1)]
        int Init(IDispatch pBackConnection);
        
        [DispId(2)]
        int Done();

        [DispId(3)]
        int GetInfo([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] ref IntPtr pInfo);
    }

    [Guid("6D7DDED5-E94F-43BC-B729-767E9835FA41"),
        ClassInterface(ClassInterfaceType.None),
        ComSourceInterfaces(typeof(DSReader_Events))]
    [ProgId("AddIn.DSReader")]
    public class Reader : DSReader_Interface, IInitDone
    {
        public const int S_OK = unchecked((int)0x00000000);
        // @timeout -  time in seconds during which network gets polled
        // returns unsigned 64-bit ID of first iButton detected during selected timeframe
        // returns 0 if no iButton device was presented)
        // return -1 if an exception is raised (1-Wire network not available, most likely driver misconfig)
        public long GetID(int timeout = 10)
        {
            try
            {
                // adapter init. 1-Wire Default Device must be selected in driver beforehand!
                DSPortAdapter adapter = OneWireAccessProvider.getDefaultAdapter();
                DeviceMonitor dMonitor;
                
                // vectors for J# interop
                java.util.Vector arrivals = new java.util.Vector();
                java.util.Vector departures = new java.util.Vector();

                // get exclusive use of adapter
                adapter.beginExclusive(true);

                // clear any previous search restrictions
                adapter.setSearchAllDevices();
                adapter.targetAllFamilies();
                adapter.setSpeed(DSPortAdapter.SPEED_REGULAR);

                // release exclusive use of adapter
                adapter.endExclusive();

                // Monitor of the network
                dMonitor = new DeviceMonitor(adapter);
                dMonitor.setDoAlarmSearch(false);

                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // enforcing internal timeout
                while (sw.Elapsed <= TimeSpan.FromSeconds(timeout))
                {
                    dMonitor.search(arrivals, departures);

                    if (arrivals.size() != 0)
                    {
                        // found device, returning it's address as unsigned long
                        return ((java.lang.Long)arrivals.firstElement()).longValue();
                    }
                }

                // no iButton detected
                return 0;
            }
            catch (Exception)
            {
                // 1-Wire Net is unavailiable (no receptor detected/driver misconfiguration)
                return -1;
            }
            
        }


        public int Init(IDispatch pBackConnection)
        {
            return S_OK;
        }

        public int Done()
        {
            return S_OK;
        }

        public int GetInfo([MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] ref IntPtr pInfo)
        {
            // defererencing double pointer to actual pointer - not needed since we're accepting ref ptr
            //var deref1 = (IntPtr)Marshal.PtrToStructure(pInfo, typeof(IntPtr));

            // dereferencing pointer to actual int[]
            var deref2 = (int[])Marshal.PtrToStructure(pInfo, typeof(int[]));

            deref2[0] = 2000;

            Marshal.StructureToPtr(deref2, pInfo, false);

            return S_OK;
        }

    }


    [Guid("A653C8C0-7D93-4B85-B503-57665E2D7A93")]
    public interface DSReader_Events
    {

    }
}
