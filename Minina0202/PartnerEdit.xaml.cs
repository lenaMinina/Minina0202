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
    /// Логика взаимодействия для PartnerEdit.xaml
    /// </summary>
    public partial class PartnerEdit : Window
    {
        private int _partnerId;
        private bool _isEditMode;
        private List<ProductInRequest> _productsInRequest;

        public event EventHandler PartnerSaved;

        public PartnerEdit(int partnerId = 0)
        {
            InitializeComponent();
            _partnerId = partnerId;
            _isEditMode = partnerId > 0;
            _productsInRequest = new List<ProductInRequest>();

            if (_isEditMode)
            {
                Title = "Редактирование заявки партнера";
            }
            else
            {
                Title = "Добавление заявки партнера";
            }

            Loaded += PartnerEditWindow_Loaded;
        }

        private void PartnerEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPartnerTypes();
                LoadProducts();

                if (_isEditMode)
                {
                    LoadPartnerData();
                    LoadPartnerRequests();
                }

                UpdateTotalCost();
            }
            catch
            {
                MessageBox.Show($"Ошибка при загрузке данных");
                Close();
            }
        }

        private void LoadPartnerTypes()
        {
            using (var context = new Entities())
            {
                var partnerTypes = context.Partner_type.ToList();
                PartnerTypeComboBox.ItemsSource = partnerTypes;

                if (partnerTypes.Any())
                {
                    PartnerTypeComboBox.SelectedIndex = 0;
                }
            }
        }

        private void LoadProducts()
        {
            using (var context = new Entities())
            {
                var products = context.Products.ToList();
                ProductComboBox.ItemsSource = products;

                if (products.Any())
                {
                    ProductComboBox.SelectedIndex = 0;
                    UpdateUnitCost();
                }
            }
        }

        private void LoadPartnerData()
        {
            using (var context = new Entities())
            {
                var partner = context.Partners
                    .Include("Partner_type")
                    .FirstOrDefault(p => p.ID == _partnerId);

                if (partner == null)
                {
                    MessageBox.Show("Партнер не найден!");
                    Close();
                    return;
                }

                PartnerTypeComboBox.SelectedValue = partner.ID_Partner_type;
                NameTextBox.Text = partner.Name;
                DirectorTextBox.Text = partner.Director;
                AddressTextBox.Text = partner.Address;
                RatingTextBox.Text = partner.Rating?.ToString() ?? "0";
                PhoneTextBox.Text = partner.Phone;
                EmailTextBox.Text = partner.Email;
            }
        }

        private void LoadPartnerRequests()
        {
            using (var context = new Entities())
            {
                var requests = context.Partner_products_request
                    .Include("Products")
                    .Where(r => r.ID_Partner == _partnerId)
                    .ToList();

                foreach (var request in requests)
                {
                    if (request.Products != null && request.Count.HasValue)
                    {
                        _productsInRequest.Add(new ProductInRequest
                        {
                            ProductId = request.Products.ID,
                            ProductName = request.Products.Name,
                            Count = request.Count.Value,
                            UnitCost = request.Products.Minimal_cost_for_partner ?? 0,
                            TotalCost = Math.Round((request.Products.Minimal_cost_for_partner ?? 0) * request.Count.Value, 2)
                        });
                    }
                }

                ProductsDataGrid.ItemsSource = _productsInRequest;
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateUnitCost();
        }

        private void UpdateUnitCost()
        {
            var selectedProduct = ProductComboBox.SelectedItem as Products;
            if (selectedProduct != null)
            {
                double unitCost = selectedProduct.Minimal_cost_for_partner ?? 0;
                UnitCostTextBlock.Text = $"{unitCost:N2} руб.";
                UpdateTotalCost();
            }
        }

        /// <summary>
        /// Метод для обновления итоговой стоимости
        /// </summary>
        private void UpdateTotalCost()
        {
            double totalCost = _productsInRequest.Sum(p => p.TotalCost);
            TotalCostTextBlock.Text = $"{totalCost:N2} руб.";
        }


        private void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedProduct = ProductComboBox.SelectedItem as Products;
                if (selectedProduct == null)
                {
                    MessageBox.Show("Выберите продукт!");
                    return;
                }

                if (!int.TryParse(CountTextBox.Text, out int count) || count <= 0)
                {
                    MessageBox.Show("Введите корректное количество!");
                    return;
                }

                double unitCost = selectedProduct.Minimal_cost_for_partner ?? 0;
                double totalCost = Math.Round(unitCost * count, 2);

                var productInRequest = new ProductInRequest
                {
                    ProductId = selectedProduct.ID,
                    ProductName = selectedProduct.Name,
                    Count = count,
                    UnitCost = unitCost,
                    TotalCost = totalCost
                };

                _productsInRequest.Add(productInRequest);
                ProductsDataGrid.ItemsSource = null;
                ProductsDataGrid.ItemsSource = _productsInRequest;
                UpdateTotalCost();

                CountTextBox.Text = "1";
            }
            catch
            {
                MessageBox.Show("Ошибка при добавлении продукта");
            }
        }

        private void RemoveProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductInRequest product)
            {
                _productsInRequest.Remove(product);
                ProductsDataGrid.ItemsSource = null;
                ProductsDataGrid.ItemsSource = _productsInRequest;
                UpdateTotalCost();
            }
        }

        /// <summary>
        /// Метод для сохранения
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidatePartnerInput())
                {
                    return;
                }

                if (!ValidateRequestInput())
                {
                    return;
                }

                SavePartnerAndRequests();
                DialogResult = true;
                PartnerSaved?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch
            {
                MessageBox.Show("Ошибка при сохранении");
            }
        }

        /// <summary>
        /// Метод для проверки корректного ввода в поля 
        /// </summary>
        private bool ValidatePartnerInput()
        {
            if (PartnerTypeComboBox.SelectedValue == null)
            {
                MessageBox.Show("Выберите тип партнера!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите наименование компании!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(DirectorTextBox.Text))
            {
                MessageBox.Show("Введите ФИО директора!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
            {
                MessageBox.Show("Введите юридический адрес!");
                return false;
            }

            if (!int.TryParse(RatingTextBox.Text, out int rating) || rating < 0)
            {
                MessageBox.Show("Рейтинг должен быть целым неотрицательным числом!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон!");
                return false;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email!");
                return false;
            }

            return true;
        }

        private bool ValidateRequestInput()
        {
            if (_productsInRequest.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы один продукт в заявку!");
                return false;
            }

            return true;
        }

        private void SavePartnerAndRequests()
        {
            using (var context = new Entities())
            {
                Partners partner;

                if (_isEditMode)
                {
                    partner = context.Partners.FirstOrDefault(p => p.ID == _partnerId);
                    if (partner == null)
                    {
                        throw new Exception("Партнер не найден в базе данных");
                    }
                    var oldRequests = context.Partner_products_request
                        .Where(r => r.ID_Partner == _partnerId)
                        .ToList();

                    foreach (var request in oldRequests)
                    {
                        context.Partner_products_request.Remove(request);
                    }
                }
                else
                {
                    partner = new Partners();
                    context.Partners.Add(partner);
                }

                partner.ID_Partner_type = (int?)PartnerTypeComboBox.SelectedValue;
                partner.Name = NameTextBox.Text.Trim();
                partner.Director = DirectorTextBox.Text.Trim();
                partner.Address = AddressTextBox.Text.Trim();
                partner.Rating = int.Parse(RatingTextBox.Text);
                partner.Phone = PhoneTextBox.Text.Trim();
                partner.Email = EmailTextBox.Text.Trim();

                context.SaveChanges();

                foreach (var productInRequest in _productsInRequest)
                {
                    var request = new Partner_products_request
                    {
                        ID_Partner = partner.ID,
                        ID_Product = productInRequest.ProductId,
                        Count = productInRequest.Count
                    };
                    context.Partner_products_request.Add(request);
                }

                context.SaveChanges();

                MessageBox.Show("Данные партнера и заявки успешно сохранены!");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Вы уверены, что хотите удалить эту заявку? Это действие нельзя отменить.",
                    "Подтверждение удаления", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    DeletePartnerAndRequests();
                    DialogResult = true;
                    PartnerSaved?.Invoke(this, EventArgs.Empty);
                    Close();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при удалении заявки");
            }
        }

        private void DeletePartnerAndRequests()
        {
            using (var context = new Entities())
            {
                var requests = context.Partner_products_request
                    .Where(r => r.ID_Partner == _partnerId)
                    .ToList();

                foreach (var request in requests)
                {
                    context.Partner_products_request.Remove(request);
                }

                var partner = context.Partners.FirstOrDefault(p => p.ID == _partnerId);
                if (partner != null)
                {
                    context.Partners.Remove(partner);
                }

                context.SaveChanges();

                MessageBox.Show("Заявка и данные партнера успешно удалены!");
            }
        }
    }

    /// <summary>
    /// Класс для отображения продукта в заявке
    /// </summary>
    public class ProductInRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Count { get; set; }
        public double UnitCost { get; set; }
        public double TotalCost { get; set; }
    }
}