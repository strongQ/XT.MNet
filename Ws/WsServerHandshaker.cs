using System.Reflection.PortableExecutable;
using System.Text.RegularExpressions;
using XT.MNet.Tcp;
using XT.MNet.Tcp.Interfaces;

namespace XT.MNet.Ws;

public sealed partial class WsServerHandshaker : ITcpServerHandshaker
{

    public bool Handshake(TcpServerConnection connection, ref ReadOnlySequence<byte> buffer, out SequencePosition position)
    {

        var reader = new SequenceReader<byte>(buffer);

        // 尝试读取到分隔符
        if (!reader.TryReadTo(out ReadOnlySequence<byte> target, Delimiter.Span, true))
        {
            position = buffer.Start;
            return false;
        }

        string content = Encoding.UTF8.GetString(target);
        // 更新正则表达式以匹配 GET 请求
        Regex getRegex = new Regex(@"^GET\s+\/\S*\s+HTTP\/1\.1", RegexOptions.IgnoreCase);
        Match getRegexMatch = getRegex.Match(content);

        // TODO: 获取并处理必要的头部信息

        if (getRegexMatch.Success)
        {

            // 确保 WebSocketKeyRegex 方法能够正确提取 Sec-WebSocket-Key
            var mem = GetHandshakeBody(content);
            if (mem.IsEmpty)
            {
                position = buffer.Start;
                return false;
            }

            connection.Send(mem);

            buffer = buffer.Slice(target.Length);
            position = buffer.Start;

            return true;

        }

        position = buffer.Start;
        return false;

    }

    public void StartHandshake(TcpServerConnection connection)
    {
        // 服务器等待客户端的第一个请求
    }

    private static readonly Memory<byte> Delimiter = new Memory<byte>(new byte[] { 13, 10, 13, 10 });


    private static Memory<byte> GetHandshakeBody(string data)
    {
        // 使用更新的正则表达式来匹配 Sec-WebSocket-Key
        Regex webSocketKeyRegex = new Regex("Sec-WebSocket-Key: (.*)", RegexOptions.IgnoreCase);
        Match match = webSocketKeyRegex.Match(data);

        if (!match.Success)
        {
            return Memory<byte>.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(match.Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        var hash = Convert.ToBase64String(SHA1.HashData(bytes));

        return Encoding.UTF8.GetBytes(
                "HTTP/1.1 101 Switching Protocols" + NewLine
                + "Connection: Upgrade" + NewLine
                + "Upgrade: websocket" + NewLine
                + "Sec-Websocket-Accept: " + hash + NewLine
                + NewLine
            );
    }

    private static readonly string NewLine = "\r\n"; // HTTP 协议

    // 移除 GeneratedRegex 特性，直接在 GetHandshakeBody 中使用正则表达式
    // private static partial Regex WebSocketKeyRegex();
}
