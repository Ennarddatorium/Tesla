using System;
using System.Data.SQLite;
using System.IO;

namespace TeslaRental
{
    class Program
    {
        static string dbFile = "tesla_rental.db";

        static void Main(string[] args)
        {
            // Datubāzes izveide
            InitializeDatabase();

            // Piemērs: Datu ievadīšana un izsaukšana
            AddCar("Model 3", 15.0, 0.5);
            AddCar("Model Y", 20.0, 0.6);

            AddCustomer("Jānis Bērziņš", "janis.berzins@example.com");

            StartRental(1, 1, DateTime.Now);
            EndRental(1, DateTime.Now.AddHours(2), 50);

            DisplayRentals();
        }

        // Datubāzes inicializācija
        static void InitializeDatabase()
        {
            if (!File.Exists(dbFile))
            {
                SQLiteConnection.CreateFile(dbFile);
                using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
                {
                    connection.Open();
                    string createCars = @"CREATE TABLE Cars (
                                            ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                            Model TEXT NOT NULL,
                                            HourlyRate REAL NOT NULL,
                                            KilometerRate REAL NOT NULL)";

                    string createCustomers = @"CREATE TABLE Customers (
                                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                FullName TEXT NOT NULL,
                                                Email TEXT NOT NULL UNIQUE)";

                    string createRentals = @"CREATE TABLE Rentals (
                                                ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                CustomerID INTEGER NOT NULL,
                                                CarID INTEGER NOT NULL,
                                                StartTime TEXT NOT NULL,
                                                EndTime TEXT,
                                                KilometersDriven REAL,
                                                TotalAmount REAL,
                                                FOREIGN KEY (CustomerID) REFERENCES Customers(ID),
                                                FOREIGN KEY (CarID) REFERENCES Cars(ID))";

                    ExecuteNonQuery(connection, createCars);
                    ExecuteNonQuery(connection, createCustomers);
                    ExecuteNonQuery(connection, createRentals);

                    Console.WriteLine("Datubāze izveidota veiksmīgi.");
                }
            }
        }

        // Funkcija izpildīt SQL komandu
        static void ExecuteNonQuery(SQLiteConnection connection, string query)
        {
            using (var cmd = new SQLiteCommand(query, connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        // Pievienot automašīnu
        static void AddCar(string model, double hourlyRate, double kilometerRate)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                string query = "INSERT INTO Cars (Model, HourlyRate, KilometerRate) VALUES (@Model, @HourlyRate, @KilometerRate)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Model", model);
                    cmd.Parameters.AddWithValue("@HourlyRate", hourlyRate);
                    cmd.Parameters.AddWithValue("@KilometerRate", kilometerRate);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Auto '{model}' pievienots.");
            }
        }

        // Pievienot klientu
        static void AddCustomer(string fullName, string email)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                string query = "INSERT INTO Customers (FullName, Email) VALUES (@FullName, @Email)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@FullName", fullName);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine($"Klients '{fullName}' reģistrēts.");
            }
        }

        // Uzsākt īri
        static void StartRental(int customerId, int carId, DateTime startTime)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                string query = "INSERT INTO Rentals (CustomerID, CarID, StartTime) VALUES (@CustomerID, @CarID, @StartTime)";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@CustomerID", customerId);
                    cmd.Parameters.AddWithValue("@CarID", carId);
                    cmd.Parameters.AddWithValue("@StartTime", startTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine("Īre uzsākta.");
            }
        }

        // Pabeigt īri
        static void EndRental(int rentalId, DateTime endTime, double kilometersDriven)
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                string query = @"UPDATE Rentals 
                                 SET EndTime = @EndTime, KilometersDriven = @KilometersDriven, 
                                     TotalAmount = ((julianday(@EndTime) - julianday(StartTime)) * 24) * (SELECT HourlyRate FROM Cars WHERE ID = Rentals.CarID)
                                                 + (@KilometersDriven * (SELECT KilometerRate FROM Cars WHERE ID = Rentals.CarID))
                                 WHERE ID = @RentalID";
                using (var cmd = new SQLiteCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@EndTime", endTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters.AddWithValue("@KilometersDriven", kilometersDriven);
                    cmd.Parameters.AddWithValue("@RentalID", rentalId);
                    cmd.ExecuteNonQuery();
                }
                Console.WriteLine("Īre pabeigta un maksājums aprēķināts.");
            }
        }

        // Īres pārskats
        static void DisplayRentals()
        {
            using (var connection = new SQLiteConnection($"Data Source={dbFile}"))
            {
                connection.Open();
                string query = "SELECT * FROM Rentals";
                using (var cmd = new SQLiteCommand(query, connection))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"Rental ID: {reader["ID"]}, Total Amount: {reader["TotalAmount"]}");
                    }
                }
            }
        }
    }
}