using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class ExAssembler
    {
        private const int ARG_RCX = 0;
        private const int ARG_RDX = 1;
        private const int ARG_R8 = 2;
        private const int ARG_R9 = 3;

        public static byte[] CreateRemoteCall(PointerEx jumpLocation, PointerEx[] args, int pointerSize, PointerEx raxStorAddress, PointerEx threadStateAddress, byte xmmMask_64 = 0, ExXMMReturnType xmmReturnType = ExXMMReturnType.XMMR_NONE)
        {
            if (pointerSize == 8)
            {
                return CreateRemoteCall64(jumpLocation, args, raxStorAddress, threadStateAddress, xmmMask_64, xmmReturnType);
            }
            return CreateRemoteCall32(jumpLocation, args, raxStorAddress, threadStateAddress);
        }

        private static byte[] CreateRemoteCall64(PointerEx jumpLocation, PointerEx[] args, PointerEx raxStorAddress, PointerEx threadStateAddress, byte xmmMask, ExXMMReturnType xmmReturnType)
        {
            if (!raxStorAddress)
                throw new InvalidOperationException("Unable to execute a 64 bit function without RAXStor");

            List<byte> data = new List<byte>();

            // movabs rax, raxStorAddress
            data.AddRange(new byte[] { 0x48, 0xb8 });
            data.AddRange(BitConverter.GetBytes((long)raxStorAddress));

            // mov QWORD PTR [rax], rsp
            // mov rax, -16
            // and rsp, rax
            // sub rsp, 32
            data.AddRange(new byte[] { 0x48, 0x89, 0x20, 0x48, 0xC7, 0xC0, 0xF0, 0xFF, 0xFF, 0xFF, 0x48, 0x21, 0xC4, 0x48, 0x83, 0xEC, 0x20 });

            for (int i = args.Length - 1; i > -1; i--)
            {
                var arg = args[i];
                if (i < 4 && (PointerEx)(xmmMask & (1 << i)))
                {
                    bool is64xmm = (PointerEx)(xmmMask & (1 << i + 4));

                    // mov rax, arg
                    data.AddRange(new byte[] { 0x48, 0xb8 });
                    data.AddRange(BitConverter.GetBytes((long)arg));

                    if (is64xmm)
                    {
                        // movlpd xmm<?>, QWORD PTR [rax]
                        data.AddRange(new byte[] { 0x66, 0x0f, 0x12 });
                        data.Add((byte)(i * 8));
                    }
                    else
                    {
                        // movss xmm<?>, DWORD PTR [rax]
                        data.AddRange(new byte[] { 0xf3, 0x0f, 0x10 });
                        data.Add((byte)(i * 8));
                    }
                    continue;
                }

                switch (i)
                {
                    case ARG_RCX:
                        {
                            if (!arg)
                            {
                                // xor ecx, ecx
                                data.AddRange(new byte[] { 0x31, 0xC9 });
                                break;
                            }

                            if (arg <= (long)uint.MaxValue)
                            {
                                // mov ecx, arg
                                data.Add(0xB9);
                                data.AddRange(BitConverter.GetBytes((int)arg));
                                break;
                            }

                            // mov rcx, arg
                            data.AddRange(new byte[] { 0x48, 0xB9 });
                            data.AddRange(BitConverter.GetBytes((long)arg));
                        }
                        break;
                    case ARG_RDX:
                        {
                            if (!arg)
                            {
                                // xor edx, edx
                                data.AddRange(new byte[] { 0x31, 0xD2 });
                                break;
                            }

                            if (arg <= (long)uint.MaxValue)
                            {
                                // mov edx, arg
                                data.Add(0xBA);
                                data.AddRange(BitConverter.GetBytes((int)arg));
                                break;
                            }

                            // mov rdx, arg
                            data.AddRange(new byte[] { 0x48, 0xBA });
                            data.AddRange(BitConverter.GetBytes((long)arg));
                        }
                        break;
                    case ARG_R8:
                        {
                            if (!arg)
                            {
                                // xor r8d, r8d
                                data.AddRange(new byte[] { 0x45, 0x31, 0xC0 });
                                break;
                            }

                            if (arg <= (long)uint.MaxValue)
                            {
                                // mov r8d, arg
                                data.AddRange(new byte[] { 0x41, 0xB8 });
                                data.AddRange(BitConverter.GetBytes((int)arg));
                                break;
                            }

                            // mov r8, arg
                            data.AddRange(new byte[] { 0x49, 0xB8 });
                            data.AddRange(BitConverter.GetBytes((long)arg));
                        }
                        break;
                    case ARG_R9:
                        {
                            if (!arg)
                            {
                                // xor r9d, r8d
                                data.AddRange(new byte[] { 0x45, 0x31, 0xC9 });
                                break;
                            }

                            if (arg <= (long)uint.MaxValue)
                            {
                                // mov r9d, arg
                                data.AddRange(new byte[] { 0x41, 0xB9 });
                                data.AddRange(BitConverter.GetBytes((int)arg));
                                break;
                            }

                            // mov r9, arg
                            data.AddRange(new byte[] { 0x49, 0xB9 });
                            data.AddRange(BitConverter.GetBytes((long)arg));
                        }
                        break;
                    default:
                        {
                            if (!arg)
                            {
                                // push 0
                                data.AddRange(new byte[] { 0x6a, 0x00 });
                                break;
                            }

                            // mov rax, arg
                            data.AddRange(new byte[] { 0x48, 0xb8 });
                            data.AddRange(BitConverter.GetBytes((long)arg));

                            // push rax
                            data.Add(0x50);
                        }
                        break;
                }
            }

            // sub rsp, 0x20 for the shadow store
            data.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x20 });

            // mov rax, jumploc
            data.AddRange(new byte[] { 0x48, 0xB8 });
            data.AddRange(BitConverter.GetBytes((long)jumpLocation));

            // call rax
            data.AddRange(new byte[] { 0xFF, 0xD0 });

            // add rsp, 0x20 for removing shadow store
            data.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x20 });

            // movabs rbx, raxStorAddress
            data.AddRange(new byte[] { 0x48, 0xBB });
            data.AddRange(BitConverter.GetBytes((long)raxStorAddress));

            // mov rsp, QWORD PTR[rbx]
            data.AddRange(new byte[] { 0x48, 0x8B, 0x23 });

            if (xmmReturnType == ExXMMReturnType.XMMR_NONE)
            {
                // mov ReturnAddress, rax
                data.AddRange(new byte[] { 0x48, 0xA3 });
                data.AddRange(BitConverter.GetBytes((long)raxStorAddress));
            }
            else
            {
                // mov rax, ReturnAddress
                data.AddRange(new byte[] { 0x48, 0xB8 });
                data.AddRange(BitConverter.GetBytes((long)raxStorAddress));

                if (xmmReturnType == ExXMMReturnType.XMMR_SINGLE)
                {
                    // movss DWORD PTR [rax], xmm0
                    data.AddRange(new byte[] { 0xF3, 0x0F, 0x11, 0x00 });
                }
                else
                {
                    // movlpd QWORD PTR [rax],xmm0
                    data.AddRange(new byte[] { 0x66, 0x0F, 0x13, 0x00 });
                }
            }

            // mov rax, threadStateAddress
            data.AddRange(new byte[] { 0x48, 0xB8 });
            data.AddRange(BitConverter.GetBytes((long)threadStateAddress));

            // change thread state to finished
            // mov QWORD PTR [rax], 0x1
            data.AddRange(new byte[] { 0x48, 0xC7, 0x00, 0x01, 0x00, 0x00, 0x00 });

            // xor rax, rax
            data.AddRange(new byte[] { 0x31, 0xC0 });

            // ret
            data.Add(0xC3);
            return data.ToArray();
        }

        private static byte[] CreateRemoteCall32(PointerEx jumpLocation, PointerEx[] args, PointerEx eaxStorAddress, PointerEx threadStateAddress)
        {
            List<byte> data = new List<byte>();

            foreach (var arg in args.Reverse())
            {
                if (arg <= byte.MaxValue)
                {
                    // push byte
                    data.AddRange(new byte[] { 0x6A, arg });
                }
                else
                {
                    // push int32
                    data.Add(0x68);
                    data.AddRange(BitConverter.GetBytes((int)arg));
                }
            }

            // mov eax, jumpLoc
            data.Add(0xB8);
            data.AddRange(BitConverter.GetBytes((int)jumpLocation));

            // call eax
            data.AddRange(new byte[] { 0xFF, 0xD0 });

            if (eaxStorAddress)
            {
                // mov eaxStorAddress, eax
                data.Add(0xA3);
                data.AddRange(BitConverter.GetBytes((int)eaxStorAddress));
            }

            // mov eax, threadStateAddress
            data.Add(0xB8);
            data.AddRange(BitConverter.GetBytes((int)threadStateAddress));

            // change thread state to finished
            // mov DWORD PTR [eax], 0x1
            data.AddRange(new byte[] { 0xC7, 0x00, 0x01, 0x00, 0x00, 0x00 });

            // xor eax, eax
            data.AddRange(new byte[] { 0x33, 0xC0 });

            // ret
            data.Add(0xC3);
            return data.ToArray();
        }

        internal static byte[] CreateThreadIntercept32(PointerEx jumpTo, PointerEx originalIP)
        {
            List<byte> data = new List<byte>();

            // pusha
            data.Add(0x60);

            // pushf
            data.Add(0x9c);

            // mov eax, jumpTo
            data.Add(0xb8);
            data.AddRange(BitConverter.GetBytes((int)jumpTo));

            // call eax
            data.AddRange(new byte[] { 0xff, 0xd0 });

            // popf
            data.Add(0x9d);

            // popa
            data.Add(0x61);

            // push originalIP
            data.Add(0x68);
            data.AddRange(BitConverter.GetBytes((int)originalIP));

            // ret
            data.Add(0xC3);
            return data.ToArray();
        }

        internal static byte[] CreateThreadIntercept64(PointerEx jumpTo, PointerEx originalIP, PointerEx xmmSpace)
        {
            List<byte> data = new List<byte>();

            // pushf TWICE
            // the reason is because we need padding space behind the saved flags so that we can place our fake return pointer back there
            data.AddRange(new byte[] { 0x9C, 0x9C });

            // push all the standard registers
            data.AddRange(new byte[] { 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x41, 0x50, 0x41, 0x51, 0x41, 0x52, 0x41, 0x53, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57 });

            // mov rax, xmmSpace
            data.AddRange(new byte[] { 0x48, 0xB8 });
            data.AddRange(BitConverter.GetBytes((long)xmmSpace));

            // save all the xmm registers to xmmSpace
            data.AddRange(new byte[] { 0x66, 0x0F, 0x29, 0x00, 0x66, 0x0F, 0x29, 0x48, 0x10, 0x66, 0x0F, 0x29, 0x50, 0x20, 0x66, 0x0F, 0x29, 0x58, 0x30,
                                       0x66, 0x0F, 0x29, 0x60, 0x40, 0x66, 0x0F, 0x29, 0x68, 0x50, 0x66, 0x0F, 0x29, 0x70, 0x60, 0x66, 0x0F, 0x29, 0x78,
                                       0x70, 0x66, 0x44, 0x0F, 0x29, 0x80, 0x80, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x29, 0x88, 0x90, 0x00, 0x00, 0x00,
                                       0x66, 0x44, 0x0F, 0x29, 0x90, 0xA0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x29, 0x98, 0xB0, 0x00, 0x00, 0x00, 0x66,
                                       0x44, 0x0F, 0x29, 0xA0, 0xC0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x29, 0xA8, 0xD0, 0x00, 0x00, 0x00, 0x66, 0x44,
                                       0x0F, 0x29, 0xB0, 0xE0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x29, 0xB8, 0xF0, 0x00, 0x00, 0x00 });

            // mov rax, jumpTo
            data.AddRange(new byte[] { 0x48, 0xB8 });
            data.AddRange(BitConverter.GetBytes((long)jumpTo));

            // call rax
            data.AddRange(new byte[] { 0xFF, 0xD0 });

            // mov rax, xmmSpace
            data.AddRange(new byte[] { 0x48, 0xB8 });
            data.AddRange(BitConverter.GetBytes((long)xmmSpace));

            // load all the xmm registers from xmmSpace
            data.AddRange(new byte[] { 0x66, 0x0F, 0x28, 0x00, 0x66, 0x0F, 0x28, 0x48, 0x10, 0x66, 0x0F, 0x28, 0x50, 0x20, 0x66, 0x0F, 0x28, 0x58, 0x30,
                                       0x66, 0x0F, 0x28, 0x60, 0x40, 0x66, 0x0F, 0x28, 0x68, 0x50, 0x66, 0x0F, 0x28, 0x70, 0x60, 0x66, 0x0F, 0x28, 0x78,
                                       0x70, 0x66, 0x44, 0x0F, 0x28, 0x80, 0x80, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x28, 0x88, 0x90, 0x00, 0x00, 0x00,
                                       0x66, 0x44, 0x0F, 0x28, 0x90, 0xA0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x28, 0x98, 0xB0, 0x00, 0x00, 0x00, 0x66,
                                       0x44, 0x0F, 0x28, 0xA0, 0xC0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x28, 0xA8, 0xD0, 0x00, 0x00, 0x00, 0x66, 0x44,
                                       0x0F, 0x28, 0xB0, 0xE0, 0x00, 0x00, 0x00, 0x66, 0x44, 0x0F, 0x28, 0xB8, 0xF0, 0x00, 0x00, 0x00 });

            // pop all the standard registers
            data.AddRange(new byte[] { 0x41, 0x5F, 0x41, 0x5E, 0x41, 0x5D, 0x41, 0x5C, 0x41, 0x5B, 0x41, 0x5A, 0x41, 0x59, 0x41, 0x58, 0x5F, 0x5E, 0x5D, 0x5C, 0x5B, 0x5A, 0x59, 0x58 });

            // sub rsp, 0x8
            // data.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x08 });
            // no longer need to move rsp since we reserve space by flags push

            // mov DWORD PTR [rsp+8], originalIP_l
            data.AddRange(new byte[] { 0xC7, 0x44, 0x24, 0x08 });
            data.AddRange(BitConverter.GetBytes((int)originalIP));

            // mov DWORD PTR [rsp+0xC], originalIP_h
            data.AddRange(new byte[] { 0xC7, 0x44, 0x24, 0x0C });
            data.AddRange(BitConverter.GetBytes((int)((long)originalIP >> 32)));

            // popf here, so flags are 100% correct
            data.Add(0x9D);

            // ret
            data.Add(0xC3);
            return data.ToArray();
        }
    }

    public enum ExXMMReturnType
    {
        XMMR_NONE,
        XMMR_SINGLE,
        XMMR_DOUBLE
    }
}
