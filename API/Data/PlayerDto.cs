namespace SEUtilityTools.API.Data
{
    public class PlayerDto
    {
        public string Name { get; set; } = string.Empty;
        public int Score { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
