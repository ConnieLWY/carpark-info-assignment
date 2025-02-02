using CsvHelper.Configuration;
using CsvHelper;
using HandshakesByDC_BEAssignment.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Newtonsoft.Json;

namespace HandshakesByDC_BEAssignment
{
    public interface IFileReaderStrategy
    {
        IEnumerable<Carpark> ReadFile(string filePath);
    }

    public class CsvFileReaderStrategy : IFileReaderStrategy
    {
        public IEnumerable<Carpark> ReadFile(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                HeaderValidated = null,
                MissingFieldFound = null
            }))
            {
                csv.Read();
                csv.ReadHeader();

                while (csv.Read())
                {
                    var carpark = new Carpark
                    {
                        CarparkNo = csv.GetField<string>("car_park_no"),
                        Address = csv.GetField<string>("address"),
                        XCoord = csv.GetField<float>("x_coord"),
                        YCoord = csv.GetField<float>("y_coord"),
                        CarParkType = csv.GetField<string>("car_park_type"),
                        TypeOfParkingSystem = csv.GetField<string>("type_of_parking_system"),
                        ShortTermParking = csv.GetField<string>("short_term_parking"),
                        FreeParking = csv.GetField<string>("free_parking"),
                        NightParking = csv.GetField<string>("night_parking") == "YES",
                        CarParkDecks = csv.GetField<int>("car_park_decks"),
                        GantryHeight = csv.GetField<float>("gantry_height"),
                        CarParkBasement = csv.GetField<string>("car_park_basement"),
                        LastUpdated = DateTime.Now
                    };
                    yield return carpark;
                }
            }
        }
    }

    public class JsonFileReaderStrategy : IFileReaderStrategy
    {
        public IEnumerable<Carpark> ReadFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var carparks = JsonConvert.DeserializeObject<List<Carpark>>(json);
            return carparks;
        }
    }

    public class BackgroundRefresh : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly SampleData _data;
        private readonly string _filePath;
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IFileReaderStrategy _fileReaderStrategy;

        public BackgroundRefresh(SampleData data, IConfiguration configuration, IDbContextFactory<AppDbContext> dbContextFactory, IFileReaderStrategy fileReaderStrategy)
        {
            _data = data;
            _filePath = configuration["FilePath"];
            _dbContextFactory = dbContextFactory;
            _fileReaderStrategy = fileReaderStrategy;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ReadFileAndSaveToDatabase, null, TimeSpan.Zero, TimeSpan.FromDays(1));
            return Task.CompletedTask;
        }

        private void LogMessage(string message)
        {
            _data.Logs.Add($"{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff", CultureInfo.InvariantCulture)} {message}");
        }

        private void ValidateRecord(Carpark carpark, int rowNumber)
        {
            var nullFields = new List<string>();

            // Check each property for null or empty values
            if (string.IsNullOrWhiteSpace(carpark.CarparkNo)) nullFields.Add("CarparkNo");
            if (string.IsNullOrWhiteSpace(carpark.Address)) nullFields.Add("Address");
            if (string.IsNullOrWhiteSpace(carpark.CarParkType)) nullFields.Add("CarParkType");
            if (string.IsNullOrWhiteSpace(carpark.TypeOfParkingSystem)) nullFields.Add("TypeOfParkingSystem");
            if (string.IsNullOrWhiteSpace(carpark.ShortTermParking)) nullFields.Add("ShortTermParking");
            if (string.IsNullOrWhiteSpace(carpark.FreeParking)) nullFields.Add("FreeParking");
            if (string.IsNullOrWhiteSpace(carpark.CarParkBasement)) nullFields.Add("CarParkBasement");

            if (nullFields.Any())
            {
                throw new Exception($"Row {rowNumber} contains null values in the following fields: {string.Join(", ", nullFields)}");
            }
        }

        private async void ReadFileAndSaveToDatabase(object? state)
        {
            try
            {
                LogMessage("Reading file");

                // First pass: Check for duplicates and null values
                var carparkNos = new HashSet<string>();
                var duplicates = new HashSet<string>();
                var rowNumber = 1;

                var carparks = _fileReaderStrategy.ReadFile(_filePath).ToList();

                foreach (var carpark in carparks)
                {
                    rowNumber++;
                    ValidateRecord(carpark, rowNumber);

                    if (!carparkNos.Add(carpark.CarparkNo))
                    {
                        duplicates.Add(carpark.CarparkNo);
                    }
                }

                // If duplicates found, throw error
                if (duplicates.Any())
                {
                    throw new Exception($"Duplicate CarparkNo found in file: {string.Join(", ", duplicates)}");
                }

                // If no duplicates and no null values, proceed with processing
                using (var dbContext = _dbContextFactory.CreateDbContext())
                using (var transaction = await dbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        foreach (var carpark in carparks)
                        {
                            var existingCarpark = await dbContext.Carparks.FirstOrDefaultAsync(c => c.CarparkNo == carpark.CarparkNo);
                            if (existingCarpark != null)
                            {
                                // Update existing carpark
                                existingCarpark.Address = carpark.Address;
                                existingCarpark.XCoord = carpark.XCoord;
                                existingCarpark.YCoord = carpark.YCoord;
                                existingCarpark.CarParkType = carpark.CarParkType;
                                existingCarpark.TypeOfParkingSystem = carpark.TypeOfParkingSystem;
                                existingCarpark.ShortTermParking = carpark.ShortTermParking;
                                existingCarpark.FreeParking = carpark.FreeParking;
                                existingCarpark.NightParking = carpark.NightParking;
                                existingCarpark.CarParkDecks = carpark.CarParkDecks;
                                existingCarpark.GantryHeight = carpark.GantryHeight;
                                existingCarpark.CarParkBasement = carpark.CarParkBasement;
                                existingCarpark.LastUpdated = DateTime.Now;
                            }
                            else
                            {
                                // Insert new carpark
                                dbContext.Carparks.Add(carpark);
                            }
                        }

                        await dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                        LogMessage("File processed and saved to database.");
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error processing file: {ex.Message}");
            }
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        void IDisposable.Dispose()
        {
            _timer?.Dispose();
        }
    }
}