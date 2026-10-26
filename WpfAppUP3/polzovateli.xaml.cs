using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для polzovateli.xaml — управление пользователями (только администратор).
    /// </summary>
    public partial class polzovateli : Window
    {
        public string ConnectString;
        private readonly DataTable _table = new DataTable();

        public polzovateli(string connectString)
        {
            InitializeComponent();
            ConnectString = connectString;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadRoles();
            RefreshDataGrid();
        }

        private void LoadRoles()
        {
            List<IdName> roles = new List<IdName>();
            using (SqlConnection con = Db.Open(ConnectString))
            using (SqlCommand cmd = new SqlCommand("SELECT Id_roli, Nazvanie_roli FROM UP03_Roli ORDER BY Id_roli", con))
            using (SqlDataReader r = cmd.ExecuteReader())
                while (r.Read())
                    roles.Add(new IdName { Id = Convert.ToInt32(r[0]), Name = r[1].ToString() });
            cmbRole.ItemsSource = roles;
        }

        private void RefreshDataGrid()
        {
            try
            {
                _table.Clear();
                string query = @"
                    SELECT p.Id_polzovatelya, p.FIO, p.Login, p.Parol, r.Nazvanie_roli,
                           p.Data_registracii,
                           CASE WHEN p.Aktiven = 1 THEN N'Да' ELSE N'Нет' END AS AktivenText,
                           p.Id_roli, p.Aktiven
                    FROM UP03_Polzovateli p
                    JOIN UP03_Roli r ON p.Id_roli = r.Id_roli
                    ORDER BY p.FIO";

                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                using (SqlDataReader reader = cmd.ExecuteReader())
                    _table.Load(reader);

                foreach (DataRow row in _table.Rows)
                    if (row["Data_registracii"] != DBNull.Value)
                        row["Data_registracii"] = Convert.ToDateTime(row["Data_registracii"]).ToString("dd.MM.yyyy");

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
            txtFio.Clear();
            txtLogin.Clear();
            txtParol.Clear();
            cmbRole.SelectedIndex = -1;
            chkAktiven.IsChecked = true;
            txtID.Clear();
            dg.SelectedItem = null;
        }

        private void clear_Click(object sender, RoutedEventArgs e) => ClearFields();

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(txtFio.Text) || string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(txtParol.Text))
            {
                MessageBox.Show("Заполните ФИО, логин и пароль.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (cmbRole.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите роль пользователя.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void add_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate()) return;
            if (MessageBox.Show("Добавить нового пользователя?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"INSERT INTO UP03_Polzovateli (FIO, Login, Parol, Id_roli, Aktiven)
                      VALUES (@fio, @login, @parol, @role, @aktiven)", con))
                {
                    cmd.Parameters.AddWithValue("@fio", txtFio.Text.Trim());
                    cmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                    cmd.Parameters.AddWithValue("@parol", txtParol.Text.Trim());
                    cmd.Parameters.AddWithValue("@role", (int)cmbRole.SelectedValue);
                    cmd.Parameters.AddWithValue("@aktiven", chkAktiven.IsChecked == true ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Пользователь добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении (возможно, логин занят): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void upd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtID.Text))
            {
                MessageBox.Show("Выберите пользователя для изменения.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!Validate()) return;
            if (MessageBox.Show("Изменить данные пользователя?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand(
                    @"UPDATE UP03_Polzovateli
                      SET FIO = @fio, Login = @login, Parol = @parol, Id_roli = @role, Aktiven = @aktiven
                      WHERE Id_polzovatelya = @id", con))
                {
                    cmd.Parameters.AddWithValue("@fio", txtFio.Text.Trim());
                    cmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                    cmd.Parameters.AddWithValue("@parol", txtParol.Text.Trim());
                    cmd.Parameters.AddWithValue("@role", (int)cmbRole.SelectedValue);
                    cmd.Parameters.AddWithValue("@aktiven", chkAktiven.IsChecked == true ? 1 : 0);
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Данные пользователя изменены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Выберите пользователя для удаления.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show("Удалить пользователя? Действие необратимо.", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using (SqlConnection con = Db.Open(ConnectString))
                using (SqlCommand cmd = new SqlCommand("DELETE FROM UP03_Polzovateli WHERE Id_polzovatelya = @id", con))
                {
                    cmd.Parameters.AddWithValue("@id", Convert.ToInt32(txtID.Text));
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Пользователь удалён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                RefreshDataGrid();
                ClearFields();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при удалении (возможно, есть связанные операции): " + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dg_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dg.SelectedItem is DataRowView row)
            {
                txtID.Text = row["Id_polzovatelya"].ToString();
                txtFio.Text = row["FIO"].ToString();
                txtLogin.Text = row["Login"].ToString();
                txtParol.Text = row["Parol"].ToString();
                chkAktiven.IsChecked = row["Aktiven"] != DBNull.Value && Convert.ToBoolean(row["Aktiven"]);

                int roleId = row["Id_roli"] == DBNull.Value ? 0 : Convert.ToInt32(row["Id_roli"]);
                if (cmbRole.ItemsSource is IEnumerable<IdName> roles)
                    foreach (IdName it in roles)
                        if (it.Id == roleId) { cmbRole.SelectedItem = it; break; }
            }
        }
    }
}