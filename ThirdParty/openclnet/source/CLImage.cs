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

namespace OpenCLNet
{
    public unsafe class CLImage : Mem
    {
        internal CLImage(Context context, IntPtr memID)
            : base(context,memID)
        {
        }

        #region Properties

        public CLImageFormat ImageFormat
        {
            get
            {
                IntPtr size = GetPropertySize((uint)ImageInfo.FORMAT);
                byte* pBuffer = stackalloc byte[(int)size];

                ReadProperty((uint)ImageInfo.FORMAT, size, pBuffer);
                return (CLImageFormat)Marshal.PtrToStructure((IntPtr)pBuffer, typeof(CLImageFormat));
            }
        }
        public long ElementSize { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.ELEMENT_SIZE).ToInt64(); } }
        public long RowPitch { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.ROW_PITCH).ToInt64(); } }
        public long SlicePitch { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.SLICE_PITCH).ToInt64(); } }
        public long Width { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.WIDTH).ToInt64(); } }
        public long Height { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.HEIGHT).ToInt64(); } }
        public long Depth { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.DEPTH).ToInt64(); } }
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        public long ArraySize { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.ARRAY_SIZE).ToInt64(); } }
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        public IntPtr Buffer { get { return InteropTools.ReadIntPtr(this, (uint)ImageInfo.BUFFER); } }
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        public uint NumMipLevels { get { return InteropTools.ReadUInt(this, (uint)ImageInfo.NUM_MIP_LEVELS); } }
        /// <summary>
        /// OpenCL 1.2
        /// </summary>
        public uint NumSamples { get { return InteropTools.ReadUInt(this, (uint)ImageInfo.NUM_SAMPLES); } }

        #endregion

        // Override the IPropertyContainer interface of the Mem class.
        #region IPropertyContainer Members

        public override unsafe IntPtr GetPropertySize(uint key)
        {
            IntPtr size;
            ErrorCode result;

            result = (ErrorCode)OpenCL.GetImageInfo(MemID, key, IntPtr.Zero, null, out size);
            if (result != ErrorCode.SUCCESS)
            {
                size = base.GetPropertySize(key);
            }
            return size;
        }

        public override unsafe void ReadProperty(uint key, IntPtr keyLength, void* pBuffer)
        {
            IntPtr size;
            ErrorCode result;

            result = (ErrorCode)OpenCL.GetImageInfo(MemID, key, keyLength, pBuffer, out size);
            if (result != ErrorCode.SUCCESS)
            {
                base.ReadProperty(key, keyLength, pBuffer);
            }
        }

        #endregion
    }
}
