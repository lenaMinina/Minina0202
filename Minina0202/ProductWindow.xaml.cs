using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Minina0202
{
    /// <summary>
    /// Логика взаимодействия для ProductWindow.xaml
    /// </summary>
    public partial class ProductWindow : Window
    {
        private List<Products> _products;

        public ProductWindow()
        {
            InitializeComponent();
            Loaded += ProductsWindow_Loaded;
        }

        private void ProductsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void LoadProducts()
        {
            using (var context = new Entities())
            {
                _products = context.Products
                    .Include("Product_type")
                    .ToList();

                var productViewModels = _products.Select(p => new ProductViewModel
                {
                    Id = p.ID,
                    Name = p.Name ?? "Неизвестно",
                    Article = p.Article ?? 0,
                    StockCount = CalculateProductStockCount(p.ID),
                    MinimalCost = p.Minimal_cost_for_partner ?? 0,
                    ProductTypeName = p.Product_type?.Product_type_name ?? "Не указан",
                    ProductTypeFactor = p.Product_type?.Product_Type_Factor ?? 1.0
                }).ToList();

                ProductsDataGrid.ItemsSource = productViewModels;
            }
        }

        private int CalculateProductStockCount(int productId)
        {
            // Здесь можно реализовать реальную логику расчета остатков
            // Пока используем случайные значения для демонстрации
            var random = new Random(productId); // Для детерминированности
            return random.Next(0, 1000);
        }

        private void CalculateMaterialsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProduct = ProductsDataGrid.SelectedItem as ProductViewModel;
                if (selectedProduct == null)
                {
                    MessageBox.Show("Выберите продукт для расчета материалов!", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Открываем окно расчета материалов
                var calcWindow = new MaterialCalc(selectedProduct.Id);
                calcWindow.Owner = this;
                calcWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете материалов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Article { get; set; }
        public int StockCount { get; set; }
        public double MinimalCost { get; set; }
        public string ProductTypeName { get; set; }
        public double ProductTypeFactor { get; set; }
    }
}
