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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Minina0202
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<PartnerRequestViewModel> _partnerRequests;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPartnerRequests();
            }
            catch
            {
                MessageBox.Show("Ошибка при загрузке данных");
            }
        }

        private void LoadPartnerRequests()
        {
            using (var context = new Entities())
            {
                var partnerRequests = context.Partners
                    .Include("Partner_type")
                    .Include("Partner_products_request.Products")
                    .Where(p => p.Partner_products_request.Any())
                    .ToList();

                _partnerRequests = new List<PartnerRequestViewModel>();

                foreach (var partner in partnerRequests)
                {
                    var requestViewModel = new PartnerRequestViewModel
                    {
                        PartnerId = partner.ID,
                        PartnerType = partner.Partner_type?.Name ?? "Тип не указан",
                        PartnerName = partner.Name ?? "Неизвестный партнер",
                        Address = partner.Address ?? "Юридический адрес не указан",
                        Phone = partner.Phone ?? "Телефон не указан",
                        Rating = partner.Rating ?? 0,
                        TotalCost = 0
                    };

                    //Расчет общей стоимости
                    foreach (var request in partner.Partner_products_request)
                    {
                        if (request.Products != null && request.Count.HasValue)
                        {
                            double unitCost = request.Products.Minimal_cost_for_partner ?? 0;
                            int count = request.Count.Value;
                            double productTotalCost = Math.Round(unitCost * count, 2);

                            if (productTotalCost < 0) productTotalCost = 0;
                            requestViewModel.TotalCost += productTotalCost;
                        }
                    }

                    requestViewModel.TotalCost = Math.Round(requestViewModel.TotalCost, 2);
                    _partnerRequests.Add(requestViewModel);
                }

                RequestsListBox.ItemsSource = _partnerRequests;
            }
        }

        /// <summary>
        /// Метод при нажатии лкм  мыши по ячейке
        /// </summary>
        
        private void PartnerItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is PartnerRequestViewModel partner)
            {
                try
                {
                    var editWindow = new PartnerEdit(partner.PartnerId);
                    editWindow.Owner = this;
                    editWindow.PartnerSaved += (s, args) => LoadPartnerRequests();

                    if (editWindow.ShowDialog() == true)
                    {
                        LoadPartnerRequests();
                    }
                }
                catch
                {
                    MessageBox.Show("Ошибка при открытии формы редактирования");
                }
            }
        }


        private void AddPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var editWindow = new PartnerEdit();
                editWindow.Owner = this;
                editWindow.PartnerSaved += (s, args) => LoadPartnerRequests();

                if (editWindow.ShowDialog() == true)
                {
                    LoadPartnerRequests();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии формы добавления");
            }
        }

        private void ViewProductsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var productsWindow = new ProductWindow();
                productsWindow.Owner = this;
                productsWindow.ShowDialog();
            }
            catch
            {
                MessageBox.Show("Ошибка при открытии каталога продукции");
            }
        }
    }

    /// <summary>
    /// Класс для отображения заявки
    /// </summary>
    public class PartnerRequestViewModel
    {
        public int PartnerId { get; set; }
        public string PartnerType { get; set; }
        public string PartnerName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public int Rating { get; set; }
        public double TotalCost { get; set; }

        public string RatingText => $"Рейтинг: {Rating}";
    }
}