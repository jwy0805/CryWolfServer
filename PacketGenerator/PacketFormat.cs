using System;
using System.Collections.Generic;
using System.Text;

namespace PacketGenerator
{
	static class PacketFormat
	{
		// {0} 패킷 등록
		public static readonly string ManagerFormat =
@"using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance {{ get {{ return _instance; }} }}
	#endregion

	PacketManager()
	{{
		Register();
	}}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler {{ get; set; }}

	public void Register()
	{{{0}
	}}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		if (_onRecv.TryGetValue(id, out var action))
			action.Invoke(session, buffer, id);
	}}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if (CustomHandler != null)
		{{
			CustomHandler.Invoke(session, pkt, id);
		}}
		else
		{{
			if (_handler.TryGetValue(id, out var action))
				action.Invoke(session, pkt);
		}}
	}}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{{
		if (_handler.TryGetValue(id, out var action))
			return action;
		return null;
	}}
}}";

		// {0} MsgId
		// {1} 패킷 이름
		public static readonly string ManagerRegisterFormat =
@"		
		_onRecv.Add((ushort)MessageId.{0}, MakePacket<{1}>);
		_handler.Add((ushort)MessageId.{0}, PacketHandler.{1}Handler);";

	}
}
