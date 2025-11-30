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
    /// Логика взаимодействия для MaterialCalc.xaml
    /// </summary>
    public partial class MaterialCalc : Window
    {
        private int _productId;
        private Products _product;

        public MaterialCalc(int productId)
        {
            InitializeComponent();
            _productId = productId;
            Loaded += MaterialCalculationWindow_Loaded;
        }

        /// <summary>
        /// Метод для открытия загрузки информации о продукте
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaterialCalculationWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadProductInfo();
            }
            catch
            {
                MessageBox.Show("Ошибка при загрузке данных");
                Close();
            }
        }

        private void LoadProductInfo()
        {
            using (var context = new Entities())
            {
                _product = context.Products
                    .Include("Product_type")
                    .FirstOrDefault(p => p.ID == _productId);

                if (_product != null)
                {
                    ProductNameTextBlock.Text = _product.Name ?? "Неизвестно";
                }
            }
        }

        private void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }

        private void DoubleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, 0) && e.Text != ".")
            {
                e.Handled = true;
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!int.TryParse(RequiredCountTextBox.Text, out int requiredCount) || requiredCount <= 0)
                {
                    MessageBox.Show("Введите корректное требуемое количество");
                    return;
                }

                if (!double.TryParse(Param1TextBox.Text, out double param1) || param1 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 1");
                    return;
                }

                if (!double.TryParse(Param2TextBox.Text, out double param2) || param2 <= 0)
                {
                    MessageBox.Show("Введите корректное значение параметра 2");
                    return;
                }

                int materialTypeId = GetMaterialTypeIdForProduct(_product.ID);

                int stockCount = GetProductStockCount(_product.ID);

                int result = MaterialCalculator.CalculateRequiredMaterial(
                    _product.ID_Product_type ?? 0,
                    materialTypeId,
                    requiredCount,
                    stockCount,
                    param1,
                    param2
                );

                if (result == -1)
                {
                    ResultTextBlock.Text = "Ошибка: неверные входные данные";
                    ResultTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    ResultTextBlock.Text = $"Необходимо материалов: {result} ед.";
                    ResultTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}", "Ошибка расчета",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Метод для получения типа материала
        /// </summary>
        /// <param name="productId">Передаем id продукта</param>
        /// <returns></returns>
        private int GetMaterialTypeIdForProduct(int productId)
        {
            using (var context = new Entities())
            {
                var materialTypeId = context.Materials_in_products
                    .Where(mp => mp.ID_Product == productId)
                    .Select(mp => mp.Material.ID_Material_Type)
                    .FirstOrDefault();

                return materialTypeId ?? 1;
            }
        }

        private int GetProductStockCount(int productId)
        {
            using (var context = new Entities())
            {
                var totalRequested = context.Partner_products_request
                    .Where(r => r.ID_Product == productId && r.Count.HasValue)
                    .Sum(r => r.Count.Value);

                return totalRequested;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}