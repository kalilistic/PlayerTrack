using System.Collections.Generic;
using PlayerTrack.Models;

namespace PlayerTrack.Domain.Caches.Interfaces;

public interface IPlayerCache
{
    void Initialize(IComparer<Player> comparer);
    void Add(Player playerToAdd);
    void Remove(Player playerToRemove);
    void Resort(IComparer<Player> comparer);
    Player? Get(int playerId);
}