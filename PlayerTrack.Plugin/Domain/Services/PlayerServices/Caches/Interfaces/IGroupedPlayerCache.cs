using System;
using System.Collections.Generic;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches.Interfaces;

public interface IGroupedPlayerCache: IPlayerCache
{
    Player? FindFirst(int groupId, Func<Player, bool> filter);
    List<Player> Get(int groupId, int start, int count);
    List<Player> Get(int groupId, Func<Player, bool> filter, int start, int count);
    List<Player> GetAll(int groupId);
    Dictionary<int, Player>? GetGroup(int groupId);
    void AddGroup(int groupId);
    void RemoveGroup(int groupId);
    int Count(int groupId);
    int Count(int groupId, Func<Player, bool> filter);
}