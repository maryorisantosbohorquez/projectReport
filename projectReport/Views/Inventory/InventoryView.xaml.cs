using System.Windows;
using System.Windows.Controls;
using ProjectReport.ViewModels.Inventory;
using ProjectReport.Models.Inventory;

namespace ProjectReport.Views.Inventory
{
    public partial class InventoryView : UserControl
    {
        private readonly InventoryViewModel _viewModel;

        public InventoryView()
        {
            InitializeComponent();
            _viewModel = new InventoryViewModel();
            DataContext = _viewModel;
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            // Molde: aquí después puedes abrir un diálogo de creación
            var newItem = new InventoryItem
            {
                ItemCode = "NEW-ITEM",
                Name = "New inventory item",
                Category = "Misc",
                Location = "Undefined",
                QuantityAvailable = 0,
                Unit = "pcs",
                MinStock = 0,
                MaxStock = 0,
                Status = "Available"
            };

            _viewModel.Items.Add(newItem);
            _viewModel.SelectedItem = newItem;
        }

        private void EditItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
                return;

            // Aquí en serio luego abren ventana de edición.
            MessageBox.Show($"Edit: {_viewModel.SelectedItem.ItemCode}", "Edit item");
        }

        private void DeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel.SelectedItem == null)
                return;

            var item = _viewModel.SelectedItem;
            var result = MessageBox.Show($"Delete {item.ItemCode}?",
                                         "Confirm delete",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _viewModel.Items.Remove(item);
            }
        }
    }
}
