using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace PacketGenerator;

public static class Program
{
    private static string _clientRegister = string.Empty;
    private static string _serverRegister = string.Empty;

    public static void Main(string[] args)
    {
        string file = "../../Common/Packet/Packet.proto";
        if (args.Length >= 1)
            file = args[0];

        bool startParsing = false;
        foreach (string line in File.ReadAllLines(file))
        {
            if (!startParsing && line.Contains("enum MessageId"))
            {
                startParsing = true;
                continue;
            }

            if (!startParsing)
                continue;

            if (line.Contains("}"))
                break;

            string[] names = line.Trim().Split(" =");
            if (names.Length == 0)
                continue;

            string name = names[0];
            if (name.StartsWith("S_"))
            {
                string[] words = name.Split("_");

                string msgName = "";
                foreach (string word in words)
                    msgName += FirstCharToUpper(word);

                string packetName = $"S_{msgName.Substring(1)}";
                _clientRegister += string.Format(PacketFormat.ManagerRegisterFormat, msgName, packetName);
            }
            else if (name.StartsWith("C_"))
            {
                string[] words = name.Split("_");

                string msgName = "";
                foreach (string word in words)
                    msgName += FirstCharToUpper(word);

                string packetName = $"C_{msgName.Substring(1)}";
                _serverRegister += string.Format(PacketFormat.ManagerRegisterFormat, msgName, packetName);
            }
        }

        string clientManagerText = string.Format(PacketFormat.ManagerFormat, _clientRegister);
        File.WriteAllText("ClientPacketManager.cs", clientManagerText);
        string serverManagerText = string.Format(PacketFormat.ManagerFormat, _serverRegister);
        File.WriteAllText("ServerPacketManager.cs", serverManagerText);
    }

    private static string FirstCharToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";
        return input[0].ToString().ToUpper() + input.Substring(1).ToLower();
    }
}
