using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для postavshchiki.xaml — справочник поставщиков.
    /// </summary>
    public partial class postavshchiki : Window
    {
        public string ConnectString;
        private readonly DataTable _table = new DataTable();

        public postavshchiki(string connectString)
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
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM UP03_Postavshchiki ORDER BY Naimenovanie", con))
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
            txtINN.Clear();
            txtTel.Clear();
            txtEmail.Clear();
            txtAdres.Clear();
            dpData.SelectedDate = null;
            txtID.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите наименование поставщика.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;

            if (MessageBox.Show("Добавить нового поставщика?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Postavshchiki (Naimenovanie, INN, Telefon, Email, Adres, Data_dogovora)
                      VALUES (@name, @inn, @tel, @email, @adres, @data)", con))
                {
                    cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@inn", (object)txtINN.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tel", (object)txtTel.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", (object)txtEmail.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@adres", (object)txtAdres.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.HasValue ? (object)dpData.SelectedDate.Value.Date : DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Поставщик успешно добавлен.", "Успех",
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
                MessageBox.Show("Выберите поставщика для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;

            if (MessageBox.Show("Изменить данные поставщика?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Postavshchiki
                      SET Naimenovanie = @name, INN = @inn, Telefon = @tel, Email = @email,
                          Adres = @adres, Data_dogovora = @data
                      WHERE Id_postavshchika = @id", con))
                {
                    cmd.Parameters.AddWithValue("@name", txtName.Text.Trim());
                    cmd.Parameters.AddWithValue("@inn", (object)txtINN.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@tel", (object)txtTel.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", (object)txtEmail.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@adres", (object)txtAdres.Text.Trim() ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@data", dpData.SelectedDate.HasValue ? (object)dpData.SelectedDate.Value.Date : DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Данные поставщика изменены.", "Успех",
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
                MessageBox.Show("Выберите поставщика для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Удалить поставщика? Действие необратимо.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    "DELETE FROM UP03_Postavshchiki WHERE Id_postavshchika = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Поставщик удалён.", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, есть связанные партии): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_postavshchika"].ToString();
                txtName.Text = row["Naimenovanie"].ToString();
                txtINN.Text = row["INN"].ToString();
                txtTel.Text = row["Telefon"].ToString();
                txtEmail.Text = row["Email"].ToString();
                txtAdres.Text = row["Adres"].ToString();
                if (row["Data_dogovora"] != DBNull.Value)
                    dpData.SelectedDate = Convert.ToDateTime(row["Data_dogovora"]);
                else
                    dpData.SelectedDate = null;
            }
        }
    }
}