using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Geometry.Config;
using ProjectReport.Models.Geometry.DrillString;

namespace ProjectReport.Views.Geometry
{
    public partial class ToolJointConfigWindow : Window
    {
        public ToolJointConfig Config => ((ToolJointConfigViewModel)DataContext).Model;

        public ToolJointConfigWindow(ToolJointConfig? model, ComponentType componentType = ComponentType.DrillPipe)
        {
            InitializeComponent();
            var vm = new ToolJointConfigViewModel(model ?? new ToolJointConfig(), componentType);
            vm.RequestClose += result =>
            {
                DialogResult = result;
                Close();
            };
            DataContext = vm;
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Select all text when focused for easy editing
                textBox.SelectAll();
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Format the value when focus is lost
                if (double.TryParse(textBox.Text, out double value))
                {
                    textBox.Text = value.ToString("F2");
                }
                else if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = "0.00";
                }
            }
        }
    }
}
