using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ParserLib;

namespace ParserApp {
    /// <summary>
    /// Отрисовка синтаксического дерева и палитры его цветов
    /// </summary>
    internal class TreeCanvas : Canvas {
        /// <summary>
        /// Шрифт, который везде используется
        /// </summary>
        private readonly FontFamily font = new FontFamily("Consolas");

        /// <summary>
        /// Ставит в соответствие названия правил и их цвета
        /// </summary>
        private Dictionary<string, Brush> colorDict;

        /// <summary>
        /// Поле ввода, где пользователь вводит строку, синтаксический анализ которой будет визуализироваться.
        /// </summary>
        private TextBox inputBox;

        /// <summary>
        /// Тот кадр, который сейчас нарисован
        /// </summary>
        private HistoryEntry lastHistoryEntry;

        /// <summary>
        /// Нарисовано ли сейчас дерево в режиме новичка
        /// </summary>
        private bool lastHelp;

        /// <summary>
        /// Ширина каждого символа в строке
        /// </summary>
        private const int CharWidth = 20;

        /// <summary>
        /// Начальная позиция строки
        /// </summary>
        private const int TextStart = 15;

        /// <summary>
        /// Размер шрифта
        /// </summary>
        private const int FontSize = 26;

        /// <summary>
        /// Размер шрифта подписей к цветам палитры
        /// </summary>
        private const int LegendFontSize = 12;

        /// <summary>
        /// Позиция поля ввода
        /// </summary>
        private const int TextblockTop = 10;

        /// <summary>
        /// Позиция верхушки дерева
        /// </summary>
        private const int TreeTop = 50;

        /// <summary>
        /// Вертикальное расстояние между узлами дерева
        /// </summary>
        private const int TreeVSpace = 15;

        /// <summary>
        /// Высота каждого узла дерева
        /// </summary>
        private const int RectHeight = 10;

        /// <summary>
        /// Рисует строку
        /// </summary>
        public void WriteString(string text) {
            var pos = TextStart;

            // удаляем весь старый тект (если он есть)
            foreach (var tb in Children.OfType<TextBlock>().ToList()) {
                Children.Remove(tb);
            }

            foreach (var chr in text) {
                var txt = new TextBlock();
                txt.FontSize = FontSize;
                txt.Text = chr.ToString();
                txt.FontFamily = font;
                SetTop(txt, TextblockTop);
                SetLeft(txt, pos);
                Children.Add(txt);
                pos += CharWidth;
            }

            if (inputBox == null) {
                inputBox = Children.OfType<TextBox>().First();
            }
            SetTop(inputBox, TextblockTop + 4);
            SetLeft(inputBox, TextStart);
            inputBox.Width = pos - TextStart;
            inputBox.Height = FontSize;
            inputBox.FontSize = FontSize - 8;
        }

        /// <summary>
        /// Рисует цветовую палитру
        /// </summary>
        public void InitLegend(
            List<Brush> colors,
            IEnumerable<string> names,
            bool drawText = false
        ) {
            if (colors.Count == 0) return;

            // удаляем все старые кружочки
            foreach (var el in Children.OfType<Ellipse>().ToList()) {
                Children.Remove(el);
            }

            // удаляем все старые подписи
            foreach (var tb in Children.OfType<TextBlock>().ToList()) {
                if (tb.FontSize == LegendFontSize) Children.Remove(tb);
            }

            colorDict = new Dictionary<string, Brush>();

            var i = 0;
            var pos = 5;
            var originalColorsLength = colors.Count;
            foreach (var ruleName in names) {
                if (i == colors.Count) colors.Add(colors[i % originalColorsLength]);
                var value = colors[i];
                colorDict[ruleName] = value;

                var el = new Ellipse();
                el.Width = el.Height = LegendFontSize;
                el.Fill = value;
                el.ToolTip = ruleName;
                SetTop(el, pos);
                SetRight(el, 5);
                Children.Add(el);

                // изменение цветов при клике на Ellipse
                var currentIndex = i;
                el.MouseDown += (o, e) => {
                    var c = GetColor();
                    if (c != null) colors[currentIndex] = c;
                    InitLegend(colors, names);
                    DisplayHistoryEntry(lastHistoryEntry, lastHelp);
                };

                if (drawText) {
                    var txt = new TextBlock();
                    txt.FontSize = LegendFontSize;
                    txt.Text = ruleName;
                    txt.FontFamily = font;
                    SetTop(txt, pos);
                    SetRight(txt, 20);
                    Children.Add(txt);
                }

                pos += 19;
                i++;
            }
        }

        /// <summary>
        /// Рисует один узел дерева
        /// </summary>
        private void DrawRect(HistoryToken tok, int pos) {
            var rect = new Border();
            rect.CornerRadius = new CornerRadius(5, 5, 5, 5);
            var end = tok.EndPos;
            if (end == -1) {
                end = pos;
                rect.CornerRadius = new CornerRadius(5, 0, 0, 5);
            }

            rect.Background = colorDict[tok.Name];
            rect.Height = RectHeight;
            rect.Width = (end - tok.StartPos) * CharWidth;
            rect.ToolTip = tok.Name;

            if (tok.Trimmable) rect.Opacity = .5;

            SetTop(rect, TreeTop + tok.DisplayLevel * TreeVSpace);
            SetLeft(rect, TextStart + tok.StartPos * CharWidth);
            Children.Add(rect);
        }

        /// <summary>
        /// Рисует дерево
        /// </summary>
        public void DisplayHistoryEntry(HistoryEntry entry, bool help) {
            lastHistoryEntry = entry;
            lastHelp = help;

            // удаляем строе дерево
            foreach (var tb in Children.OfType<Border>().ToList()) {
                Children.Remove(tb);
            }

            foreach (var tok in entry) {
                DrawRect(tok, entry.CursorPos);
            }

            // удаляем стрые линии
            foreach (var line in Children.OfType<Line>().ToList()) {
                Children.Remove(line);
            }

            if (help) DrawConventionalTree(entry.GetEdges());
        }

        /// <summary>
        /// Рисует ветки дерева
        /// </summary>
        private void DrawConventionalTree(Dictionary<HistoryToken, HistoryToken> edges) {
            foreach (var edge in edges) {
                var line = new Line();
                line.X1 = GetNodeCenterX(edge.Key);
                line.Y1 = GetNodeCenterY(edge.Key);
                line.X2 = GetNodeCenterX(edge.Value);
                line.Y2 = GetNodeCenterY(edge.Value);
                line.Stroke = Brushes.Orange;
                line.StrokeThickness = 2;
                Children.Add(line);
            }
        }

        /// <summary>
        /// Считает X координату центра узла дерева
        /// </summary>
        private double GetNodeCenterX(HistoryToken node) {
            var end = node.EndPos;
            if (end == -1) {
                end = lastHistoryEntry.CursorPos;
            }

            return TextStart + node.StartPos * CharWidth + (end - node.StartPos) * CharWidth / 2.0;
        }

        /// <summary>
        /// Считает Y координату центра узла дерева
        /// </summary>
        private double GetNodeCenterY(HistoryToken node) {
            return TreeTop + node.DisplayLevel * TreeVSpace + RectHeight / 2.0;
        }


        /// <summary>
        /// Запрашивает у пользователя собственный цвет, открывая диалоговое окно
        /// </summary>
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
