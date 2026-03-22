using System.Runtime.InteropServices;

namespace UsbUirtRenamer
{
    public static class UUIRT
    {
        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr UUIRTOpen();

        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UUIRTClose(IntPtr handle);

        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern bool UUIRTEEProgram(IntPtr handle, byte[] buffer, int length );
    }
}
