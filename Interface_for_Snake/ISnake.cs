using System.Windows;

namespace Interface_for_Snake
{
    public interface ISnake
    {
        UIElement UiElement { get; set; }

        Point Position { get; set; }

        bool IsHead { get; set; }
    }
}
