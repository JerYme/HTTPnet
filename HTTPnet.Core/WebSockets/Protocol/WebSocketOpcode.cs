namespace HTTPnet.Core.WebSockets.Protocol
{
    public enum WebSocketOpcode
    {
        Continuation = 0,
        Text = 1,
        Binary = 2,
        FurtherNonControl3 = 3,
        FurtherNonControl4 = 4,
        FurtherNonControl5 = 5,
        FurtherNonControl6 = 6,
        FurtherNonControl7 = 7,
        // 0x3-0x7 have no meaning,
        ConnectionClose = 8,
        Ping = 9,
        Pong = 0xA,
        FurtherControlB = 0xB,
        FurtherControlC = 0xC,
        FurtherControlD = 0xD,
        FurtherControlE = 0xE,
        FurtherControlF = 0xF,
    }

    public static class WebSocketOpcodeExtension
    {
        public static bool IsControl(this WebSocketOpcode opcode) => opcode >= WebSocketOpcode.ConnectionClose && opcode <= WebSocketOpcode.FurtherControlF;
    }
}
