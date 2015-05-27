using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.EnterpriseServices;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.monitor;

namespace DSReader
{
    // COM interface
    [Guid("E332DF33-8AAF-4BBD-830A-5C3C8E9E5F5F")]
    public interface DSReader_Interface
    {
        long GetID(int timeout = 10);
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

        public Reader()
        { }

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
