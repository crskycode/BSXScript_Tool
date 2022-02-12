using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BSXScript_Tool
{
    class BSXScript
    {
        static readonly byte[] Signature_3_0 = Encoding.ASCII.GetBytes("BSXScript 3.0\x00\x00\x00");
        static readonly byte[] Signature_3_1 = Encoding.ASCII.GetBytes("BSXScript 3.1\x00\x00\x00");
        static readonly byte[] Signature_3_2 = Encoding.ASCII.GetBytes("BSXScript 3.2\x00\x00\x00");
        static readonly byte[] Signature_3_3 = Encoding.ASCII.GetBytes("BSXScript 3.3\x00\x00\x00");

        byte[] _scriptBuffer;

        int _codeBlockOffset;
        int _codeBlockSize;
        int _charNameOffsetListOffset;
        int _charNameOffsetListSize;
        int _charNameBlockOffset;
        int _messageOffsetListOffset;
        int _messageOffsetListSize;
        int _messageBlockOffset;

        int[] _charNameOffsetList;
        int[] _messageOffsetList;

        public void Load(string filePath)
        {
            Console.WriteLine("Loading script ...");

            using var reader = new BinaryReader(File.OpenRead(filePath));

            var header = reader.ReadBytes(16);

            if (!header.SequenceEqual(Signature_3_0) &&
                !header.SequenceEqual(Signature_3_1) &&
                !header.SequenceEqual(Signature_3_2) &&
                !header.SequenceEqual(Signature_3_3))
            {
                throw new Exception("Not a valid BSXScript file.");
            }

            reader.BaseStream.Position = 0;

            _scriptBuffer = reader.ReadBytes(Convert.ToInt32(reader.BaseStream.Length));

            if (_scriptBuffer.Length != reader.BaseStream.Length)
            {
                throw new Exception("Failed to read script.");
            }

            var field_1c = BitConverter.ToInt32(_scriptBuffer, 0x10); // +04 tbl0 size
            var field_24 = BitConverter.ToInt32(_scriptBuffer, 0x14); // +05 tbl1 size
            var field_2c = BitConverter.ToInt32(_scriptBuffer, 0x18); // +06 tbl2 size
            var field_34 = BitConverter.ToInt32(_scriptBuffer, 0x34); // +0d tbl3 size
            var field_3c = BitConverter.ToInt32(_scriptBuffer, 0x1C); // +07 tbl4 size
            var field_44 = BitConverter.ToInt32(_scriptBuffer, 0x20); // +08 tbl5 size
            var field_4c = BitConverter.ToInt32(_scriptBuffer, 0x24); // +09 tbl6 size
            var field_54 = BitConverter.ToInt32(_scriptBuffer, 0x28); // +0a

            var field_58 = BitConverter.ToInt32(_scriptBuffer, 0x2C); // +0b code
            var field_5c = BitConverter.ToInt32(_scriptBuffer, 0x30); // +0c code size

            var field_60 = BitConverter.ToInt32(_scriptBuffer, 0x38); // +0e
            var field_68 = BitConverter.ToInt32(_scriptBuffer, 0x3C); // +0f
            var field_64 = BitConverter.ToInt32(_scriptBuffer, 0x40); // +10
            // +11
            var field_6c = BitConverter.ToInt32(_scriptBuffer, 0x48); // +12
            var field_74 = BitConverter.ToInt32(_scriptBuffer, 0x4C); // +13
            var field_70 = BitConverter.ToInt32(_scriptBuffer, 0x50); // +14
            // +15
            var field_78 = BitConverter.ToInt32(_scriptBuffer, 0x58); // +16
            var field_80 = BitConverter.ToInt32(_scriptBuffer, 0x5C); // +17
            var field_7c = BitConverter.ToInt32(_scriptBuffer, 0x60); // +18
            // +19
            var field_84 = BitConverter.ToInt32(_scriptBuffer, 0x68); // +1a
            var field_8c = BitConverter.ToInt32(_scriptBuffer, 0x6C); // +1b
            var field_88 = BitConverter.ToInt32(_scriptBuffer, 0x70); // +1c
            // +1d
            var field_90 = BitConverter.ToInt32(_scriptBuffer, 0x78); // +1e
            var field_98 = BitConverter.ToInt32(_scriptBuffer, 0x7C); // +1f
            var field_94 = BitConverter.ToInt32(_scriptBuffer, 0x80); // +20
            // +21
            var field_9c = BitConverter.ToInt32(_scriptBuffer, 0x88); // +22
            var field_a4 = BitConverter.ToInt32(_scriptBuffer, 0x8C); // +23
            var field_a0 = BitConverter.ToInt32(_scriptBuffer, 0x90); // +24
            // +25
            var field_a8 = BitConverter.ToInt32(_scriptBuffer, 0x98); // +26
            var field_b0 = BitConverter.ToInt32(_scriptBuffer, 0x9C); // +27
            var field_ac = BitConverter.ToInt32(_scriptBuffer, 0xA0); // +28

            field_68 >>= 3;
            field_74 >>= 2;
            field_80 >>= 2;
            field_8c >>= 2;
            field_98 >>= 2;
            field_a4 >>= 2;
            field_b0 >>= 2;

            _codeBlockOffset = field_58;
            _codeBlockSize = field_5c;
            _charNameOffsetListOffset = field_9c;
            _charNameOffsetListSize = field_a4;
            _charNameBlockOffset = field_a0;
            _messageOffsetListOffset = field_a8;
            _messageOffsetListSize = field_b0;
            _messageBlockOffset = field_ac;

            Console.WriteLine("Prepare character name offset");

            _charNameOffsetList = new int[_charNameOffsetListSize];
            for (var i = 0; i < _charNameOffsetListSize; i++)
                _charNameOffsetList[i] = _charNameBlockOffset + BitConverter.ToInt32(_scriptBuffer, _charNameOffsetListOffset + 4 * i) * 2;

            Console.WriteLine("Prepare message offset");

            _messageOffsetList = new int[_messageOffsetListSize];
            for (var i = 0; i < _messageOffsetListSize; i++)
                _messageOffsetList[i] = _messageBlockOffset + BitConverter.ToInt32(_scriptBuffer, _messageOffsetListOffset + 4 * i) * 2;
        }

        public void Save(string filePath)
        {
            File.WriteAllBytes(filePath, _scriptBuffer);
        }

        void ExtractTextByOpcodeOrder(StreamWriter output, byte[] code)
        {
            Console.WriteLine("Analyzing code ...");

            var addr = 0;

            while (addr < code.Length)
            {
                switch (code[addr])
                {
                    case 0x00: // goto boot label
                    {
                        addr += 1;
                        break;
                    }
                    case 0x01: // return 1
                    {
                        addr += 1;
                        break;
                    }
                    case 0x02: // return 2
                    {
                        addr += 1;
                        break;
                    }
                    case 0x03: // goto label
                    {
                        addr += 5; // label id
                        break;
                    }
                    case 0x04: // jump
                    {
                        addr += 5; // addr
                        break;
                    }
                    case 0x05: // true jump
                    {
                        addr += 5; // addr
                        break;
                    }
                    case 0x06: // false jump
                    {
                        addr += 5; // addr
                        break;
                    }
                    case 0x07:
                    case 0x08: // goto label
                    {
                        addr += 5; // label id
                        break;
                    }
                    case 0x09: // call
                    {
                        addr += 5; // label id
                        break;
                    }
                    case 0x0A: // return
                    {
                        addr += 1;
                        break;
                    }
                    case 0x0B: // reset stack pointer
                    {
                        addr += 1;
                        break;
                    }
                    case 0x0C:
                    case 0x0D:
                    case 0x0E:
                    case 0x0F:
                    case 0x10:
                    case 0x11:
                    case 0x13:
                    case 0x14:
                    case 0x15:
                    case 0x16:
                    case 0x17:
                    case 0x18:
                    case 0x19:
                    case 0x3A:
                    case 0x3B:
                    case 0x3C: // operator
                    {
                        addr += 18;
                        break;
                    }
                    case 0x12: // mov
                    {
                        addr += 18;
                        break;
                    }
                    case 0x1A: // neg
                    {
                        addr += 12;
                        break;
                    }
                    case 0x1B:
                    case 0x1C: // ?
                    {
                        addr += 12;
                        break;
                    }
                    case 0x1D: // display message
                    {
                        OpcodeDisplayMessage(output, code, addr + 1);

                        var v1 = 0;
                        var v2 = code[addr + 1];

                        if (v2 > 1)
                        {
                            v1 = 4 * BitConverter.ToInt32(code, addr + 10);
                        }

                        addr += v1 + 4 * v2 + 6;

                        break;
                    }
                    case 0x1E:
                    case 0x1F:
                    case 0x20:
                    case 0x21:
                    case 0x22:
                    case 0x23:
                    case 0x24:
                    case 0x25:
                    case 0x26:
                    case 0x27:
                    case 0x28:
                    case 0x29:
                    case 0x2A:
                    case 0x2B:
                    case 0x2C:
                    case 0x2D:
                    case 0x2E:
                    case 0x2F:
                    case 0x30:
                    case 0x31:
                    case 0x32:
                    {
                        addr += 1;
                        break;
                    }
                    case 0x33:
                    {
                        addr += 13;
                        break;
                    }
                    case 0x34:
                    {
                        addr += 5;
                        break;
                    }
                    case 0x35:
                    {
                        addr += 1;
                        break;
                    }
                    case 0x36:
                    {
                        addr += 5;
                        break;
                    }
                    case 0x37:
                    {
                        addr += 1;
                        break;
                    }
                    case 0x38:
                    {
                        addr += 5;
                        break;
                    }
                    case 0x39:
                    {
                        addr += 1;
                        break;
                    }
                    case 0x3D: // not
                    {
                        addr += 12;
                        break;
                    }
                    case 0x3E:
                    {
                        addr += 5 + 4 * BitConverter.ToInt32(code, addr + 1);
                        break;
                    }
                    case 0x3F:
                    case 0x40:
                    case 0x41:
                    {
                        addr += 1;
                        break;
                    }
                    default:
                    {
                        throw new Exception("Unknown opcode.");
                    }
                }
            }

            if (addr != code.Length)
            {
                Console.WriteLine("WARNING: Found unresolved code.");
            }

            Console.WriteLine("Done");
        }

        void OpcodeDisplayMessage(StreamWriter output, byte[] code, int addr)
        {
            var mesgId = -1;
            var charId = -1;

            switch (code[addr])
            {
                case 0:
                {
                    mesgId = BitConverter.ToInt32(code, addr + 1);
                    break;
                }
                case 1:
                case 2:
                case 3:
                {
                    mesgId = BitConverter.ToInt32(code, addr + 1);
                    charId = BitConverter.ToInt32(code, addr + 5);
                    break;
                }
                default:
                {
                    throw new Exception("Unknown message type.");
                }
            }

            if (charId != -1)
            {
                var s = Binary.GetCString(_scriptBuffer, _charNameOffsetList[charId]);

                if (!string.IsNullOrEmpty(s))
                {
                    output.WriteLine($"◇A{charId:X7}◇{s}");
                    output.WriteLine($"◆A{charId:X7}◆{s}");
                    output.WriteLine();
                }
            }

            if (mesgId != -1)
            {
                var s = Binary.GetCString(_scriptBuffer, _messageOffsetList[mesgId]);

                if (!string.IsNullOrEmpty(s))
                {
                    output.WriteLine($"◇B{mesgId:X7}◇{s}");
                    output.WriteLine($"◆B{mesgId:X7}◆{s}");
                    output.WriteLine();
                }
            }
        }

        public void ExportText(string filePath)
        {
            using var writer = File.CreateText(filePath);
            var codeBlock = _scriptBuffer.AsSpan(_codeBlockOffset, _codeBlockSize).ToArray();
            ExtractTextByOpcodeOrder(writer, codeBlock);
            writer.Flush();
        }

        public void ImportText(string filePath)
        {
            var translatedCharName = new Dictionary<int, string>();
            var translatedMessage = new Dictionary<int, string>();

            // Read translated text

            Console.WriteLine("Loading translation ...");

            using (var reader = File.OpenText(filePath))
            {
                var _lineNo = 0;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var lineNo = _lineNo++;

                    if (line.Length == 0 || line[0] != '◆')
                    {
                        continue;
                    }

                    var m = Regex.Match(line, @"◆(\w+)◆(.+)$");

                    if (!m.Success || m.Groups.Count != 3)
                    {
                        throw new Exception($"Bad format at line: {lineNo}");
                    }

                    var strIndex = m.Groups[1].Value;
                    var strVal = m.Groups[2].Value;

                    switch (strIndex[0])
                    {
                        // character name
                        case 'A':
                        {
                            var index = int.Parse(strIndex[1..], NumberStyles.HexNumber);

                            if (index < 0 || index >= _charNameOffsetList.Length)
                            {
                                throw new Exception($"Bad text index at line: {lineNo}");
                            }

                            translatedCharName.TryAdd(index, strVal);

                            break;
                        }
                        // message
                        case 'B':
                        {
                            var index = int.Parse(strIndex[1..], NumberStyles.HexNumber);

                            if (index < 0 || index >= _messageOffsetList.Length)
                            {
                                throw new Exception($"Bad text index at line: {lineNo}");
                            }

                            translatedMessage.TryAdd(index, strVal);

                            break;
                        }
                        default:
                        {
                            throw new Exception($"Bad text type at line: {lineNo}");
                        }
                    }
                }
            }

            // Create offset list and block

            Console.WriteLine("Write character name block");

            var stream = new MemoryStream(16 * 1024 * 1024); // 16 Mib
            var writer = new BinaryWriter(stream);

            var newCharNameOffsetList = new int[_charNameOffsetList.Length];

            for (var i = 0; i < _charNameOffsetList.Length; i++)
            {
                // Try to get the translated text, otherwise get the original text.
                if (!translatedCharName.TryGetValue(i, out string s))
                {
                    s = Binary.GetCString(_scriptBuffer, _charNameOffsetList[i]);
                }

                var bytes = Encoding.Unicode.GetBytes(s);

                newCharNameOffsetList[i] = Convert.ToInt32(stream.Position) / 2;

                writer.Write(bytes);
                writer.Write((short)0); // null-terminated
            }

            var newCharNameBlock = stream.ToArray();

            // Create offset list and block

            Console.WriteLine("Write message block");

            stream.Reset();

            var newMessageOffsetList = new int[_messageOffsetList.Length];

            for (var i = 0; i < _messageOffsetList.Length; i++)
            {
                // Try to get the translated text, otherwise get the original text.
                if (!translatedMessage.TryGetValue(i, out string s))
                {
                    s = Binary.GetCString(_scriptBuffer, _messageOffsetList[i]);
                }

                var bytes = Encoding.Unicode.GetBytes(s);

                newMessageOffsetList[i] = Convert.ToInt32(stream.Position) / 2;

                writer.Write(bytes);
                writer.Write((short)0); // null-terminated
            }

            var newMessageBlock = stream.ToArray();

            // Update the script

            Console.WriteLine("Building script ...");

            stream.Reset();

            // Align to 16 bytes
            void Alignment()
            {
                var numBytesToPad = Binary.GetAlignedValue(Convert.ToInt32(stream.Position), 16) - Convert.ToInt32(stream.Position);

                for (var i = 0; i < numBytesToPad; i++)
                {
                    writer.Write((byte)0);
                }
            }

            // Copy original

            writer.Write(_scriptBuffer, 0, _charNameOffsetListOffset);

            // Write character name offset list

            for (var i = 0; i < newCharNameOffsetList.Length; i++)
            {
                writer.Write(newCharNameOffsetList[i]);
            }

            Alignment();

            // Write character name block

            var newCharNameBlockOffset = Convert.ToInt32(stream.Position);

            writer.Write(newCharNameBlock);

            Alignment();

            // Write message offset list

            var newMessageOffsetListOffset = Convert.ToInt32(stream.Position);

            for (var i = 0; i < newMessageOffsetList.Length; i++)
            {
                writer.Write(newMessageOffsetList[i]);
            }

            Alignment();

            // Write message block

            var newMessageBlockOffset = Convert.ToInt32(stream.Position);

            writer.Write(newMessageBlock);

            // Update offset

            stream.Position = 0x90;
            writer.Write(newCharNameBlockOffset);
            stream.Position = 0x98;
            writer.Write(newMessageOffsetListOffset);
            stream.Position = 0xA0;
            writer.Write(newMessageBlockOffset);

            // Done

            _scriptBuffer = stream.ToArray();

            Console.WriteLine("Done");
        }
    }
}
