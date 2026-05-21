using System;
using System.Collections.Generic;
using System.Text;

namespace SEUtilityTools.API.Data
{
    public class ServerDataDto
    {
        public string Name { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
        public int MaxPlayers { get; set; }
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; }
        public List<PlayerDto> Players { get; set; } = [];
    }
}
