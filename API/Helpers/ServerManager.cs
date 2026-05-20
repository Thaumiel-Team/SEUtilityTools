using Okolni.Source.Query;
using Okolni.Source.Query.Responses;
using SEUtilityTools.API.Data;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace SEUtilityTools.API.Helpers
{
    public class ServerManager
    {
        public static Dictionary<Guid, ServerData> PreviousQueries = [];
        public static event Action<ServerData>? OnSentQuery;
        public static async Task<ServerData?> QueryServerAsync(string host, int port)
        {
            return await Task.Run(() =>
            {
                try
                {
                    QueryConnection conn = new()
                    {
                        Host = host,
                        Port = port
                    };

                    conn.Connect();

                    InfoResponse infoResp = conn.GetInfo();
                    PlayerResponse playersResp = conn.GetPlayers();

                    ServerData info = new()
                    {
                        Name = infoResp?.Name ?? string.Empty,
                        PlayerCount = infoResp?.Players ?? 0,
                        MaxPlayers = infoResp?.MaxPlayers ?? 0,
                        Players = playersResp.Players ?? [],
                        Ip = host,
                        Port = port,
                        PlayerResponse = playersResp ?? new(),
                        InfoResponse = infoResp ?? new(),
                    };

                    OnSentQuery?.Invoke(info);
                    LogManager.Info($"Successfully queried {infoResp?.Name ?? string.Empty}: {info.Name} ({info.PlayerCount}/{info.MaxPlayers})");
                    PreviousQueries.Add(Guid.NewGuid(), info);
                    return info;
                }
                catch (Exception innerEx)
                {
                    LogManager.Warn($"Query failed for {host}:{port} - {innerEx.Message}");
                    MessageBoxManager.GetMessageBoxStandard("Query Failed", $"Query failed for {host}:{port} - {innerEx.Message}", ButtonEnum.Ok, Icon.Error).ShowAsync();

                    return null;
                }
            });
        }
    }
}