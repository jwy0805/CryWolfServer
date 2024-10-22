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
		_onRecv.Add((ushort)MessageId.CEnterGame, MakePacket<C_EnterGame>);
		_handler.Add((ushort)MessageId.CEnterGame, PacketHandler.C_EnterGameHandler);		
		_onRecv.Add((ushort)MessageId.CEnterGameNpc, MakePacket<C_EnterGameNpc>);
		_handler.Add((ushort)MessageId.CEnterGameNpc, PacketHandler.C_EnterGameNpcHandler);		
		_onRecv.Add((ushort)MessageId.CSetSession, MakePacket<C_SetSession>);
		_handler.Add((ushort)MessageId.CSetSession, PacketHandler.C_SetSessionHandler);		
		_onRecv.Add((ushort)MessageId.CSpawn, MakePacket<C_Spawn>);
		_handler.Add((ushort)MessageId.CSpawn, PacketHandler.C_SpawnHandler);		
		_onRecv.Add((ushort)MessageId.CPlayerMove, MakePacket<C_PlayerMove>);
		_handler.Add((ushort)MessageId.CPlayerMove, PacketHandler.C_PlayerMoveHandler);		
		_onRecv.Add((ushort)MessageId.CMove, MakePacket<C_Move>);
		_handler.Add((ushort)MessageId.CMove, PacketHandler.C_MoveHandler);		
		_onRecv.Add((ushort)MessageId.CState, MakePacket<C_State>);
		_handler.Add((ushort)MessageId.CState, PacketHandler.C_StateHandler);		
		_onRecv.Add((ushort)MessageId.CEffectActivate, MakePacket<C_EffectActivate>);
		_handler.Add((ushort)MessageId.CEffectActivate, PacketHandler.C_EffectActivateHandler);		
		_onRecv.Add((ushort)MessageId.CBaseSkillRun, MakePacket<C_BaseSkillRun>);
		_handler.Add((ushort)MessageId.CBaseSkillRun, PacketHandler.C_BaseSkillRunHandler);		
		_onRecv.Add((ushort)MessageId.CSkillUpgrade, MakePacket<C_SkillUpgrade>);
		_handler.Add((ushort)MessageId.CSkillUpgrade, PacketHandler.C_SkillUpgradeHandler);		
		_onRecv.Add((ushort)MessageId.CPortraitUpgrade, MakePacket<C_PortraitUpgrade>);
		_handler.Add((ushort)MessageId.CPortraitUpgrade, PacketHandler.C_PortraitUpgradeHandler);		
		_onRecv.Add((ushort)MessageId.CUnitUpgrade, MakePacket<C_UnitUpgrade>);
		_handler.Add((ushort)MessageId.CUnitUpgrade, PacketHandler.C_UnitUpgradeHandler);		
		_onRecv.Add((ushort)MessageId.CUnitRepair, MakePacket<C_UnitRepair>);
		_handler.Add((ushort)MessageId.CUnitRepair, PacketHandler.C_UnitRepairHandler);		
		_onRecv.Add((ushort)MessageId.CChangeResource, MakePacket<C_ChangeResource>);
		_handler.Add((ushort)MessageId.CChangeResource, PacketHandler.C_ChangeResourceHandler);		
		_onRecv.Add((ushort)MessageId.CLeave, MakePacket<C_Leave>);
		_handler.Add((ushort)MessageId.CLeave, PacketHandler.C_LeaveHandler);		
		_onRecv.Add((ushort)MessageId.CUnitSpawnPos, MakePacket<C_UnitSpawnPos>);
		_handler.Add((ushort)MessageId.CUnitSpawnPos, PacketHandler.C_UnitSpawnPosHandler);		
		_onRecv.Add((ushort)MessageId.CGetRanges, MakePacket<C_GetRanges>);
		_handler.Add((ushort)MessageId.CGetRanges, PacketHandler.C_GetRangesHandler);		
		_onRecv.Add((ushort)MessageId.CGetSpawnableBounds, MakePacket<C_GetSpawnableBounds>);
		_handler.Add((ushort)MessageId.CGetSpawnableBounds, PacketHandler.C_GetSpawnableBoundsHandler);		
		_onRecv.Add((ushort)MessageId.CSetTextUI, MakePacket<C_SetTextUI>);
		_handler.Add((ushort)MessageId.CSetTextUI, PacketHandler.C_SetTextUIHandler);		
		_onRecv.Add((ushort)MessageId.CUnitDelete, MakePacket<C_UnitDelete>);
		_handler.Add((ushort)MessageId.CUnitDelete, PacketHandler.C_UnitDeleteHandler);		
		_onRecv.Add((ushort)MessageId.CSetUpgradePopup, MakePacket<C_SetUpgradePopup>);
		_handler.Add((ushort)MessageId.CSetUpgradePopup, PacketHandler.C_SetUpgradePopupHandler);		
		_onRecv.Add((ushort)MessageId.CSetUpgradeButtonCost, MakePacket<C_SetUpgradeButtonCost>);
		_handler.Add((ushort)MessageId.CSetUpgradeButtonCost, PacketHandler.C_SetUpgradeButtonCostHandler);		
		_onRecv.Add((ushort)MessageId.CSetUnitUpgradeCost, MakePacket<C_SetUnitUpgradeCost>);
		_handler.Add((ushort)MessageId.CSetUnitUpgradeCost, PacketHandler.C_SetUnitUpgradeCostHandler);		
		_onRecv.Add((ushort)MessageId.CSetUnitDeleteCost, MakePacket<C_SetUnitDeleteCost>);
		_handler.Add((ushort)MessageId.CSetUnitDeleteCost, PacketHandler.C_SetUnitDeleteCostHandler);		
		_onRecv.Add((ushort)MessageId.CSetUnitRepairCost, MakePacket<C_SetUnitRepairCost>);
		_handler.Add((ushort)MessageId.CSetUnitRepairCost, PacketHandler.C_SetUnitRepairCostHandler);		
		_onRecv.Add((ushort)MessageId.CSetBaseSkillCost, MakePacket<C_SetBaseSkillCost>);
		_handler.Add((ushort)MessageId.CSetBaseSkillCost, PacketHandler.C_SetBaseSkillCostHandler);
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