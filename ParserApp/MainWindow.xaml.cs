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
using KурсачLib;

namespace KурсачWpf {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private ParserHistory theHistory;

        public MainWindow() {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
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

        private void DisplayHistoryEntry(HistoryEntry entry) {
            SetRtf(entry.RtfGrammar);
        }

        private void DisplayHistoryEntry(int i) {
            DisplayHistoryEntry(theHistory[i]);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            Parser parser = new ParserSpawner("simple");
            theHistory = parser.Run("1+1");

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        private int historyIndex = 0;
        private void dispatcherTimer_Tick(object sender, EventArgs e) {
            DisplayHistoryEntry(historyIndex++);
        }
    }
}
