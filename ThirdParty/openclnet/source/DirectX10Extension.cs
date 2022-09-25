using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Security;

namespace OpenCLNet
{
    [SuppressUnmanagedCodeSecurity()]
    public unsafe class DirectX10Extension : Extension
    {
        public static readonly string ExtensionName = "cl_khr_d3d10_sharing";

        internal clGetDeviceIDsFromD3D10KHRDelegate clGetDeviceIDsFromD3D10KHR;
        internal clCreateFromD3D10BufferKHRDelegate clCreateFromD3D10BufferKHR;
        internal clCreateFromD3D10Texture2DKHRDelegate clCreateFromD3D10Texture2DKHR;
        internal clCreateFromD3D10Texture3DKHRDelegate clCreateFromD3D10Texture3DKHR;
        internal clEnqueueAcquireD3D10ObjectsKHRDelegate clEnqueueAcquireD3D10ObjectsKHR;
        internal clEnqueueReleaseD3D10ObjectsKHRDelegate clEnqueueReleaseD3D10ObjectsKHR;
        
        #region Delegates

        // D3D10 Delegates
        internal unsafe delegate ErrorCode clGetDeviceIDsFromD3D10KHRDelegate(
            IntPtr platform,
            uint d3d_device_source,
            IntPtr d3d_object,
            uint d3d_device_set,
            uint num_entries,
            [In][Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)]IntPtr[] devices,
            uint* num_devices);
        internal unsafe delegate IntPtr clCreateFromD3D10BufferKHRDelegate(IntPtr context, ulong flags, IntPtr resource, out ErrorCode errcode_ret);
        internal unsafe delegate IntPtr clCreateFromD3D10Texture2DKHRDelegate(IntPtr context, ulong flags, IntPtr resource, uint subresource, out ErrorCode errcode_ret);
        internal unsafe delegate IntPtr clCreateFromD3D10Texture3DKHRDelegate(IntPtr context, ulong flags, IntPtr resource, uint subresource, out ErrorCode errcode_ret);
        internal unsafe delegate ErrorCode clEnqueueAcquireD3D10ObjectsKHRDelegate(
            IntPtr command_queue,
            uint num_objects,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] mem_objects,
            uint num_events_in_wait_list,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] IntPtr[] event_wait_list,
            IntPtr* _event);
        internal unsafe delegate ErrorCode clEnqueueReleaseD3D10ObjectsKHRDelegate(
            IntPtr command_queue,
            uint num_objects,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] IntPtr[] mem_objects,
            uint num_events_in_wait_list,
            [In][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] IntPtr[] event_wait_list,
            IntPtr* _event);

        #endregion

        #region Constructors

        internal DirectX10Extension(Platform platform)
            : base(platform)
        {
            IntPtr entryPoint;

            if (!platform.HasExtension(ExtensionName))
                return;

            if (!platform.VersionCheck(1, 2))
                return;

            entryPoint = ImportFunction("clGetDeviceIDsFromD3D10KHR");
            clGetDeviceIDsFromD3D10KHR = (clGetDeviceIDsFromD3D10KHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clGetDeviceIDsFromD3D10KHRDelegate));

            entryPoint = ImportFunction("clCreateFromD3D10BufferKHR");
            clCreateFromD3D10BufferKHR = (clCreateFromD3D10BufferKHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clCreateFromD3D10BufferKHRDelegate));

            entryPoint = ImportFunction("clCreateFromD3D10Texture2DKHR");
            clCreateFromD3D10Texture2DKHR = (clCreateFromD3D10Texture2DKHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clCreateFromD3D10Texture2DKHRDelegate));

            entryPoint = ImportFunction("clCreateFromD3D10Texture3DKHR");
            clCreateFromD3D10Texture3DKHR = (clCreateFromD3D10Texture3DKHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clCreateFromD3D10Texture3DKHRDelegate));

            entryPoint = ImportFunction("clEnqueueAcquireD3D10ObjectsKHR");
            clEnqueueAcquireD3D10ObjectsKHR = (clEnqueueAcquireD3D10ObjectsKHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clEnqueueAcquireD3D10ObjectsKHRDelegate));

            entryPoint = ImportFunction("clEnqueueReleaseD3D10ObjectsKHR");
            clEnqueueReleaseD3D10ObjectsKHR = (clEnqueueReleaseD3D10ObjectsKHRDelegate)Marshal.GetDelegateForFunctionPointer(entryPoint, typeof(clEnqueueReleaseD3D10ObjectsKHRDelegate));

            IsAvailable = true;
        }

        #endregion

        #region GetDeviceIDsFromD3D10

        public Device[] GetDeviceIDsFromD3D10(Platform platform, D3D10DeviceSource d3d_device_source, IntPtr d3d_object, D3D10DeviceSet d3d_device_set)
        {
            ErrorCode status;
            uint numDevices;

            status = GetDeviceIDsFromD3D10(platform, d3d_device_source, d3d_object, d3d_device_set, 0, null, &numDevices);
            if (status != ErrorCode.SUCCESS)
                return null;

            IntPtr[] deviceArray = new IntPtr[numDevices];
            status = GetDeviceIDsFromD3D10(platform, d3d_device_source, d3d_object, d3d_device_set, numDevices, deviceArray, &numDevices);
            if (status != ErrorCode.SUCCESS)
                return null;

            return InteropTools.ConvertDeviceIDsToDevices(platform, deviceArray);
        }

        public virtual ErrorCode GetDeviceIDsFromD3D10(Platform platform, D3D10DeviceSource d3d_device_source, IntPtr d3d_object, D3D10DeviceSet d3d_device_set, uint num_entries, IntPtr[] devices, uint* num_devices)
        {
            ErrorCode status;

            status = clGetDeviceIDsFromD3D10KHR(platform, (uint)d3d_device_source, d3d_object, (uint)d3d_device_set, num_entries, devices, num_devices);
            return status;
        }

        #endregion

        #region CreateFromD3D10Buffer

        public Mem CreateFromD3D10Buffer(Context context, ulong flags, IntPtr resource, out ErrorCode status)
        {
            IntPtr memID;

            memID = clCreateFromD3D10BufferKHR(context, flags, resource, out status);
            if (status != ErrorCode.SUCCESS)
                throw new OpenCLException("CreateFromD3D10Buffer: Failed with ErrorCode=" + status);
            return new Mem(context, memID);
        }

        #endregion

        #region CreateFromD3D10Texture2D

        public Mem CreateFromD3D10Texture2D(Context context, ulong flags, IntPtr resource, uint subresource)
        {
            ErrorCode status;
            IntPtr memID;

            memID = clCreateFromD3D10Texture2DKHR(context, flags, resource, subresource, out status);
            if (status != ErrorCode.SUCCESS)
                throw new OpenCLException("clCreateFromD3D10Texture2DKHR: Failed with ErrorCode=" + status);
            return new Mem(context, memID);
        }

        #endregion

        #region CreateFromD3D10Texture3D

        public Mem CreateFromD3D10Texture3D(Context context, ulong flags, IntPtr resource, uint subresource)
        {
            IntPtr memID;
            ErrorCode status;

            memID = clCreateFromD3D10Texture3DKHR(context, flags, resource, subresource, out status);
            if (status != ErrorCode.SUCCESS)
                throw new OpenCLException("CreateFromD3D10Texture3D: Failed with ErrorCode=" + status);
            return new Mem(context, memID);
        }

        #endregion

        #region EnqueueAcquireD3D10Objects

        public ErrorCode EnqueueAcquireD3D10Objects(CommandQueue command_queue, IList<Mem> objects)
        {
            return EnqueueAcquireD3D10Objects(command_queue, objects, null, null);
        }

        public ErrorCode EnqueueAcquireD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list)
        {
            return EnqueueAcquireD3D10Objects(command_queue, objects, event_wait_list, null);
        }

        public ErrorCode EnqueueAcquireD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list, out Event _event)
        {
            ErrorCode status;
            IntPtr tmpEvent;

            status = EnqueueAcquireD3D10Objects(command_queue, objects, event_wait_list, &tmpEvent);
            _event = new Event(command_queue.Context, command_queue, tmpEvent);
            return status;
        }

        public virtual ErrorCode EnqueueAcquireD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list, IntPtr* _event)
        {
            ErrorCode status;
            IntPtr[] repackedObjects;
            IntPtr[] repackedEvents;
            IntPtr tmpEvent;

            if (objects == null)
                throw new ArgumentNullException("EnqueueAcquireD3D10Objects: object list was null");

            repackedObjects = RepackMems(objects);
            repackedEvents = RepackEvents(event_wait_list);

            status = clEnqueueAcquireD3D10ObjectsKHR(command_queue, (uint)objects.Count, repackedObjects, (uint)event_wait_list.Count, repackedEvents, &tmpEvent);
            if (status != ErrorCode.SUCCESS)
                throw new OpenCLException("EnqueueAcquireD3D10Objects: Failed with ErrorCode=" + status);

            *_event = tmpEvent;
            return status;
        }

        #endregion

        #region EnqueueReleaseD3D10Objects

        public ErrorCode EnqueueReleaseD3D10Objects(CommandQueue command_queue, IList<Mem> objects)
        {
            return EnqueueReleaseD3D10Objects(command_queue, objects, null, null);
        }

        public ErrorCode EnqueueReleaseD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list)
        {
            return EnqueueReleaseD3D10Objects(command_queue, objects, event_wait_list, null);
        }

        public ErrorCode EnqueueReleaseD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list, out Event _event)
        {
            ErrorCode status;
            IntPtr tmpEvent;

            status = EnqueueAcquireD3D10Objects(command_queue, objects, event_wait_list, &tmpEvent);
            _event = new Event(command_queue.Context, command_queue, tmpEvent);
            return status;
        }

        public virtual ErrorCode EnqueueReleaseD3D10Objects(CommandQueue command_queue, IList<Mem> objects, IList<Event> event_wait_list, IntPtr* _event)
        {
            ErrorCode status;
            IntPtr[] repackedObjects;
            IntPtr[] repackedEvents;
            IntPtr tmpEvent;

            if (objects == null)
                throw new ArgumentNullException("EnqueueReleaseD3D10Objects: object list was null");

            repackedObjects = RepackMems(objects);
            repackedEvents = RepackEvents(event_wait_list);

            status = clEnqueueReleaseD3D10ObjectsKHR(command_queue, (uint)objects.Count, repackedObjects, (uint)event_wait_list.Count, repackedEvents, &tmpEvent);
            if (status != ErrorCode.SUCCESS)
                throw new OpenCLException("EnqueueReleaseD3D10Objects: Failed with ErrorCode=" + status);

            *_event = tmpEvent;
            return status;
        }

        #endregion

        internal override string GetName()
        {
            return ExtensionName;
        }
    }
}
