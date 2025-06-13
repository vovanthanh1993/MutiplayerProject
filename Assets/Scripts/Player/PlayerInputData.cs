using Fusion;
using UnityEngine;

public struct PlayerInputData : INetworkInput
{
    public Vector2 MoveInput;
    public bool JumpPressed;
}
