using System.Windows;

namespace CtrlV
{
    public partial class NoteEditDialog : Window
    {
        public string NoteText { get; private set; } = string.Empty;

        public NoteEditDialog(string currentNote)
        {
            InitializeComponent();
            NoteTextBox.Text = currentNote ?? string.Empty;
            NoteTextBox.Focus();
            NoteTextBox.SelectAll();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            NoteText = NoteTextBox.Text?.Trim() ?? string.Empty;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
