using System;
using System.Globalization;

namespace WpfAppUP3
{
    /// <summary>
    /// Пара "идентификатор — наименование" для выпадающих списков.
    /// </summary>
    public class IdName
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int MaterialId { get; set; }
    }

    /// <summary>
    /// Вспомогательные методы преобразования значений.
    /// </summary>
    public static class Utils
    {
        public static object DecimalOrNull(string text)
        {
            if (decimal.TryParse((text ?? "").Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                return value;
            return DBNull.Value;
        }

        public static decimal DecimalOrZero(string text)
        {
            if (decimal.TryParse((text ?? "").Replace(",", "."),
                NumberStyles.Any, CultureInfo.InvariantCulture, out decimal value))
                return value;
            return 0m;
        }
    }
}