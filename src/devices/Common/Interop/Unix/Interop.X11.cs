using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SA1132 // Every member should be declared on its own line
#pragma warning disable SA1307 // 'x' should start with an upper-case letter

partial class Interop
{
    internal const string X11 = "libX11.so";
    // Definitions for this file come from X.h and Xlib.h from the Raspberry Pi (32 Bit Raspberry Pi OS)
    // Some structures may have different alignment on 64 bit systems, so that it may be required to replace
    // some field types with nint/nuint
    internal static UInt32 AllPlanes = 0xFFFF_FFFF;

    internal static UInt32 XYBitmap = 0; /* depth 1, XYFormat */
    internal static UInt32 XYPixmap = 1; /* depth == drawable depth */
    internal static UInt32 ZPixmap = 2; /* depth == drawable depth */

    /// <summary>
    /// Opens the display and returns an image pointer
    /// </summary>
    /// <param name="displayName">The display name (can be null)</param>
    /// <returns>The raw image pointer. To get the image data, use <see cref="Marshal.PtrToStructure{XImage}(IntPtr)"/>. The raw pointer is required
    /// in the call to <see cref="XDestroyImage"/></returns>
    [DllImport(X11, CharSet = CharSet.Ansi)]
    internal static extern unsafe IntPtr XOpenDisplay(char* displayName);

    [DllImport(X11)]
    internal static extern unsafe void XCloseDisplay(IntPtr display);

    [DllImport(X11)]
    internal static extern unsafe IntPtr XDefaultRootWindow(IntPtr display);

    [DllImport(X11)]
    internal static extern int XGetWindowAttributes(
        IntPtr display,
        IntPtr w,
        ref XWindowAttributes window_attributes_return);

    [DllImport(X11)]
    internal static extern UInt32 XGetPixel(XImage image, int x, int y);

    /// <summary>
    /// Free the image.
    /// </summary>
    /// <param name="image">Raw image pointer</param>
    /// <returns>Error code</returns>
    [DllImport(X11)]
    internal static extern int XDestroyImage(IntPtr image);

    [DllImport(X11)]
    internal static extern int XFree(XImage image);

    [DllImport(X11)]
    internal static extern IntPtr XGetImage(
        IntPtr display,
        IntPtr d, // Window
        int x,
        int y,
        UInt32 width,
        UInt32 height,
        UInt32 plane_mask,
        UInt32 format);

    [StructLayout(LayoutKind.Sequential)]
    internal struct XWindowAttributes
    {
        public int x, y;           /* location of window */
        public int width, height;      /* width and height of window */
        public int border_width;       /* border width of window */
        public int depth;              /* depth of window */
        public IntPtr visual;     /* the associated visual structure */
        public IntPtr root;            /* root of screen containing window */
        public int c_class;          /* InputOutput, InputOnly*/
        public int bit_gravity;        /* one of bit gravity values */
        public int win_gravity;        /* one of the window gravity values */
        public int backing_store;      /* NotUseful, WhenMapped, Always */
        public uint backing_planes; /* planes to be preserved if possible */
        public uint backing_pixel; /* value to be used when restoring planes */
        public bool save_under;        /* boolean, should bits under be saved? */
        public uint colormap;      /* color map to be associated with window */
        public bool map_installed;     /* boolean, is color map currently installed*/
        public int map_state;      /* IsUnmapped, IsUnviewable, IsViewable */
        public int all_event_masks;   /* set of events all people have interest in*/
        public int your_event_mask;   /* my event mask */
        public int do_not_propagate_mask; /* set of events that should not propagate */
        public bool override_redirect; /* boolean value for override-redirect */
        public IntPtr screen;     /* back pointer to correct screen */
    }

    internal unsafe delegate XImage CreateImage(IntPtr display,
        IntPtr visual, UInt32 depth, int format, int offset, IntPtr data, UInt32 width, UInt32 height, int bitmap_pad, int bytes_per_line);

    internal unsafe delegate int DestroyImage(XImage image);

    internal unsafe delegate UInt32 GetPixel(XImage image, int x, int y);

    internal unsafe delegate int PutPixel(XImage image, int x, int y, UInt32 color);

    internal unsafe delegate XImage SubImage(XImage image, int x, int y, UInt32 width, UInt32 height);

    internal unsafe delegate int AddPixel(XImage image, int x, int y, UInt32 color);

    [StructLayout(LayoutKind.Sequential)]
    internal class XImage
    {
        public int width, height;      /* size of image */
        public int xoffset;        /* number of pixels offset in X direction */
        public int format;         /* XYBitmap, XYPixmap, ZPixmap */
        public IntPtr data;         /* pointer to image data */
        public int byte_order;     /* data byte order, LSBFirst, MSBFirst */
        public int bitmap_unit;        /* quant. of scanline 8, 16, 32 */
        public int bitmap_bit_order;   /* LSBFirst, MSBFirst */
        public int bitmap_pad;     /* 8, 16, 32 either XY or ZPixmap */
        public int depth;          /* depth of image */
        public int bytes_per_line;     /* accelarator to next line */
        public int bits_per_pixel;     /* bits per pixel (ZPixmap) */
        public UInt32 red_mask; /* bits in z arrangment */
        public UInt32 green_mask;
        public UInt32 blue_mask;
        public IntPtr obdata;        /* hook for the object routines to hang on */
        public ImageFuncs funcs;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ImageFuncs
    {
        /* image manipulation routines */
        public CreateImage createImage;
        public DestroyImage destroyImage;
        public GetPixel getPixel;
        public PutPixel putPixel;
        public SubImage subImage;
        public AddPixel addPixel;
    }
}
