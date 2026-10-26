using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для materialy.xaml — справочник материалов.
    /// </summary>
    public partial class materialy : Window
    {
        public string ConnectString;
        private readonly DataTable _table = new DataTable();

        public materialy(string connectString)
        {
            InitializeComponent();
            ConnectString = connectString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDataGrid();
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM UP03_Materialy ORDER BY Naimenovanie", con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    _table.Load(reader);
                }
                dg.ItemsSource = _table.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearFields()
        {
            txtName.Clear();
            txtEd.Clear();
            txtTip.Clear();
            txtCena.Clear();
            txtID.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите наименование материала.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private object CenaValue()
        {
            if (decimal.TryParse(txtCena.Text.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal c))
                return c;
            return DBNull.Value;
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            if (MessageBox.Show("Добавить новый материал?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Materialy (Naimenovanie, Edinica_izmereniya, Tip, Cena)
                      VALUES (@name, @ed, @tip, @cena)", con))
                {
                    cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@ed", (object)txtEd.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tip", (object)txtTip.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cena", CenaValue());
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Материал успешно добавлен.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void upd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text))
            {
                MessageBox.Show("Выберите материал для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;

            if (MessageBox.Show("Изменить материал?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Materialy
                      SET Naimenovanie = @name, Edinica_izmereniya = @ed, Tip = @tip, Cena = @cena
                      WHERE Id_materiala = @id", con))
                {
                    cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@ed", (object)txtEd.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tip", (object)txtTip.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cena", CenaValue());
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Материал изменён.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при изменении: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void del_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text))
            {
                MessageBox.Show("Выберите материал для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить материал? Действие необратимо.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM UP03_Materialy WHERE Id_materiala = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Материал удалён.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, есть связанные партии/брак): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_materiala"].ToString();
                txtName.Text = row["Naimenovanie"].ToString();
                txtEd.Text = row["Edinica_izmereniya"].ToString();
                txtTip.Text = row["Tip"].ToString();
                txtCena.Text = row["Cena"] == DBNull.Value ? "" : Convert.ToDecimal(row["Cena"]).ToString("0.##");
            }
        }
    }
}