using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LocresReader
{
    static class Program
    {
        static void Main(string[] args)
        {
            var dict = new SortedDictionary<string, SortedDictionary<string, SortedDictionary<string, string>>>();
            string readString(BinaryReader br)
            {
                var length = br.ReadInt32();
                if (length > 0)
                {
                    return Encoding.ASCII.GetString(br.ReadBytes(length)).TrimEnd('\0');
                }
                else if (length == 0)
                {
                    return "";
                }
                else
                {
                    var data = br.ReadBytes((-1 - length) * 2);
                    br.ReadBytes(2); // Null terminated string I guess?
                    return Encoding.Unicode.GetString(data);
                }
            }
            foreach (var lang in new[] { /* "de", "en", "es", "fr", "it", "ja", "ko", "pl", "pt-BR", "ru", "th", "zh-Hans", "zh-Hant" */ "en", "zh-Hans" })
            {
                var reader = new BinaryReader(new MemoryStream(File.ReadAllBytes($@"E:\Projects\dbdt\dbd\DeadByDaylight\Content\Localization\Game\{lang}\DeadByDaylight.locres")));
                var MagicNumber = reader.ReadBytes(16);
                var VersionNumber = reader.ReadByte();
                var LocalizedStringArrayOffset = reader.ReadInt64();
                var CurrentFileOffset = reader.BaseStream.Position;
                reader.BaseStream.Position = LocalizedStringArrayOffset;
                var arrayLength = reader.ReadInt32();
                var LocalizedStringArray = new string[arrayLength];
                for (var i = 0; i < arrayLength; i++)
                {
                    LocalizedStringArray[i] = readString(reader);
                }
                reader.BaseStream.Position = CurrentFileOffset;
                var NamespaceCount = reader.ReadUInt32();
                for (var i = 0; i < NamespaceCount; i++)
                {
                    var Namespace = readString(reader);
                    if (!dict.ContainsKey(Namespace))
                    {
                        dict[Namespace] = new SortedDictionary<string, SortedDictionary<string, string>>();
                    }
                    var KeyCount = reader.ReadInt32();
                    for (var j = 0; j < KeyCount; j++)
                    {
                        var Key = readString(reader);
                        if (!dict[Namespace].ContainsKey(Key))
                        {
                            dict[Namespace][Key] = new SortedDictionary<string, string>();
                        }
                        dict[Namespace][Key]["SourceStringHash"] = BitConverter.ToString(reader.ReadBytes(4)).Replace("-", "");
                        var LocalizedStringIndex = reader.ReadInt32();
                        dict[Namespace][Key][lang] = LocalizedStringArray[LocalizedStringIndex];
                    }
                }
                #region Legacy
                /*
                var data = File.ReadAllBytes($@"E:\Projects\dbdt\dbd\DeadByDaylight\Content\Localization\Game\{lang}\DeadByDaylight.locres");
                var dataStart = BitConverter.ToInt32(data, 17);
                var count = BitConverter.ToInt32(data, dataStart);
                Console.WriteLine($"{lang} has {count} items.");
                var keysData = new byte[dataStart - 0x29];
                Buffer.BlockCopy(data, 0x29, keysData, 0, keysData.Length);
                dataStart += 4;
                var valueData = new byte[data.Length - dataStart];
                Buffer.BlockCopy(data, dataStart, valueData, 0, valueData.Length);
                var kbr = new BinaryReader(new MemoryStream(keysData));
                var vbr = new BinaryReader(new MemoryStream(valueData));
                for (var i = 0; i < count; i++)
                {
                    var str = Encoding.Unicode.GetString(kbr.ReadBytes(64)); // 一个Unicode的32位十六进制字符串，貌似是Hash
                    kbr.ReadBytes(2); // 总是00 00
                    var key = kbr.ReadInt32(); // 无规律的疑似随机数，和Hash相关（Hash相同时这个值相同）
                    var unknown = kbr.ReadInt32(); // 从0开始 貌似是按顺序增加
                    kbr.ReadBytes(4); // 总是DF FF FF FF
                    if (!dict.ContainsKey(key))
                    {
                        dict[key] = new Loc();
                    }
                    // 字符数量 ^ 0xFFFFFFFF，* 2后为字节数
                    var value = Encoding.Unicode.GetString(vbr.ReadBytes(-2 - (vbr.ReadInt32() * 2))) + $" #{str},{key},{unknown}";
                    vbr.ReadInt16(); // 总是0
                    switch (lang)
                    {
                        case "en":
                            dict[key].English = value;
                            break;
                        case "zh-Hans":
                            dict[key].Chinese = value;
                            break;
                    }
                }
                */
                #endregion
            }
            File.WriteAllText(@"E:\Projects\dbdt\data.json", JsonConvert.SerializeObject(dict, Formatting.Indented));
        }
    }
}
