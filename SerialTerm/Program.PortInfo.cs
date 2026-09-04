using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.IO.Ports;
using Microsoft.Win32;

namespace TerminalConsole
{
    partial class Program
    {
        // Windows records what is behind each COM port under the device tree,
        // laid out as Enum\<enumerator>\<device>\<instance>. The instance key
        // holds the friendly name, and its "Device Parameters" subkey holds the
        // port name. Reading it costs nothing and disturbs nothing - which is
        // the point, because opening a port to see whether it answers asserts
        // DTR and RTS and reboots whatever is plugged into it.
        private const string DeviceTree = @"SYSTEM\CurrentControlSet\Enum";

        private static string[] MatchingPorts(string match)
        {
            string[] ports = SerialPort.GetPortNames();

            if (string.IsNullOrEmpty(match))
                return ports;

            var descriptions = GetPortDescriptions();
            var matched = new List<string>();

            foreach (string port in ports)
            {
                descriptions.TryGetValue(port, out string description);

                if (Contains(port, match) || Contains(description, match))
                    matched.Add(port);
            }

            return matched.ToArray();
        }

        internal static bool Contains(string value, string match)
        {
            return value != null && value.Contains(match, StringComparison.OrdinalIgnoreCase);
        }

        private static string DescriptionOf(string port)
        {
            return GetPortDescriptions().TryGetValue(port, out string description)
                ? $"({description})"
                : string.Empty;
        }

        private static Dictionary<string, string> GetPortDescriptions()
        {
            var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!OperatingSystem.IsWindows())
                return descriptions;

            try
            {
                using RegistryKey tree = Registry.LocalMachine.OpenSubKey(DeviceTree);
                if (tree == null)
                    return descriptions;

                foreach (string enumeratorName in tree.GetSubKeyNames())
                {
                    using RegistryKey enumerator = OpenSubKey(tree, enumeratorName);
                    if (enumerator == null)
                        continue;

                    foreach (string deviceName in enumerator.GetSubKeyNames())
                    {
                        using RegistryKey device = OpenSubKey(enumerator, deviceName);
                        if (device == null)
                            continue;

                        foreach (string instanceName in device.GetSubKeyNames())
                        {
                            using RegistryKey instance = OpenSubKey(device, instanceName);
                            if (instance == null)
                                continue;

                            AddDescription(descriptions, instance, deviceName);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // the device tree is best effort - a port with no description
                // still lists, just without one
            }

            return descriptions;
        }

        [SupportedOSPlatform("windows")]
        private static void AddDescription(Dictionary<string, string> descriptions, RegistryKey instance, string deviceName)
        {
            using RegistryKey parameters = OpenSubKey(instance, "Device Parameters");

            if (parameters?.GetValue("PortName") is not string portName || portName.Length == 0)
                return;

            string description =
                instance.GetValue("FriendlyName") as string ??
                instance.GetValue("DeviceDesc") as string ??
                string.Empty;

            // "Silicon Labs CP210x USB to UART Bridge (COM3)" - the port is
            // already its own column, and DeviceDesc arrives as
            // "@driver.inf,%string%;Real Name", so keep only the real name
            int semicolon = description.LastIndexOf(';');
            if (semicolon >= 0)
                description = description.Substring(semicolon + 1);

            description = description.Replace($" ({portName})", string.Empty).Trim();

            string hardwareId = UsbHardwareId(deviceName);
            if (hardwareId != null)
                description = description.Length > 0 ? $"{description}  {hardwareId}" : hardwareId;

            if (description.Length > 0)
                descriptions[portName] = description;
        }

        // "USB\VID_10C4&PID_EA60" -> "10C4:EA60"
        internal static string UsbHardwareId(string deviceName)
        {
            int vid = deviceName.IndexOf("VID_", StringComparison.OrdinalIgnoreCase);
            int pid = deviceName.IndexOf("PID_", StringComparison.OrdinalIgnoreCase);

            if (vid < 0 || pid < 0 || deviceName.Length < vid + 8 || deviceName.Length < pid + 8)
                return null;

            return $"{deviceName.Substring(vid + 4, 4)}:{deviceName.Substring(pid + 4, 4)}";
        }

        [SupportedOSPlatform("windows")]
        private static RegistryKey OpenSubKey(RegistryKey parent, string name)
        {
            try
            {
                return parent.OpenSubKey(name);
            }
            catch (Exception)
            {
                // parts of the device tree are not readable without elevation
                return null;
            }
        }
    }
}
