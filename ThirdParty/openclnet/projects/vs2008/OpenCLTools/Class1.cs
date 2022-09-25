using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using OpenCLNet;

namespace OpenCLTools
{
    /// <summary>
    /// A .Net Bitmap backed by an OpenCL Image with some utility functions
    /// for transferring data back and forth.
    /// Data format is hardcoded to 32 bit pixels with 8 bit unsigned channels
    /// -> ARGB.
    /// </summary>
    public class OpenCLBackedBitmap : IDisposable
    {
        public Bitmap Bitmap { get; set; }
        public OpenCLNet.CLImage Image { get; set; }
        public BitmapData BitmapData;
        protected ImageLockMode ImageLockMode;
        protected Context Context;
        protected CommandQueue CQ;
        IntPtr[] Origin = new IntPtr[3];
        IntPtr[] Region = new IntPtr[3];
        // Track whether Dispose has been called.
        private bool disposed = false;


        internal OpenCLBackedBitmap(Context context, CommandQueue cq, int width, int height)
        {
            Initialize(context, cq, width, height);
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~OpenCLBackedBitmap()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        protected virtual void Initialize(Context context, CommandQueue cq, int width, int height)
        {
            BitmapData bd;

            Bitmap = new Bitmap(width,height,PixelFormat.Format32bppArgb);
            bd = LockEntireBitmap( Bitmap, ImageLockMode.ReadOnly );
            Image = context.CreateImage2D(MemFlags.COPY_HOST_PTR, OpenCLNet.ImageFormat.ARGB8U, width, height, bd.Stride, bd.Scan0);
            Context = context;
            CQ = cq;
        }

        public void UpdateHostBitmap( int left, int top, int width, int height )
        {
            Mem mem = Context.CreateBuffer(MemFlags.USE_HOST_PTR,BitmapData.Stride*BitmapData.Height, BitmapData.Scan0);

            Origin[0] = (IntPtr)left;
            Origin[1] = (IntPtr)top;
            Origin[2] = (IntPtr)0;
            Region[0] = (IntPtr)width;
            Region[1] = (IntPtr)height;
            Region[2] = (IntPtr)0;
            CQ.EnqueueCopyBufferToImage(mem, Image, IntPtr.Zero, Origin, Region);
            
            mem.Dispose();
        }

        public void UpdateOpenCLBitmap(int left, int top, int width, int height)
        {
            Mem mem = Context.CreateBuffer(MemFlags.USE_HOST_PTR, BitmapData.Stride * BitmapData.Height, BitmapData.Scan0);
            
            Origin[0] = (IntPtr)left;
            Origin[1] = (IntPtr)top;
            Origin[2] = (IntPtr)0;
            Region[0] = (IntPtr)width;
            Region[1] = (IntPtr)height;
            Region[2] = (IntPtr)0;
            CQ.EnqueueCopyImageToBuffer(Image, mem, Origin, Region, IntPtr.Zero);
            
            mem.Dispose();
        }

        public void LockHostBitmap(ImageLockMode imageLockMode)
        {
            ImageLockMode = imageLockMode;
            BitmapData = LockEntireBitmap(Bitmap, imageLockMode);
            if (ImageLockMode == ImageLockMode.ReadWrite || imageLockMode == ImageLockMode.WriteOnly)
            {
            }
        }

        public void UnlockHostBitmap()
        {
            Bitmap.UnlockBits(BitmapData);
            BitmapData = null;
            ImageLockMode = ImageLockMode.ReadWrite;
        }

        protected BitmapData LockEntireBitmap( Bitmap bitmap, ImageLockMode imageLockMode )
        {
            Rectangle r = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            return bitmap.LockBits(r, imageLockMode, PixelFormat.Format32bppArgb);
        }

        public OpenCLBackedBitmap FromFile(string path)
        {
            return null;
        }

        #region IDisposable Members

        // Implement IDisposable.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    Bitmap.Dispose();
                    Bitmap = null;
                    Image.Dispose();
                    Image = null;
                    CQ = null;
                    Context = null;
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.

                // Note disposing has been done.
                disposed = true;

            }
        }

        #endregion
    }
}
