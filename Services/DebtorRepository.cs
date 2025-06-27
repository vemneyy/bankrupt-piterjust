using bankrupt_piterjust.Models;
using System.Data;

namespace bankrupt_piterjust.Services
{
    public class DebtorRepository
    {
        private readonly DatabaseService _databaseService;

        public DebtorRepository()
        {
            _databaseService = new DatabaseService();
            // Ensure tables exist when repository is initialized
            _ = EnsureTablesExistAsync();
        }

        /// <summary>
        /// Ensures that all necessary tables exist in the database
        /// </summary>
        private async Task EnsureTablesExistAsync()
        {
            // Ensure person table exists with all necessary columns
            await EnsurePersonTableExistsAsync();

            // Ensure passport table exists
            await EnsurePassportTableExistsAsync();

            // Ensure address table exists with address_type enum
            await EnsureAddressTableExistsAsync();

            // Ensure table for debtor statuses exists
            await EnsureDebtorStatusTableExistsAsync();
        }

        /// <summary>
        /// Ensures that the person table exists with all required columns
        /// </summary>
        private async Task EnsurePersonTableExistsAsync()
        {
            // First, check if the person table exists
            string createPersonTableSql = @"
                CREATE TABLE IF NOT EXISTS person (
                    person_id SERIAL PRIMARY KEY,
                    last_name VARCHAR(100) NOT NULL,
                    first_name VARCHAR(100) NOT NULL,
                    middle_name VARCHAR(100),
                    phone VARCHAR(20),
                    email VARCHAR(100)
                )";

            await _databaseService.ExecuteNonQueryAsync(createPersonTableSql);

            // Then check if phone and email columns exist, and add them if they don't
            string checkColumnsSql = @"
                DO $$
                BEGIN
                    -- Check if phone column exists
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='person' AND column_name='phone'
                    ) THEN
                        ALTER TABLE person ADD COLUMN phone VARCHAR(20);
                    END IF;
                    
                    -- Check if email column exists
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name='person' AND column_name='email'
                    ) THEN
                        ALTER TABLE person ADD COLUMN email VARCHAR(100);
                    END IF;
                END $$;";

            await _databaseService.ExecuteNonQueryAsync(checkColumnsSql);
        }

        /// <summary>
        /// Ensures that the passport table exists
        /// </summary>
        private async Task EnsurePassportTableExistsAsync()
        {
            string createPassportTableSql = @"
                CREATE TABLE IF NOT EXISTS passport (
                    passport_id SERIAL PRIMARY KEY,
                    person_id INTEGER NOT NULL REFERENCES person(person_id),
                    series VARCHAR(10) NOT NULL,
                    number VARCHAR(20) NOT NULL,
                    issued_by TEXT NOT NULL,
                    division_code VARCHAR(20),
                    issue_date DATE NOT NULL
                )";

            await _databaseService.ExecuteNonQueryAsync(createPassportTableSql);
        }

        /// <summary>
        /// Ensures that the address table exists with address_type enum
        /// </summary>
        private async Task EnsureAddressTableExistsAsync()
        {
            // First create the address_type_enum if it doesn't exist
            string createEnumSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'address_type_enum') THEN
                        CREATE TYPE address_type_enum AS ENUM ('registration', 'residence', 'mailing');
                    END IF;
                END$$;";

            await _databaseService.ExecuteNonQueryAsync(createEnumSql);

            // Then create the address table
            string createAddressTableSql = @"
                CREATE TABLE IF NOT EXISTS address (
                    address_id SERIAL PRIMARY KEY,
                    person_id INTEGER NOT NULL REFERENCES person(person_id),
                    address_type address_type_enum NOT NULL,
                    address_text TEXT NOT NULL
                )";

            await _databaseService.ExecuteNonQueryAsync(createAddressTableSql);
        }

        /// <summary>
        /// Ensures that the debtor_status table exists
        /// </summary>
        private async Task EnsureDebtorStatusTableExistsAsync()
        {
            string createStatusTableSql = @"
                CREATE TABLE IF NOT EXISTS debtor_status (
                    person_id INTEGER PRIMARY KEY REFERENCES person(person_id) ON DELETE CASCADE,
                    main_category VARCHAR(50) NOT NULL,
                    filter_category VARCHAR(100) NOT NULL,
                    status VARCHAR(100) NOT NULL,
                    updated_at TIMESTAMP DEFAULT NOW()
                )";

            await _databaseService.ExecuteNonQueryAsync(createStatusTableSql);
        }

        /// <summary>
        /// Get all debtors from the database
        /// </summary>
        public async Task<List<Debtor>> GetAllDebtorsAsync()
        {
            string sql = "SELECT p.person_id, p.last_name, p.first_name, p.middle_name, p.phone, p.email, " +
                         "a.address_text as region, ds.status, ds.main_category, ds.filter_category, ds.updated_at " +
                         "FROM person p " +
                         "LEFT JOIN address a ON p.person_id = a.person_id AND a.address_type = 'registration' " +
                         "LEFT JOIN debtor_status ds ON p.person_id = ds.person_id";

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

                if (row["status"] != DBNull.Value)
                    debtor.Status = row["status"].ToString() ?? string.Empty;
                if (row["main_category"] != DBNull.Value)
                    debtor.MainCategory = row["main_category"].ToString() ?? string.Empty;
                if (row["filter_category"] != DBNull.Value)
                    debtor.FilterCategory = row["filter_category"].ToString() ?? string.Empty;
                if (row["updated_at"] != DBNull.Value)
                    debtor.Date = Convert.ToDateTime(row["updated_at"]).ToString("dd.MM.yyyy");

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
            // Ensure tables exist before trying to insert data
            await EnsureTablesExistAsync();

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

        /// <summary>
        /// Inserts or updates status information for a debtor
        /// </summary>
        public async Task UpsertDebtorStatusAsync(int personId, string mainCategory, string filterCategory, string status)
        {
            await EnsureDebtorStatusTableExistsAsync();

            string sql = @"
                INSERT INTO debtor_status (person_id, main_category, filter_category, status, updated_at)
                VALUES (@personId, @mainCategory, @filterCategory, @status, NOW())
                ON CONFLICT (person_id) DO UPDATE
                SET main_category = EXCLUDED.main_category,
                    filter_category = EXCLUDED.filter_category,
                    status = EXCLUDED.status,
                    updated_at = NOW();";

            var parameters = new Dictionary<string, object>
            {
                { "@personId", personId },
                { "@mainCategory", mainCategory },
                { "@filterCategory", filterCategory },
                { "@status", status }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// Updates person information
        /// </summary>
        public async Task UpdatePersonAsync(Person person)
        {
            string sql = @"UPDATE person SET last_name=@lastName, first_name=@firstName, middle_name=@middleName, phone=@phone, email=@email WHERE person_id=@personId";

            var parameters = new Dictionary<string, object>
            {
                { "@lastName", person.LastName },
                { "@firstName", person.FirstName },
                { "@middleName", person.MiddleName != null ? person.MiddleName : DBNull.Value },
                { "@phone", person.Phone != null ? person.Phone : DBNull.Value },
                { "@email", person.Email != null ? person.Email : DBNull.Value },
                { "@personId", person.PersonId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// Updates passport information
        /// </summary>
        public async Task UpdatePassportAsync(Passport passport)
        {
            string sql = @"UPDATE passport SET series=@series, number=@number, issued_by=@issuedBy, division_code=@divisionCode, issue_date=@issueDate WHERE passport_id=@passportId";

            var parameters = new Dictionary<string, object>
            {
                { "@series", passport.Series },
                { "@number", passport.Number },
                { "@issuedBy", passport.IssuedBy },
                { "@divisionCode", passport.DivisionCode != null ? passport.DivisionCode : DBNull.Value },
                { "@issueDate", passport.IssueDate },
                { "@passportId", passport.PassportId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        /// <summary>
        /// Updates address information
        /// </summary>
        public async Task UpdateAddressAsync(Address address)
        {
            string sql = @"UPDATE address SET address_text=@addressText WHERE address_id=@addressId";

            var parameters = new Dictionary<string, object>
            {
                { "@addressText", address.AddressText },
                { "@addressId", address.AddressId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }
    }
}