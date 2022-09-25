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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using OpenCLNet;

namespace UnitTests
{
    public unsafe partial class Form1 : Form
    {
        OpenCLManager OpenCLManager = new OpenCLManager();
        Platform[] Platforms;
        Regex ParseOpenCLVersion = new Regex(@"OpenCL (?<MajorVersion>\d+)\.(?<MinorVersion>\d+).*");

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                listBoxErrors.Items.Clear();
                listBoxWarnings.Items.Clear();
                listBoxOutput.Items.Clear();
                RunTests();
                Output("Unit testing complete");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Test terminated with a fatal exception.");
            }
        }

        private void RunTests()
        {
            TestOpenCLClass();
            TestOpenCLManager();
        }

        private void TestOpenCLManager()
        {
            Output("======================================================");
            Output("Testing OpenCLManager");
            Output("======================================================");

            OpenCLManager.CreateDefaultContext(0,DeviceType.DEFAULT);
            OpenCLManager.BuildOptions = "";
            OpenCLManager.Defines = "";
            OpenCLNet.CLProgram p0 = OpenCLManager.CompileFile("MemoryTests.cl");

            OpenCLManager.BuildOptions = "-D TestDefinition=1";
            OpenCLManager.Defines = "/*Woo defines!*/";
            OpenCLNet.CLProgram p1 = OpenCLManager.CompileFile("MemoryTests.cl");

            OpenCLManager.BuildOptions = "";
            OpenCLManager.Defines = "";
            OpenCLNet.CLProgram p2 = OpenCLManager.CompileSource("kernel void NullFunction(){}");

            OpenCLManager.BuildOptions = "-D TESTDEFINE=1";
            OpenCLManager.Defines = "";
            OpenCLNet.CLProgram p3 = OpenCLManager.CompileSource("kernel void NullFunction(){ int a=TESTDEFINE; if( a<3 ) ; }");

            OpenCLManager.BuildOptions = "";
            OpenCLManager.Defines = "#define TESTDEFINE 1";
            OpenCLNet.CLProgram p4 = OpenCLManager.CompileSource("kernel void NullFunction(){ int a=TESTDEFINE; if( a<3 ) ; }");

            Dictionary<string, Kernel> kernels0 = p0.CreateKernelDictionary();
            Dictionary<string, Kernel> kernels1 = p1.CreateKernelDictionary();
            Dictionary<string, Kernel> kernels2 = p2.CreateKernelDictionary();
            Dictionary<string, Kernel> kernels3 = p3.CreateKernelDictionary();
            Dictionary<string, Kernel> kernels4 = p4.CreateKernelDictionary();

            if (OpenCLManager.Context.Devices[0].HasExtension("cl_ext_device_fission"))
            {
                Output("Testing CreateSubDevices");
                List<object> properties = new List<object>();
                properties.Add((ulong)DevicePartition.EQUALLY);
                properties.Add((int)2);
                properties.Add((ulong)ListTerminators.PROPERTIES_LIST_END);

                Device[] subDevices;
                subDevices = OpenCLManager.Context.Devices[0].CreateSubDevices(properties);
                foreach (Device d in subDevices)
                    d.Dispose();
            }
            else
            {
                Output("Skipping test of CreateSubDevices: cl_ext_device_fission not supported");
            }
            Output("");
            Output("");
        }

        private void TestOpenCLClass()
        {
            if (OpenCL.NumberOfPlatforms <= 0)
            {
                listBoxOutput.Items.Add("OpenCL.NumberOfPlatforms=" + OpenCL.NumberOfPlatforms);
                throw new Exception("TestOpenCLClass: NumberOfPlatforms<0. Is the API available at all?");
            }

            Platforms = OpenCL.GetPlatforms();
            if (Platforms.Length != OpenCL.NumberOfPlatforms)
                Error("OpenCL.NumberOfPlatforms!=Length of openCL.GetPlatforms()" + OpenCL.NumberOfPlatforms);

            for (int platformIndex = 0; platformIndex < Platforms.Length; platformIndex++)
            {
                if (OpenCL.GetPlatform(platformIndex) != Platforms[platformIndex])
                    Error("openCL.GetPlatform(platformIndex)!=Platforms[platformIndex]");

                Output("======================================================");
                Output("Testing Platform Index=" + platformIndex + " Name=" + Platforms[platformIndex].Name);
                Output("======================================================");
                TestPlatform(Platforms[platformIndex]);
                Output("");
                Output("");
            }
        }

        private void TestPlatform(Platform p)
        {
            Device[] allDevices;
            Device[] cpuDevices;
            Device[] gpuDevices;
            Device[] acceleratorDevices;

            Output("Name: " + p.Name);
            Output("Vendor:" + p.Vendor);
            Output("Version:" + p.Version);

            // Check format of version string
            Match m = ParseOpenCLVersion.Match(p.Version);
            if (!m.Success)
                Warning("Platform " + p.Name + " has an invalid version string");
            else
            {
                if (m.Groups["MajorVersion"].Value != "1" && m.Groups["MinorVersion"].Value != "0")
                    Warning("Platform " + p.Name + " has a version number!=1.0(Not really a problem, but this test is written for 1.0)");
            }

            // Check format of profile
            Output("Profile:" + p.Profile);
            if (p.Profile == "FULL_PROFILE" || p.Profile == "EMBEDDED_PROFILE")
                Output("Profile:" + p.Profile);
            else
                Warning("Platform " + p.Name + " has unknown profile "+p.Profile);
            
            Output("Extensions: " + p.Extensions);

            // Test whether number of devices is consistent
            allDevices = p.QueryDevices(DeviceType.ALL);
            if( allDevices.Length<=0 )
                Warning( "Platform "+p.Name+" has no devices" );

            StringBuilder sb = new StringBuilder();
            foreach (Device d in allDevices)
                sb.Append("\""+d.Name+"\" ");
            Output("Devices: "+sb.ToString());

            cpuDevices = p.QueryDevices(DeviceType.CPU);
            gpuDevices = p.QueryDevices(DeviceType.GPU);
            acceleratorDevices = p.QueryDevices(DeviceType.ACCELERATOR);
            if( allDevices.Length!=cpuDevices.Length+gpuDevices.Length+acceleratorDevices.Length )
                Warning( "QueryDevices( DeviceType.ALL ) return length inconsistent with sum of special purpose queries" );

            // Create a few contexts and test them
            Output( "Testing Platform.CreateDefaultContext()" );
            using (Context c = p.CreateDefaultContext())
            {
                Output("Testing context"+c);
                TestContext(c);
            }
            Output("");
            Output("");

            if (cpuDevices.Length > 0)
            {
                Output("Testing Platform.CreateContext() with CPU devices");
                using (Context c = p.CreateContext(null, cpuDevices, new ContextNotify(ContextNotifyFunc), (IntPtr)0x01234567))
                {
                    Output("Testing context " + c);
                    TestContext(c);
                }
                Output("");
                Output("");
            }

            if (gpuDevices.Length > 0)
            {
                Output("Testing Platform.CreateContext() with GPU devices");
                using (Context c = p.CreateContext(null, gpuDevices, new ContextNotify(ContextNotifyFunc), (IntPtr)0x01234567))
                {
                    //Output("Testing context " + c);
                    TestContext(c);
                }
                Output("");
                Output("");
            }

            if (cpuDevices.Length > 0)
            {
                Output("Testing Platform.CreateContextFromType()");
                IntPtr[] contextProperties = new IntPtr[]
                {
                    (IntPtr)ContextProperties.PLATFORM, p.PlatformID,
                    IntPtr.Zero
                };
                using (Context c = p.CreateContextFromType(contextProperties, DeviceType.CPU, new ContextNotify(ContextNotifyFunc), (IntPtr)0x01234567))
                {
                    //Output("Testing context " + c);
                    TestContext(c);
                }
                Output("");
                Output("");
            }

            if (gpuDevices.Length > 0)
            {
                Output("Testing Platform.CreateContextFromType()");
                IntPtr[] contextProperties = new IntPtr[]
                {
                    (IntPtr)ContextProperties.PLATFORM, p.PlatformID,
                    IntPtr.Zero
                };
                using (Context c = p.CreateContextFromType(contextProperties, DeviceType.GPU, new ContextNotify(ContextNotifyFunc), (IntPtr)0x01234567))
                {
                    //Output("Testing context " + c);
                    TestContext(c);
                    Output("");
                    Output("");
                }
            }
        }

        private void ContextNotifyFunc( string errInfo, byte[] privateInfo, IntPtr cb, IntPtr userData )
        {
            Error(errInfo);
        }

        int NativeKernelCalled = 0;
        NativeKernel NativeKernelCallRef;

        private void TestContext(Context c)
        {
            Device[] devices = c.Devices;
            OpenCLNet.CLProgram p = c.CreateProgramFromFile("OpenCL" + Path.DirectorySeparatorChar + "src" + Path.DirectorySeparatorChar + "MemoryTests.cl");
            Dictionary<string, Kernel> kernelDictionary;

            try
            {
                p.Build();
            }
            catch (OpenCLException ocle)
            {
                throw ocle;
            }
            kernelDictionary = p.CreateKernelDictionary();
            NativeKernelCallRef = new NativeKernel(NativeKernelTest);
            for (int deviceIndex = 0; deviceIndex < devices.Length; deviceIndex++)
            {
                Device d;

                d = devices[deviceIndex];
                using (CommandQueue cq = c.CreateCommandQueue(d))
                {
                    if ( ((ulong)d.ExecutionCapabilities & (ulong)DeviceExecCapabilities.NATIVE_KERNEL)!=0 )
                    {
                        Output("Testing native kernel execution");
                        cq.EnqueueNativeKernel(NativeKernelCallRef, this, null);
                        cq.Finish();
                        if (NativeKernelCalled != 1)
                            Error("EnqueueNativeKernel failed");
                        Interlocked.Decrement(ref NativeKernelCalled);
                    }
                    else
                    {
                        Output("Testing native kernel execution: Not supported");
                    }

                    TestMem(c, cq, kernelDictionary);
                    TestDevice(d);
                    TestCommandQueue(c, cq);
                    TestKernel(c, cq, kernelDictionary["ArgIO"]);
                    TestUserEventCallbacks(c, cq);
                    TestVecKernel(c, cq, kernelDictionary["TestVectorFloat2"]);
                }
            }
        }

        public unsafe void NativeKernelTest(object o, void*[] buffers)
        {
            Interlocked.Increment(ref NativeKernelCalled);
        }

        [StructLayout(LayoutKind.Sequential, Pack=1)]
        internal struct IOKernelArgs
        {
            internal long outLong;
            internal int outInt;
            internal float outSingle;
        }

        private unsafe void TestVecKernel(Context c, CommandQueue cq, Kernel k)
        {
            Float2 f2 = new Float2(0.0f,1.0f);
            float[] memory = new float[2];

            fixed (float* pMemory = memory)
            {
                Mem mem = c.CreateBuffer((MemFlags)((ulong)MemFlags.READ_WRITE | (ulong)MemFlags.USE_HOST_PTR), 4 * 2, pMemory);

                k.SetArg(0, f2);
                k.SetArg(1, mem);
                cq.EnqueueTask(k);
                cq.EnqueueBarrier();
                IntPtr pMap = cq.EnqueueMapBuffer(mem, true, MapFlags.READ, 0, 2 * 4);
                cq.EnqueueUnmapMemObject(mem, pMap);
            }
        }

        private unsafe void TestKernel(Context c, CommandQueue cq, Kernel argIOKernel)
        {
            Mem outArgBuffer = c.CreateBuffer((MemFlags)((ulong)MemFlags.ALLOC_HOST_PTR|(ulong)MemFlags.READ_WRITE), sizeof(IOKernelArgs), IntPtr.Zero);
            byte[] data = new byte[sizeof(IOKernelArgs)];
            Output("Testing kernel - Argument return");

            argIOKernel.SetArg(0, 1);
            argIOKernel.SetArg(1, 65L);
            argIOKernel.SetArg(2, 38.4f);
            argIOKernel.SetArg(3, outArgBuffer);

            Event ev;
            cq.EnqueueTask(argIOKernel,0,null,out ev);
            cq.Finish();

            if ((int)ev.ExecutionStatus < 0)
            {
                Error(cq.Device.Name + ": argIOKernel failed with error code " + (ErrorCode)ev.ExecutionStatus);
                ev.Dispose();
            }
            else
            {
                outArgBuffer.Read(cq, 0L, data, 0, sizeof(IOKernelArgs));
                IntPtr outArgPtr = cq.EnqueueMapBuffer(outArgBuffer, true, MapFlags.READ, IntPtr.Zero, (IntPtr)sizeof(IOKernelArgs));
                IOKernelArgs args = (IOKernelArgs)Marshal.PtrToStructure(outArgPtr, typeof(IOKernelArgs));
                cq.EnqueueUnmapMemObject(outArgBuffer, outArgPtr);

                if (args.outInt != 1)
                    Error(cq.Device.Name + ": argIOKernel failed to return correct arguments");
                if (args.outLong != 65)
                    Error(cq.Device.Name + ": argIOKernel failed to return correct arguments");
                if (args.outSingle != 38.4f)
                    Error(cq.Device.Name + ": argIOKernel failed to return correct arguments");
            }            
        }

        private bool CompareArray(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        #region TestMem

        private void TestMem(Context c, CommandQueue cq, Dictionary<string, Kernel> kernelDictionary)
        {
            int size = 8192;
            byte[] testData = new byte[size];

            Output( "Testing Mem class" );
            //Output( "Allocating "+size+" bytes of READ_WRITE memory" );
            using (Mem buffer = c.CreateBuffer(MemFlags.READ_WRITE, size, IntPtr.Zero))
            {
                for (int i = 0; i < size / 2; i++)
                {
                    testData[i] = 0;
                    testData[size/2+i] = 1;
                }
                //Output("Mem.MemSize=" + size);
                if (buffer.MemSize.ToInt64() != size)
                    Error("Mem.Size!=input size");
                //Output("Mem.MemType=" + buffer.MemType);
                if (buffer.MemType != MemObjectType.BUFFER)
                    Error("Mem.MemType!=MemObjectType.BUFFER");

                //Output("Mem.MapCount=" + buffer.MapCount);
                if (buffer.MapCount != 0)
                    Error("Mem.MapCount!=0");

                buffer.Write(cq, 0L, testData, 0, size);
                Kernel k = kernelDictionary["TestReadWriteMemory"];
                k.SetArg(0, buffer);
                k.SetArg(1, (long)size);
                Event bleh;
                cq.EnqueueTask(k,0,null,out bleh);
                cq.EnqueueBarrier();
                cq.Finish();
                buffer.Read(cq, 0L, testData, 0, size);
                for (int i = 0; i < size / 2; i++)
                {
                    if (testData[i] != 1)
                    {
                        Error("TestReadWriteMemory failed");
                        break;
                    }
                    if( testData[size / 2 + i] != 0 )
                    {
                        Error("TestReadWriteMemory failed");
                        break;
                    }
                }
            }

            //Output("Allocating " + size + " bytes of READ memory");
            using (Mem buffer = c.CreateBuffer(MemFlags.READ_ONLY, size, IntPtr.Zero))
            {
                //Output("Mem.MemSize=" + size);
                if (buffer.MemSize.ToInt64() != size)
                    Error("Mem.Size!=input size");
                //Output("Mem.MemType=" + buffer.MemType);
                if (buffer.MemType != MemObjectType.BUFFER)
                    Error("Mem.MemType!=MemObjectType.BUFFER");

                //Output("Mem.MapCount=" + buffer.MapCount);
                if (buffer.MapCount != 0)
                    Error("Mem.MapCount!=0");

                Kernel k = kernelDictionary["TestReadMemory"];
                k.SetArg(0, buffer);
                k.SetArg(1, (long)size);
                cq.EnqueueTask(k);
                cq.Finish();
            }

            //Output("Allocating " + size + " bytes of WRITE memory");
            using (Mem buffer = c.CreateBuffer(MemFlags.WRITE_ONLY, size, IntPtr.Zero))
            {
                Array.Clear(testData, 0, size);
                //Output("Mem.MemSize=" + size);
                if (buffer.MemSize.ToInt64() != size)
                    Error("Mem.Size!=input size");
                //Output("Mem.MemType=" + buffer.MemType);
                if (buffer.MemType != MemObjectType.BUFFER)
                    Error("Mem.MemType!=MemObjectType.BUFFER");

                //Output("Mem.MapCount=" + buffer.MapCount);
                if (buffer.MapCount != 0)
                    Error("Mem.MapCount!=0");

                Kernel k = kernelDictionary["TestWriteMemory"];
                k.SetArg(0, buffer);
                k.SetArg(1, (long)size);
                cq.EnqueueTask(k);
                cq.Finish();
                buffer.Read(cq, 0L, testData, 0, size);
                for (int i = 0; i < size; i++)
                {
                    if (testData[i] != 1)
                    {
                        Error("TestWriteMemory failed");
                        break;
                    }
                }
            }
            TestReadWriteCopyOps(c, cq);
            TestImageReadWriteCopyOps(c, cq);
            TestTransfersBetweenImageAndBuffers(c, cq);
            TestMapBuffer(c, cq);
            TestEnqueueNDRangeKernel(c, cq, kernelDictionary["EmptyKernel"]);
            TestBufferRectFunctions(c, cq);
        }

        #endregion

        #region TestEnqueueNDRangeKernel

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueNDRangeKernel
        /// 
        /// The tests just issue a dummy kernel a bunch of times with the various overloads
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestEnqueueNDRangeKernel(Context c, CommandQueue cq, Kernel k )
        {
            Output("Testing EnqueueNDRangeKernel");

            Event event0 = null;
            Event event1 = null;

            try
            {
                {
                    IntPtr[] globalWorkSize = new IntPtr[] { (IntPtr)10 };
                    IntPtr[] localWorkSize = new IntPtr[] { (IntPtr)1 };
                    cq.EnqueueNDRangeKernel(k, (uint)1, null, globalWorkSize, localWorkSize);
                    cq.EnqueueNDRangeKernel(k, (uint)1, null, globalWorkSize, localWorkSize, 0, null, out event0);
                    Event[] waitList = new Event[] { event0 };
                    cq.EnqueueNDRangeKernel(k, (uint)1, null, globalWorkSize, localWorkSize, 1, waitList, out event1);
                    cq.Finish();
                    event0.Dispose();
                    event1.Dispose();
                }
                {
                    int[] globalWorkSize = new int[] { (int)10 };
                    int[] localWorkSize = new int[] { (int)1 };
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize);
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize, 0, null, out event0);
                    Event[] waitList = new Event[] { event0 };
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize, 1, waitList, out event1);
                    cq.Finish();
                    event0.Dispose();
                    event1.Dispose();
                }
                {
                    long[] globalWorkSize = new long[] { (long)10 };
                    long[] localWorkSize = new long[] { (long)1 };
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize);
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize, 0, null, out event0);
                    Event[] waitList = new Event[] { event0 };
                    cq.EnqueueNDRangeKernel(k, 1, null, globalWorkSize, localWorkSize, 1, waitList, out event1);
                    cq.Finish();
                    event0.Dispose();
                    event1.Dispose();
                }
            }
            catch (Exception e)
            {
                Error("Exception during testing: " + e.ToString());
            }
            finally
            {
                if (event0 != null)
                    event0.Dispose();
                if (event1 != null)
                    event1.Dispose();
            }
        }

        #endregion

        #region TestMapBuffer

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueMapBuffer
        /// EnqueueMapImage
        /// 
        /// The test bounces an array from a managed byte buffer to a mapped buffer,
        /// to an image. The image is then mapped and copied to a new managed buffer
        /// where the result is compared to the original.
        /// 
        /// On error, the actual point of failure will have to be identified manually.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestMapBuffer(Context c, CommandQueue cq)
        {
            if (!cq.Device.ImageSupport)
            {
                Output("Skipping EnqueueMapBuffer and EnqueueMapImage tests(not supported on this device)");
                return;
            }

            Output("Testing MapBuffer");

            OpenCLNet.CLImage img0 = null;
            OpenCLNet.Mem mem0 = null;
            int imgWidth = 1024;
            int imgHeight = 1024;
            int bufLen = imgWidth * 4 * imgHeight;
            byte[] srcData = new byte[bufLen];
            byte[] cmpData = new byte[bufLen];
            Event event0;
            Event event1;

            for (int i = 0; i < srcData.Length; i++)
                srcData[i] = (byte)(i);
            Array.Clear(cmpData, 0, cmpData.Length);

            try
            {
                img0 = c.CreateImage2D(MemFlags.READ_WRITE, ImageFormat.RGBA8U, imgWidth, imgHeight);
                mem0 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);

                Array.Clear(cmpData, 0, cmpData.Length);
                fixed (byte* pSrc = srcData)
                {
                    fixed (byte* pCmp = cmpData)
                    {
                        {
                            IntPtr[] origin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] region = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };
                            IntPtr[] dstOrigin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] dstRegion = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };
                            IntPtr mapPtr;
                            byte* pMapPtr;
                            IntPtr image_row_pitch;
                            IntPtr image_slice_pitch;

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, true, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y*imgWidth*4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (IntPtr version)Copy not identical to source when using no event args");


                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            Event fdjk;
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region, 0, null, out fdjk);
                            cq.Finish();

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 0, null, out event0 );
                            cq.EnqueueWaitForEvent(event0);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (IntPtr version)Copy not identical to source when using event output and no wait list");
                            

                            Event[] waitList = new Event[] { event0 };
                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 1, waitList, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (IntPtr version)Copy not identical to source when using event output and no wait list");

                            event0.Dispose();
                            event1.Dispose();
                        }
                        {
                            int[] origin = new int[3] { (int)0, (int)0, (int)0 };
                            int[] region = new int[3] { (int)imgWidth, (int)imgHeight, (int)1 };
                            IntPtr mapPtr;
                            byte* pMapPtr;
                            int image_row_pitch;
                            int image_slice_pitch;

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, true, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y*imgWidth*4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (int version)Copy not identical to source when using no event args");


                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 0, null, out event0 );
                            cq.EnqueueWaitForEvent(event0);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (int version)Copy not identical to source when using event output and no wait list");
                            

                            Event[] waitList = new Event[] { event0 };
                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 1, waitList, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * (int)image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (int version)Copy not identical to source when using event output and no wait list");

                            event0.Dispose();
                            event1.Dispose();
                        }
                        {
                            long[] origin = new long[3] { (long)0, (long)0, (long)0 };
                            long[] region = new long[3] { (long)imgWidth, (long)imgHeight, (long)1 };
                            IntPtr mapPtr;
                            byte* pMapPtr;
                            long image_row_pitch;
                            long image_slice_pitch;

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, (long)0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, true, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (long version)Copy not identical to source when using no event args");


                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, (long)0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 0, null, out event0);
                            cq.EnqueueWaitForEvent(event0);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (long version)Copy not identical to source when using event output and no wait list");


                            Event[] waitList = new Event[] { event0 };
                            Array.Clear(cmpData, 0, cmpData.Length);
                            mapPtr = cq.EnqueueMapBuffer(mem0, true, MapFlags.WRITE, 0, bufLen);
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int i = 0; i < bufLen; i++)
                                pMapPtr[i] = srcData[i];
                            cq.EnqueueUnmapMemObject(mem0, mapPtr);
                            cq.EnqueueCopyBufferToImage(mem0, img0, (long)0, origin, region);

                            mapPtr = cq.EnqueueMapImage(img0, false, MapFlags.READ, origin, region, out image_row_pitch, out image_slice_pitch, 1, waitList, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            cq.Finish();
                            pMapPtr = (byte*)mapPtr.ToPointer();
                            for (int y = 0; y < imgHeight; y++)
                            {
                                byte* pSrcRowPtr = pMapPtr + y * image_row_pitch;
                                byte* pDstRowPtr = pCmp + y * imgWidth * 4;
                                for (int x = 0; x < imgWidth * 4; x++)
                                {
                                    pDstRowPtr[x] = pSrcRowPtr[x];
                                }
                            }
                            cq.EnqueueUnmapMemObject(img0, mapPtr);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueEnqueueMapBuffer/EnqueueMapImage: (long version)Copy not identical to source when using event output and no wait list");

                            event0.Dispose();
                            event1.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("Exception during testing: " + e.ToString());
            }
            finally
            {
                if (img0 != null)
                    img0.Dispose();
                if (mem0 != null)
                    mem0.Dispose();
            }
        }

        #endregion

        #region TestUserEventCallbacks

        volatile bool TestUserEventCallbackCalled = false;
        private void TestUserEventCallbacks(Context c, CommandQueue cq)
        {
            if (!(cq.Device.Platform.OpenCLMajorVersion >= 1 && cq.Device.Platform.OpenCLMinorVersion >= 1))
            {
                Output("Skipping TestUserEventCallbacks tests(Requires OpenCL 1.1 or higher)");
                return;
            }

            Output( "Testing User Events and event callbacks" );

            TestUserEventCallbackCalled = false;
            Event event0 = c.CreateUserEvent();
            event0.SetCallback(ExecutionStatus.COMPLETE, TestUserEventCallback, this);
            event0.SetUserEventStatus(ExecutionStatus.COMPLETE);
            if (TestUserEventCallbackCalled != true)
            {
                Error("Event callback not called");
                return;
            }
            c.WaitForEvent(event0);
        }

        private void TestUserEventCallback(Event e, ExecutionStatus executionStatus, object userData)
        {
            TestUserEventCallbackCalled = true;
        }

        #endregion

        #region TestBufferRectFunctions

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueReadBufferRect
        /// EnqueueWriteBufferRect
        /// EnqueueCopyBufferRect
        /// 
        /// The test just copies the entirety of a buffer and checks if the result is equal to the original.
        /// An error indicates that one of the above functions failed and further manual analysis is required
        /// to pinpoint the error.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestBufferRectFunctions(Context c, CommandQueue cq)
        {
            if (!(cq.Device.Platform.OpenCLMajorVersion >= 1 && cq.Device.Platform.OpenCLMinorVersion >= 1))
            {
                Output("Skipping EnqueueReadBufferRect, EnqueueWriteBufferRect and EnqueueCopyBufferRect tests(Requires OpenCL 1.1 or higher)");
                return;
            }

            Output("Testing EnqueueReadBufferRect, EnqueueWriteBufferRect and EnqueueCopyBufferRect");

            OpenCLNet.Mem mem0 = null;
            OpenCLNet.Mem mem1 = null;
            int bufWidth = 16;
            int bufHeight = 16;
            int bufLen = bufWidth * bufHeight;
            byte[] srcData = new byte[bufLen];
            byte[] cmpData = new byte[bufLen];
            Event event0;
            Event event1;
            Event event2;
            Event event3;
            Event event4;
            Event event5;

            Array.Clear(srcData, 0, srcData.Length);
            for (int i = 8; i < 12; i++)
                for (int j = 8; j < 12; j++)
                    srcData[bufWidth * i + j] = (byte)1;
            Array.Clear(cmpData, 0, cmpData.Length);

            try
            {
                mem0 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);
                mem1 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);

                fixed (byte* pSrc = srcData)
                {
                    fixed (byte* pCmp = cmpData)
                    {
                        {
                            IntPtr[] bufferOffset = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] hostOffset = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] region = new IntPtr[3] { (IntPtr)bufWidth, (IntPtr)bufHeight, (IntPtr)1 };
                            IntPtr bufferRowPitch = (IntPtr)bufWidth;
                            IntPtr bufferSlicePitch = (IntPtr)0;
                            IntPtr hostRowPitch = (IntPtr)bufWidth;
                            IntPtr hostSlicePitch = (IntPtr)0;

                            cq.EnqueueWriteBufferRect(mem0, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch);
                            cq.Finish();
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (IntPtr version)Copy not identical to source when using no event args");

                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueWaitForEvent(event0);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 0, null, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 0, null, out event2);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (IntPtr version)Copy not identical to source when using event output and no event args");

                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueWaitForEvent(event3);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 3, events, out event4);
                            cq.EnqueueWaitForEvent(event4);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 3, events, out event5);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (IntPtr version)Copy not identical to source when using event output and event args");
                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                        {
                            int[] bufferOffset = new int[3] { 0, 0, 0 };
                            int[] hostOffset = new int[3] { 0, 0, 0 };
                            int[] region = new int[3] { bufWidth, bufHeight, 1 };
                            int bufferRowPitch = bufWidth;
                            int bufferSlicePitch = 0;
                            int hostRowPitch = bufWidth;
                            int hostSlicePitch = 0;

                            cq.EnqueueWriteBufferRect(mem0, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch);
                            cq.Finish();
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (int version)Copy not identical to source when using no event args");

                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueWaitForEvent(event0);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 0, null, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 0, null, out event2);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (int version)Copy not identical to source when using event output and no event args");

                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueWaitForEvent(event3);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 3, events, out event4);
                            cq.EnqueueWaitForEvent(event4);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 3, events, out event5);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (int version)Copy not identical to source when using event output and event args");
                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                        {
                            long[] bufferOffset = new long[3] { 0L, 0L, 0L };
                            long[] hostOffset = new long[3] { 0L, 0L, 0L };
                            long[] region = new long[3] { (long)bufWidth, (long)bufHeight, (long)1 };
                            long bufferRowPitch = bufWidth;
                            long bufferSlicePitch = 0;
                            long hostRowPitch = bufWidth;
                            long hostSlicePitch = 0;

                            cq.EnqueueWriteBufferRect(mem0, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch);
                            cq.Finish();
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, true, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (long version)Copy not identical to source when using no event args");

                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueWaitForEvent(event0);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 0, null, out event1);
                            cq.EnqueueWaitForEvent(event1);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 0, null, out event2);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (long version)Copy not identical to source when using event output and no event args");

                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteBufferRect(mem0, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueWaitForEvent(event3);
                            cq.EnqueueCopyBufferRect(mem0, mem1, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, 3, events, out event4);
                            cq.EnqueueWaitForEvent(event4);
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueReadBufferRect(mem1, false, bufferOffset, hostOffset, region, bufferRowPitch, bufferSlicePitch, hostRowPitch, hostSlicePitch, (IntPtr)pCmp, 3, events, out event5);
                            cq.Finish();
                            if (!CompareArray(cmpData, srcData))
                                Error("Read-/Write-/CopyRect: (long version)Copy not identical to source when using event output and event args");
                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("Exception during testing: " + e.ToString());
            }
            finally
            {
                if (mem0 != null)
                    mem0.Dispose();
                if (mem1 != null)
                    mem1.Dispose();
            }
        }

        #endregion

        #region TestTransfersBetweenImageAndBuffers

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueCopyImageToBuffer
        /// EnqueueCopyBufferToImage
        /// 
        /// The test just copies the entirety of a buffer and checks if the result is equal to the original.
        /// An error indicates that one of the above functions failed and further manual analysis is required
        /// to pinpoint the error.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestTransfersBetweenImageAndBuffers(Context c, CommandQueue cq)
        {
            if (!cq.Device.ImageSupport)
            {
                Output("Skipping CopyImageToBuffer and CopyBufferToImage tests(not supported on this device)");
                return;
            }

            Output("Testing CopyImageToBuffer and CopyBufferToImage");

            OpenCLNet.CLImage img0 = null;
            OpenCLNet.Mem mem0 = null;
            int imgWidth = 1024;
            int imgHeight = 1024;
            int bufLen = imgWidth * 4 * imgHeight;
            byte[] srcData = new byte[bufLen];
            byte[] cmpData = new byte[bufLen];
            Event event0;
            Event event1;
            Event event2;
            Event event3;

            for (int i = 0; i < srcData.Length; i++)
                srcData[i] = (byte)(i);
            Array.Clear(cmpData, 0, cmpData.Length);

            try
            {
                img0 = c.CreateImage2D(MemFlags.READ_WRITE, ImageFormat.RGBA8U, imgWidth, imgHeight);
                mem0 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);

                Array.Clear(cmpData, 0, cmpData.Length);
                fixed (byte* pSrc = srcData)
                {
                    fixed (byte* pCmp = cmpData)
                    {
                        {
                            IntPtr[] origin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] region = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };
                            IntPtr[] dstOrigin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] dstRegion = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, (IntPtr)0);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (IntPtr version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region, 0, null, out event0);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, (IntPtr)0, 0, null, out event1);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (IntPtr version)Copy not identical to source when using event output and event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1 };
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, (IntPtr)0, origin, region, 2, events, out event2);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, (IntPtr)0, 2, events, out event3);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (IntPtr version)Copy not identical to source when using event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                        }
                        {
                            int[] origin = new int[3] { 0, 0, 0 };
                            int[] region = new int[3] { imgWidth, imgHeight, 1 };
                            int[] dstOrigin = new int[3] { 0, 0, 0 };
                            int[] dstRegion = new int[3] { imgWidth, imgHeight, 1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (int version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region, 0, null, out event0);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0, 0, null, out event1);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (int version)Copy not identical to source when using event output no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1 };
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0, origin, region, 2, events, out event2);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0, 2, events, out event3);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (int version)Copy not identical to source when using event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                        }
                        {
                            long[] origin = new long[3] { 0, 0, 0 };
                            long[] region = new long[3] { imgWidth, imgHeight, 1 };
                            long[] dstOrigin = new long[3] { 0, 0, 0 };
                            long[] dstRegion = new long[3] { imgWidth, imgHeight, 1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0L, origin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0L);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (long version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0L, origin, region, 0, null, out event0);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0L, 0, null, out event1);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (long version)Copy not identical to source when using event output and no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1 };
                            mem0.Write(cq, 0L, srcData, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyBufferToImage(mem0, img0, 0L, origin, region, 2, events, out event2);
                            cq.EnqueueBarrier();
                            cq.EnqueueCopyImageToBuffer(img0, mem0, origin, region, 0L, 2, events, out event3);
                            cq.EnqueueBarrier();
                            mem0.Read(cq, 0L, cmpData, 0, bufLen);
                            if (!CompareArray(cmpData, srcData))
                                Error("EnqueueCopyBufferToImage/EnqueueCopyImageToBuffer: (long version)Copy not identical to source when using event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("Exception during testing: " + e.ToString());
            }
            finally
            {
                if (img0 != null)
                    img0.Dispose();
                if (mem0 != null)
                    mem0.Dispose();
            }
        }

        #endregion

        #region TestImageReadWriteCopyOps

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueWriteImage
        /// EnqueueReadImage
        /// EnqueueCopyImage
        /// 
        /// The test just copies the entirety of a buffer and checks if the result is equal to the original.
        /// An error indicates that one of the above functions failed and further manual analysis is required
        /// to pinpoint the error.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestImageReadWriteCopyOps(Context c, CommandQueue cq)
        {
            if (!cq.Device.ImageSupport)
            {
                Output("Skipping image read/write/copy tests(not supported on this device)");
                return;
            }

            Output("Testing image read/write/copy functions");

            OpenCLNet.CLImage img0 = null;
            OpenCLNet.CLImage img1 = null;
            OpenCLNet.CLImage img2 = null;
            int imgWidth = 1024;
            int imgHeight = 1024;
            int bufLen = imgWidth*4*imgHeight;
            byte[] srcData = new byte[bufLen];
            byte[] cmpData = new byte[bufLen];
            Event event0;
            Event event1;
            Event event2;
            Event event3;
            Event event4;
            Event event5;

            for (int i = 0; i < srcData.Length; i++)
                srcData[i] = (byte)(i);
            Array.Clear(cmpData, 0, cmpData.Length);

            try
            {
                img0 = c.CreateImage2D(MemFlags.READ_WRITE, ImageFormat.RGBA8U, imgWidth, imgHeight);
                img1 = c.CreateImage2D(MemFlags.READ_WRITE, ImageFormat.RGBA8U, imgWidth, imgHeight);
                img2 = c.CreateImage2D(MemFlags.READ_WRITE, ImageFormat.RGBA8U, imgWidth, imgHeight);

                Array.Clear(cmpData, 0, cmpData.Length);
                fixed (byte* pSrc = srcData)
                {
                    fixed (byte* pCmp = cmpData)
                    {
                        {
                            IntPtr[] origin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] region = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };
                            IntPtr[] dstOrigin = new IntPtr[3] { (IntPtr)0, (IntPtr)0, (IntPtr)0 };
                            IntPtr[] dstRegion = new IntPtr[3] { (IntPtr)imgWidth, (IntPtr)imgHeight, (IntPtr)1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pSrc);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (IntPtr version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (IntPtr version)Copy not identical to source with event output and no wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteImage(img0, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 3, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (IntPtr version)Copy not identical to source using no event output and a wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 3, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, IntPtr.Zero, IntPtr.Zero, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (IntPtr version)Copy not identical to source using event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                        {
                            int[] origin = new int[3] { 0, 0, 0 };
                            int[] region = new int[3] { imgWidth, imgHeight, 1 };
                            int[] dstOrigin = new int[3] { 0, 0, 0 };
                            int[] dstRegion = new int[3] { imgWidth, imgHeight, 1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0, 0, (IntPtr)pSrc);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0, 0, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (int version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0, 0, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0, 0, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (int version)Copy not identical to source with event output and no wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteImage(img0, true, origin, region, 0, 0, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0, 0, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (int version)Copy not identical to source using no event output and a wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0, 0, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0, 0, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (int version)Copy not identical to source using no event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                        {
                            long[] origin = new long[3] { 0, 0, 0 };
                            long[] region = new long[3] { imgWidth, imgHeight, 1 };
                            long[] dstOrigin = new long[3] { 0, 0, 0 };
                            long[] dstRegion = new long[3] { imgWidth, imgHeight, 1 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0L, 0L, (IntPtr)pSrc);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0L, 0L, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (long version)Copy not identical to source when using no event args");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0L, 0L, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0L, 0L, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (long version)Copy not identical to source with event output and no wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            Event[] events = new Event[] { event0, event1, event2 };
                            cq.EnqueueWriteImage(img0, true, origin, region, 0L, 0L, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0L, 0L, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (long version)Copy not identical to source using no event output and a wait list");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteImage(img0, true, origin, region, 0L, 0L, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyImage(img0, img1, origin, dstOrigin, region, 0, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadImage(img1, true, origin, region, 0L, 0L, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestImageReadWriteCopyOps: (long version)Copy not identical to source using no event output and a wait list");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Error("Exception during testing: "+e.ToString());
            }
            finally
            {
                if (img0 != null)
                    img0.Dispose();
                if (img1 != null)
                    img1.Dispose();
                if (img2 != null)
                    img2.Dispose();
            }
        }

        #endregion

        #region TestReadWriteCopyOps

        /// <summary>
        /// Test all versions of:
        /// 
        /// EnqueueReadBuffer
        /// EnqueueWriteBuffer
        /// EnqueueCopyBuffer
        /// 
        /// The test just copies the entirety of a buffer and checks if the result is equal to the original.
        /// An error indicates that one of the above functions failed and further manual analysis is required
        /// to pinpoint the error.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="cq"></param>
        private void TestReadWriteCopyOps(Context c, CommandQueue cq)
        {
            Output("Testing read/write/copy functions");
            Mem buf0 = null;
            Mem buf1 = null;
            Mem buf2 = null;
            int bufLen = 1024 * 1024;
            byte[] srcData = new byte[bufLen];
            byte[] cmpData = new byte[bufLen];
            Event event0;
            Event event1;
            Event event2;
            Event event3;
            Event event4;
            Event event5;

            for (int i = 0; i < srcData.Length; i++)
                srcData[i] = (byte)(i);
            Array.Clear(cmpData, 0, cmpData.Length);

            try
            {
                buf0 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);
                buf1 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);
                buf2 = c.CreateBuffer(MemFlags.READ_WRITE, bufLen, IntPtr.Zero);

                #region Test EnqueueReadBuffer EnqueueWriteBuffer EnqueueCopyBuffer

                fixed (byte* pSrc = srcData)
                {
                    fixed (byte* pCmp = cmpData)
                    {
                        {
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pSrc);
                            cq.EnqueueCopyBuffer(buf0, buf1, (IntPtr)0, (IntPtr)0, (IntPtr)bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(IntPtr version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pSrc, 0, null, out event0 );
                            cq.EnqueueCopyBuffer(buf0, buf1, (IntPtr)0, (IntPtr)0, (IntPtr)bufLen, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(IntPtr version): Copy not identical to source");

                            Event[] events = new Event[] { event0, event1, event2 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyBuffer(buf0, buf1, (IntPtr)0, (IntPtr)0, (IntPtr)bufLen, 3, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(IntPtr version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyBuffer(buf0, buf1, (IntPtr)0, (IntPtr)0, (IntPtr)bufLen, 3, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, (IntPtr)0, (IntPtr)bufLen, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(IntPtr version): Copy not identical to source");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }

                        {
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0, bufLen, (IntPtr)pSrc);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0, 0, bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0, bufLen, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(int version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0, bufLen, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0, 0, bufLen, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0, bufLen, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(int version): Copy not identical to source");

                            Event[] events = new Event[] { event0, event1, event2 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0, bufLen, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0, 0, bufLen, 3, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0, bufLen, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(int version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0, bufLen, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0, 0, bufLen, 3, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0, bufLen, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(int version): Copy not identical to source");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }

                        {
                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0L, (long)bufLen, (IntPtr)pSrc);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0L, 0L, (long)bufLen);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0L, (long)bufLen, (IntPtr)pCmp);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(long version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0L, (long)bufLen, (IntPtr)pSrc, 0, null, out event0);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0L, 0L, (long)bufLen, 0, null, out event1);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0L, (long)bufLen, (IntPtr)pCmp, 0, null, out event2);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(long version): Copy not identical to source");

                            Event[] events = new Event[] { event0, event1, event2 };

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0L, (long)bufLen, (IntPtr)pSrc, 3, events);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0L, 0L, (long)bufLen, 3, events);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0L, (long)bufLen, (IntPtr)pCmp, 3, events);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(long version): Copy not identical to source");

                            Array.Clear(cmpData, 0, cmpData.Length);
                            cq.EnqueueWriteBuffer(buf0, true, 0L, (long)bufLen, (IntPtr)pSrc, 3, events, out event3);
                            cq.EnqueueCopyBuffer(buf0, buf1, 0L, 0L, (long)bufLen, 3, events, out event4);
                            cq.EnqueueBarrier();
                            cq.EnqueueReadBuffer(buf1, true, 0L, (long)bufLen, (IntPtr)pCmp, 3, events, out event5);
                            if (!CompareArray(cmpData, srcData))
                                Error("TestReadWriteCopyOps(long version): Copy not identical to source");

                            event0.Dispose();
                            event1.Dispose();
                            event2.Dispose();
                            event3.Dispose();
                            event4.Dispose();
                            event5.Dispose();
                        }
                    }
                }
                #endregion
            }
            catch (Exception e)
            {
                Error("Exception during testing: " + e.ToString());
            }
            finally
            {
                if (buf0 != null)
                    buf0.Dispose();
                if (buf1 != null)
                    buf1.Dispose();
                if (buf2 != null)
                    buf2.Dispose();
            }
        }

        #endregion

        private void TestDevice( Device d )
        {
            Output("");
            Output("Testing device: \"" + d.Name+"\"");
            // d.ToString() is overloaded to output all properties as a string, so every property will be used that way
            Output(d.ToString());
        }

        private void TestCommandQueue(Context c, CommandQueue cq )
        {
            string programName = "OpenCL" + Path.DirectorySeparatorChar + "src" + Path.DirectorySeparatorChar + "MemoryTests.cl";

            Output("Testing compilation of: " + programName);
            OpenCLNet.CLProgram p0 = c.CreateProgramWithSource(File.ReadAllLines(programName));
            OpenCLNet.CLProgram p = c.CreateProgramWithSource(File.ReadAllText(programName));
            p0.Build();
            p.Build();
            Kernel k = p.CreateKernel(@"LoopAndDoNothing");
            
            TestCommandQueueMemCopy(c, cq);
            TestCommandQueueAsync(c, cq, k );
        }

        #region TestCommandQueue helper functions

        private void TestCommandQueueAsync(Context c, CommandQueue cq, Kernel kernel )
        {
            List<Event> events = new List<Event>();
            Event clEvent;

            Output("Testing asynchronous task issuing (clEnqueueTask) and waiting for events");

            // Issue a bunch of slow operations
            kernel.SetArg(0, 5000000);
            for (int i = 0; i < 10; i++)
            {
                cq.EnqueueTask(kernel, 0, null, out clEvent);
                events.Add(clEvent);
            }

            // Issue a bunch of fast operations
            kernel.SetArg(0, 500);
            for (int i = 0; i < 10; i++)
            {
                cq.EnqueueTask(kernel, 0, null, out clEvent);
                events.Add(clEvent);
            }

            Event[] eventList = events.ToArray();
            cq.EnqueueWaitForEvents(eventList.Length, eventList);
            while (events.Count > 0)
            {
                if ((int)events[0].ExecutionStatus < 0)
                {
                    Output(cq.Device.Name + ": TestCommandQueueAsync failed with error code " + (ErrorCode)events[0].ExecutionStatus);
                }
                events[0].Dispose();
                events.RemoveAt(0);
            }
        }

        private void TestCommandQueueMemCopy(Context c, CommandQueue cq)
        {
            Output("Testing synchronous host memory->memory copy");
            
            AlignedArrayFloat aafSrc = new AlignedArrayFloat(1024 * 1024, 64);
            AlignedArrayFloat aafDst = new AlignedArrayFloat(1024 * 1024, 64);

            SetAAF(aafSrc, 0.0f);
            SetAAF(aafDst, 1.0f);

            /// Test HOST_PTR -> HOST_PTR copy
            /// The call to EnqueueMapBuffer synchronizes caches before testing the result
            using (Mem memSrc = c.CreateBuffer((MemFlags)((ulong)MemFlags.READ_WRITE+(ulong)MemFlags.USE_HOST_PTR), aafSrc.ByteLength, aafSrc))
            {
                using (Mem memDst = c.CreateBuffer((MemFlags)((ulong)MemFlags.READ_WRITE+(ulong)MemFlags.USE_HOST_PTR), aafDst.ByteLength, aafDst))
                {
                    cq.EnqueueCopyBuffer(memSrc, memDst, IntPtr.Zero, IntPtr.Zero, (IntPtr)aafSrc.ByteLength);
                    cq.EnqueueBarrier();
                    IntPtr mappedPtr = cq.EnqueueMapBuffer(memDst, true, MapFlags.READ_WRITE, (IntPtr)0, (IntPtr)aafDst.ByteLength);
                    if (!TestAAF(aafDst, 0.0f))
                        Error("EnqueueCopyBuffer failed, destination is invalid");
                    cq.EnqueueUnmapMemObject(memDst, mappedPtr);
                    cq.EnqueueBarrier();
                }
            }

            /// Test COPY_HOST_PTR -> COPY_HOST_PTR copy
            /// Verify that original source buffers are intact and that the copy was successful
            SetAAF(aafSrc, 0.0f);
            SetAAF(aafDst, 1.0f);
            using (Mem memSrc = c.CreateBuffer(MemFlags.COPY_HOST_PTR, aafSrc.ByteLength, aafSrc))
            {
                using (Mem memDst = c.CreateBuffer(MemFlags.COPY_HOST_PTR, aafSrc.ByteLength, aafDst))
                {
                    SetAAF(aafSrc, 2.0f);
                    SetAAF(aafDst, 3.0f);

                    cq.EnqueueCopyBuffer(memSrc, memDst, IntPtr.Zero, IntPtr.Zero, (IntPtr)aafSrc.ByteLength);
                    cq.Finish();

                    if (!TestAAF(aafSrc, 2.0f))
                        Error("Memory copy destroyed src buffer");
                    if (!TestAAF(aafDst, 3.0f))
                        Error("Memory copy destroyed dst buffer");
                    Event ev;
                    cq.EnqueueReadBuffer(memDst, false, IntPtr.Zero, (IntPtr)aafDst.ByteLength, aafDst,0, null, out ev);
                    cq.EnqueueWaitForEvents(1, new Event[] { ev });
                    ev.Dispose();
                    cq.Finish();
                    if (!TestAAF(aafDst, 0.0f))
                        Error("Memory copy failed");
                }
            }

            /// Test ALLOC_HOST_PTR -> ALLOC_HOST_PTR copy
            SetAAF(aafSrc, 0.0f);
            SetAAF(aafDst, 1.0f);
            using (Mem memSrc = c.CreateBuffer((MemFlags)((ulong)MemFlags.ALLOC_HOST_PTR + (ulong)MemFlags.READ_WRITE), aafSrc.ByteLength, IntPtr.Zero))
            {
                using (Mem memDst = c.CreateBuffer((MemFlags)((ulong)MemFlags.ALLOC_HOST_PTR + (ulong)MemFlags.WRITE_ONLY), aafSrc.ByteLength, IntPtr.Zero))
                {
                    cq.EnqueueWriteBuffer(memSrc, false, (IntPtr)0, (IntPtr)aafSrc.ByteLength, aafSrc);
                    cq.EnqueueWriteBuffer(memDst, false, (IntPtr)0, (IntPtr)aafSrc.ByteLength, aafSrc);
                    cq.EnqueueBarrier();

                    cq.EnqueueCopyBuffer(memSrc, memDst, IntPtr.Zero, IntPtr.Zero, (IntPtr)aafSrc.ByteLength);
                    cq.EnqueueBarrier();

                    cq.EnqueueReadBuffer(memDst, true, IntPtr.Zero, (IntPtr)aafDst.ByteLength, aafDst);
                    if (!TestAAF(aafDst, 0.0f))
                        Error("Memory copy failed");
                }
            }

            /// Test DEFAULT -> DEFAULT copy
            SetAAF(aafSrc, 0.0f);
            SetAAF(aafDst, 1.0f);
            using (Mem memSrc = c.CreateBuffer((MemFlags)((ulong)MemFlags.ALLOC_HOST_PTR + (ulong)MemFlags.READ_ONLY), aafSrc.ByteLength, IntPtr.Zero))
            {
                using (Mem memDst = c.CreateBuffer((MemFlags)((ulong)MemFlags.ALLOC_HOST_PTR + (ulong)MemFlags.WRITE_ONLY), aafSrc.ByteLength, IntPtr.Zero))
                {
                    cq.EnqueueWriteBuffer(memSrc, false, (IntPtr)0, (IntPtr)aafSrc.ByteLength, aafSrc);
                    cq.EnqueueWriteBuffer(memDst, false, (IntPtr)0, (IntPtr)aafSrc.ByteLength, aafSrc);
                    cq.EnqueueBarrier();

                    cq.EnqueueCopyBuffer(memSrc, memDst, IntPtr.Zero, IntPtr.Zero, (IntPtr)aafSrc.ByteLength);
                    cq.EnqueueBarrier();

                    cq.EnqueueReadBuffer(memDst, true, IntPtr.Zero, (IntPtr)aafDst.ByteLength, aafDst);
                    if (!TestAAF(aafDst, 0.0f))
                        Error("Memory copy failed");
                }
            }
        }

        private bool TestAAF(AlignedArrayFloat aaf, float c)
        {
            for (int i = 0; i < aaf.Length; i++)
                if (aaf[i] != c)
                    return false;
            return true;
        }

        private void SetAAF(AlignedArrayFloat aaf, float c)
        {
            for (int i = 0; i < aaf.Length; i++)
                aaf[i] = c;
        }

        private void SetAAFLinear(AlignedArrayFloat aaf)
        {
            for (int i = 0; i < aaf.Length; i++)
                aaf[i] = (float)i;
        }

        #endregion

        private void Output(string s)
        {
            listBoxOutput.Items.Add(s);
            listBoxOutput.SelectedIndex = listBoxOutput.Items.Count-1;
            Application.DoEvents();
        }

        private void Warning(string s)
        {
            listBoxWarnings.Items.Add(s);
            listBoxOutput.SelectedIndex = listBoxOutput.Items.Count-1;
            Application.DoEvents();
        }

        private void Error(string s)
        {
            listBoxErrors.Items.Add(s);
            listBoxOutput.SelectedIndex = listBoxOutput.Items.Count-1;
            Application.DoEvents();
        }

    }
}
