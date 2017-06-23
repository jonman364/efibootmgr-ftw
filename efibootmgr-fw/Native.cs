using System;
using System.Runtime.InteropServices;

namespace efibootmgr_fw {
    class Native {
        public enum SePrivilage : uint {
            ENABLED_BY_DEFAULT = 0x00000001,
            ENABLED = 0x00000002,
            REMOVED = 0x00000004,
            USED_FOR_ACCESS = 0x80000000
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public LUID_AND_ATTRIBUTES[] Privileges;
        }

        public enum VariableAttributes : int {
            VARIABLE_ATTRIBUTE_NON_VOLATILE = 0x00000001,
            VARIABLE_ATTRIBUTE_BOOTSERVICE_ACCESS = 0x00000002,
            VARIABLE_ATTRIBUTE_RUNTIME_ACCESS = 0x00000004,
            VARIABLE_ATTRIBUTE_HARDWARE_ERROR_RECORD = 0x00000008,
            VARIABLE_ATTRIBUTE_AUTHENTICATED_WRITE_ACCESS = 0x00000010,
            VARIABLE_ATTRIBUTE_APPEND_WRITE = 0x00000040
        }

        public enum AccessRights : uint {
            STANDARD_RIGHTS_REQUIRED = 0x000F0000,
            STANDARD_RIGHTS_READ = 0x00020000,
            TOKEN_ASSIGN_PRIMARY = 0x0001,
            TOKEN_DUPLICATE = 0x0002,
            TOKEN_IMPERSONATE = 0x0004,
            TOKEN_QUERY = 0x0008,
            TOKEN_QUERY_SOURCE = 0x0010,
            TOKEN_ADJUST_PRIVILEGES = 0x0020,
            TOKEN_ADJUST_GROUPS = 0x0040,
            TOKEN_ADJUST_DEFAULT = 0x0080,
            TOKEN_ADJUST_SESSIONID = 0x0100,
            TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY),
            TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
                TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
                TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
                TOKEN_ADJUST_SESSIONID)
        }

        public static readonly string SE_SYSTEM_ENVIRONMENT_NAME = "SeSystemEnvironmentPrivilege";

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLengthInBytesref, IntPtr PreviousState, IntPtr ReturnLengthInBytes);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, AccessRights DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        //[DllImport("kernel32.dll")]
        //public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int GetFirmwareEnvironmentVariableEx(string lpName, string lpGuid, IntPtr pValue, int nSize, out UIntPtr pdwAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int SetFirmwareEnvironmentVariableEx(string lpName, string lpGuid, IntPtr pValue, int nSize, VariableAttributes dwAttributes);

        public static IntPtr malloc(int size) {
            return Marshal.AllocHGlobal(size);
        }

        public static void copy(IntPtr ptr, ref byte[] data, int size) {
            Marshal.Copy(ptr, data, 0, size);
        }

        public static void copy(byte[] data, IntPtr ptr, int size) {
            Marshal.Copy(data, 0, ptr, size);
        }

        public static void free(IntPtr hglobal) {
            Marshal.FreeHGlobal(hglobal);
        }

        public static int GetLastError() {
            return Marshal.GetLastWin32Error();
        }
    }
}
