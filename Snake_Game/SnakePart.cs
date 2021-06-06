using System.Windows;
using Interface_for_Snake;

namespace Snake_Game
{
    /// <summary>
    /// Создание змейки
    /// </summary>
    public class SnakePart : ISnake
    {
        public UIElement UiElement { get; set; } 

        public Point Position { get; set; }

        public bool IsHead { get; set; }
    }
}
