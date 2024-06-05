namespace PlayerTrack.Models;

public enum LodestoneStatus
{
    Unverified = 0, // created but not yet looked up
    Verified = 1, // successfully looked up
    Failed = 2, // failed to look up but can be retried
    Banned = 3, // failed to look up and cannot be retried
    NotApplicable = 4, // can't be looked up (e.g. test character)
    Blocked = 5, // blocked from looking up (e.g. dependency on another lookup)
    Cancelled = 6, // superseded by another lookup or system found bad data
    Unavailable = 7, // not able to lookup (e.g. deleted character)
    Invalid = 8, // invalid character name/world (shouldn't happen...)
}
