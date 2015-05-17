using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

using com.dalsemi.onewire;
using com.dalsemi.onewire.adapter;
using com.dalsemi.onewire.application.monitor;

namespace DSReader
{
    // COM interface
    [Guid("E332DF33-8AAF-4BBD-830A-5C3C8E9E5F5F")]
    public interface DSReader_Interface
    {
        [DispId(1)]
        long GetID(int timeout = 10);
    }

    [Guid("6D7DDED5-E94F-43BC-B729-767E9835FA41"),
        ClassInterface(ClassInterfaceType.None),
        ComSourceInterfaces(typeof(DSReader_Events))]
    public class Reader : DSReader_Interface
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
    }


    [Guid("A653C8C0-7D93-4B85-B503-57665E2D7A93")]
    public interface DSReader_Events
    {

    }
}
