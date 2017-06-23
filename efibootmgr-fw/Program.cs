using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace efibootmgr_fw {
    class Program {
        static FirmwareEnvironmentVariable fw;
        static void Main(string[] args) {
            fw = new FirmwareEnvironmentVariable();

            if (args.Length < 1) {
                Display();
            }
            else {
                for (int i = 0; i < args.Length; i++) {
                    if (args[i] == "-n" || args[i] == "--bootnext") {
                        if (i + 1 < args.Length) {
                            if (IsHexNum(args[++i]))
                                fw.BootNext = Convert.ToInt16(args[i], 16);
                        }
                    }
                    else if (args[i] == "-N" || args[i] == "--delete-bootnext")
                        fw.BootNext = null;
                    else if(args[i] == "-B" || args[i] == "--delete-bootnum") {
                        if(i + 1 < args.Length) {
                            if (IsHexNum(args[++i]))
                                fw.DeleteBootNum(Convert.ToInt16(args[i], 16));
                        }
                    }
                }
            }
        }

        static bool IsHexNum(string num) {
            return Regex.IsMatch(num, "^[0-9A-Fa-f]{1,4}$");
        }

        static void Display() {
            short[] bootOrder = fw.BootOrder;

            if (bootOrder != null) {
                Console.WriteLine("Boot Order:");
                foreach (short bootNum in bootOrder) {
                    FirmwareEnvironmentVariable.EFI_LOAD_OPTION? lo = fw.GetLoadOption(bootNum);
                    if(lo.HasValue)
                        Console.WriteLine("Boot{0:X4}: {1}", bootNum, lo.Value.Description);
                    else 
                        Console.WriteLine("Boot{0:X4}: Null", bootNum);
                }
            }
            else
                Console.WriteLine("Null bootOrder");

            short? bootNext = fw.BootNext;
            short? bootCurrent = fw.BootCurrent;

            Console.WriteLine("\nBootCurrent: {0}", bootCurrent.HasValue ? string.Format("Boot{0:X4}", bootCurrent.Value) : "Null");
            Console.WriteLine("\nBootNext   : {0}", bootNext.HasValue ? string.Format("Boot{0:X4}", bootNext.Value) : "Null");
        }
    }
}
