namespace Game.Contracts.Enums;

public enum MoveResultCode
{
    Success = 0,
    InvalidMove = 1,
    NotYourTurn = 2,
    SessionNotFound = 3,
    SessionCompleted = 4,
    TimedOut = 5
}
