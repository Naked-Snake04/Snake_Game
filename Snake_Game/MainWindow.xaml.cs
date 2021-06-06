using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Speech.Synthesis;

namespace Snake_Game
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SpeechSynthesizer speechSynthesizer = new SpeechSynthesizer(); //Учим змейку разговаривать ;)
        //Добавляем таймер для поддержания змйки в движении
        private System.Windows.Threading.DispatcherTimer gameTickTimer = /*таймеры обычно используются как механизм, позволяющий засекать точные промежутки времени для повторяющихся действий. 
                                                                          * То есть срабатывание таймера (tick) происходит через определенный интервал времени и в результате выполняется определенный код. 
                                                                          * Это именно то, что нам нужно для поддержания нашей Змейки в движении*/
            new System.Windows.Threading.DispatcherTimer();

        private Random random = new Random(); //Рандом
        private UIElement snakeFood = null; //Еда
        private SolidColorBrush foodBrush = Brushes.Red; //кисть красного цвета

        const int SnakeSquareSize = 20; //Размер квадрата змейки
        const int SnakeStartLength = 3; //Стартовая длина змейки
        const int SnakeStartSpeed = 400; //Стартовая скорость
        const int SnakeSpeedThreshold = 100; //Порог скорости
        const int MaxHighscoreListEntryCount = 5; //Максимальное число записей в таблицу рекордов

        private SolidColorBrush snakeBodyBrush = Brushes.Green; //кисть для тела змейки
        private SolidColorBrush snakeHeadBrush = Brushes.YellowGreen; //кисть для головы змейки
        private List<SnakePart> snakeParts = new List<SnakePart>(); //список для хранение ссылки на каждый из фрагментов змейки

        public enum SnakeDirection { Left, Right, Up, Down}; //перечисление для представления направления движения
        private SnakeDirection snakeDirection = SnakeDirection.Right; //текущее направление - направо
        private int snakeLength; //длина змейки
        private int currentScore = 0; //текущий результат

        /// <summary>
        /// Главное окно
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            LoadHighscoreList();
        }

        /// <summary>
        /// Список рекордов
        /// </summary>
        public ObservableCollection<SnakeHighscore> HighscoreList /*ObservableCollection: 
                                                                   * Представляет динамическую коллекцию данных, которая выдает уведомления при добавлении и удалении элементов,
                                                                   * а также при обновлении списка.*/
        {
            get;
            set;
        } = new ObservableCollection<SnakeHighscore>();
        
        /// <summary>
        /// Загрузить список рекордов
        /// </summary>
        private void LoadHighscoreList()
        {
            try
            {
                if (File.Exists("snake_highscorelist.xml"))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<SnakeHighscore>)); /*Сериализует и десериализует объекты в XML-документы и из них. XmlSerializer позволяет контролировать способ кодирования объектов в XML. 
                                                                                             * Сериализация XML — это процесс преобразования открытых свойств и полей объекта в серийный формат (в данном случае в формат XML) для хранения или транспортировки.*/
                    using (Stream reader = new FileStream("snake_highscorelist.xml", FileMode.Open)) //Оператор using позволяет создавать объект в блоке кода, по завершению которого вызывается метод Dispose у этого объекта, и, таким образом, объект уничтожается.
                    {
                        List<SnakeHighscore> tempList = (List<SnakeHighscore>)serializer.Deserialize(reader); /*При десериализации объект повторно создается в исходном состоянии из выходных данных XML.*/
                        this.HighscoreList.Clear();
                        foreach (var item in tempList.OrderByDescending(x => x.Score)) //показать рекорды в обратном порядке (использование LINQ).
                            this.HighscoreList.Add(item);
                    }
                }
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        /// <summary>
        /// Сохранить список рекордов
        /// </summary>
        private void SaveHighscoreList()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<SnakeHighscore>));
                using (Stream writer = new FileStream("snake_highscorelist.xml", FileMode.Create))
                {
                    serializer.Serialize(writer, this.HighscoreList);
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        
        /// <summary>
        /// Начать новую игру
        /// </summary>
        private void StartNewGame()
        {
            speechSynthesizer.SpeakAsync("Я родился!"); //Синтез речи
            /*Закрываем все возможные окна, кроме игрового поля*/
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrEndOfGame.Visibility = Visibility.Collapsed;
            //Убираем потенциальных мёртвую змею и оставшуюся еду
            foreach (SnakePart snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArena.Children.Remove(snakeBodyPart.UiElement);
            }
            snakeParts.Clear();
            if (snakeFood != null)
                GameArena.Children.Remove(snakeFood);
            //обновляем всё
            currentScore = 0;
            snakeLength = SnakeStartLength; //Устанавливем длину
            snakeDirection = SnakeDirection.Right; //Изначальное направленеи - вправо
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) }); //добавляем одну часть змейки в список snakeParts, задавая ей начальную позицию
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            //Рисуем змейку
            DrawSnake();
            //Рисуем еду
            DrawSnakeFood();
            //Обновляем статус
            UpdateGameStatus();
            //Запускаем таймер
            gameTickTimer.IsEnabled = true;
        }
        
        /// <summary>
        /// Запускаем змейку
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /*Делаем движения змейки зависимыми от таймера*/
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }

        /// <summary>
        /// Делаем окно передвигаемым
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Закрыть приложение
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// "Добавить в список рекордов"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddToHighscoreList_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = 0;
            //Куда следует вставить новую запись?
            if ((this.HighscoreList.Count > 0) && (currentScore < this.HighscoreList.Max(x => x.Score))) 
            {
                SnakeHighscore justAbove = this.HighscoreList.OrderByDescending(x => x.Score).First(x => x.Score >= currentScore);
                if (justAbove != null)
                    newIndex = this.HighscoreList.IndexOf(justAbove) + 1;
            }
            // Создаём и вставляем новую запись
            this.HighscoreList.Insert(newIndex, new SnakeHighscore()
            {
                PlayerName = txtPlayerName.Text,
                Score = currentScore
            });
            //Убеждаемся, что количество записей не превышает максимальное
            while (this.HighscoreList.Count > MaxHighscoreListEntryCount)
                this.HighscoreList.RemoveAt(MaxHighscoreListEntryCount);

            SaveHighscoreList();

            bdrNewHighscore.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Задаём управление змейкой при помощи кнопок WASD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up: //вперёд
                    if (bdrWelcomeMessage.Visibility != Visibility.Visible && bdrEndOfGame.Visibility != Visibility.Visible
                        && bdrHighscoreList.Visibility != Visibility.Visible && bdrNewHighscore.Visibility != Visibility.Visible)
                    {
                        if (snakeDirection != SnakeDirection.Down)
                            snakeDirection = SnakeDirection.Up;
                    }
                    break;
                case Key.Down: //назад
                    if (bdrWelcomeMessage.Visibility != Visibility.Visible && bdrEndOfGame.Visibility != Visibility.Visible
                        && bdrHighscoreList.Visibility != Visibility.Visible && bdrNewHighscore.Visibility != Visibility.Visible)
                    {
                        if (snakeDirection != SnakeDirection.Up)
                            snakeDirection = SnakeDirection.Down;
                    }
                    break;
                case Key.Left: //влево
                    if (bdrWelcomeMessage.Visibility != Visibility.Visible && bdrEndOfGame.Visibility != Visibility.Visible
                        && bdrHighscoreList.Visibility != Visibility.Visible && bdrNewHighscore.Visibility != Visibility.Visible)
                    {
                        if (snakeDirection != SnakeDirection.Right)
                            snakeDirection = SnakeDirection.Left;
                    }
                    break;
                case Key.Right: //вправо
                    if (bdrWelcomeMessage.Visibility != Visibility.Visible && bdrEndOfGame.Visibility != Visibility.Visible
                        && bdrHighscoreList.Visibility != Visibility.Visible && bdrNewHighscore.Visibility != Visibility.Visible)
                    {
                        if (snakeDirection != SnakeDirection.Left)
                            snakeDirection = SnakeDirection.Right;
                    }
                    break;
                case Key.Space: //начать игру
                    if (bdrNewHighscore.Visibility != Visibility.Visible)
                        StartNewGame();
                    break;
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }

        /// <summary>
        /// Рендер игрового поля
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered (object sender, EventArgs e)
        {
            DrawGameArea();
            //StartNewGame();
        }

        /// <summary>
        /// Нарисовать игровую арену
        /// </summary>
        private void DrawGameArea()
        {
            bool doneDrawingBackground = false; //окончание отрисовки задника
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false; //Следующий нечётный?
            /*Делаем задний фон*/
            while(doneDrawingBackground==false)
            {
                /*делаем квадраты размером со змейку,
                каждая нечётная клетка будет белой, а чётная - серой*/
                Rectangle rectangle = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.LightGray
                };
                /*добавляем клетки на поле*/
                GameArena.Children.Add(rectangle);
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);


                nextIsOdd = !nextIsOdd;//делаем чётное - нечётным
                nextX += SnakeSquareSize; 
                if (nextX >= GameArena.ActualWidth) //перенос на следующую строку
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if (nextY >= GameArena.ActualHeight) //если строка последняя, то фон мы сделали
                    doneDrawingBackground = true;
            }
        }

        /// <summary>
        /// Добавляем рандомно еду на поле
        /// </summary>
        /// <returns></returns>
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArena.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArena.ActualHeight / SnakeSquareSize);
            /*получаем координаты рандомно*/
            int foodX = random.Next(0, maxX) * SnakeSquareSize; 
            int foodY = random.Next(0, maxY) * SnakeSquareSize;

            foreach (SnakePart snakePart in snakeParts)
            {
                /*Если координаты еды совпадают с положением змейки, мы запрашиваем другую позицию*/
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }

        /// <summary>
        /// Отрисовка еды
        /// </summary>
        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition(); //получаем позицию еду
            snakeFood = new Ellipse() //делаем еду в виде эллипса
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };

            GameArena.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }
        /// <summary>
        /// Отрисовка змейки
        /// </summary>
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts) //обрабатываем список snakeParts в цикле
            {
                if (snakePart.UiElement == null) //проверяем, назначен ли для каждого фрагмента UIElement
                {                                //если не назначен, то создаём прямоугольник и добавляем его к игровому полю
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize, //ширина
                        Height = SnakeSquareSize, //высота
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush) //заполняем цветом голову и остальное тело
                    };
                    GameArena.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        /// <summary>
        /// Движения змейки
        /// </summary>
        private void MoveSnake()
        {
            //Удаляяем последнюю часть змеи, чтобы подготовить новую часть, добавленную ниже.
            while (snakeParts.Count >= snakeLength)
            {
                GameArena.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            /*Затем мы добавим к змейке новый элемент, который будет (новой) головой.
             * Поэтому мы помечаем все существующие части как элементы без головы (тела),
             * а затем убеждаемся, что они используют кисть тела.
             */

            foreach (SnakePart snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            /*Определяем, в каком направлении развернуть змейку,
             * исходя из текущего направления.
            */
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1]; //создаём голову
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            /*Теперь добавляем новую часть головы в наш список частей змеи*/
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            //Рисуем змейку
            DrawSnake();
            //Взаимодействие змейки с окружающим миром
            DoCollissionCheck();
        }
        /// <summary>
        /// Взаимодействие змейки с окружающим миром
        /// </summary>
        private void DoCollissionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1]; //голова
            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) //если голова встретилась с едой
                && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood(); //съесть еду
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y>=GameArena.ActualHeight) //если голова столкнулась с краем поля
                || (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArena.ActualWidth))
            {
                EndGame(); //конец игры
            }

            foreach(SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) //если голова столкнулась со своим телом
                    && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                    EndGame(); //конец игры
            }
        }
        /// <summary>
        /// Конец игры
        /// </summary>
        private void EndGame()
        {
            speechSynthesizer.SpeakAsync("Я погиб...");
            bool isNewHighscore = false;
            if(currentScore > 0)
            {
                int lowestHighscore = (this.HighscoreList.Count > 0 ? this.HighscoreList.Min(x => x.Score) : 0);
                if ((currentScore>lowestHighscore)||(this.HighscoreList.Count < MaxHighscoreListEntryCount))
                {
                    bdrNewHighscore.Visibility = Visibility.Visible;
                    txtPlayerName.Focus();
                    isNewHighscore = true;
                }
            }
            if (!isNewHighscore)
            {
                tbFinalScore.Text = currentScore.ToString();
                bdrEndOfGame.Visibility = Visibility.Visible;
            }
            gameTickTimer.IsEnabled = false;
            //MessageBox.Show("Game Over! Press Space bar for restart.", "Snake");
        }
        /// <summary>
        /// Если змейка съела еду
        /// </summary>
        private void EatSnakeFood()
        {
            speechSynthesizer.SpeakAsync("Ням");
            snakeLength++; //увеличиваем змейку
            currentScore++; //увеличиваем счёт
            int timerInterval = Math.Max(SnakeSpeedThreshold,
                (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2)); /*Мы изменяем в gameTickTimer значение Interval руководствуясь следующим правилом:
                                                                                      * sУмножаем currentScore на 2 и, затем, вычитаем полученное из текущего значения интервала таймера скорости.
                                                                                      * Это приводит к экспоненциальному росту скорости, что, вкупе с увеличением длины змейки*/
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArena.Children.Remove(snakeFood); //удаляем съеденную еду
            DrawSnakeFood(); //добавляем новую еду
            UpdateGameStatus();
        }
        /// <summary>
        /// Изменение игрового статуса
        /// </summary>
        private void UpdateGameStatus()
        {
            this.tbStatusScore.Text = currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
            //this.Title = "Snake - Score: " + currentScore + " Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
        }
        /// <summary>
        /// "Показать список рекордов"
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnShowHighScoreList_Click(object sender, RoutedEventArgs e)
        {
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Visible;
        }
    }
}
