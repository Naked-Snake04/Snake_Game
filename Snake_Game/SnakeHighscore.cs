using Interface_for_Snake;

namespace Snake_Game
{
    public class SnakeHighscore : ISnakeHighscore
    {
        public string PlayerName { get; set; }

        public int Score { get; set; }
    }

}