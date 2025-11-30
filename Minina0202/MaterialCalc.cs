using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minina0202
{
    public static class MaterialCalculator
    {
        public static int CalculateRequiredMaterial(
            int productTypeId,
            int materialTypeId,
            int requiredProductCount,
            int productStockCount,
            double productParam1,
            double productParam2)
        {
            try
            {
                //Проверка параметров
                if (requiredProductCount <= 0 || productStockCount < 0 ||
                    productParam1 <= 0 || productParam2 <= 0)
                {
                    return -1;
                }

                using (var context = new Entities())
                {
                    //Проверка типов продукции и материалов
                    var productType = context.Product_type.FirstOrDefault(pt => pt.ID == productTypeId);
                    var materialType = context.Material_type.FirstOrDefault(mt => mt.ID == materialTypeId);

                    if (productType == null || materialType == null)
                    {
                        return -1;
                    }

                    //Наличие продукции на складе
                    int productionCount = Math.Max(0, requiredProductCount - productStockCount);
                    if (productionCount == 0)
                    {
                        return 0;
                    }

                    //Коэффициент типа продукции
                    double productTypeFactor = productType.Product_Type_Factor ?? 1.0;

                    //Процент брака материала
                    double defectPercentage = materialType.Percentage_of_defective_materials ?? 0.0;
                    double defectFactor = 1.0 + (defectPercentage / 100.0);

                    //Расчет материала на единицу продукции
                    double materialPerUnit = productParam1 * productParam2 * productTypeFactor;

                    //Количество материала с учетом брака
                    double totalMaterial = materialPerUnit * productionCount * defectFactor;

                    //Округление
                    return (int)Math.Ceiling(totalMaterial);
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}