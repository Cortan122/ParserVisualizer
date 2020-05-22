using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using ParserLib;

namespace ParserApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public partial class MainWindow : Window {
        #region json properties
        /// <summary>
        /// Индекс текущего кадра
        /// </summary>
        [JsonProperty("historyIndex")]
        private int historyIndex = 0;
        /// <summary>
        /// Включена ли пауза
        /// </summary>
        [JsonProperty("isPaused")]
        private bool isPaused = true;
        /// <summary>
        /// Идёт ли прокрутка в обратною сторону
        /// </summary>
        [JsonProperty("isReversed")]
        private bool isReversed = false;
        /// <summary>
        /// Список цветов
        /// </summary>
        [JsonProperty("colors")]
        private List<Brush> colors;

        /// <summary>
        /// Скорость автоматической прокрутки
        /// </summary>
        [JsonProperty("speed")]
        private double speed {
            get => speedSlider.Value;
            set => SetSpeed(value);
        }
        /// <summary>
        /// Надо ли подровнять дерево
        /// </summary>
        [JsonProperty("treeTrim")]
        private bool treeTrim {
            get => (bool)trimTreeButton.IsChecked;
            set => trimTreeButton.IsChecked = value;
        }
        /// <summary>
        /// Ориентация дерева
        /// </summary>
        [JsonProperty("treeOrientation")]
        private bool treeOrientation {
            get => (bool)oriTreeButton.IsChecked;
            set => oriTreeButton.IsChecked = value;
        }
        /// <summary>
        /// Гравитация дерева
        /// </summary>
        [JsonProperty("treeGravity")]
        private bool treeGravity {
            get => (bool)gravTreeButton.IsChecked;
            set => gravTreeButton.IsChecked = value;
        }
        /// <summary>
        /// Режим новичка
        /// </summary>
        [JsonProperty("treeHelp")]
        private bool treeHelp {
            get => (bool)helpTreeButton.IsChecked;
            set => helpTreeButton.IsChecked = value;
        }

        /// <summary>
        /// Строка, синтаксический анализ которой будет визуализироваться
        /// </summary>
        [JsonProperty("inputString")]
        private string inputString {
            get => theHistory.InputString;
            set => RunParser(value);
        }
        #endregion

        /// <summary>
        /// Индекс текущей страницы объяснений
        /// </summary>
        private int tutorialIndex;

        /// <summary>
        /// Таймер автоматической прокрутки
        /// </summary>
        private DispatcherTimer mainTimer = new DispatcherTimer();
        /// <summary>
        /// Таймер для проверки корректности числа, введенного в поле ввода скорости
        /// </summary>
        private DispatcherTimer speedBoxTimer = new DispatcherTimer();

        /// <summary>
        /// Синтаксический анализатор
        /// </summary>
        private Parser parser = new Parser("simple");
        /// <summary>
        /// История
        /// </summary>
        private ParserHistory theHistory;
        /// <summary>
        /// Путь к файлу автосохранения
        /// </summary>
        private const string autosavePath = "./autosave.json"; // todo

        public MainWindow() {
            InitializeComponent();
            InitializeEvents();

            speed = 4;
            SelectTutorialPage(0);
        }

        /// <summary>
        /// Инициализирует события элементов интерфейса
        /// </summary>
        private void InitializeEvents() {
            Drop += MainWindowDrop;
            mainSlider.ValueChanged += MainSliderChange;
            speedSlider.ValueChanged += SpeedSliderChange;

            speedBoxTimer.Interval = TimeSpan.FromSeconds(2);
            speedBoxTimer.Tick += SpeedBoxChange;

            speedBox.LostFocus += SpeedBoxChange;
            speedBox.TextChanged += (o, e) => {
                speedBoxTimer.Stop();
                speedBoxTimer.Start();

                var r = speed;
                double.TryParse(
                    speedBox.Text.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out r
                );
                if (r < 0) r = 0;
                if (r > 60) r = 60;
                if (r.ToString("0.###", CultureInfo.InvariantCulture) == speedBox.Text) {
                    SpeedBoxChange(o, e);
                }
            };
            speedBox.KeyDown += (o, e) => {
                if (e.Key == Key.Return || e.Key == Key.Escape) {
                    SpeedBoxChange(o, e);
                }
            };

            inputBox.LostKeyboardFocus += InputBoxChange;
            inputBox.KeyDown += (o, e) => {
                if (e.Key == Key.Return || e.Key == Key.Escape) {
                    InputBoxChange(o, e);
                    Keyboard.ClearFocus();
                }
            };
            inputBox.GotKeyboardFocus += (o, e) => { inputBox.Opacity = 1; };

            mainTimer.Tick += (ob, ea) => { if (!isPaused) NextFrame(); };
            mainTimer.Start();

            RoutedEventHandler t = (ob, ea) => DisplayHistoryEntry();
            trimTreeButton.Click += t;
            oriTreeButton.Click += t;
            gravTreeButton.Click += t;
            helpTreeButton.Click += t;
        }

        /// <summary>
        /// Подгружает страницу объяснений с данным индексом
        /// </summary>
        private void SelectTutorialPage(int i) {
            try {
                if (!File.Exists($"./tutorials/{i}.rtf")) return;
                using (var fs = File.OpenRead($"./tutorials/{i}.rtf")) {
                    tutorialBox.SelectAll();
                    tutorialBox.Selection.Load(fs, DataFormats.Rtf);
                }
                if (File.Exists($"./tutorials/{i}.json")) Load($"./tutorials/{i}.json");

                tutorialIndex = i;
                prevTutorialButton.IsEnabled = tutorialIndex != 0;
                nextTutorialButton.IsEnabled = File.Exists($"./tutorials/{i + 1}.rtf");
            } catch (Exception) {
                MessageBox.Show(
                    "Что-то пошло не так, и у нас не получилось загрузить туториал",
                    "Туториал",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // костыль потомучто richTextBox.Rtf = str; неработает на wpf
        // тоесть "он менят содержимое richTextBox-а на то что написано в строке document"
        /// <summary>
        /// Меняет содержимое richTextBox-а на то, что написано в строке
        /// </summary>
        private void SetRtf(string document) {
            var documentBytes = Encoding.UTF8.GetBytes(document);
            using (var reader = new MemoryStream(documentBytes)) {
                reader.Position = 0;
                richTextBox.SelectAll();
                richTextBox.Selection.Load(reader, DataFormats.Rtf);
            }
        }

        /// <summary>
        /// Обновляет текущий кадр
        /// </summary>
        private void DisplayHistoryEntry() {
            var entry = theHistory[historyIndex];
            mainSlider.ToolTip = historyIndex.ToString();
            mainSlider.Value = historyIndex;
            entry.SetSettings(
                treeTrim,
                treeOrientation,
                treeGravity
            );
            SetRtf(entry.RtfGrammar);

            canvas.DisplayHistoryEntry(entry, treeHelp);
        }

        /// <summary>
        /// Обновляет цветовую палитру
        /// </summary>
        private void CanvasLegend(bool drawText = false) {
            if (colors == null) return; // just in case
            canvas.InitLegend(colors, theHistory.RuleNames, drawText);
            DisplayHistoryEntry();
        }

        /// <summary>
        /// Запускает синтаксический анализатор
        /// </summary>
        private void RunParser(string input) {
            if (input == "") return;
            double historyProgress = 0;
            if (theHistory != null) {
                historyProgress = historyIndex / (double)(theHistory.Count() - 1);
            }

            inputBox.Text = input;
            theHistory = parser.Run(input);

            historyIndex = (int)(historyProgress * (theHistory.Count() - 1));

            mainSlider.Maximum = theHistory.Count() - 1;
            canvas.WriteString(input);
            CanvasLegend();
        }

        /// <summary>
        /// Изменяет скорость автоматической прокрутки
        /// </summary>
        private void SetSpeed(double newValue) {
            if (newValue < 0) newValue = 0;
            if (newValue > 60) newValue = 60;
            if (double.IsNaN(newValue)) newValue = 4;

            speedBox.Text = newValue.ToString("0.###", CultureInfo.InvariantCulture);
            speedSlider.Value = newValue;

            var t = 1 / newValue;
            var max = int.MaxValue / 1e7;
            if (t > max || t <= 0) t = max;
            mainTimer.Interval = TimeSpan.FromSeconds(t);
        }

        /// <summary>
        /// Загружает сохранение
        /// </summary>
        private void Load(string path = autosavePath) {
            var colorBackup = colors;
            colors = null;
            try {
                var str = File.ReadAllText(path);
                JsonConvert.PopulateObject(str, this);
                if (colors == null || colors.Count == 0) colors = colorBackup;
                reverseButton.IsChecked = isReversed;
                playButton.Content = isPaused ? "▶" : "⏸";
                playButton.ToolTip = isPaused ? "Воспроизведение" : "Пауза";

                // это надо если у нас в сэйве нету inputString
                CanvasLegend();
            } catch (Exception) {
                if (path == autosavePath) return;
                MessageBox.Show(
                    "Что-то пошло не так, и у нас не получилось загрузить файл",
                    "Загрузка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            } finally {
                if (colors == null) colors = colorBackup;
            }
        }

        /// <summary>
        /// Сохраняет текущее состояние в файл
        /// </summary>
        private void Save(string path = autosavePath) {
            try {
                if (path.EndsWith(".png")) {
                    CanvasLegend(true);
                    ExportToPng(path, canvas);
                    CanvasLegend(false);
                } else if (path.EndsWith(".svg")) {
                    throw new NotImplementedException();
                } else if (path.EndsWith(".xaml")) {
                    File.WriteAllText(path, XamlWriter.Save(canvas));
                } else {
                    var str = JsonConvert.SerializeObject(this, Formatting.Indented);
                    File.WriteAllText(path, str);
                }
            } catch (Exception) {
                if (path == autosavePath) return;
                MessageBox.Show(
                    "Что-то пошло не так, и у нас не получилось сохранить файл",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        #region events
        /// <summary>
        /// Обработчик события, которое вызывается при изменении значения основного бегунка
        /// </summary>
        private void MainSliderChange(object o, EventArgs e) {
            historyIndex = (int)mainSlider.Value;
            DisplayHistoryEntry();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при изменении значения бегунка скорости
        /// </summary>
        private void SpeedSliderChange(object o, EventArgs e) {
            SetSpeed(speedSlider.Value);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при изменении значения поля ввода скорости
        /// </summary>
        private void SpeedBoxChange(object o, EventArgs e) {
            speedBoxTimer.Stop();
            // есть ли какойто менее костыльный способ парсить оба стиля дабла?
            var r = speed;
            double.TryParse(
                speedBox.Text.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out r
            );
            SetSpeed(r);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при изменении значения поля ввода строки
        /// </summary>
        private void InputBoxChange(object o, EventArgs e) {
            inputBox.Opacity = 0;
            if (inputBox.Text == "") {
                inputBox.Text = inputString;
                MessageBox.Show(
                    "В поле ввода была введена пустая строка, а пустую строку\nнельзя парсить.",
                    "Входная строка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            } else inputString = inputBox.Text;
        }

        /// <summary>
        /// Обработчик события, которое вызывается после перетаскивания файла в это окно
        /// </summary>
        private void MainWindowDrop(object o, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Load(files[0]);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии на гиперссылку
        /// </summary>
        private void HyperlinkClick(object o, EventArgs e) {
            var hyperlink = (Hyperlink)o;
            Process.Start(hyperlink.NavigateUri.ToString());
        }

        /// <summary>
        /// Показывает следующий кадр
        /// </summary>
        private void NextFrame() {
            historyIndex += isReversed ? -1 : 1;

            if (historyIndex >= theHistory.Count() || historyIndex < 0) {
                historyIndex -= isReversed ? -1 : 1;
                if (!isPaused) TogglePause();
                return;
            }

            DisplayHistoryEntry();
        }

        /// <summary>
        /// Показывает предыдущий кадр
        /// </summary>
        private void PrevFrame() {
            isReversed = !isReversed;
            NextFrame();
            isReversed = !isReversed;
        }

        /// <summary>
        /// Показывает первый кадр
        /// </summary>
        private void FirstFrame() {
            historyIndex = !isReversed ? 0 : theHistory.Count() - 1;
            DisplayHistoryEntry();
        }

        /// <summary>
        /// Показывает последний кадр
        /// </summary>
        private void LastFrame() {
            historyIndex = isReversed ? 0 : theHistory.Count() - 1;
            DisplayHistoryEntry();
        }

        /// <summary>
        /// Переключает паузу
        /// </summary>
        private void TogglePause() {
            isPaused = !isPaused;
            playButton.Content = isPaused ? "▶" : "⏸";
            playButton.ToolTip = isPaused ? "Воспроизведение" : "Пауза";
        }

        /// <summary>
        /// Меняет направление автоматической прокрутки
        /// </summary>
        private void Reverse() {
            isReversed = !isReversed;
            reverseButton.IsChecked = isReversed;
            if (isPaused && isReversed) TogglePause();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа следующего кадра
        /// </summary>
        private void NextFrameEvent(object o, EventArgs e) {
            NextFrame();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа предыдущего кадра
        /// </summary>
        private void PrevFrameEvent(object o, EventArgs e) {
            PrevFrame();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа первого кадра
        /// </summary>
        private void FirstFrameEvent(object o, EventArgs e) {
            FirstFrame();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа последнего кадра
        /// </summary>
        private void LastFrameEvent(object o, EventArgs e) {
            LastFrame();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для переключения паузы
        /// </summary>
        private void TogglePauseEvent(object o, EventArgs e) {
            TogglePause();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для изменения направления автоматической прокрутки
        /// </summary>
        private void ReverseEvent(object o, EventArgs e) {
            Reverse();
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для сохранения текущего состояния
        /// </summary>
        private void SaveEvent(object o, EventArgs e) {
            var dia = new SaveFileDialog();
            dia.Filter = "Сохранить текущее состояние (*.json)|*.json|Векторный рисунок дерева (*.xaml)|*.xaml|Растровый рисунок дерева (*.png)|*.png";
            dia.DefaultExt = "json";
            dia.FileName = "tree.json";
            dia.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var t = isPaused;
            isPaused = true;
            var rt = dia.ShowDialog();
            isPaused = t;
            if (rt != true) return; // так надо: !tr неработает

            Save(dia.FileName);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для загрузки сохранения
        /// </summary>
        private void LoadEvent(object o, EventArgs e) {
            var dia = new OpenFileDialog();
            dia.Filter = "Загрузить текущее состояние (*.json)|*.json";
            dia.DefaultExt = "json";
            dia.FileName = "tree.json";
            dia.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            var t = isPaused;
            isPaused = true;
            var rt = dia.ShowDialog();
            isPaused = t;
            if (rt != true) return; // так надо: !tr неработает

            Load(dia.FileName);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа следующей страницы объяснений
        /// </summary>
        private void NextTutorialEvent(object o, EventArgs e) {
            SelectTutorialPage(tutorialIndex + 1);
        }

        /// <summary>
        /// Обработчик события, которое вызывается при нажатии кнопки или клавиши быстрого вызова для показа предыдущей страницы объяснений
        /// </summary>
        private void PrevTutorialEvent(object o, EventArgs e) {
            SelectTutorialPage(tutorialIndex - 1);
        }

        #endregion

        /// <summary>
        /// Делает снимок данного элемента и сохраняет его в файл с данным именем
        /// </summary>
        static private void ExportToPng(string path, FrameworkElement element) {
            if (path == null) return;

            // Save current canvas transform
            var transform = element.LayoutTransform;
            // reset current transform (in case it is scaled or rotated)
            element.LayoutTransform = null;

            // Get the size of canvas
            var size = new Size(element.ActualWidth, element.ActualHeight);
            // Measure and arrange the surface
            // VERY IMPORTANT
            element.Measure(size);
            element.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            var renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(element);

            // Create a file stream for saving image
            using (var outStream = new FileStream(path, FileMode.Create)) {
                // Use png encoder for our data
                var encoder = new PngBitmapEncoder();
                // push the rendered bitmap to it
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                // save the data to the stream
                encoder.Save(outStream);
            }

            // Restore previously saved layout
            element.LayoutTransform = transform;
        }
    }
}
