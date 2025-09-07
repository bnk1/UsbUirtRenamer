using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Runtime.CompilerServices;                 // for CallConvStdcall
using System.Runtime.InteropServices.Marshalling;      // (optional; for StringMarshalling constants)


namespace UsbUirtRenamer
{
    public partial class Form1 : Form
    {
        // Interface GUIDs (HID + generic USB device) - we enumerate devnodes and interfaces
        private static readonly Guid GUID_DEVINTERFACE_HID = new("4D1E55B2-F16F-11CF-88CB-001111000030");
        private static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new("A5DCBF10-6530-11D2-901F-00C04FB951ED");

        // --- DEVPROPKEY: Bus-reported device description (firmware product string / "evalName") ---
        [StructLayout(LayoutKind.Sequential)]
        private struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }
        // DEVPKEY_Device_BusReportedDeviceDesc
        private static readonly DEVPROPKEY DEVPKEY_Device_BusReportedDeviceDesc =
            new DEVPROPKEY { fmtid = new Guid("540B947E-8B40-45BC-A8A2-6A0B894CBDA2"), pid = 4 };

        public Form1()
        {
            InitializeComponent();
            sslAdmin.Text = $"Admin: {(Program.IsAdministrator() ? "yes" : "no")}";
            RefreshDevices();
        }

        private void BtnRefresh_Click(object? sender, EventArgs e) => RefreshDevices();
        private void BtnRename_Click(object? sender, EventArgs e) => RenameSelected(txtNewName.Text.Trim());

        private void LvDevices_DoubleClick(object? sender, EventArgs e)
        {
            if (lvDevices.SelectedItems.Count == 0)
                return;
            var current = lvDevices.SelectedItems[0].SubItems[lvDevices.Columns.IndexOf(colFriendly)].Text;
            string? newName = Prompt($"New name for:\n{current}", current);
            if (!string.IsNullOrWhiteSpace(newName))
                RenameSelected(newName!);
        }

        private void BtnElevate_Click(object? sender, EventArgs e)
        {
            try
            {
                var exe = Application.ExecutablePath;
                var psi = new ProcessStartInfo(exe) { UseShellExecute = true, Verb = "runas" };
                Process.Start(psi);
                Application.Exit();
            }
            catch (Win32Exception) { sslStatus.Text = "Elevation canceled."; }
        }

        // Context menu hooks
        private void MiCopyInstanceId_Click(object? sender, EventArgs e)
        {
            if (lvDevices.SelectedItems.Count == 0)
                return;
            string text = lvDevices.SelectedItems[0].SubItems[lvDevices.Columns.IndexOf(colInstanceId)].Text;
            if (!string.IsNullOrEmpty(text))
                Clipboard.SetText(text);
        }
        private void MiCopyVidPid_Click(object? sender, EventArgs e)
        {
            if (lvDevices.SelectedItems.Count == 0)
                return;
            var item = lvDevices.SelectedItems[0];
            string hwid = item.SubItems[lvDevices.Columns.IndexOf(colHwId)].Text;
            string inst = item.SubItems[lvDevices.Columns.IndexOf(colInstanceId)].Text;
            Clipboard.SetText(BuildDeviceIdString(hwid, inst));
        }

        private void RefreshDevices()
        {
            if (InvokeRequired)
            { BeginInvoke(new Action(RefreshDevices)); return; }

            try
            {
                Cursor = Cursors.WaitCursor;
                sslStatus.Text = "Enumerating devices…";

                var all = EnumerateAllDevicesIncludingInterfaces();

                string nameFilter = txtFilterName.Text.Trim();
                string vidHex = txtVid.Text.Trim();
                string pidHex = txtPid.Text.Trim();

                IEnumerable<DeviceInfo> q = all;

                if (!string.IsNullOrWhiteSpace(nameFilter))
                {
                    q = q.Where(d =>
                        ((d.FriendlyName ?? d.DeviceDesc) ?? string.Empty).Contains(nameFilter, StringComparison.OrdinalIgnoreCase)
                      || (d.BusReportedName ?? string.Empty).Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
                }

                if (TryParseHex(vidHex, out int vid))
                {
                    string vidStr = $"VID_{vid:X4}";
                    q = q.Where(d => (d.HardwareId ?? string.Empty).Contains(vidStr, StringComparison.OrdinalIgnoreCase));
                }

                if (TryParseHex(pidHex, out int pid))
                {
                    string pidStr = $"PID_{pid:X4}";
                    q = q.Where(d => (d.HardwareId ?? string.Empty).Contains(pidStr, StringComparison.OrdinalIgnoreCase));
                }

                var list = q.ToList();

                if (list.Count == 0 && (!string.IsNullOrWhiteSpace(nameFilter) || !string.IsNullOrWhiteSpace(vidHex) || !string.IsNullOrWhiteSpace(pidHex)))
                {
                    list = [.. all];
                    sslStatus.Text = $"No matches for current filter — showing all {list.Count} device(s).";
                }

                // Prefer bus-reported name (what uurename.exe changes) for display if present
                foreach (var d in list)
                {
                    if (!string.IsNullOrWhiteSpace(d.BusReportedName) &&
                        d.BusReportedName.StartsWith("USB-UIRT", StringComparison.OrdinalIgnoreCase))
                    {
                        d.DisplayName = d.BusReportedName;
                    }
                }

                // Cosmetic numbering for multiple UIRTs without firmware numbers
                //AutoNumberUsbUirts(list);

                // Paint ListView
                lvDevices.BeginUpdate();
                try
                {
                    lvDevices.Items.Clear();

                    for (int i = 0; i < list.Count; i++)
                    {
                        var d = list[i];

                        // Derive Unit # like the EventGhost plugin: last char of eval-ish name if digit
                        int? unit = UnitFromEvalishName(d.BusReportedName)
                                    ?? UnitFromEvalishName(d.FriendlyName)
                                    ?? UnitFromEvalishName(d.DeviceDesc);

                        string unitText = unit?.ToString() ?? string.Empty;
                        string displayName = d.DisplayName ?? d.FriendlyName ?? d.DeviceDesc ?? "(no name)";
                        string deviceIdText = BuildDeviceIdString(d.HardwareId, d.InstanceId);

                        ListViewItem item = new ListViewItem(
                        [
                            (i + 1).ToString(),  // #
                            unitText,            // Unit #
                            displayName,
                            d.DeviceDesc ?? "",
                            d.HardwareId ?? "",
                            deviceIdText,
                            d.InstanceId
                        ]);
                        item.Tag = d.InstanceId;
                        lvDevices.Items.Add(item);
                    }

                    lvDevices.View = View.Details;
                    lvDevices.UseCompatibleStateImageBehavior = false;
                    lvDevices.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                    lvDevices.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
                }
                finally { lvDevices.EndUpdate(); }

                sslStatus.Text = $"Found {list.Count} device(s).";
            }
            catch (Exception ex)
            {
                sslStatus.Text = "Error while enumerating.";
                MessageBox.Show(this, ex.Message, "Enumeration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        // Extract last char digit like the plugin: evalName[-1] if digit
        // Extract Unit # from name (like EventGhost plugin)
        private static int? UnitFromEvalishName(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            s = s.Trim();

            // Exact "USB-UIRT" (case-insensitive) means Unit #1
            if (s.Equals("USB-UIRT", StringComparison.OrdinalIgnoreCase))
                return 1;

            // If the last char is a digit, use it
            char last = s[^1];
            return char.IsDigit(last) ? (int)char.GetNumericValue(last) : (int?)null;
        }

        private void RenameSelected(string newName)
        {
            if (lvDevices.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select a device in the list.", "No selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show(this, "Enter a new friendly name (or double-click a device to prompt).",
                    "Missing name", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!Program.IsAdministrator())
            {
                MessageBox.Show(this, "Renaming requires Administrator. Click 'Restart as Admin…' and try again.",
                    "Elevation required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string? instanceId = lvDevices.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(instanceId))
            {
                MessageBox.Show(this, "Device ID not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Cursor = Cursors.WaitCursor;
                sslStatus.Text = "Renaming device…";

                bool okFriendly = SetDevPropByInstanceId(instanceId, SPDRP_FRIENDLYNAME, newName);
                bool okDesc     = SetDevPropByInstanceId(instanceId, SPDRP_DEVICEDESC, newName);

                try
                { RestartDeviceByInstanceId(instanceId); }
                catch { /* ignore */ }

                RefreshDevices();

                if (!okFriendly && !okDesc)
                    MessageBox.Show(this, "Failed to set Friendly Name or Device Description. The driver may prevent changes.",
                        "Rename failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    sslStatus.Text = "Rename complete.";
            }
            catch (Win32Exception w32)
            {
                sslStatus.Text = $"Win32 error {w32.NativeErrorCode}";
                MessageBox.Show(this, $"{w32.Message} (code {w32.NativeErrorCode})", "Win32 Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                sslStatus.Text = "Rename error.";
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        // ---------- Device ID helpers (VID/PID/Serial shown as "Device ID" column) ----------
        private static (string? vid, string? pid) ParseVidPid(string? hardwareId)
        {
            if (string.IsNullOrWhiteSpace(hardwareId))
                return (null, null);
            string upper = hardwareId.ToUpperInvariant();
            string? vid = null, pid = null;
            int vidIdx = upper.IndexOf("VID_");
            if (vidIdx >= 0 && vidIdx + 8 <= upper.Length)
                vid = upper.Substring(vidIdx + 4, 4);
            int pidIdx = upper.IndexOf("PID_");
            if (pidIdx >= 0 && pidIdx + 8 <= upper.Length)
                pid = upper.Substring(pidIdx + 4, 4);
            return (vid, pid);
        }
        private static string? ExtractSerialFromInstanceId(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
                return null;
            int slash = instanceId.LastIndexOf('\\');
            if (slash >= 0 && slash + 1 < instanceId.Length)
            {
                string tail = instanceId[(slash + 1)..];
                if (!string.IsNullOrWhiteSpace(tail))
                    return tail;
            }
            return null;
        }
        private static string BuildDeviceIdString(string? hardwareId, string instanceId)
        {
            var (vid, pid) = ParseVidPid(hardwareId);
            string? serial = ExtractSerialFromInstanceId(instanceId);
            string vp = (vid != null && pid != null) ? $"{vid}:{pid}" :
                        (vid != null) ? $"{vid}:(PID?)" :
                        (pid != null) ? $"(VID?):{pid}" : "(VID/PID?)";
            return !string.IsNullOrWhiteSpace(serial) ? $"{vp}  SN: {serial}" : vp;
        }

        #region SetupAPI interop and helpers

        private const int DIGCF_PRESENT = 0x00000002;
        private const int DIGCF_ALLCLASSES = 0x00000004;
        private const int DIGCF_DEVICEINTERFACE = 0x00000010;

        private const uint SPDRP_DEVICEDESC = 0x00000000;
        private const uint SPDRP_HARDWAREID = 0x00000001;
        private const uint SPDRP_FRIENDLYNAME = 0x0000000C;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public uint cbSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint StateChange;
            public uint Scope;
            public uint HwProfile;
        }

        private const int DIF_PROPERTYCHANGE = 0x12;
        private const uint DICS_ENABLE = 0x00000001;
        private const uint DICS_FLAG_GLOBAL = 0x00000001;

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetupDiGetClassDevs(IntPtr ClassGuid, string? Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property, out uint PropertyRegDataType, byte[] PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiSetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property, byte[] PropertyBuffer, uint PropertyBufferSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData,
            ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, out int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize,
            out int RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

        // Windows Vista+ device property API
        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDevicePropertyW(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            byte[] PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize,
            uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiSetClassInstallParams(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams, int ClassInstallParamsSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiCallClassInstaller(int InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        private sealed class DeviceInfo
        {
            public string InstanceId { get; set; } = "";
            public string? HardwareId { get; set; }
            public string? FriendlyName { get; set; }
            public string? DeviceDesc { get; set; }
            public string? DisplayName { get; set; }     // computed for grid
            public string? BusReportedName { get; set; } // firmware-level (what uurename.exe writes)
        }

        private static List<DeviceInfo> EnumerateAllDevicesIncludingInterfaces()
        {
            var list = new List<DeviceInfo>();
            list.AddRange(EnumerateByEnumerator(null));                         // all present devnodes
            list.AddRange(EnumerateInterfacesForGuid(GUID_DEVINTERFACE_HID));   // HID interfaces
            list.AddRange(EnumerateInterfacesForGuid(GUID_DEVINTERFACE_USB_DEVICE)); // USB device interfaces

            // De-dup by InstanceId; prefer entries with BusReportedName, then FriendlyName, then DeviceDesc
            return [.. list.GroupBy(d => d.InstanceId, StringComparer.OrdinalIgnoreCase)
                       .Select(g => g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.BusReportedName))
                                     .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.FriendlyName))
                                     .ThenByDescending(x => !string.IsNullOrWhiteSpace(x.DeviceDesc))
                                     .First())];
        }

        private static List<DeviceInfo> EnumerateByEnumerator(string? enumerator)
        {
            IntPtr hDevInfo = SetupDiGetClassDevs(IntPtr.Zero, enumerator, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (hDevInfo == IntPtr.Zero || hDevInfo.ToInt64() == -1)
                ThrowLastWin32("SetupDiGetClassDevs");

            try
            {
                var list = new List<DeviceInfo>();
                uint index = 0;
                while (true)
                {
                    var devInfo = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                    bool ok = SetupDiEnumDeviceInfo(hDevInfo, index, ref devInfo);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 259)
                            break; // NO_MORE_ITEMS
                        ThrowLastWin32("SetupDiEnumDeviceInfo", err);
                    }

                    string? hwid = GetRegStr(hDevInfo, ref devInfo, SPDRP_HARDWAREID);
                    string? friendly = GetRegStr(hDevInfo, ref devInfo, SPDRP_FRIENDLYNAME);
                    string? desc = GetRegStr(hDevInfo, ref devInfo, SPDRP_DEVICEDESC);
                    string instanceId = GetInstanceId(hDevInfo, ref devInfo);
                    string? bus = GetBusReportedDeviceDesc(hDevInfo, ref devInfo);

                    list.Add(new DeviceInfo
                    {
                        InstanceId = instanceId,
                        HardwareId = hwid,
                        FriendlyName = friendly,
                        DeviceDesc = desc,
                        BusReportedName = bus
                    });

                    index++;
                }
                return list;
            }
            finally { SetupDiDestroyDeviceInfoList(hDevInfo); }
        }

        private static List<DeviceInfo> EnumerateInterfacesForGuid(Guid ifaceGuid)
        {
            var result = new List<DeviceInfo>();
            IntPtr hDevInfo = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES | DIGCF_DEVICEINTERFACE);
            if (hDevInfo == IntPtr.Zero || hDevInfo.ToInt64() == -1)
                ThrowLastWin32("SetupDiGetClassDevs (interfaces)");

            try
            {
                uint index = 0;
                while (true)
                {
                    var ifData = new SP_DEVICE_INTERFACE_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>() };
                    bool ok = SetupDiEnumDeviceInterfaces(hDevInfo, IntPtr.Zero, ref ifaceGuid, index, ref ifData);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 259)
                            break; // NO_MORE_ITEMS
                        ThrowLastWin32("SetupDiEnumDeviceInterfaces", err);
                    }

                    var devInfo = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                    SetupDiGetDeviceInterfaceDetail(hDevInfo, ref ifData, IntPtr.Zero, 0, out int needed, ref devInfo);

                    var detail = new SP_DEVICE_INTERFACE_DETAIL_DATA { cbSize = (uint)(IntPtr.Size == 8 ? 8 : 6) };
                    bool ok2 = SetupDiGetDeviceInterfaceDetail(hDevInfo, ref ifData, ref detail, needed, out _, ref devInfo);
                    if (!ok2)
                        ThrowLastWin32("SetupDiGetDeviceInterfaceDetail");

                    string? hwid = GetRegStr(hDevInfo, ref devInfo, SPDRP_HARDWAREID);
                    string? friendly = GetRegStr(hDevInfo, ref devInfo, SPDRP_FRIENDLYNAME);
                    string? desc = GetRegStr(hDevInfo, ref devInfo, SPDRP_DEVICEDESC);
                    string instanceId = GetInstanceId(hDevInfo, ref devInfo);
                    string? bus = GetBusReportedDeviceDesc(hDevInfo, ref devInfo);

                    result.Add(new DeviceInfo
                    {
                        InstanceId = instanceId,
                        HardwareId = hwid,
                        FriendlyName = friendly,
                        DeviceDesc = desc,
                        BusReportedName = bus
                        // we don't need DevicePath for this approach
                    });

                    index++;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(hDevInfo); }

            return result;
        }

        private static string? GetRegStr(IntPtr h, ref SP_DEVINFO_DATA data, uint prop)
        {
            byte[] buf = new byte[1024];
            bool ok = SetupDiGetDeviceRegistryProperty(h, ref data, prop, out _, buf, (uint)buf.Length, out uint req);
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == 122)
                { buf = new byte[req]; ok = SetupDiGetDeviceRegistryProperty(h, ref data, prop, out _, buf, (uint)buf.Length, out _); }

                if (!ok)
                    return null;
            }
            string s = Encoding.Unicode.GetString(buf);
            int i = s.IndexOf('\0');
            return i >= 0 ? s[..i] : s;
        }

        // ---- Bus-reported device description (firmware product string a.k.a. evalName) ----
        private static string? GetBusReportedDeviceDesc(IntPtr h, ref SP_DEVINFO_DATA data)
        {
            byte[] buf = new byte[512];
            var key = DEVPKEY_Device_BusReportedDeviceDesc;
            bool ok = SetupDiGetDevicePropertyW(h, ref data, ref key, out _, buf, (uint)buf.Length, out uint req, 0);
            if (!ok)
            {
                int err = Marshal.GetLastWin32Error();
                if (err == 122) // INSUFFICIENT_BUFFER
                {
                    buf = new byte[req];
                    ok = SetupDiGetDevicePropertyW(h, ref data, ref key, out _, buf, (uint)buf.Length, out _, 0);
                }
                if (!ok)
                    return null;
            }
            string s = Encoding.Unicode.GetString(buf);
            int i = s.IndexOf('\0');
            return i >= 0 ? s[..i] : s;
        }

        private static string GetInstanceId(IntPtr h, ref SP_DEVINFO_DATA data)
        {
            var sb = new StringBuilder(1);
            bool ok = SetupDiGetDeviceInstanceId(h, ref data, sb, sb.Capacity, out int required);
            if (!ok && Marshal.GetLastWin32Error() == 122)
            { sb = new StringBuilder(required + 2); ok = SetupDiGetDeviceInstanceId(h, ref data, sb, sb.Capacity, out _); }
            if (!ok)
                ThrowLastWin32("SetupDiGetDeviceInstanceId");
            return sb.ToString();
        }

        private static bool SetDevPropByInstanceId(string instanceId, uint prop, string value)
        {
            IntPtr h = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (h == IntPtr.Zero || h.ToInt64() == -1)
                ThrowLastWin32("SetupDiGetClassDevs");

            try
            {
                uint idx = 0;
                while (true)
                {
                    var di = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                    if (!SetupDiEnumDeviceInfo(h, idx, ref di))
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 259)
                            return false; // not found
                        ThrowLastWin32("SetupDiEnumDeviceInfo", err);
                    }

                    string id = GetInstanceId(h, ref di);
                    if (string.Equals(id, instanceId, StringComparison.OrdinalIgnoreCase))
                    {
                        byte[] bytes = Encoding.Unicode.GetBytes(value + "\0");
                        return SetupDiSetDeviceRegistryProperty(h, ref di, prop, bytes, (uint)bytes.Length);
                    }
                    idx++;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(h); }
        }

        private static bool RestartDeviceByInstanceId(string instanceId)
        {
            IntPtr h = SetupDiGetClassDevs(IntPtr.Zero, null, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
            if (h == IntPtr.Zero || h.ToInt64() == -1)
                ThrowLastWin32("SetupDiGetClassDevs");

            try
            {
                uint index = 0;
                while (true)
                {
                    var di = new SP_DEVINFO_DATA { cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>() };
                    bool ok = SetupDiEnumDeviceInfo(h, index, ref di);
                    if (!ok)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 259)
                            return false; // not found
                        ThrowLastWin32("SetupDiEnumDeviceInfo", err);
                    }

                    string id = GetInstanceId(h, ref di);
                    if (string.Equals(id, instanceId, StringComparison.OrdinalIgnoreCase))
                    {
                        var hdr = new SP_CLASSINSTALL_HEADER { cbSize = (uint)Marshal.SizeOf<SP_CLASSINSTALL_HEADER>(), InstallFunction = (uint)DIF_PROPERTYCHANGE };
                        var p = new SP_PROPCHANGE_PARAMS { ClassInstallHeader = hdr, StateChange = DICS_ENABLE, Scope = DICS_FLAG_GLOBAL, HwProfile = 0 };

                        if (!SetupDiSetClassInstallParams(h, ref di, ref p, Marshal.SizeOf<SP_PROPCHANGE_PARAMS>()))
                            ThrowLastWin32("SetupDiSetClassInstallParams");
                        if (!SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, h, ref di))
                            ThrowLastWin32("SetupDiCallClassInstaller (DIF_PROPERTYCHANGE)");
                        return true;
                    }

                    index++;
                }
            }
            finally { SetupDiDestroyDeviceInfoList(h); }
        }

        private static void ThrowLastWin32(string api, int? err = null)
        {
            int code = err ?? Marshal.GetLastWin32Error();
            throw new Win32Exception(code, $"{api} failed with error {code}");
        }

        private static bool TryParseHex(string s, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(s))
                return false;
            s = s.Trim();
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s[2..];
            return int.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out value);
        }

        private static string? Prompt(string text, string defaultValue = "")
        {
            using var form = new Form() { Width = 520, Height = 160, Text = "Rename", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
            var lbl = new Label() { Left = 12, Top = 12, Width = 480, Text = text };
            var tb = new TextBox() { Left = 12, Top = 40, Width = 480, Text = defaultValue };
            var ok = new Button() { Text = "OK", Left = 326, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "Cancel", Left = 412, Width = 80, Top = 80, DialogResult = DialogResult.Cancel };
            form.Controls.Add(lbl);
            form.Controls.Add(tb);
            form.Controls.Add(ok);
            form.Controls.Add(cancel);
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            return form.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }

        // Read 'length' bytes starting at offset 0 in small chunks to be nice to the driver
        private static bool ReadEepromChunked(IntPtr h, byte[] buffer, uint length)
        {
            const int CHUNK = 64; // conservative chunk size
            if (buffer.Length < length)
                return false;

            int total = 0;
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                while (total < length)
                {
                    int ask = Math.Min(CHUNK, (int)(length - total));
                    IntPtr ptr = handle.AddrOfPinnedObject() + total;
                    bool ok = UUIRTEERead(h, ptr, (uint)total, (uint)ask);
                    if (!ok)
                        return false;
                    total += ask;
                }
                return true;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }
        }

        // Simple hex dump of the first 'maxBytes' bytes
        private static string BuildHexPreview(byte[] data, int maxBytes = 128)
        {
            int n = Math.Min(maxBytes, data.Length);
            var sb = new StringBuilder();
            for (int i = 0; i < n; i += 16)
            {
                int line = Math.Min(16, n - i);
                sb.Append($"{i:X4}: ");
                for (int j = 0; j < 16; j++)
                {
                    if (j < line)
                        sb.Append($"{data[i + j]:X2} ");
                    else
                        sb.Append("   ");
                }
                sb.Append(" | ");
                for (int j = 0; j < line; j++)
                {
                    byte b = data[i + j];
                    sb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
                sb.AppendLine();
            }
            if (n < data.Length)
                sb.Append($"… ({data.Length - n} more bytes)");
            return sb.ToString();
        }

        #endregion

        private void BtnReadEeprom_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                sslStatus.Text = "Opening USB-UIRT driver…";

                // Try to open driver
                if (!UUIRTOpen(out IntPtr h))
                {
                    int err = Marshal.GetLastWin32Error();
                    MessageBox.Show(this,
                        "Failed to open uuirtdrv.dll (UUIRTOpen).\n" +
                        "• Ensure the USB-UIRT driver is installed.\n" +
                        "• Match your app bitness to the DLL (x86 vs x64).\n" +
                        (err != 0 ? $"Win32 error: {err}" : ""),
                        "USB-UIRT driver", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                try
                {
                    // Heuristic: many USB-UIRTs use 512–1024 bytes of EEPROM.
                    // We'll probe sizes safely and take the largest successful.
                    uint[] probeSizes = [2048, 1024, 512, 256, 128, 64, 32, 16, 1];
                    byte[]? eeprom = null;
                    uint usedSize = 0;

                    foreach (var size in probeSizes)
                    {
                        var buf = new byte[size];
                        bool ok = ReadEepromChunked(h, buf, size);
                        if (ok)
                        {
                            eeprom = buf;
                            usedSize = size;
                            break;
                        }
                    }

                    if (eeprom == null)
                    {
                        MessageBox.Show(this,
                            "UUIRTEERead failed for all tested sizes.\n" +
                            "This device/driver build may not expose EEPROM read.",
                            "EEPROM read", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Offer to save the dump
                    using var sfd = new SaveFileDialog
                    {
                        Title = "Save EEPROM dump",
                        Filter = "Binary dump (*.bin)|*.bin|All files|*.*",
                        FileName = $"USB-UIRT_EEPROM_{DateTime.Now:yyyyMMdd_HHmmss}.bin"
                    };
                    if (sfd.ShowDialog(this) == DialogResult.OK)
                    {
                        System.IO.File.WriteAllBytes(sfd.FileName, eeprom);
                    }

                    // Show a quick hex preview (first 128 bytes)
                    string preview = BuildHexPreview(eeprom, maxBytes: 128);

                    MessageBox.Show(this,
                        $"EEPROM read OK ({usedSize} bytes).\n\n" +
                        $"{preview}\n\n" +
                        "Tip: look for ASCII like \"USB-UIRT\"; the trailing digit (if any) is the unit number the plugin shows.",
                        "EEPROM read", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    sslStatus.Text = $"EEPROM read OK ({usedSize} bytes).";
                }
                finally
                {
                    try
                    { UUIRTClose(h); }
                    catch { /* ignore */ }
                }
            }
            catch (DllNotFoundException)
            {
                MessageBox.Show(this,
                    "uuirtdrv.dll not found.\n\n" +
                    "• On 64-bit Windows, the 64-bit DLL is in C:\\Windows\\System32, the 32-bit DLL is in C:\\Windows\\SysWOW64.\n" +
                    "• Make sure your app bitness matches the DLL you intend to load.",
                    "DLL not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (EntryPointNotFoundException ex)
            {
                MessageBox.Show(this,
                    $"The function wasn’t found in uuirtdrv.dll:\n{ex.Message}\n\n" +
                    "Double-check the DLL version (some older/newer builds differ).",
                    "Entry point not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "EEPROM read error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
        // --- uuirtdrv.dll (x86) ---

        // BOOL UUIRTOpen(HANDLE* ph)
        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "UUIRTOpen")]
        private static extern bool UUIRTOpen(out IntPtr hUuirtdrv);

        // BOOL UUIRTOpenEx(HANDLE* ph, void* rxCallback, void* user, DWORD flags)
        // Exact signature is not published; many builds export it as taking a handle first.
        // We'll provide two overloads and try both (the wrong one will fail cleanly).
        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "UUIRTOpenEx")]
        private static extern bool UUIRTOpenEx1(out IntPtr hUuirtdrv, IntPtr rxCallback, IntPtr userData, uint flags);

        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "UUIRTOpenEx")]
        private static extern bool UUIRTOpenEx2(IntPtr rxCallback, IntPtr userData, uint flags, out IntPtr hUuirtdrv);

        // BOOL UUIRTClose(HANDLE h)
        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "UUIRTClose")]
        private static extern bool UUIRTClose(IntPtr hUuirtdrv);

        // BOOL UUIRTEERead(HANDLE h, BYTE* pBuf, DWORD offset, DWORD len)
        [DllImport("uuirtdrv.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, EntryPoint = "UUIRTEERead")]
        private static extern bool UUIRTEERead(IntPtr hUuirtdrv, IntPtr pBuffer, uint offset, uint length);

    }
}
