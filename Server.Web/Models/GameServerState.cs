namespace Server.Web.Models;

public enum GameServerState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Faulted
}

public sealed record ServerMetricsSnapshot(
    DateTimeOffset CapturedAt,
    GameServerState State,
    int Connections,
    int ActiveObjects,
    int Objects,
    int ProcessedObjects,
    int LoopCount,
    long TotalBytesReceived,
    long TotalBytesSent,
    long DownloadSpeed,
    long UploadSpeed,
    long ConnectionDelayMilliseconds,
    long SaveDelayMilliseconds,
    int EmailsSent,
    string? LastError);
