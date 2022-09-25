/*
 * Copyright (c) 2009 Olav Kalgraf(olav.kalgraf@gmail.com)
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Security;

namespace OpenCLNet
{
    #region Using statements

    using cl_platform_id = IntPtr;
    using cl_device_id=IntPtr;
    using cl_context=IntPtr;
    using cl_command_queue=IntPtr;
    using cl_mem=IntPtr;
    using cl_program=IntPtr;
    using cl_kernel=IntPtr;
    using cl_event=IntPtr;
    using cl_sampler=IntPtr;
    using cl_device_partition_property = IntPtr;
    using cl_context_properties=IntPtr;
    using ID3D10Buffer = IntPtr;
    using ID3D10Texture2D = IntPtr;
    using ID3D10Texture3D = IntPtr;

    #endregion

    // void (CL_CALLBACK * pfn_notify)(cl_context program, uint printf_data_len, char * printf_data_ptr, void * user_data)
    public unsafe delegate void PrintFCallbackDelegate(cl_context contect, uint printf_data_len, [MarshalAs(UnmanagedType.LPStr,SizeParamIndex=1)]string printf_data_ptr, void* user_data);

    /// <summary>
    /// OpenCLAPI - native bindings
    /// </summary>
    [SuppressUnmanagedCodeSecurity()]
    unsafe public static class OpenCLAPI
    {
        internal static class Configuration
        {
            public const string Library = "opencl.dll";
        }
    
        static OpenCLAPI()
        {
        }

        #region Platform API

        // Platform API
        [DllImport(Configuration.Library)]
        public extern static int clGetPlatformIDs(uint num_entries, [Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] cl_platform_id[] platforms, out uint num_platforms);
        [DllImport(Configuration.Library)]
        public extern static int clGetPlatformIDs(uint num_entries, IntPtr* pPlatforms, out uint num_platforms);

        [DllImport(Configuration.Library)]
        public extern static int clGetPlatformInfo(cl_platform_id platform, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        #endregion

        #region Device API

        // Device APIs
        [DllImport(Configuration.Library)]
        public extern static int clGetDeviceIDs(cl_platform_id platform, DeviceType device_type, uint num_entries, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)]cl_device_id[] devices, out uint num_devices);
        [DllImport(Configuration.Library)]
        public extern static int clGetDeviceIDs(IntPtr platform, ulong device_type, uint num_entries, IntPtr* pDevices, out uint num_devices);

        [DllImport(Configuration.Library)]
        public extern static int clGetDeviceInfo(cl_device_id device, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="device"></param>
        /// <param name="properties"></param>
        /// <param name="num_devices"></param>
        /// <param name="out_devices"></param>
        /// <param name="num_devices_ret"></param>
        /// <returns></returns>
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clCreateSubDevices(cl_device_id in_device,
            [In] byte[] properties,
            uint num_entries,
            [In] [Out] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] cl_device_id[] out_devices,
            out uint num_devices);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clCreateSubDevices(cl_device_id in_device,
            byte* pProperties,
            uint num_entries,
            IntPtr* pOutDevices,
            out uint num_devices);

        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainDevice(cl_device_id device);

        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseDevice(cl_device_id device);
    
        #endregion

        #region Context API

        // Context APIs 
        [DllImport(Configuration.Library)]
        public extern static cl_context clCreateContext([In] cl_context_properties[] properties, uint num_devices, [In]cl_device_id[] devices, ContextNotify pfn_notify, IntPtr user_data, [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_context clCreateContext(IntPtr* pProperties, uint num_devices, IntPtr* pDevices, ContextNotify pfn_notify, IntPtr user_data, [MarshalAs(UnmanagedType.I4)]out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_context clCreateContextFromType([In] cl_context_properties[] properties, DeviceType device_type, ContextNotify pfn_notify, IntPtr user_data, [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_context clCreateContextFromType(IntPtr* properties, DeviceType device_type, ContextNotify pfn_notify, IntPtr user_data, [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static int clRetainContext(cl_context context);
        [DllImport(Configuration.Library)]
        public extern static int clReleaseContext(cl_context context);
        [DllImport(Configuration.Library)]
        public extern static int clGetContextInfo(cl_context context, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        #endregion

        #region Command Queue API

        // Command Queue APIs
        [DllImport(Configuration.Library)]
        public extern static IntPtr clCreateCommandQueue(cl_context context, cl_device_id device, ulong properties, [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainCommandQueue(cl_command_queue command_queue);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseCommandQueue(cl_command_queue command_queue);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetCommandQueueInfo(cl_command_queue command_queue, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);
        [DllImport(Configuration.Library)]
        [Obsolete("Function deprecated in OpenCL 1.1 due to being inherently unsafe", false)]
        public extern static ErrorCode clSetCommandQueueProperty(cl_command_queue command_queue, ulong properties, [MarshalAs(UnmanagedType.I4)]bool enable, out ulong old_properties);

        #endregion

        #region Memory Object API

        // Memory Object APIs
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateBuffer(cl_context context, ulong flags, IntPtr size, void* host_ptr, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. See clCreateImage",false)]
        public extern static cl_mem clCreateImage2D(cl_context context, ulong flags, CLImageFormat* image_format, IntPtr image_width, IntPtr image_height, IntPtr image_row_pitch, void* host_ptr, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. See clCreateImage",false)]
        public extern static cl_mem clCreateImage3D(cl_context context, ulong flags, CLImageFormat* image_format, IntPtr image_width, IntPtr image_height, IntPtr image_depth, IntPtr image_row_pitch, IntPtr image_slice_pitch, void* host_ptr, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainMemObject(cl_mem memobj);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseMemObject(cl_mem memobj);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetSupportedImageFormats(cl_context context,
            ulong flags,
            uint image_type,
            uint num_entries,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)] CLImageFormat[] image_formats,
            out uint num_image_formats);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetMemObjectInfo(cl_mem memobj, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetImageInfo(cl_mem image, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        // OpenCL 1.1
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateSubBuffer(cl_mem buffer, ulong flags, BufferCreateType buffer_create_type, void* buffer_create_info, out ErrorCode errcode_ret );
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clSetMemObjectDestructorCallback(cl_mem memobj, void* pfn_notify, void* user_data);
        
        // OpenCL 1.2
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateSubBuffer(cl_mem buffer, ulong flags, uint buffer_create_type, void* buffer_create_info, int* errcode_ret);
        #endregion

        #region Sampler API

        // Sampler APIs
        [DllImport(Configuration.Library)]
        public extern static cl_sampler clCreateSampler(cl_context context, uint normalized_coords, uint addressing_mode, uint filter_mode, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainSampler(cl_sampler sampler);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseSampler(cl_sampler sampler);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetSamplerInfo(cl_sampler sampler, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        #endregion

        #region Program Object API

        // Program Object APIs
        [DllImport(Configuration.Library)]
        public extern static cl_program clCreateProgramWithSource(cl_context context,
            uint count,
            [In] string[] strings,
            [In] IntPtr[] lengths,
            [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_program clCreateProgramWithBinary(cl_context context,
            uint num_devices,
            [In] cl_device_id[] device_list,
            [In] IntPtr[] lengths,
            [In] IntPtr[] pBinaries,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] int[] binary_status,
            [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static int clRetainProgram(cl_program program);
        [DllImport(Configuration.Library)]
        public extern static int clReleaseProgram(cl_program program);
        [DllImport(Configuration.Library)]
        public extern static int clBuildProgram(cl_program program,
            uint num_devices,
            [In] cl_device_id[] device_list,
            string options,
            ProgramNotify pfn_notify,
            IntPtr user_data);
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. Use clUnloadPlatformCompiler.")]
        public extern static int clUnloadCompiler();
        [DllImport(Configuration.Library)]
        public extern static int clGetProgramInfo(cl_program program, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);
        [DllImport(Configuration.Library)]
        public extern static int clGetProgramBuildInfo(cl_program program, cl_device_id device, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        // OpenCL 1.2
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="context"></param>
        /// <param name="num_devices"></param>
        /// <param name="device_list"></param>
        /// <param name="kernel_names"></param>
        /// <param name="errcode_ret"></param>
        /// <returns></returns>
        [DllImport(Configuration.Library)]
        public extern static cl_program clCreateProgramWithBuiltInKernels(
            cl_context context,
            uint num_devices,
            cl_device_id* device_list,
            char* kernel_names,
            [MarshalAs(UnmanagedType.I4)] ErrorCode* errcode_ret );
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clCompileProgram(
            cl_program program,
            uint num_devices,
            [In] cl_device_id[] device_list,
            string options,
            int num_input_headers,
            [In] cl_program[] input_headers,
            [In] string[] header_include_names,
            ProgramNotify pfn_notify,
            IntPtr user_data );
        [DllImport(Configuration.Library)]
        public extern static cl_program clLinkProgram(
            cl_context context,
            uint num_devices,
            cl_device_id[] device_list,
            string options,
            uint num_input_programs,
            cl_program[] input_programs,
            ProgramNotify pfn_notify,
            IntPtr user_data,
            [MarshalAs(UnmanagedType.I4)] out ErrorCode errcode_ret );
        
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        [DllImport(Configuration.Library)]
        public extern static int clUnloadPlatformCompiler(cl_platform_id platform);

        #endregion

        #region Kernel Object API

        // Kernel Object APIs
        [DllImport(Configuration.Library)]
        public extern static cl_kernel clCreateKernel(cl_program program, string kernel_name, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clCreateKernelsInProgram(cl_program program,
            uint num_kernels,
            [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]cl_kernel[] kernels,
            out uint num_kernels_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainKernel(cl_kernel kernel);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseKernel(cl_kernel kernel);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clSetKernelArg(cl_kernel kernel, uint arg_index, IntPtr arg_size, void* arg_value);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetKernelInfo(cl_kernel kernel, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetKernelWorkGroupInfo(cl_kernel kernel, cl_device_id device, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);

        // OpenCL 1.2
        [DllImport(Configuration.Library)]
        public extern static int clGetKernelArgInfo(
            cl_kernel kernel,
            uint arg_indx,
            uint param_name,
            IntPtr param_value_size,
            void* param_value,
            IntPtr* param_value_size_ret);
        #endregion

        #region Enqueued Commands API

        // Enqueued Commands APIs
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_read,
            IntPtr offset,
            IntPtr cb,
            void* ptr,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_read,
            IntPtr offset,
            IntPtr cb,
            void* ptr,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);


        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_write,
            IntPtr offset,
            IntPtr cb,
            void* ptr,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_write,
            IntPtr offset,
            IntPtr cb,
            void* ptr,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBuffer(cl_command_queue command_queue,
            cl_mem src_buffer,
            cl_mem dst_buffer,
            IntPtr src_offset,
            IntPtr dst_offset,
            IntPtr cb,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBuffer(cl_command_queue command_queue,
            cl_mem src_buffer,
            cl_mem dst_buffer,
            IntPtr src_offset,
            IntPtr dst_offset,
            IntPtr cb,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_read,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            IntPtr row_pitch,
            IntPtr slice_pitch,
            void* ptr,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_read,
            IntPtr* origin,
            IntPtr* region,
            IntPtr row_pitch,
            IntPtr slice_pitch,
            void* ptr,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_write,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            IntPtr input_row_pitch,
            IntPtr input_slice_pitch,
            void* ptr,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_write,
            IntPtr* origin,
            IntPtr* region,
            IntPtr input_row_pitch,
            IntPtr input_slice_pitch,
            void* ptr,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyImage(cl_command_queue command_queue,
            cl_mem src_image,
            cl_mem dst_image,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] src_origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] dst_origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyImage(cl_command_queue command_queue,
            cl_mem src_image,
            cl_mem dst_image,
            IntPtr* src_origin,
            IntPtr* dst_origin,
            IntPtr* region,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);


        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyImageToBuffer(cl_command_queue command_queue,
            cl_mem src_image,
            cl_mem dst_buffer,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] src_origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            IntPtr dst_offset,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyImageToBuffer(cl_command_queue command_queue,
            cl_mem src_image,
            cl_mem dst_buffer,
            IntPtr* src_origin,
            IntPtr* region,
            IntPtr dst_offset,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBufferToImage(cl_command_queue command_queue,
            cl_mem src_buffer,
            cl_mem dst_image,
            IntPtr src_offset,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] dst_origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBufferToImage(cl_command_queue command_queue,
            cl_mem src_buffer,
            cl_mem dst_image,
            IntPtr src_offset,
            IntPtr* dst_origin,
            IntPtr* region,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);


        [DllImport(Configuration.Library)]
        public extern static void* clEnqueueMapBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_map,
            ulong map_flags,
            IntPtr offset,
            IntPtr cb,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static void* clEnqueueMapBuffer(cl_command_queue command_queue,
            cl_mem buffer,
            uint blocking_map,
            ulong map_flags,
            IntPtr offset,
            IntPtr cb,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event,
            out ErrorCode errcode_ret);

        [DllImport(Configuration.Library)]
        public extern static void* clEnqueueMapImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_map,
            ulong map_flags,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] origin,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeConst = 3)]IntPtr[] region,
            out IntPtr image_row_pitch,
            out IntPtr image_slice_pitch,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static void* clEnqueueMapImage(cl_command_queue command_queue,
            cl_mem image,
            uint blocking_map,
            ulong map_flags,
            IntPtr* origin,
            IntPtr* region,
            out IntPtr image_row_pitch,
            out IntPtr image_slice_pitch,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event,
            out ErrorCode errcode_ret);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueUnmapMemObject(cl_command_queue command_queue,
            cl_mem memobj,
            void* mapped_ptr,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueUnmapMemObject(cl_command_queue command_queue,
            cl_mem memobj,
            void* mapped_ptr,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueNDRangeKernel(cl_command_queue command_queue,
            cl_kernel kernel,
            uint work_dim,
            [In] [MarshalAs(UnmanagedType.LPArray)] IntPtr[] global_work_offset,
            [In] [MarshalAs(UnmanagedType.LPArray)] IntPtr[] global_work_size,
            [In] [MarshalAs(UnmanagedType.LPArray)] IntPtr[] local_work_size,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueNDRangeKernel(cl_command_queue command_queue,
            cl_kernel kernel,
            uint work_dim,
            IntPtr* global_work_offset,
            IntPtr* global_work_size,
            IntPtr* local_work_size,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueTask(cl_command_queue command_queue,
            cl_kernel kernel,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueTask(cl_command_queue command_queue,
            cl_kernel kernel,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueNativeKernel(cl_command_queue command_queue,
            NativeKernelInternal user_func,
            void* args,
            IntPtr cb_args,
            uint num_mem_objects,
            [In] cl_mem[] mem_list,
            [In] [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] IntPtr[] args_mem_loc,
            uint num_events_in_wait_list,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueNativeKernel(cl_command_queue command_queue,
            NativeKernelInternal user_func,
            void* args,
            IntPtr cb_args,
            uint num_mem_objects,
            IntPtr* mem_list,
            IntPtr* args_mem_loc,
            uint num_events_in_wait_list,
            IntPtr* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. Use clEnqueueMarkerWithWaitList.",false)]
        public extern static ErrorCode clEnqueueMarker(cl_command_queue command_queue, cl_event* _event);

        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. Use clEnqueueMarkerWithWaitList.", false)]
        public extern static ErrorCode clEnqueueWaitForEvents(cl_command_queue command_queue,
            uint num_events,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] _event_list);
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2 Use clEnqueueMarkerWithWaitList.", false)]
        public extern static ErrorCode clEnqueueWaitForEvents(cl_command_queue command_queue,
            uint num_events,
            IntPtr* _event_list);
        
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. Use clEnqueueBarrierWithWaitList.", false)]
        public extern static ErrorCode clEnqueueBarrier(cl_command_queue command_queue);

        // OpenCL 1.1
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadBufferRect(cl_command_queue command_queue,
                                cl_mem buffer,
                                uint blocking_read,
                                [In] IntPtr[] buffer_offset,
                                [In] IntPtr[] host_offset,
                                [In] IntPtr[] region,
                                IntPtr buffer_row_pitch,
                                IntPtr buffer_slice_pitch,
                                IntPtr host_row_pitch,
                                IntPtr host_slice_pitch,
                                void* ptr,
                                uint num_events_in_wait_list,
                                [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list,
                                cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReadBufferRect(cl_command_queue command_queue,
                                cl_mem buffer,
                                uint blocking_read,
                                IntPtr* buffer_offset,
                                IntPtr* host_offset,
                                IntPtr* region,
                                IntPtr buffer_row_pitch,
                                IntPtr buffer_slice_pitch,
                                IntPtr host_row_pitch,
                                IntPtr host_slice_pitch,
                                void* ptr,
                                uint num_events_in_wait_list,
                                cl_event* event_wait_list,
                                cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteBufferRect(cl_command_queue command_queue,
                                 cl_mem buffer,
                                 uint blocking_write,
                                 [In] IntPtr[] buffer_offset,
                                 [In] IntPtr[] host_offset,
                                 [In] IntPtr[] region,
                                 IntPtr buffer_row_pitch,
                                 IntPtr buffer_slice_pitch,
                                 IntPtr host_row_pitch,
                                 IntPtr host_slice_pitch,
                                 void* ptr,
                                 uint num_events_in_wait_list,
                                 [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] _event_wait_list,
                                 cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueWriteBufferRect(cl_command_queue command_queue,
                                 cl_mem buffer,
                                 uint blocking_write,
                                 IntPtr* buffer_offset,
                                 IntPtr* host_offset,
                                 IntPtr* region,
                                 IntPtr buffer_row_pitch,
                                 IntPtr buffer_slice_pitch,
                                 IntPtr host_row_pitch,
                                 IntPtr host_slice_pitch,
                                 void* ptr,
                                 uint num_events_in_wait_list,
                                 cl_event* _event_wait_list,
                                 cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBufferRect(cl_command_queue command_queue,
                                cl_mem src_buffer,
                                cl_mem dst_buffer,
                                [In] IntPtr[] src_origin,
                                [In] IntPtr[] dst_origin,
                                [In] IntPtr[] region,
                                IntPtr src_row_pitch,
                                IntPtr src_slice_pitch,
                                IntPtr dst_row_pitch,
                                IntPtr dst_slice_pitch,
                                uint num_events_in_wait_list,
                                [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] _event_wait_list,
                                cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueCopyBufferRect(cl_command_queue command_queue,
                                cl_mem src_buffer,
                                cl_mem dst_buffer,
                                IntPtr* src_origin,
                                IntPtr* dst_origin,
                                IntPtr* region,
                                IntPtr src_row_pitch,
                                IntPtr src_slice_pitch,
                                IntPtr dst_row_pitch,
                                IntPtr dst_slice_pitch,
                                uint num_events_in_wait_list,
                                cl_event* _event_wait_list,
                                cl_event* _event);

        // OpenCL 1.2

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueFillBuffer(
            cl_command_queue command_queue,
            cl_mem buffer,
            void*  pattern,
            IntPtr pattern_size,
            IntPtr offset,
            IntPtr size,
            uint num_events_in_wait_list,
            cl_event* event_wait_list,
            cl_event* _event );

        [DllImport(Configuration.Library)]
        public extern static int clEnqueueFillImage(
            cl_command_queue command_queue,
            cl_mem image,
            void * fill_color,
            IntPtr* origin,
            IntPtr* region,
            uint num_events_in_wait_list,
            cl_event* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueMigrateMemObjects(
            cl_command_queue command_queue,
            uint num_mem_objects,
            cl_mem * mem_objects,
            ulong flags,
            uint num_events_in_wait_list,
            cl_event* event_wait_list,
            cl_event* _event ) ;

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueMarkerWithWaitList(
            cl_command_queue command_queue,
            uint num_events_in_wait_list,
            cl_event* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueBarrierWithWaitList(
            cl_command_queue command_queue,
            uint num_events_in_wait_list,
            cl_event* event_wait_list,
            cl_event* _event);

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clSetPrintfCallback(
            cl_context context,
            PrintFCallbackDelegate pfn_notify,
            void * user_data);

        #endregion

        #region Flush and Finish API

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clFlush(cl_command_queue command_queue);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clFinish(cl_command_queue command_queue);

        #endregion

        #region Event Object API

        [DllImport(Configuration.Library)]
        public extern static ErrorCode clWaitForEvents(uint num_events,
            [In] [MarshalAs(UnmanagedType.LPArray)] cl_event[] event_wait_list);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clWaitForEvents(uint num_events,
            IntPtr* event_wait_list);
        
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetEventInfo(cl_event _event,
            uint param_name,
            IntPtr param_value_size,
            void* param_value,
            out IntPtr param_value_size_ret);
        
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clRetainEvent(cl_event _event);
        
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clReleaseEvent(cl_event _event);

        // OpenCL 1.1
        [DllImport(Configuration.Library)]
        public extern static cl_event clCreateUserEvent(cl_context context, out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clSetUserEventStatus(cl_event _event, ExecutionStatus execution_status);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clSetEventCallback(cl_event _event, int command_exec_callback_type, EventNotifyInternal pfn_notify, IntPtr user_data);
        #endregion

        #region GLObject API

        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateFromGLBuffer(cl_context context,
            ulong flags,
            uint bufobj,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateFromGLTexture2D(cl_context context,
            ulong flags,
            int target,
            int mipLevel,
            uint texture,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateFromGLTexture3D(cl_context context,
            ulong flags,
            int target,
            int mipLevel,
            uint texture,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static cl_mem clCreateFromGLRenderbuffer(cl_context context,
            ulong flags,
            uint renderBuffer,
            out ErrorCode errcode_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetGLObjectInfo(cl_mem memobj,
            out uint gl_object_type,
            out uint gl_object_name);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clGetGLTextureInfo(cl_mem memobj,
            uint param_name,
            IntPtr param_value_size,
            void* param_value,
            out IntPtr param_value_size_ret);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueAcquireGLObjects(cl_command_queue command_queue,
            uint num_objects,
            [In] cl_mem[] mem_objects,
            uint num_events_in_wait_list,
            [In] cl_event[] event_wait_list,
            cl_event* _event);
        [DllImport(Configuration.Library)]
        public extern static ErrorCode clEnqueueReleaseGLObjects(cl_command_queue command_queue,
            uint num_objects,
            [In] cl_mem[] mem_objects,
            uint num_events_in_wait_list,
            [In] cl_event[] event_wait_list,
            cl_event* _event);

        #endregion

        // Extension function access
        [DllImport(Configuration.Library)]
        [Obsolete("Deprecated in OpenCL 1.2. Use clGetExtensionFunctionAddressForPlatform", false)]
        public extern static IntPtr clGetExtensionFunctionAddress(string func_name);

        [DllImport(Configuration.Library)]
        public extern static IntPtr clGetExtensionFunctionAddressForPlatform(cl_platform_id platform, string func_name);

        [DllImport(Configuration.Library)]
        public extern static int clGetEventProfilingInfo(cl_event _event, uint param_name, IntPtr param_value_size, void* param_value, out IntPtr param_value_size_ret);
    }
}
