using Fusion;
using UnityEngine;

// Sử dụng một enum để định nghĩa các nút bấm cho dễ đọc và quản lý
public enum InvectorButtons
{
    Jump = 0,
    Sprint = 1,
    Strafe = 2
}

// Struct này chứa tất cả dữ liệu mà client sẽ gửi cho host ở mỗi tick
public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public NetworkButtons buttons; // Struct có sẵn của Fusion để xử lý các nút bấm
}
