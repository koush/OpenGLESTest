using System;
using OpenGLES;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TestOpenGLES
{
    public partial class TestForm : Form
    {
        [DllImport("coredll")]
        extern static IntPtr GetDC(IntPtr hwnd);

        public TestForm()
        {
            InitializeComponent();

            myDisplay = egl.GetDisplay(new EGLNativeDisplayType(this));

            int major, minor;
            egl.Initialize(myDisplay, out major, out minor);

            EGLConfig[] configs = new EGLConfig[10];
            int[] attribList = new int[] 
            { 
                egl.EGL_RED_SIZE, 5, 
                egl.EGL_GREEN_SIZE, 6, 
                egl.EGL_BLUE_SIZE, 5, 
                egl.EGL_DEPTH_SIZE, 16 , 
                egl.EGL_SURFACE_TYPE, egl.EGL_WINDOW_BIT,
                egl.EGL_STENCIL_SIZE, egl.EGL_DONT_CARE,
                egl.EGL_NONE, egl.EGL_NONE 
            };

            int numConfig;
            if (!egl.ChooseConfig(myDisplay, attribList, configs, configs.Length, out numConfig) || numConfig < 1)
                throw new InvalidOperationException("Unable to choose config.");

            EGLConfig config = configs[0];
            mySurface = egl.CreateWindowSurface(myDisplay, config, Handle, null);
            myContext = egl.CreateContext(myDisplay, config, EGLContext.None, null);

            egl.MakeCurrent(myDisplay, mySurface, mySurface, myContext);
            gl.ClearColor(0, 0, 0, 0);
            
            InitGL();
        }

        OpenGLFont myHugeFont;
        OpenGLFont myFont;
        GlyphRun myLeftAligned;
        GlyphRun myCentered;
        GlyphRun myJustified;
        Texture myTexture;
        int[] myFrameTracker = new int[30];
        int myLastFrame = 0;
        float myFps = 0;
        void InitGL()
        {
            gl.ShadeModel(gl.GL_SMOOTH);
            gl.ClearColor(0.0f, 0.0f, 0.0f, 0.5f);
            gl.ClearDepthf(1.0f);
            //gl.Enable(gl.GL_DEPTH_TEST);
            gl.BlendFunc(gl.GL_SRC_ALPHA, gl.GL_ONE_MINUS_SRC_ALPHA);
            gl.DepthFunc(gl.GL_LEQUAL);
            gl.Hint(gl.GL_PERSPECTIVE_CORRECTION_HINT, gl.GL_NICEST);
            myFont = new OpenGLFont(new Font(FontFamily.GenericSerif, 12, FontStyle.Regular));
            myHugeFont = new OpenGLFont(new Font(FontFamily.GenericSerif, 32, FontStyle.Regular));
            myLeftAligned = new GlyphRun(myHugeFont, "hello world", float.PositiveInfinity, float.PositiveInfinity, OpenGLTextAlignment.Left, true);
            myCentered = new GlyphRun(myFont, "centered text", ClientSize.Width, ClientSize.Height, OpenGLTextAlignment.Center, true);
            myJustified = new GlyphRun(myFont, "this text blob is really long and hopefully it will be justified to the edges as the lines wrap and stuff", ClientSize.Width, ClientSize.Height, OpenGLTextAlignment.Justified, true);
            myTexture = Texture.LoadStream(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("TestOpenGLES.BitmapBrush.bmp"), false);

            myLeftAligned.ApplyTextureShader((glyphPos) =>
                {
                    float left = glyphPos.TopLeft.X / myLeftAligned.Width;
                    float top = glyphPos.TopLeft.Y / myLeftAligned.Height;
                    float right = glyphPos.TopRight.X / myLeftAligned.Width;
                    float bottom = glyphPos.BottomLeft.Y / myLeftAligned.Height;
                    return new GlyphTexCoords(left, top, right, bottom);
                }
            );

            myLeftAligned.Texture = myTexture;

            myJustified.ApplyColorShader((glyphPos) =>
                {
                    GlyphColors colors = new GlyphColors();
                    colors.TopLeft = new Vector4f(1, 0, 0, 1);
                    colors.BottomLeft = new Vector4f(0, 1, 0, 1);
                    colors.TopRight = new Vector4f(1, 0, 0, 1);
                    colors.BottomRight = new Vector4f(0, 0, 1, 1);

                    return colors;
                }
            );
        }

        float myRotation = 0;
        unsafe void DrawGLScene()
        {
            int count;
            gl.GetIntegerv(gl.GL_MAX_TEXTURE_UNITS, &count);

            gl.Viewport(ClientRectangle.Left, ClientRectangle.Top, ClientRectangle.Width, ClientRectangle.Height);
            gl.MatrixMode(gl.GL_PROJECTION);
            gl.LoadIdentity();
            gluPerspective(45, (float)ClientSize.Width / (float)ClientSize.Height, .1f, 100);

            gl.MatrixMode(gl.GL_MODELVIEW);
            gl.LoadIdentity(); 
            
            float[] triangle = new float[] { 0.0f, 1.0f, 0.0f, -1.0f, -1.0f, 0.0f, 1.0f, -1.0f, 0.0f };
            float[] colors = new float[] { 1.0f, 0.0f, 0.0f, 9, 0.0f, 1.0f, 0.0f, 0, 0.0f, 0.0f, 1.0f, 0 };

            gl.LoadIdentity();
            gl.Translatef(0.0f, 0.0f, -6.0f);
            gl.Rotatef(myRotation, 0.0f, 1.0f, 0.0f);

            fixed (float* trianglePointer = &triangle[0], colorPointer = &colors[0])
            {
                gl.EnableClientState(gl.GL_VERTEX_ARRAY);
                gl.VertexPointer(3, gl.GL_FLOAT, 0, (IntPtr)trianglePointer);
                gl.EnableClientState(gl.GL_COLOR_ARRAY);
                gl.ColorPointer(4, gl.GL_FLOAT, 0, (IntPtr)colorPointer);

                gl.DrawArrays(gl.GL_TRIANGLES, 0, 3);
                gl.DisableClientState(gl.GL_VERTEX_ARRAY);
                gl.DisableClientState(gl.GL_COLOR_ARRAY);
                gl.Flush();
            }

            myRotation += 2f;

            gl.MatrixMode(gl.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Orthof(0, ClientSize.Width, ClientSize.Height, 0, -10, 10);

            gl.MatrixMode(gl.GL_MODELVIEW);
            gl.LoadIdentity();

            myLeftAligned.Draw();

            gl.Translatef(0, 80, 0);
            myCentered.Draw();
            GlyphRun fpsText = new GlyphRun(myFont, Math.Round(myFps, 2).ToString() + " FPS");
            fpsText.Draw();
            gl.Translatef(0, 80, 0);
            myJustified.Draw();
        }


        void gluPerspective(float fovy, float aspect, float zNear, float zFar)
        {
            float xmin, xmax, ymin, ymax;

            ymax = zNear * (float)Math.Tan(fovy * 3.1415962f / 360.0);
            ymin = -ymax;
            xmin = ymin * aspect;
            xmax = ymax * aspect;

            gl.Frustumf(xmin, xmax, ymin, ymax, zNear, zFar);
        }


        EGLDisplay myDisplay;
        EGLSurface mySurface;
        EGLContext myContext;

        private void myExitMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            DrawGLScene();
            egl.SwapBuffers(myDisplay, mySurface);
            gl.Clear(gl.GL_COLOR_BUFFER_BIT | gl.GL_DEPTH_BUFFER_BIT);
            Invalidate();

            int tickCount = Environment.TickCount;
            int nextFrame = (myLastFrame + 1) % myFrameTracker.Length;
            // elapsed is how long it took to draw 30 frames
            float elapsed = (tickCount - myFrameTracker[nextFrame]) / 1000f;
            float timePerFrame = elapsed / 30;
            myFps = 1 / timePerFrame;
            myLastFrame = nextFrame;
            myFrameTracker[nextFrame] = tickCount;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!egl.DestroySurface(myDisplay, mySurface))
                throw new Exception("Error while destroying surface.");
            if (!egl.DestroyContext(myDisplay, myContext))
                throw new Exception("Error while destroying context.");
            if (!egl.Terminate(myDisplay))
                throw new Exception("Error while terminating display.");
            base.OnClosing(e);
        }
    }
}