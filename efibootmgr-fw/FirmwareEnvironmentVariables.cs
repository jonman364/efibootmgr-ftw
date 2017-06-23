using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace efibootmgr_fw {
    public class FirmwareEnvironmentVariable : IDisposable {
        static readonly string EFI_GLOBAL_GUID = "{8BE4DF61-93CA-11D2-AA0D-00E098032B8C}";
        static int counter = 0;

        public struct EFI_LOAD_OPTION {
            public uint Attributes;
            public ushort FilePathListingsLength;
            public string Description;
            public EFI_DEVICE_PATH_PROTOCOL[] FilePathList;
            public byte[] OptionalData;

            public override string ToString() {
                string retval = "Attributes:     ";
                List<string> attributes = new List<string>();
                if ((Attributes & 0x1) == 0x1)
                    attributes.Add("Active");
                if ((Attributes & 0x2) == 0x2)
                    attributes.Add("Force Reconnect");
                if ((Attributes & 0x8) == 0x8)
                    attributes.Add("Hidden");
                if ((Attributes & 0x1F00) == 0x1F00)
                    attributes.Add("Category");
                if (Attributes == 0)
                    attributes.Add("Boot");
                if ((Attributes & 0x100) == 0x100)
                    attributes.Add("App");

                retval += string.Format("{0}\n", string.Join(", ", attributes));

                retval += string.Format("FilePathLength: {0}\n", FilePathListingsLength);
                retval += string.Format("Descriptsion:   {0}\n", Description);

                return retval;
            }
        }

        public struct EFI_DEVICE_PATH_PROTOCOL {
            public byte Type;
            public byte SubType;
            public ushort Length;
            public byte[] Data;
        }

        public short? BootCurrent {
            get {
                return GetShort("BootCurrent");
            }
            set {
                throw new NotImplementedException();
            }
        }

        public short? BootNext {
            get {
                return GetShort("BootNext");
            }

            set {
                if (value == null)
                    Delete("BootNext", Native.VariableAttributes.VARIABLE_ATTRIBUTE_NON_VOLATILE | Native.VariableAttributes.VARIABLE_ATTRIBUTE_RUNTIME_ACCESS | Native.VariableAttributes.VARIABLE_ATTRIBUTE_BOOTSERVICE_ACCESS);
                else {
                    byte[] num = new byte[2];
                    num[0] = Convert.ToByte(value.Value & 0xFF);
                    num[1] = Convert.ToByte((value.Value >> 8) & 0xFF);
                    Set("BootNext", num, Native.VariableAttributes.VARIABLE_ATTRIBUTE_NON_VOLATILE | Native.VariableAttributes.VARIABLE_ATTRIBUTE_RUNTIME_ACCESS | Native.VariableAttributes.VARIABLE_ATTRIBUTE_BOOTSERVICE_ACCESS);
                }
            }
        }

        public short[] BootOrder {
            get {
                byte[] data = Get("BootOrder");

                if (data != null) {
                    short[] retval = new short[data.Length / 2];
                    for (int i = 0; i < data.Length; i += 2)
                        retval[i / 2] = BitConverter.ToInt16(data, i);
                    return retval;
                }
                else
                    return null;
            }
            set {
                throw new NotImplementedException();
            }
        }

        public FirmwareEnvironmentVariable() {
            if (Interlocked.Increment(ref counter) == 1) {
                IntPtr hToken;

                if (!Native.OpenProcessToken(Process.GetCurrentProcess().Handle, Native.AccessRights.TOKEN_ADJUST_PRIVILEGES | Native.AccessRights.TOKEN_QUERY, out hToken))
                    throw new ApplicationException(string.Format("Unable to open process token: {0:X}", Native.GetLastError()));

                Native.TOKEN_PRIVILEGES tkp = new Native.TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new Native.LUID_AND_ATTRIBUTES[1] };
                Native.LookupPrivilegeValue(null, Native.SE_SYSTEM_ENVIRONMENT_NAME, out tkp.Privileges[0].Luid);
                tkp.Privileges[0].Attributes = (uint)Native.SePrivilage.ENABLED;

                if (!Native.AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero))
                    throw new ApplicationException(string.Format("Unable to adjust process token: {0:X}", Native.GetLastError()));

                Native.CloseHandle(hToken);

            }
        }

        ~FirmwareEnvironmentVariable() {
            if (Interlocked.Decrement(ref counter) < 1) {
                IntPtr hToken;

                Native.OpenProcessToken(Process.GetCurrentProcess().Handle, Native.AccessRights.TOKEN_ADJUST_PRIVILEGES | Native.AccessRights.TOKEN_QUERY, out hToken);
                Native.TOKEN_PRIVILEGES tkp = new Native.TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new Native.LUID_AND_ATTRIBUTES[1] };
                Native.LookupPrivilegeValue(null, Native.SE_SYSTEM_ENVIRONMENT_NAME, out tkp.Privileges[0].Luid);
                tkp.Privileges[0].Attributes = (uint)Native.SePrivilage.REMOVED;
                Native.AdjustTokenPrivileges(hToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);

                Native.CloseHandle(hToken);
            }
        }

        public void Dispose() {
            throw new NotImplementedException();
        }

        public byte[] Get(string variable) {
            IntPtr var = Native.malloc(4096);
            UIntPtr attrs;
            int varsz = Native.GetFirmwareEnvironmentVariableEx(variable, EFI_GLOBAL_GUID, var, 4096, out attrs);
            if (varsz != 0) {
                byte[] data = new byte[varsz];
                Native.copy(var, ref data, varsz);

                return data;
            }
            else
                return null;
        }

        public void DeleteBootNum(short num) {
            Delete(string.Format("Boot{0:X4}", num), Native.VariableAttributes.VARIABLE_ATTRIBUTE_NON_VOLATILE | Native.VariableAttributes.VARIABLE_ATTRIBUTE_RUNTIME_ACCESS | Native.VariableAttributes.VARIABLE_ATTRIBUTE_BOOTSERVICE_ACCESS);
        }

        void Set(string varible, byte[] data, Native.VariableAttributes attrs) {
            IntPtr pData = Native.malloc(data.Length);
            Native.copy(data, pData, data.Length);
            if (Native.SetFirmwareEnvironmentVariableEx(varible, EFI_GLOBAL_GUID, pData, data.Length, attrs) == 0)
                throw new NotImplementedException("Correct error handling not implemented yet");
        }

        void Delete(string variable, Native.VariableAttributes attrs) {
            if (Native.SetFirmwareEnvironmentVariableEx(variable, EFI_GLOBAL_GUID, IntPtr.Zero, 0, attrs) == 0)
                throw new NotImplementedException("CorrectError handling not implemented yet");
        }

        public EFI_LOAD_OPTION? GetLoadOption(int num) {
            EFI_LOAD_OPTION retval = new EFI_LOAD_OPTION();

            string optString = string.Format("Boot{0:X4}", num);
            byte[] data = Get(optString);
            if (data != null) {
                retval.Attributes = BitConverter.ToUInt32(data, 0);
                retval.FilePathListingsLength = BitConverter.ToUInt16(data, 4);

                retval.Description = "";
                int i = 6;
                for (; ; i += 2) {
                    char c = BitConverter.ToChar(data, i);
                    if (c == 0)
                        break;
                    else
                        retval.Description += c;
                }

                i += 2;

                List<EFI_DEVICE_PATH_PROTOCOL> filePathList = new List<EFI_DEVICE_PATH_PROTOCOL>();
                byte[] fplData = new byte[retval.FilePathListingsLength];
                Array.Copy(data, i, fplData, 0, retval.FilePathListingsLength);

                for (int k = 0; k < retval.FilePathListingsLength;) {
                    EFI_DEVICE_PATH_PROTOCOL dpp = new EFI_DEVICE_PATH_PROTOCOL();
                    dpp.Type = fplData[k++];
                    dpp.SubType = fplData[k++];
                    dpp.Length = BitConverter.ToUInt16(fplData, k);
                    k += 2;

                    if (dpp.Length == 4)
                        dpp.Data = null;
                    else {
                        dpp.Data = new byte[dpp.Length - 4];
                        Array.Copy(fplData, k, dpp.Data, 0, dpp.Data.Length);
                        k += dpp.Data.Length;
                    }

                    filePathList.Add(dpp);
                }
                retval.FilePathList = filePathList.ToArray();

                if (data.Length - i - retval.FilePathListingsLength == 0)
                    retval.OptionalData = null;
                else {
                    retval.OptionalData = new byte[data.Length - i - retval.FilePathListingsLength];
                    Array.Copy(data, i + retval.FilePathListingsLength, retval.OptionalData, 0, retval.OptionalData.Length);
                }
            }

            return retval;
        }

        short? GetShort(string KEY) {
            byte[] data = Get(KEY);

            if (data != null)
                return BitConverter.ToInt16(data, 0);
            else
                return null;
        }
    }
}
