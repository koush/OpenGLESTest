using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace OpenGLES
{
    public class OpenGLFont : IDisposable
    {
        [DllImport("coredll")]
        extern static bool GetCharWidth32(IntPtr hdc, int iFirstChar, int iLastChar, [Out] int[] lpBuffer);

        [DllImport("coredll")]
        extern static IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("coredll")]
        extern static bool GetTextMetrics(IntPtr hdc, ref tagTEXTMETRIC lptm);

        readonly static Bitmap myTempBitmap = new Bitmap(1, 1, PixelFormat.Format16bppRgb565);
        readonly static Graphics myTempGraphics = Graphics.FromImage(myTempBitmap);

        readonly static string myCharactersOfInterest = string.Empty;
        const char myFirstCharacterOfInterest = ' ';
        const char myLastCharacterOfInterest = '~';

        internal int myLeadingSpace;
        internal int myTrailingSpace;
        internal uint myName;
        internal GlyphTexCoords[] TextureCoordinates = new GlyphTexCoords[256];
        internal int[] CharacterWidths = new int[256];
        internal Point[] CharacterLocations = new Point[256];
        internal int mySquareDim;
        internal int myHeight;

        static OpenGLFont()
        {
            for (char i = myFirstCharacterOfInterest; i <= myLastCharacterOfInterest; i++)
            {
                myCharactersOfInterest += i;
            }
        }

        unsafe public OpenGLFont(Font font)
        {

            IntPtr hdc = myTempGraphics.GetHdc();
            IntPtr hfont = font.ToHfont();
            SelectObject(hdc, hfont);

            if (!GetCharWidth32(hdc, 0, 255, CharacterWidths))
                throw new SystemException("Unable to measure character widths.");

            tagTEXTMETRIC metrics = new tagTEXTMETRIC();
            GetTextMetrics(hdc, ref metrics);
            myLeadingSpace = metrics.tmInternalLeading;
            myTrailingSpace = metrics.tmExternalLeading;

            myTempGraphics.ReleaseHdc(hdc);

            int width = 0;
            for (int i = myFirstCharacterOfInterest; i <= myLastCharacterOfInterest; i++)
            {
                CharacterWidths[i] += myLeadingSpace + myTrailingSpace;
                width += CharacterWidths[i];
            }
            myHeight = (int)Math.Round(myTempGraphics.MeasureString(myCharactersOfInterest, font).Height);

            mySquareDim = (int)Math.Ceiling(Math.Sqrt(width * myHeight));
            mySquareDim = Texture.GetValidTextureDimensionFromSize(mySquareDim);
            float fSquareDim = mySquareDim;
            Bitmap bitmap = new Bitmap(mySquareDim, mySquareDim, PixelFormat.Format16bppRgb565);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                int x = 0;
                int y = 0;

                for (char i = myFirstCharacterOfInterest; i <= myLastCharacterOfInterest; i++)
                {
                    if (x + CharacterWidths[i] >= mySquareDim)
                    {
                        y += myHeight;
                        x = 0;
                    }
                    CharacterLocations[i] = new Point(x, y);
                    
                    float uStart = x / fSquareDim;
                    float uEnd = (x + CharacterWidths[i]) / fSquareDim;
                    float vStart = y / fSquareDim;
                    float vEnd = (y + myHeight) / fSquareDim;
                    TextureCoordinates[i] = new GlyphTexCoords(uStart, vStart, uEnd, vEnd);
                    
                    g.DrawString(i.ToString(), font, myWhiteBrush, x, y);
                    x += CharacterWidths[i];
                }
            }

            byte[] alphaBytes = new byte[bitmap.Width * bitmap.Height];
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format16bppRgb565);

            int pixCount = 0;
            for (int y = 0; y < bitmap.Height; y++)
            {
                short* yp = (short*)((int)data.Scan0 + data.Stride * y);
                for (int x = 0; x < bitmap.Width; x++, pixCount++)
                {
                    short* p = (short*)(yp + x);
                    short pixel = *p;
                    byte b = (byte)((pixel & 0x1F) << 3);
                    byte g = (byte)(((pixel >> 5) & 0x3F) << 2);
                    byte r = (byte)(((pixel >> 11) & 0x1F) << 3);
                    byte totalAlpha = (byte)((r + g + b) / 3);
                    alphaBytes[pixCount] = totalAlpha;
                }
            }
            bitmap.UnlockBits(data);

            uint tex = 0;
            gl.GenTextures(1, &tex);
            myName = tex;
            gl.BindTexture(gl.GL_TEXTURE_2D, myName);

            fixed (byte* alphaBytesPointer = alphaBytes)
            {
                gl.TexImage2D(gl.GL_TEXTURE_2D, 0, gl.GL_ALPHA, mySquareDim, mySquareDim, 0, gl.GL_ALPHA, gl.GL_UNSIGNED_BYTE, (IntPtr)alphaBytesPointer);
            }

            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MIN_FILTER, gl.GL_LINEAR);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_MAG_FILTER, gl.GL_LINEAR);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_S, gl.GL_CLAMP_TO_EDGE);
            gl.TexParameteri(gl.GL_TEXTURE_2D, gl.GL_TEXTURE_WRAP_T, gl.GL_CLAMP_TO_EDGE);

            // below is debug code I used to see the results of my texture generation
            
            //try
            //{
            //    Directory.CreateDirectory("\\Temp");
            //}
            //catch (Exception)
            //{
            //}
            //bitmap.Save("\\temp\\temp.png", ImageFormat.Png);

            //for (int i = myFirstCharacterOfInterest; i <= myLastCharacterOfInterest; i++)
            //{
            //    using (Bitmap ch = new Bitmap(bfont.CharacterWidths[i], height, PixelFormat.Format16bppRgb565))
            //    {
            //        using (Graphics tg = Graphics.FromImage(ch))
            //        {
            //            tg.DrawImage(bitmap, 0, 0, new Rectangle(bfont.CharacterLocations[i].X, bfont.CharacterLocations[i].Y, ch.Width, ch.Height), GraphicsUnit.Pixel);
            //        }
            //        ch.Save(string.Format("\\temp\\{0}.png", i), ImageFormat.Png);
            //    }
            //}

            bitmap.Dispose();
        }
        
        readonly static SolidBrush myWhiteBrush = new SolidBrush(Color.White);

        #region IDisposable Members

        public void Dispose()
        {
            
        }

        #endregion
    }

    struct tagTEXTMETRIC
    {
        public int tmHeight;
        public int tmAscent;
        public int tmDescent;
        public int tmInternalLeading;
        public int tmExternalLeading;
        public int tmAveCharWidth;
        public int tmMaxCharWidth;
        public int tmWeight;
        public int tmOverhang;
        public int tmDigitizedAspectX;
        public int tmDigitizedAspectY;
        public byte tmFirstChar;
        public byte tmLastChar;
        public byte tmDefaultChar;
        public byte tmBreakChar;
        public byte tmItalic;
        public byte tmUnderlined;
        public byte tmStruckOut;
        public byte tmPitchAndFamily;
        public byte tmCharSet;
    };

    public struct Vector2f
    {
        public float X;
        public float Y;
    }

    public struct GlyphPosition
    {
        public Vector3f BottomLeft;
        public Vector3f TopLeft;
        public Vector3f BottomRight;
        public Vector3f TopRight;

        public GlyphPosition(float left, float top, float width, float height)
        {
            BottomLeft = new Vector3f();
            TopRight = new Vector3f();
            TopLeft = new Vector3f();
            BottomRight = new Vector3f();

            float right = left + width;
            float bottom = top + height;

            TopLeft.X = BottomLeft.X = left;
            TopLeft.Y = TopRight.Y = top;
            TopRight.X = BottomRight.X = right;
            BottomLeft.Y = BottomRight.Y = bottom;

            TopLeft.Z = TopRight.Z = BottomLeft.Z = BottomRight.Z = 0;
        }
    }

    public struct GlyphTexCoords
    {
        public Vector2f BottomLeft;
        public Vector2f TopLeft;
        public Vector2f BottomRight;
        public Vector2f TopRight;

        public GlyphTexCoords(float left, float top, float right, float bottom)
        {
            BottomLeft = new Vector2f();
            TopRight = new Vector2f();
            TopLeft = new Vector2f();
            BottomRight = new Vector2f(); 
            
            TopLeft.X = BottomLeft.X = left;
            TopLeft.Y = TopRight.Y = top;
            TopRight.X = BottomRight.X = right;
            BottomLeft.Y = BottomRight.Y = bottom;
        }
    }

    public struct GlyphColors
    {
        public Vector4f BottomLeft;
        public Vector4f TopLeft;
        public Vector4f BottomRight;
        public Vector4f TopRight;
    }

    public enum OpenGLTextAlignment
    {
        Left,
        Center,
        Right,
        Justified
    }

    public delegate T VertexShaderHandler<T>(GlyphPosition glyph);

    public class GlyphRun
    {
        public OpenGLFont Font;
        public GlyphPosition[] Glyphs;
        public GlyphTexCoords[] FontCoords;
        public short[] Indices;
        public Texture Texture;
        public GlyphTexCoords[] TextureCoordinates;
        public GlyphColors[] Colors;

        void ApplyShader<T>(ref T[] array, VertexShaderHandler<T> shader)
        {
            if (array == null)
                array = new T[Glyphs.Length];

            for (int i = 0; i < Glyphs.Length; i++)
            {
                array[i] = shader(Glyphs[i]);
            }
        }

        public void ApplyColorShader(VertexShaderHandler<GlyphColors> colorShader)
        {
            ApplyShader(ref Colors, colorShader);
        }

        public void ApplyTextureShader(VertexShaderHandler<GlyphTexCoords> textureShader)
        {
            ApplyShader(ref TextureCoordinates, textureShader);
        }

        static int FindWhitespace(string text, int startIndex)
        {
            int space = text.IndexOf(' ', startIndex);
            int linebreak = text.IndexOf('\n', startIndex);
            if (space == -1)
                return linebreak;
            else if (linebreak == -1)
                return space;
            else
                return Math.Min(space, linebreak);
        }

        int MeasureString(string text)
        {
            int ret = 0;
            foreach (char c in text)
            {
                ret += Font.CharacterWidths[c] - Font.myLeadingSpace - Font.myTrailingSpace;
            }
            return ret;
        }

        struct LineBreak
        {
            public string Text;
            public int Index;
            public int Width;
            public int SpaceCount;
        };


        LineBreak FitString(string text, int startIndex, int width)
        {
            LineBreak lineBreak = new LineBreak();
            lineBreak.Width = 0;
            int dims = 0;

            int currentIndex = FindWhitespace(text, startIndex);
            if (currentIndex == -1)
                currentIndex = text.Length;
            int lastIndex = startIndex;

            while (currentIndex != -1 && (dims = MeasureString(text.Substring(startIndex, currentIndex - startIndex))) <= width)
            {
                // record the width while succesfully fit
                lineBreak.Width = dims;

                // done
                if (currentIndex == text.Length)
                {
                    lastIndex = currentIndex;
                    currentIndex = -1;
                }
                else if (text[currentIndex] == '\n')
                {
                    lastIndex = currentIndex + 1;
                    currentIndex = -1;
                }
                else
                {
                    // get next word
                    lastIndex = currentIndex + 1;
                    currentIndex = FindWhitespace(text, lastIndex);
                    // end of string
                    if (currentIndex == -1)
                        currentIndex = text.Length;
                }
            }

            if (lastIndex == startIndex)
            {
                // the string was either too long or we are at the end of the string
                if (currentIndex == -1)
                    throw new Exception("Somehow executing unreachable code while drawing text.");

                currentIndex = lastIndex + 1;
                while ((dims = MeasureString(text.Substring(startIndex, currentIndex - startIndex))) <= width)
                {
                    lineBreak.Width = dims;
                    currentIndex++;
                }
                lineBreak.Width = Math.Min(dims, width);
                lineBreak.Text = text.Substring(startIndex, currentIndex - startIndex);
                lineBreak.Index = currentIndex;
                return lineBreak;
            }
            else
            {
                // return the index we're painting to
                lineBreak.Text = text.Substring(startIndex, lastIndex - startIndex);
                lineBreak.Index = lastIndex;
                return lineBreak;
            }
        }

        void BuildLine(OpenGLFont font, string text, int startIndex, float x, float y, float spaceAdjust)
        {
            for (int i = 0; i < text.Length; i++, startIndex++)
            {
                char c = text[i];
                Glyphs[startIndex] = new GlyphPosition(x, y, font.CharacterWidths[c], font.myHeight);
                FontCoords[startIndex] = font.TextureCoordinates[c];
                x += font.CharacterWidths[c] - font.myLeadingSpace - font.myTrailingSpace;
                if (c == ' ')
                    x += spaceAdjust;

                short stripindex = (short)(startIndex * 4);
                short index = (short)(startIndex * 6);
                Indices[index] = stripindex;
                Indices[index + 1] = (short)(stripindex + 1);
                Indices[index + 2] = (short)(stripindex + 2);
                Indices[index + 3] = (short)(stripindex + 2);
                Indices[index + 4] = (short)(stripindex + 1);
                Indices[index + 5] = (short)(stripindex + 3);
            }
        }

        float myWidth;
        public float Width
        {
            get
            {
                return myWidth;
            }
        }
        float myHeight;
        public float Height
        {
            get
            {
                return myHeight;
            }
        }

        int myTriangleCount;
        public int TriangleCount
        {
            get
            {
                return myTriangleCount;
            }
        }

        public GlyphRun(OpenGLFont font, string text)
            : this(font, text, float.PositiveInfinity, float.PositiveInfinity, OpenGLTextAlignment.Left, true)
        {
        }

        public GlyphRun(OpenGLFont font, string text, float sizeWidth, float sizeHeight, OpenGLTextAlignment alignment, bool autoEllipsis)
        {
            Font = font;
            List<LineBreak> linebreaks = new List<LineBreak>();

            string processingText = text;
            int totalHeight = 0;
            int totalWidth = 0;
            int totalChars = 0;
            int intWidth;
            if (sizeWidth != float.PositiveInfinity)
                intWidth = (int)sizeWidth;
            else
                intWidth = int.MaxValue;
            int intHeight;
            if (sizeHeight != float.PositiveInfinity)
                intHeight = (int)sizeHeight;
            else
                intHeight = int.MaxValue;
            LineBreak lineBreak = FitString(processingText, 0, intWidth);

            while (lineBreak.Index != processingText.Length)
            {
                LineBreak nextBreak = FitString(processingText, lineBreak.Index, intWidth);
                // see if this line needs ellipsis
                if (Font.myHeight + Font.myHeight + totalHeight > intHeight && autoEllipsis)
                {
                    string lineText = lineBreak.Text;
                    int ellipsisStart = lineText.Length - 3;
                    if (ellipsisStart < 0)
                        ellipsisStart = 0;
                    lineText = lineText.Substring(0, ellipsisStart) + "...";
                    lineBreak.Width = MeasureString(lineText);
                    lineBreak.Text = lineText;
                    break;
                }

                linebreaks.Add(lineBreak);
                totalWidth = Math.Max(totalWidth, lineBreak.Width);
                totalHeight += Font.myHeight;
                totalChars += lineBreak.Text.Length;
                lineBreak = nextBreak;
            }
            linebreaks.Add(lineBreak);
            totalHeight += Font.myHeight;
            totalWidth = Math.Max(totalWidth, lineBreak.Width);
            totalChars += lineBreak.Text.Length;
            myTriangleCount = totalChars * 2;

            GlyphPosition[] rectangles = new GlyphPosition[totalChars];
            GlyphTexCoords[] texCoords = new GlyphTexCoords[totalChars];
            short[] indices = new short[totalChars * 6];

            Glyphs = rectangles;
            FontCoords = texCoords;
            Indices = indices;

            if (sizeWidth == float.PositiveInfinity)
                myWidth = totalWidth;
            else 
                myWidth = sizeWidth;
            if (sizeHeight == float.PositiveInfinity)
                myHeight = totalHeight;
            else
                myHeight = sizeHeight;

            float y = 0;
            int curChars = 0;
            for (int i = 0 ; i < linebreaks.Count; i++)
            {
                LineBreak lbreak = linebreaks[i];
                float x;
                float spaceAdjust = 0;
                string lbreakText = lbreak.Text;
                switch (alignment)
                {
                    case OpenGLTextAlignment.Left:
                        x = 0;
                        break;
                    case OpenGLTextAlignment.Right:
                        x = myWidth - lbreak.Width;
                        break;
                    case OpenGLTextAlignment.Center:
                        x = (myWidth - lbreak.Width) / 2;
                        break;
                    case OpenGLTextAlignment.Justified:
                        x = 0;
                        if (i != linebreaks.Count - 1)
                        {
                            lbreakText = lbreakText.TrimStart(' ').TrimEnd(' ');
                            int spaceCount = 0;
                            foreach (char c in lbreakText)
                            {
                                if (c == ' ')
                                    spaceCount++;
                            }
                            int newWidth = MeasureString(lbreakText);
                            if (spaceCount != 0)
                                spaceAdjust = (myWidth - newWidth) / spaceCount;
                        }
                        break;
                    default:
                        throw new ArgumentException("Unknown alignment type.");
                }

                BuildLine(font, lbreakText, curChars, x, y, spaceAdjust);
                y += Font.myHeight;
                curChars += lbreakText.Length;
            }
        }

        unsafe public void Draw()
        {
            gl.EnableClientState(gl.GL_VERTEX_ARRAY);
            gl.EnableClientState(gl.GL_TEXTURE_COORD_ARRAY);

            gl.Enable(gl.GL_BLEND);
            gl.Enable(gl.GL_TEXTURE_2D);
            gl.BindTexture(gl.GL_TEXTURE_2D, Font.myName);

            fixed (GlyphPosition* positionPointer = Glyphs)
            {
                fixed (GlyphTexCoords* fontCoordPointer = FontCoords, texCoordPointer = TextureCoordinates)
                {
                    fixed (short* indexPointer = Indices)
                    {
                        fixed (GlyphColors* colorPointer = Colors)
                        {
                            if (texCoordPointer != null)
                            {
                                gl.ActiveTexture(gl.GL_TEXTURE1);
                                gl.ClientActiveTexture(gl.GL_TEXTURE1);
                                gl.EnableClientState(gl.GL_TEXTURE_COORD_ARRAY);
                                gl.Enable(gl.GL_TEXTURE_2D);
                                gl.BindTexture(gl.GL_TEXTURE_2D, Texture.Name);
                                gl.TexCoordPointer(2, gl.GL_FLOAT, 0, (IntPtr)texCoordPointer);
                                gl.ActiveTexture(gl.GL_TEXTURE0);
                                gl.ClientActiveTexture(gl.GL_TEXTURE0);
                            }
                            if (colorPointer != null)
                            {
                                gl.EnableClientState(gl.GL_COLOR_ARRAY);
                                gl.ColorPointer(4, gl.GL_FLOAT, 0, (IntPtr)colorPointer);
                            }

                            gl.VertexPointer(3, gl.GL_FLOAT, 0, (IntPtr)positionPointer);
                            gl.TexCoordPointer(2, gl.GL_FLOAT, 0, (IntPtr)fontCoordPointer);
                            gl.DrawElements(gl.GL_TRIANGLES, Indices.Length, gl.GL_UNSIGNED_SHORT, (IntPtr)indexPointer);

                            if (colorPointer != null)
                                gl.DisableClientState(gl.GL_COLOR_ARRAY);

                            if (texCoordPointer != null)
                            {
                                gl.ActiveTexture(gl.GL_TEXTURE1);
                                gl.Disable(gl.GL_TEXTURE_2D);
                                gl.ActiveTexture(gl.GL_TEXTURE0);
                            }
                        }
                    }
                }
            }

            gl.DisableClientState(gl.GL_TEXTURE_COORD_ARRAY);
            gl.DisableClientState(gl.GL_VERTEX_ARRAY);

            gl.Disable(gl.GL_TEXTURE_2D);
            gl.Disable(gl.GL_BLEND);
        }
    }
}
