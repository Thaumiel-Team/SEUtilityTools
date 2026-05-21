using Okolni.Source.Query.Responses;

namespace SEUtilityTools.API.Data
{
    public class ServerData
    {
        public string Name { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public List<Player> Players { get; set; } = [];
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
        public required PlayerResponse PlayerResponse { get; set; }
        public required InfoResponse InfoResponse { get; set; }

        public ServerDataDto ToDto() => new()
        {
            Name = Name,
            PlayerCount = PlayerCount,
            MaxPlayers = MaxPlayers,
            Ip = Ip,
            Port = Port,
            Players = Players.Select(p => new PlayerDto
            {
                Name = p.Name ?? string.Empty,
                Duration = p.Duration
            }).ToList()
        };
    }
}