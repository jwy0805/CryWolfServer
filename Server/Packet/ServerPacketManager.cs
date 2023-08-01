using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using System;
using System.Collections.Generic;

class PacketManager
{
	#region Singleton
	static PacketManager _instance = new PacketManager();
	public static PacketManager Instance { get { return _instance; } }
	#endregion

	PacketManager()
	{
		Register();
	}

	Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv = new();
	Dictionary<ushort, Action<PacketSession, IMessage>> _handler = new();
		
	public Action<PacketSession, IMessage, ushort> CustomHandler { get; set; }

	public void Register()
	{		
		_onRecv.Add((ushort)MessageId.CSpawn, MakePacket<C_Spawn>);
		_handler.Add((ushort)MessageId.CSpawn, PacketHandler.C_SpawnHandler);		
		_onRecv.Add((ushort)MessageId.CPlayerMove, MakePacket<C_PlayerMove>);
		_handler.Add((ushort)MessageId.CPlayerMove, PacketHandler.C_PlayerMoveHandler);		
		_onRecv.Add((ushort)MessageId.CMove, MakePacket<C_Move>);
		_handler.Add((ushort)MessageId.CMove, PacketHandler.C_MoveHandler);		
		_onRecv.Add((ushort)MessageId.CSetDest, MakePacket<C_SetDest>);
		_handler.Add((ushort)MessageId.CSetDest, PacketHandler.C_SetDestHandler);		
		_onRecv.Add((ushort)MessageId.CAttack, MakePacket<C_Attack>);
		_handler.Add((ushort)MessageId.CAttack, PacketHandler.C_AttackHandler);		
		_onRecv.Add((ushort)MessageId.CSkill, MakePacket<C_Skill>);
		_handler.Add((ushort)MessageId.CSkill, PacketHandler.C_SkillHandler);
	}

	public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer)
	{
		ushort count = 0;

		ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
		count += 2;
		ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
		count += 2;

		if (_onRecv.TryGetValue(id, out var action))
			action.Invoke(session, buffer, id);
	}

	void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
	{
		T pkt = new T();
		pkt.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

		if (CustomHandler != null)
		{
			CustomHandler.Invoke(session, pkt, id);
		}
		else
		{
			if (_handler.TryGetValue(id, out var action))
				action.Invoke(session, pkt);
		}
	}

	public Action<PacketSession, IMessage> GetPacketHandler(ushort id)
	{
		if (_handler.TryGetValue(id, out var action))
			return action;
		return null;
	}
}