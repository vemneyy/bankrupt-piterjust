using bankrupt_piterjust.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace bankrupt_piterjust.Services
{
    public class DebtorRepository
    {
        private readonly DatabaseService _databaseService;

        public DebtorRepository()
        {
            _databaseService = new DatabaseService();
        }

        /// <summary>
        /// Get all debtors from the database
        /// </summary>
        public async Task<List<Debtor>> GetAllDebtorsAsync()
        {
            string sql = "SELECT p.person_id, p.last_name, p.first_name, p.middle_name, " +
                         "a.address_text as region " +
                         "FROM person p " +
                         "LEFT JOIN address a ON p.person_id = a.person_id AND a.address_type = 'registration'";

            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var debtors = new List<Debtor>();

            foreach (DataRow row in dataTable.Rows)
            {
                // Fix Int64 to Int32 conversion
                int personId = row["person_id"] is Int64 value ? (int)value : Convert.ToInt32(row["person_id"]);

                var person = new Person
                {
                    PersonId = personId,
                    LastName = row["last_name"].ToString() ?? string.Empty,
                    FirstName = row["first_name"].ToString() ?? string.Empty,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                    Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                    Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
                };

                var debtor = Debtor.FromPerson(person);
                
                // Set the region if available
                if (row["region"] != DBNull.Value)
                {
                    debtor.Region = row["region"].ToString() ?? string.Empty;
                }

                // Default values for UI display
                debtor.Status = "Подать заявление";
                debtor.MainCategory = "Клиенты";
                debtor.FilterCategory = "Подготовка заявления";
                debtor.Date = DateTime.Now.ToString("dd.MM.yyyy");

                debtors.Add(debtor);
            }

            return debtors;
        }

        /// <summary>
        /// Check if passport with given series and number already exists
        /// </summary>
        public async Task<bool> IsPassportExistsAsync(string series, string number)
        {
            if (string.IsNullOrWhiteSpace(series) || string.IsNullOrWhiteSpace(number))
                return false;
                
            string sql = "SELECT COUNT(*) FROM passport WHERE series = @series AND number = @number";
            
            var parameters = new Dictionary<string, object>
            {
                { "@series", series },
                { "@number", number }
            };

            int count = await _databaseService.ExecuteScalarAsync<int>(sql, parameters);
            return count > 0;
        }

        /// <summary>
        /// Insert a new person with their passport and address information
        /// </summary>
        public async Task<int> AddPersonWithDetailsAsync(
            Person person, 
            Passport passport, 
            IEnumerable<Address> addresses)
        {
            // Check for duplicate passport
            if (passport != null && !string.IsNullOrWhiteSpace(passport.Series) && !string.IsNullOrWhiteSpace(passport.Number))
            {
                bool passportExists = await IsPassportExistsAsync(passport.Series, passport.Number);
                if (passportExists)
                {
                    throw new Exception("Паспорт с такими серией и номером уже существует в базе данных.");
                }
            }
            
            // Insert person first to get the person_id
            string insertPersonSql = @"
                INSERT INTO person (last_name, first_name, middle_name, phone, email)
                VALUES (@lastName, @firstName, @middleName, @phone, @email)
                RETURNING person_id";

            var personParams = new Dictionary<string, object>
            {
                { "@lastName", person.LastName },
                { "@firstName", person.FirstName },
                { "@middleName", person.MiddleName != null ? person.MiddleName : DBNull.Value },
                { "@phone", person.Phone != null ? person.Phone : DBNull.Value },
                { "@email", person.Email != null ? person.Email : DBNull.Value }
            };

            // Fix Int64 to Int32 conversion
            object scalarResult = await _databaseService.ExecuteScalarAsync<object>(insertPersonSql, personParams);
            int personId = scalarResult is Int64 value ? (int)value : Convert.ToInt32(scalarResult);

            // Insert passport data
            if (passport != null && !string.IsNullOrWhiteSpace(passport.Series) && !string.IsNullOrWhiteSpace(passport.Number))
            {
                string insertPassportSql = @"
                    INSERT INTO passport (person_id, series, number, issued_by, division_code, issue_date)
                    VALUES (@personId, @series, @number, @issuedBy, @divisionCode, @issueDate)";

                var passportParams = new Dictionary<string, object>
                {
                    { "@personId", personId },
                    { "@series", passport.Series },
                    { "@number", passport.Number },
                    { "@issuedBy", passport.IssuedBy },
                    { "@divisionCode", passport.DivisionCode != null ? passport.DivisionCode : DBNull.Value },
                    { "@issueDate", passport.IssueDate }
                };

                await _databaseService.ExecuteNonQueryAsync(insertPassportSql, passportParams);
            }

            // Insert addresses
            if (addresses != null && addresses.Any())
            {
                foreach (var address in addresses)
                {
                    // Convert AddressType enum to PostgreSQL enum value
                    string addressTypeValue = address.AddressType switch
                    {
                        AddressType.Registration => "registration",
                        AddressType.Residence => "residence",
                        AddressType.Mailing => "mailing",
                        _ => throw new ArgumentOutOfRangeException(nameof(address.AddressType), $"Unexpected address type: {address.AddressType}")
                    };

                    string insertAddressSql = @"
                        INSERT INTO address (person_id, address_type, address_text)
                        VALUES (@personId, @addressType::address_type_enum, @addressText)";

                    var addressParams = new Dictionary<string, object>
                    {
                        { "@personId", personId },
                        { "@addressType", addressTypeValue },
                        { "@addressText", address.AddressText }
                    };

                    await _databaseService.ExecuteNonQueryAsync(insertAddressSql, addressParams);
                }
            }

            return personId;
        }

        /// <summary>
        /// Get person details by ID
        /// </summary>
        public async Task<Person?> GetPersonByIdAsync(int personId)
        {
            string sql = "SELECT * FROM person WHERE person_id = @personId";

            var parameters = new Dictionary<string, object>
            {
                { "@personId", personId }
            };

            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            var row = dataTable.Rows[0];
            
            // Fix Int64 to Int32 conversion
            int id = row["person_id"] is Int64 value ? (int)value : Convert.ToInt32(row["person_id"]);
            
            return new Person
            {
                PersonId = id,
                LastName = row["last_name"].ToString() ?? string.Empty,
                FirstName = row["first_name"].ToString() ?? string.Empty,
                MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
            };
        }

        /// <summary>
        /// Get passport details by person ID
        /// </summary>
        public async Task<Passport?> GetPassportByPersonIdAsync(int personId)
        {
            string sql = "SELECT * FROM passport WHERE person_id = @personId";

            var parameters = new Dictionary<string, object>
            {
                { "@personId", personId }
            };

            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            var row = dataTable.Rows[0];
            
            // Fix Int64 to Int32 conversion for both IDs
            int passportId = row["passport_id"] is Int64 value1 ? (int)value1 : Convert.ToInt32(row["passport_id"]);
            int pId = row["person_id"] is Int64 value2 ? (int)value2 : Convert.ToInt32(row["person_id"]);
            
            return new Passport
            {
                PassportId = passportId,
                PersonId = pId,
                Series = row["series"].ToString() ?? string.Empty,
                Number = row["number"].ToString() ?? string.Empty,
                IssuedBy = row["issued_by"].ToString() ?? string.Empty,
                DivisionCode = row["division_code"] != DBNull.Value ? row["division_code"].ToString() : null,
                IssueDate = Convert.ToDateTime(row["issue_date"])
            };
        }

        /// <summary>
        /// Get addresses by person ID
        /// </summary>
        public async Task<List<Address>> GetAddressesByPersonIdAsync(int personId)
        {
            string sql = "SELECT * FROM address WHERE person_id = @personId";

            var parameters = new Dictionary<string, object>
            {
                { "@personId", personId }
            };

            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            var addresses = new List<Address>();

            foreach (DataRow row in dataTable.Rows)
            {
                // Parse the address type from string to enum
                AddressType addressType;
                Enum.TryParse(row["address_type"].ToString() ?? string.Empty, true, out addressType);

                // Fix Int64 to Int32 conversion for both IDs
                int addressId = row["address_id"] is Int64 value1 ? (int)value1 : Convert.ToInt32(row["address_id"]);
                int pId = row["person_id"] is Int64 value2 ? (int)value2 : Convert.ToInt32(row["person_id"]);
                
                addresses.Add(new Address
                {
                    AddressId = addressId,
                    PersonId = pId,
                    AddressType = addressType,
                    AddressText = row["address_text"].ToString() ?? string.Empty
                });
            }

            return addresses;
        }
    }
}