using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ParserLib;

namespace ParserApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public partial class MainWindow : Window {
        #region json properties
        [JsonProperty]
        private int historyIndex = 0;
        [JsonProperty]
        private bool isPaused = true;
        [JsonProperty]
        private bool isReversed = false;
        [JsonProperty]
        private List<Brush> colors;

        [JsonProperty]
        private double speed {
            get => speedSlider.Value;
            set => SetSpeed(value);
        }
        [JsonProperty]
        private bool treeTrim {
            get => (bool)trimTreeButton.IsChecked;
            set => trimTreeButton.IsChecked = value;
        }
        [JsonProperty]
        private bool treeOrientation {
            get => (bool)oriTreeButton.IsChecked;
            set => oriTreeButton.IsChecked = value;
        }
        [JsonProperty]
        private bool treeGravity {
            get => (bool)gravTreeButton.IsChecked;
            set => gravTreeButton.IsChecked = value;
        }

        [JsonProperty]
        private string inputString {
            get => theHistory.InputString;
            set => RunParser(value);
        }
        #endregion

        private int tutorialIndex;

        private DispatcherTimer mainTimer = new DispatcherTimer();
        private DispatcherTimer speedBoxTimer = new DispatcherTimer();

        private Parser parser = new Parser("simple");
        private ParserHistory theHistory;
        private Dictionary<string, Brush> colorDict;

        private readonly FontFamily font = new FontFamily("Consolas");
        const int charWidth = 20;
        const int textStart = 15;
        const string autosavePath = "./autosave.json"; // todo

        public MainWindow() {
            InitializeComponent();
            InitializeEvents();

            speed = 4;
            SelectTutorialPage(0);
        }

        private void InitializeEvents() {
            this.Drop += MainWindow_Drop;
            mainSlider.ValueChanged += MainSlider_ValueChanged;
            speedSlider.ValueChanged += SpeedSlider_ValueChanged;

            speedBoxTimer.Interval = TimeSpan.FromSeconds(2);
            speedBoxTimer.Tick += SpeedBox_ValueChanged;

            speedBox.LostFocus += SpeedBox_ValueChanged;
            speedBox.TextChanged += (o, e) => {
                speedBoxTimer.Stop();
                speedBoxTimer.Start();

                double r = speed;
                double.TryParse(
                    speedBox.Text.Replace(',', '.'),
                    NumberStyles.Any,
                    CultureInfo.InvariantCulture,
                    out r
                );
                if (r < 0) r = 0;
                if (r > 60) r = 60;
                if (r.ToString("0.###", CultureInfo.InvariantCulture) == speedBox.Text) {
                    SpeedBox_ValueChanged(o, e);
                }
            };
            speedBox.KeyDown += (o, e) => {
                if (e.Key == Key.Return || e.Key == Key.Escape) {
                    SpeedBox_ValueChanged(o, e);
                }
            };

            inputBox.LostKeyboardFocus += InputBox_ValueChanged;
            inputBox.KeyDown += (o, e) => {
                if (e.Key == Key.Return || e.Key == Key.Escape) {
                    InputBox_ValueChanged(o, e);
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
        }

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
        private void SetRtf(string document) {
            var documentBytes = Encoding.UTF8.GetBytes(document);
            using (var reader = new MemoryStream(documentBytes)) {
                reader.Position = 0;
                richTextBox.SelectAll();
                richTextBox.Selection.Load(reader, DataFormats.Rtf);
            }
        }

        private void CanvasWrite(string text) {
            // типа такая конфигурация
            const int fontSize = 26;
            const int top = 10;
            int pos = textStart;

            // удаляем весь старый тект (если он есть)
            foreach (var tb in canvas.Children.OfType<TextBlock>().ToList()) {
                canvas.Children.Remove(tb);
            }

            foreach (var chr in text) {
                var txt = new TextBlock();
                txt.FontSize = fontSize;
                txt.Text = chr.ToString();
                txt.FontFamily = font;
                Canvas.SetTop(txt, top);
                Canvas.SetLeft(txt, pos);
                canvas.Children.Add(txt);
                pos += charWidth;
            }

            Canvas.SetTop(inputBox, top);
            Canvas.SetLeft(inputBox, textStart);
            inputBox.Width = pos - textStart;
            inputBox.Height = fontSize;
            inputBox.FontSize = fontSize - 8;
        }

        private void CanvasLegend(bool drawText = false) {
            if (colors == null) return; // just in case

            // удаляем все старые кружочки
            foreach (var el in canvas.Children.OfType<Ellipse>().ToList()) {
                canvas.Children.Remove(el);
            }

            // удаляем все старые подписи
            foreach (var tb in canvas.Children.OfType<TextBlock>().ToList()) {
                if (tb.FontSize == 12) canvas.Children.Remove(tb);
            }

            colorDict = new Dictionary<string, Brush>();

            var i = 0;
            var pos = 5;
            var originalColorsLength = colors.Count;
            foreach (var ruleName in theHistory.RuleNames) {
                if (i == colors.Count) colors.Add(colors[i % originalColorsLength]);
                var value = colors[i];
                colorDict[ruleName] = value;

                var el = new Ellipse();
                el.Width = el.Height = 12;
                el.Fill = value;
                el.ToolTip = ruleName;
                Canvas.SetTop(el, pos);
                Canvas.SetRight(el, 5);
                canvas.Children.Add(el);

                // изменение цветов при клике на Ellipse
                var currentIndex = i;
                el.MouseDown += (o, e) => {
                    var c = GetColor();
                    if (c != null) colors[currentIndex] = c;
                    CanvasLegend();
                };

                if (drawText) {
                    var txt = new TextBlock();
                    txt.FontSize = 12;
                    txt.Text = ruleName;
                    txt.FontFamily = font;
                    Canvas.SetTop(txt, pos);
                    Canvas.SetRight(txt, 20);
                    canvas.Children.Add(txt);
                }

                pos += 19;
                i++;
            }

            DisplayHistoryEntry();
        }

        private void CanvasDrawRect(HistoryToken tok, int pos) {
            var rect = new Border();
            rect.CornerRadius = new CornerRadius(5, 5, 5, 5);
            var end = tok.EndPos;
            if (end == -1) {
                end = pos;
                rect.CornerRadius = new CornerRadius(5, 0, 0, 5);
            }

            rect.Background = colorDict[tok.Name];
            rect.Height = 10;
            rect.Width = (end - tok.StartPos) * charWidth;
            rect.ToolTip = tok.Name;

            if (tok.Trimmable) rect.Opacity = .5;

            Canvas.SetTop(rect, 50 + tok.DisplayLevel * 15);
            Canvas.SetLeft(rect, textStart + tok.StartPos * charWidth);
            canvas.Children.Add(rect);
        }

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

            // удаляем строе дерево
            foreach (var tb in canvas.Children.OfType<Border>().ToList()) {
                canvas.Children.Remove(tb);
            }

            foreach (var tok in entry) {
                CanvasDrawRect(tok, entry.CursorPos);
            }
        }

        private void RunParser(string input) {
            double historyProgress = 0;
            if (theHistory != null) {
                historyProgress = historyIndex / (double)(theHistory.Count() - 1);
            }

            inputBox.Text = input;
            theHistory = parser.Run(input);

            historyIndex = (int)(historyProgress * (theHistory.Count() - 1));

            mainSlider.Maximum = theHistory.Count() - 1;
            CanvasWrite(input);
            CanvasLegend();
        }

        private void SetSpeed(double newValue) {
            if (newValue < 0) newValue = 0;
            if (newValue > 60) newValue = 60;

            speedBox.Text = newValue.ToString("0.###", CultureInfo.InvariantCulture);
            speedSlider.Value = newValue;

            var t = 1 / newValue;
            var max = int.MaxValue / 1e7;
            if (t > max || t <= 0) t = max;
            mainTimer.Interval = TimeSpan.FromSeconds(t);
        }

        private void Load(string path = autosavePath) {
            try {
                var str = File.ReadAllText(path);
                colors = null;
                JsonConvert.PopulateObject(str, this);
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
            }
        }

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
        private void MainSlider_ValueChanged(object o, EventArgs e) {
            historyIndex = (int)mainSlider.Value;
            DisplayHistoryEntry();
        }

        private void SpeedSlider_ValueChanged(object o, EventArgs e) {
            SetSpeed(speedSlider.Value);
        }

        private void SpeedBox_ValueChanged(object o, EventArgs e) {
            speedBoxTimer.Stop();
            // есть ли какойто менее костыльный способ парсить оба стиля дабла?
            double r = speed;
            double.TryParse(
                speedBox.Text.Replace(',', '.'),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out r
            );
            SetSpeed(r);
        }

        private void InputBox_ValueChanged(object o, EventArgs e) {
            inputBox.Opacity = 0;
            inputString = inputBox.Text;
        }

        private void MainWindow_Drop(object o, DragEventArgs e) {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            Load(files[0]);
        }

        private void NextFrame() {
            historyIndex += isReversed ? -1 : 1;

            if (historyIndex >= theHistory.Count() || historyIndex < 0) {
                historyIndex -= isReversed ? -1 : 1;
                if (!isPaused) TogglePause();
                return;
            }

            DisplayHistoryEntry();
        }

        private void PrevFrame() {
            isReversed = !isReversed;
            NextFrame();
            isReversed = !isReversed;
        }

        private void FirstFrame() {
            historyIndex = !isReversed ? 0 : theHistory.Count() - 1;
            DisplayHistoryEntry();
        }

        private void LastFrame() {
            historyIndex = isReversed ? 0 : theHistory.Count() - 1;
            DisplayHistoryEntry();
        }

        private void TogglePause() {
            isPaused = !isPaused;
            playButton.Content = isPaused ? "▶" : "⏸";
            playButton.ToolTip = isPaused ? "Воспроизведение" : "Пауза";
        }

        private void Reverse() {
            isReversed = !isReversed;
            reverseButton.IsChecked = isReversed;
            if (isPaused && isReversed) TogglePause();
        }

        private void NextFrameEvent(object o, EventArgs e) {
            NextFrame();
        }

        private void PrevFrameEvent(object o, EventArgs e) {
            PrevFrame();
        }

        private void FirstFrameEvent(object o, EventArgs e) {
            FirstFrame();
        }

        private void LastFrameEvent(object o, EventArgs e) {
            LastFrame();
        }

        private void TogglePauseEvent(object o, EventArgs e) {
            TogglePause();
        }

        private void ReverseEvent(object o, EventArgs e) {
            Reverse();
        }

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

        private void NextTutorialEvent(object o, EventArgs e) {
            SelectTutorialPage(tutorialIndex + 1);
        }

        private void PrevTutorialEvent(object o, EventArgs e) {
            SelectTutorialPage(tutorialIndex - 1);
        }

        #endregion

        static private void ExportToPng(string path, FrameworkElement element) {
            if (path == null) return;

            // Save current canvas transform
            Transform transform = element.LayoutTransform;
            // reset current transform (in case it is scaled or rotated)
            element.LayoutTransform = null;

            // Get the size of canvas
            Size size = new Size(element.ActualWidth, element.ActualHeight);
            // Measure and arrange the surface
            // VERY IMPORTANT
            element.Measure(size);
            element.Arrange(new Rect(size));

            // Create a render bitmap and push the surface to it
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96d, 96d, PixelFormats.Pbgra32);
            renderBitmap.Render(element);

            // Create a file stream for saving image
            using (FileStream outStream = new FileStream(path, FileMode.Create)) {
                // Use png encoder for our data
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                // push the rendered bitmap to it
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                // save the data to the stream
                encoder.Save(outStream);
            }

            // Restore previously saved layout
            element.LayoutTransform = transform;
        }

        static private Brush GetColor() {
            var dia = new System.Windows.Forms.ColorDialog();
            if (dia.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var c = dia.Color;
                return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
            }
            return null;
        }
    }
}
