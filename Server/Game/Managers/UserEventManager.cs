using Google.Protobuf.Protocol;
using Server.Data;

namespace Server.Game;

public sealed class UserEventManager
{
    public static UserEventManager Instance { get; } = new();
    
    public async Task EventProgressHandler(List<int> userIds, int roomId, string eventKey, EventCounterKey counterKey)
    {
        var packet = new SendEventProgressPacketRequired
        {
            UserIds = userIds,
            RoomId = roomId,
            EventKey = eventKey,
        };
        
        var res = await NetworkManager.Instance.SendRequestToApiAsync<SendEventProgressPacketResponse>(
            "Event/SendEventProgress", packet, HttpMethod.Put);

        if (res is not { SendEventProgressOk: true })
        {
            Console.WriteLine("Error in SendEventProgressPacketResponse");
        }
    }
}