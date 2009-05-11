using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using OpenGLES;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;

namespace OpenGLES
{
    public struct BitmapLoadData
    {
        internal IBitmapImage Bitmap;
        internal BitmapImageData Data;
        public int Width;
        public int Height;
        internal bool IsTransparent;
    }
    
    public class Texture : IDisposable
    {
        private Texture()
        {
        }

        uint myName;

        public uint Name
        {
            get { return myName; }
        }

        int myWidth;

        public int Width
        {
            get { return myWidth; }
        }
        int myHeight;

        public int Height
        {
            get { return myHeight; }
        }

        bool myIsTransparent = false;
        public bool IsTransparent
        {
            get
            {
                return myIsTransparent;
            }
        }

        public static int GetValidTextureDimensionFromSize(int size)
        {
            int shiftAmount = 0;
            int minDim = size;
            while ((minDim >> 1) >= 1)
            {
                shiftAmount++;
                minDim >>= 1;
            }
            minDim <<= shiftAmount;
            if (minDim < size)
                minDim <<= 1;

            return minDim;
        }

        static IImagingFactory myImagingFactory;

        unsafe public static BitmapLoadData BeginLoadBitmap(Stream bitmapStream, bool isTransparent)
        {
            // .NET CF does NOT support transparent images. Need to use the COM IImageFactory to create images with alpha.
            if (myImagingFactory == null)
                myImagingFactory = (IImagingFactory)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("327ABDA8-072B-11D3-9D7B-0000F81EF32E")));

            int bytesLength;
            byte[] bytes;
            MemoryStream memStream = bitmapStream as MemoryStream;
            if (memStream != null)
            {
                bytesLength = (int)memStream.Length;
                bytes = memStream.GetBuffer();
            }
            else
            {
                bytesLength = (int)bitmapStream.Length;
                bytes = new byte[bytesLength];
                bitmapStream.Read(bytes, 0, bytesLength);
            }

            IImage image;
            ImageInfo info;
            uint hresult = myImagingFactory.CreateImageFromBuffer(bytes, (uint)bytesLength, BufferDisposalFlag.BufferDisposalFlagNone, out image);
            image.GetImageInfo(out info);

            int resizedWidth = (int)info.Width;
            int resizedHeight = (int)info.Height;

            resizedWidth = Texture.GetValidTextureDimensionFromSize(resizedWidth);
            resizedHeight = Texture.GetValidTextureDimensionFromSize(resizedHeight);

            int resizedDim = Math.Max(resizedWidth, resizedHeight);
            if (resizedDim == (int)info.Width && resizedDim == (int)info.Height)
            {
                resizedWidth = 0;
                resizedHeight = 0;
            }
            else
            {
                resizedWidth = resizedDim;
                resizedHeight = resizedDim;
            }

            IBitmapImage bitmap;
            myImagingFactory.CreateBitmapFromImage(image, (uint)resizedWidth, (uint)resizedHeight, PixelFormatID.PixelFormatDontCare, InterpolationHint.InterpolationHintDefault, out bitmap);
            Marshal.FinalReleaseComObject(image);

            Size size;
            bitmap.GetSize(out size);
            RECT rect = new RECT(0, 0, size.Width, size.Height);
            BitmapImageData data;
            if (isTransparent)
            {
                bitmap.LockBits(ref rect, ImageLockMode.ImageLockModeWrite | ImageLockMode.ImageLockModeRead, PixelFormatID.PixelFormat32bppARGB, out data);
                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Stride; x += 4)
                    {
                        byte* bp = (byte*)data.Scan0 + data.Stride * y + x;
                        byte temp = bp[0];
                        bp[0] = bp[2];
                        bp[2] = temp;
                    }
                }
            }
            else
            {
                bitmap.LockBits(ref rect, ImageLockMode.ImageLockModeRead, PixelFormatID.PixelFormat16bppRGB565, out data);
            }

            BitmapLoadData ret = new BitmapLoadData();
            ret.Data = data;
            ret.Bitmap = bitmap;
            ret.Width = (int)info.Width;
            ret.Height = (int)info.Height;
            ret.IsTransparent = isTransparent;

            return ret;
        }

        unsafe public static Texture LoadBitmap(BitmapLoadData loadData)
        {
            IBitmapImage bitmap = loadData.Bitmap;
            bool isTransparent = loadData.IsTransparent;

            Size size;
            bitmap.GetSize(out size);
            RECT rect = new RECT(0, 0, size.Width, size.Height);

            uint glFormat = gl.GL_RGB;
            uint glType = gl.GL_UNSIGNED_SHORT_5_6_5;
            if (isTransparent)
            {
                glFormat = gl.GL_RGBA;
                glType = gl.GL_UNSIGNED_BYTE;
            }

            Texture ret = new Texture();

            uint tex;
            gl.GenTextures(1, &tex);
            ret.myName = tex;

            gl.BindTexture(gl.GL_TEXTURE_2D, ret.myName);

            gl.TexImage2D(gl.GL_TEXTURE_2D, 0, glFormat, size.Width, size.Height, 0, glFormat, glType, loadData.Data.Scan0);

            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MIN_FILTER, gl.GL_LINEAR);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MAG_FILTER, gl.GL_LINEAR);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_S, gl.GL_CLAMP_TO_EDGE);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_T, gl.GL_CLAMP_TO_EDGE);

            ret.myWidth = loadData.Width;
            ret.myHeight = size.Height;
            ret.myIsTransparent = isTransparent;

            return ret;
        }

        public static void EndLoadBitmap(BitmapLoadData loadData)
        {
            loadData.Bitmap.UnlockBits(ref loadData.Data);
            Marshal.FinalReleaseComObject(loadData.Bitmap);
        }

        public static Texture LoadStream(Stream bitmapStream, bool isTransparent)
        {
            BitmapLoadData loadData = BeginLoadBitmap(bitmapStream, isTransparent);
            Texture ret = LoadBitmap(loadData);
            EndLoadBitmap(loadData);
            return ret;
        }

        #region IDisposable Members

        unsafe public void Dispose()
        {
            uint name = myName;
            gl.DeleteTextures(1, &name);
        }

        #endregion
    }
}