using System;
using System.Collections.Generic;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches.Interfaces;

public interface IBasicPlayerCache: IPlayerCache
{
    Player? FindFirst(Func<Player, bool> filter);
    List<Player> Get(int start, int count);
    List<Player> Get(Func<Player, bool> filter, int start, int count);
    List<Player> Get(Func<Player, bool> filter);
    List<Player> GetAll();
    int Count();
    int Count(Func<Player, bool> filter);
}