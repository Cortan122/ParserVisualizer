using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ParserLib;

namespace ParserApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private ParserHistory theHistory;
        private Parser parser;
        private Dictionary<string, Brush> colorDict;
        private readonly Brush[] colors = {
            Brushes.Green,
            Brushes.Red,
            Brushes.LightBlue,
            Brushes.Gray,
        };
        const int charWidth = 20;
        const int textStart = 15;

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            playButton.Click += TogglePause;
            mainSlider.ValueChanged += MainSlider_ValueChanged;
        }

        private void MainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            historyIndex = (int)e.NewValue;
            DisplayHistoryEntry();
        }

        private bool isPaused = true;
        private void TogglePause(object sender = null, RoutedEventArgs e = null) {
            isPaused = !isPaused;
            playButton.Content = isPaused ? "▶" : "⏸";
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
            var font = new FontFamily("Consolas");

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
            var tt = new ToolTip();
            tt.Content = tok.Name;
            rect.ToolTip = tt;

            Canvas.SetTop(rect, 50 + tok.DisplayLevel * 15);
            Canvas.SetLeft(rect, textStart + tok.StartPos * charWidth);
            canvas.Children.Add(rect);
        }

        private void DisplayHistoryEntry(HistoryEntry entry = null) {
            if (entry == null) {
                entry = theHistory[historyIndex];
                var tt = new ToolTip();
                tt.Content = historyIndex.ToString();
                mainSlider.ToolTip = tt;
            }
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
            theHistory = parser.Run(input);
            var i = 0;
            colorDict = theHistory.RuleNames.ToDictionary(e => e, e => colors[i++]);

            mainSlider.Maximum = theHistory.Count() - 1;
            CanvasWrite(input);
            historyIndex = 0;
            DisplayHistoryEntry();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            parser = new ParserSpawner("simple");
            RunParser("(1+122*2+3)");

            var dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromSeconds(.25);
            dispatcherTimer.Start();
        }

        private int historyIndex = 0;
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            if (isPaused) return;
            historyIndex++;
            if (historyIndex == theHistory.Count()) {
                TogglePause();
                return;
            }
            mainSlider.Value = historyIndex;
            DisplayHistoryEntry();
        }
    }
}
