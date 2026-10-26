using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

namespace WpfAppUP3
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml — форма авторизации.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnAutorization_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogin.Text) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Введите логин и пароль.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

                // Строка подключения задаётся в Db.cs перед локальным запуском.
            string connectString = Db.ConnectString(txtServer.Text.Trim(), txtBd.Text.Trim());

            try
            {
                using (SqlConnection con = Db.Open(connectString))
                {
                    string query = @"SELECT p.Id_polzovatelya, p.FIO, p.Id_roli, r.Nazvanie_roli
                                     FROM UP03_Polzovateli p
                                     JOIN UP03_Roli r ON p.Id_roli = r.Id_roli
                                     WHERE p.Login = @login AND p.Parol = @parol AND p.Aktiven = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                        cmd.Parameters.AddWithValue("@parol", txtPassword.Password);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = Convert.ToInt32(reader["Id_polzovatelya"]);
                                string fio = reader["FIO"].ToString();
                                string role = reader["Nazvanie_roli"].ToString();

                                menu menuWindow = new menu(connectString, userId, fio, role);
                                menuWindow.Show();
                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Неверный логин или пароль, либо учётная запись заблокирована.",
                                    "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
