using Fusion;
using UnityEngine;

// Định nghĩa các nút bấm có thể có.
public enum MyButtons
{
    Forward,   // W
    Backward,  // S
    Left,      // A
    Right,     // D
    Jump,      // Space
}

// Cấu trúc dữ liệu Input, phải kế thừa từ INetworkInput
public struct NetworkInputData : INetworkInput
{
    public NetworkButtons Buttons; // Sử dụng NetworkButtons của Fusion để lưu trạng thái các nút bấm
    public Vector2 CameraRotation; // Dữ liệu xoay camera bằng chuột
}