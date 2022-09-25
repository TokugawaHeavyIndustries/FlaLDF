using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenCLNet
{
    public class Extension
    {
        /// <summary>
        /// IsAvailable is true if the API was imported correctly, false otherwise
        /// </summary>
        public bool IsAvailable { get; internal set; }

        public Platform Platform { get; internal set; }

        internal Extension(Platform platform)
        {
            Platform = platform;
        }

        virtual internal string GetName()
        {
            return "[Extension has not overridden GetName()]";
        }

        #region Helpers

        internal IntPtr ImportFunction(string functionName)
        {
            IntPtr entryPoint;

            entryPoint = OpenCL.GetExtensionFunctionAddressForPlatform(Platform, functionName);
            if (entryPoint == IntPtr.Zero)
                throw new EntryPointNotFoundException("Error when importing function \"" + functionName + "\" from the " + GetName() + " extension");
            return entryPoint;
        }

        internal IntPtr[] RepackEvents(IList<Event> events)
        {
            if (events == null)
                return null;

            IntPtr[] repackedEvents = new IntPtr[events.Count];

            for (int i = 0; i < events.Count; i++)
                repackedEvents[i] = events[i];
            return repackedEvents;
        }

        internal IntPtr[] RepackMems(IList<Mem> objects)
        {
            if (objects == null)
                return null;

            IntPtr[] repackedObjects = new IntPtr[objects.Count];

            for (int i = 0; i < objects.Count; i++)
                repackedObjects[i] = objects[i];
            return repackedObjects;
        }

        #endregion
    }
}