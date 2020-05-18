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
    class TreeCanvas : Canvas {
        private readonly FontFamily font = new FontFamily("Consolas");
        private Dictionary<string, Brush> colorDict;
        private TextBox inputBox;
        private HistoryEntry lastHistoryEntry;
        private bool lastHelp;
        const int charWidth = 20;
        const int textStart = 15;
        const int fontSize = 26;
        const int legendFontSize = 12;
        const int textblockTop = 10;
        const int treeTop = 50;
        const int treeVSpace = 15;
        const int rectHeight = 10;

        public void WriteString(string text) {
            int pos = textStart;

            // удаляем весь старый тект (если он есть)
            foreach (var tb in this.Children.OfType<TextBlock>().ToList()) {
                this.Children.Remove(tb);
            }

            foreach (var chr in text) {
                var txt = new TextBlock();
                txt.FontSize = fontSize;
                txt.Text = chr.ToString();
                txt.FontFamily = font;
                Canvas.SetTop(txt, textblockTop);
                Canvas.SetLeft(txt, pos);
                this.Children.Add(txt);
                pos += charWidth;
            }

            if (inputBox == null) {
                inputBox = this.Children.OfType<TextBox>().First();
            }
            Canvas.SetTop(inputBox, textblockTop);
            Canvas.SetLeft(inputBox, textStart);
            inputBox.Width = pos - textStart;
            inputBox.Height = fontSize;
            inputBox.FontSize = fontSize - 8;
        }

        public void InitLegend(
            List<Brush> colors,
            IEnumerable<string> names,
            bool drawText = false
        ) {
            // удаляем все старые кружочки
            foreach (var el in this.Children.OfType<Ellipse>().ToList()) {
                this.Children.Remove(el);
            }

            // удаляем все старые подписи
            foreach (var tb in this.Children.OfType<TextBlock>().ToList()) {
                if (tb.FontSize == legendFontSize) this.Children.Remove(tb);
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
                el.Width = el.Height = legendFontSize;
                el.Fill = value;
                el.ToolTip = ruleName;
                Canvas.SetTop(el, pos);
                Canvas.SetRight(el, 5);
                this.Children.Add(el);

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
                    txt.FontSize = legendFontSize;
                    txt.Text = ruleName;
                    txt.FontFamily = font;
                    Canvas.SetTop(txt, pos);
                    Canvas.SetRight(txt, 20);
                    this.Children.Add(txt);
                }

                pos += 19;
                i++;
            }
        }

        private void DrawRect(HistoryToken tok, int pos) {
            var rect = new Border();
            rect.CornerRadius = new CornerRadius(5, 5, 5, 5);
            var end = tok.EndPos;
            if (end == -1) {
                end = pos;
                rect.CornerRadius = new CornerRadius(5, 0, 0, 5);
            }

            rect.Background = colorDict[tok.Name];
            rect.Height = rectHeight;
            rect.Width = (end - tok.StartPos) * charWidth;
            rect.ToolTip = tok.Name;

            if (tok.Trimmable) rect.Opacity = .5;

            Canvas.SetTop(rect, treeTop + tok.DisplayLevel * treeVSpace);
            Canvas.SetLeft(rect, textStart + tok.StartPos * charWidth);
            this.Children.Add(rect);
        }

        public void DisplayHistoryEntry(HistoryEntry entry, bool help) {
            lastHistoryEntry = entry;
            lastHelp = help;

            // удаляем строе дерево
            foreach (var tb in this.Children.OfType<Border>().ToList()) {
                this.Children.Remove(tb);
            }

            foreach (var tok in entry) {
                DrawRect(tok, entry.CursorPos);
            }

            // удаляем стрые линии
            foreach (var line in this.Children.OfType<Line>().ToList()) {
                this.Children.Remove(line);
            }

            if (help) DrawConventionalTree(entry.GetEdges());
        }

        private void DrawConventionalTree(Dictionary<HistoryToken, HistoryToken> edges) {
            foreach (var edge in edges) {
                var line = new Line();
                line.X1 = GetNodeCenterX(edge.Key);
                line.Y1 = GetNodeCenterY(edge.Key);
                line.X2 = GetNodeCenterX(edge.Value);
                line.Y2 = GetNodeCenterY(edge.Value);
                line.Stroke = Brushes.Orange;
                line.StrokeThickness = 2;
                this.Children.Add(line);
            }
        }

        private double GetNodeCenterX(HistoryToken node) {
            var end = node.EndPos;
            if (end == -1) {
                end = lastHistoryEntry.CursorPos;
            }

            return textStart + node.StartPos * charWidth + (end - node.StartPos) * charWidth / 2.0;
        }

        private double GetNodeCenterY(HistoryToken node) {
            return treeTop + node.DisplayLevel * treeVSpace + rectHeight / 2.0;
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
