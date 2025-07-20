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
        }

        public async Task<List<Debtor>> GetAllDebtorsAsync()
        {
            string sql = @"
                SELECT p.person_id, p.last_name, p.first_name, p.middle_name, p.phone, p.email, p.is_male,
                       fc.name AS status,
                       mc.name AS main_category,
                       fc.name AS filter_category,
                       d.created_date
                FROM person p
                INNER JOIN debtor d ON d.person_id = p.person_id
                INNER JOIN filter_category fc ON fc.filter_category_id = d.filter_category_id
                INNER JOIN main_category mc ON mc.main_category_id = fc.main_category_id
                ORDER BY p.last_name, p.first_name";

            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var debtors = new List<Debtor>();

            foreach (DataRow row in dataTable.Rows)
            {
                int personId = Convert.ToInt32(row["person_id"]);

                var person = new Person
                {
                    PersonId = personId,
                    LastName = row["last_name"].ToString() ?? string.Empty,
                    FirstName = row["first_name"].ToString() ?? string.Empty,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                    Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                    Email = row["email"] != DBNull.Value ? row["email"].ToString() : null,
                    IsMale = row["is_male"] != DBNull.Value ? Convert.ToBoolean(row["is_male"]) : true
                };

                var debtor = Debtor.FromPerson(person);

                var addrList = await GetAddressesByPersonIdAsync(personId);
                var firstAddr = addrList.FirstOrDefault();
                if (firstAddr != null)
                    debtor.Region = firstAddr.Region ?? string.Empty;

                debtor.Status = row["status"] != DBNull.Value ? row["status"].ToString() ?? string.Empty : "";
                debtor.MainCategory = row["main_category"] != DBNull.Value ? row["main_category"].ToString() ?? string.Empty : "";
                debtor.FilterCategory = row["filter_category"] != DBNull.Value ? row["filter_category"].ToString() ?? string.Empty : "";
                if (row.Table.Columns.Contains("created_date") && row["created_date"] != DBNull.Value)
                    debtor.Date = Convert.ToDateTime(row["created_date"]).ToString("dd.MM.yyyy");

                debtors.Add(debtor);
            }

            return debtors;
        }

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

        public async Task<int> AddPersonWithDetailsAsync(
            Person person,
            Passport passport,
            IEnumerable<Address> addresses,
            string status,
            string mainCategory,
            string filterCategory)
        {
            if (passport != null && !string.IsNullOrWhiteSpace(passport.Series) && !string.IsNullOrWhiteSpace(passport.Number))
            {
                bool passportExists = await IsPassportExistsAsync(passport.Series, passport.Number);
                if (passportExists)
                {
                    throw new Exception("Паспорт с такими серией и номером уже существует в базе данных.");
                }
            }

            string insertPersonSql = @"
                INSERT INTO person (last_name, first_name, middle_name, is_male, phone, email)
                VALUES (@lastName, @firstName, @middleName, @isMale, @phone, @email);
                SELECT last_insert_rowid();";

            var personParams = new Dictionary<string, object>
            {
                { "@lastName", person.LastName },
                { "@firstName", person.FirstName },
                { "@middleName", person.MiddleName ?? (object)DBNull.Value },
                { "@phone", person.Phone ?? (object)DBNull.Value },
                { "@email", person.Email ?? (object)DBNull.Value },
                { "@isMale", person.IsMale ? 1 : 0 }
            };

            object? v = await _databaseService.ExecuteScalarAsync<object>(insertPersonSql, personParams);
            int personId = Convert.ToInt32(v);

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
                    { "@divisionCode", passport.DivisionCode ?? (object)DBNull.Value },
                    { "@issueDate", passport.IssueDate.ToString("yyyy-MM-dd") }
                };

                await _databaseService.ExecuteNonQueryAsync(insertPassportSql, passportParams);
            }

            if (addresses != null && addresses.Any())
            {
                foreach (var address in addresses)
                {
                    string insertAddressSql = @"
                        INSERT INTO address (
                            person_id, postal_code, country, region, district, city,
                            locality, street, house_number, building, apartment)
                        VALUES (
                            @personId, @postalCode, @country, @region, @district, @city,
                            @locality, @street, @houseNumber, @building, @apartment)";

                    var addressParams = new Dictionary<string, object>
                    {
                        { "@personId", personId },
                        { "@postalCode", address.PostalCode ?? (object)DBNull.Value },
                        { "@country", address.Country },
                        { "@region", address.Region ?? (object)DBNull.Value },
                        { "@district", address.District ?? (object)DBNull.Value },
                        { "@city", address.City ?? (object)DBNull.Value },
                        { "@locality", address.Locality ?? (object)DBNull.Value },
                        { "@street", address.Street ?? (object)DBNull.Value },
                        { "@houseNumber", address.HouseNumber ?? (object)DBNull.Value },
                        { "@building", address.Building ?? (object)DBNull.Value },
                        { "@apartment", address.Apartment ?? (object)DBNull.Value }
                    };

                    await _databaseService.ExecuteNonQueryAsync(insertAddressSql, addressParams);
                }
            }

            await AddDebtorRecordAsync(personId, mainCategory, filterCategory);
            return personId;
        }

        public async Task<Person?> GetPersonByIdAsync(int personId)
        {
            string sql = "SELECT * FROM person WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            var row = dataTable.Rows[0];
            return new Person
            {
                PersonId = Convert.ToInt32(row["person_id"]),
                LastName = row["last_name"].ToString() ?? string.Empty,
                FirstName = row["first_name"].ToString() ?? string.Empty,
                MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                Email = row["email"] != DBNull.Value ? row["email"].ToString() : null,
                IsMale = row["is_male"] != DBNull.Value ? Convert.ToBoolean(row["is_male"]) : true
            };
        }

        public async Task<Passport?> GetPassportByPersonIdAsync(int personId)
        {
            string sql = "SELECT * FROM passport WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0)
                return null;

            var row = dataTable.Rows[0];
            return new Passport
            {
                PersonId = Convert.ToInt32(row["person_id"]),
                Series = row["series"].ToString() ?? string.Empty,
                Number = row["number"].ToString() ?? string.Empty,
                IssuedBy = row["issued_by"].ToString() ?? string.Empty,
                DivisionCode = row["division_code"] != DBNull.Value ? row["division_code"].ToString() : null,
                IssueDate = Convert.ToDateTime(row["issue_date"])
            };
        }

        public async Task<List<Address>> GetAddressesByPersonIdAsync(int personId)
        {
            string sql = "SELECT * FROM address WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            var addresses = new List<Address>();

            foreach (DataRow row in dataTable.Rows)
            {
                addresses.Add(new Address
                {
                    PersonId = Convert.ToInt32(row["person_id"]),
                    PostalCode = row["postal_code"] != DBNull.Value ? row["postal_code"].ToString() : null,
                    Country = row["country"].ToString() ?? "Россия",
                    Region = row["region"] != DBNull.Value ? row["region"].ToString() : null,
                    District = row["district"] != DBNull.Value ? row["district"].ToString() : null,
                    City = row["city"] != DBNull.Value ? row["city"].ToString() : null,
                    Locality = row["locality"] != DBNull.Value ? row["locality"].ToString() : null,
                    Street = row["street"] != DBNull.Value ? row["street"].ToString() : null,
                    HouseNumber = row["house_number"] != DBNull.Value ? row["house_number"].ToString() : null,
                    Building = row["building"] != DBNull.Value ? row["building"].ToString() : null,
                    Apartment = row["apartment"] != DBNull.Value ? row["apartment"].ToString() : null
                });
            }

            return addresses;
        }

        private async Task<int> GetOrCreateMainCategoryIdAsync(string name)
        {
            string checkSql = "SELECT main_category_id FROM main_category WHERE name=@name";
            var p = new Dictionary<string, object> { { "@name", name } };
            var table = await _databaseService.ExecuteReaderAsync(checkSql, p);
            if (table.Rows.Count > 0)
                return Convert.ToInt32(table.Rows[0]["main_category_id"]);

            string insertSql = "INSERT INTO main_category(name) VALUES(@name); SELECT last_insert_rowid();";
            object? result = await _databaseService.ExecuteScalarAsync<object>(insertSql, p);
            return Convert.ToInt32(result);
        }

        private async Task<int> GetOrCreateFilterCategoryIdAsync(string name, int mainCategoryId)
        {
            string checkSql = "SELECT filter_category_id FROM filter_category WHERE name=@name AND main_category_id=@mainId";
            var p = new Dictionary<string, object> { { "@name", name }, { "@mainId", mainCategoryId } };
            var table = await _databaseService.ExecuteReaderAsync(checkSql, p);
            if (table.Rows.Count > 0)
                return Convert.ToInt32(table.Rows[0]["filter_category_id"]);

            string insertSql = "INSERT INTO filter_category(name, main_category_id) VALUES(@name,@main); SELECT last_insert_rowid();";
            var pp = new Dictionary<string, object> { { "@name", name }, { "@main", mainCategoryId } };
            object? result = await _databaseService.ExecuteScalarAsync<object>(insertSql, pp);
            return Convert.ToInt32(result);
        }

        private async Task AddDebtorRecordAsync(int personId, string mainCategory, string filterCategory)
        {
            int mainId = await GetOrCreateMainCategoryIdAsync(mainCategory);
            int filterId = await GetOrCreateFilterCategoryIdAsync(filterCategory, mainId);

            string sql = "INSERT INTO debtor(person_id, filter_category_id) VALUES(@pid, @fid)";
            var param = new Dictionary<string, object>
            {
                {"@pid", personId},
                {"@fid", filterId}
            };
            await _databaseService.ExecuteNonQueryAsync(sql, param);
        }

        public async Task UpdatePersonAsync(Person person)
        {
            string sql = @"UPDATE person SET last_name=@ln, first_name=@fn, middle_name=@mn, is_male=@isMale, phone=@ph, email=@em WHERE person_id=@id";
            var p = new Dictionary<string, object>
            {
                {"@ln", person.LastName},
                {"@fn", person.FirstName},
                {"@mn", person.MiddleName ?? (object)DBNull.Value},
                {"@ph", person.Phone ?? (object)DBNull.Value},
                {"@em", person.Email ?? (object)DBNull.Value},
                {"@isMale", person.IsMale ? 1 : 0},
                {"@id", person.PersonId}
            };
            await _databaseService.ExecuteNonQueryAsync(sql, p);
        }

        public async Task UpdateDebtorInfoAsync(int personId, string status, string mainCategory, string filterCategory)
        {
            int mainId = await GetOrCreateMainCategoryIdAsync(mainCategory);
            int filterId = await GetOrCreateFilterCategoryIdAsync(filterCategory, mainId);

            string sql = "UPDATE debtor SET filter_category_id=@fid WHERE person_id=@pid";
            var p = new Dictionary<string, object>
            {
                {"@fid", filterId},
                {"@pid", personId}
            };
            await _databaseService.ExecuteNonQueryAsync(sql, p);
        }

        public async Task UpsertPassportAsync(Passport passport)
        {
            var existing = await GetPassportByPersonIdAsync(passport.PersonId);
            if (existing == null)
            {
                string insertSql = @"INSERT INTO passport (person_id, series, number, issued_by, division_code, issue_date)
                                      VALUES (@personId, @series, @number, @issuedBy, @divisionCode, @issueDate)";
                var p = new Dictionary<string, object>
                {
                    {"@personId", passport.PersonId},
                    {"@series", passport.Series},
                    {"@number", passport.Number},
                    {"@issuedBy", passport.IssuedBy},
                    {"@divisionCode", passport.DivisionCode ?? (object)DBNull.Value},
                    {"@issueDate", passport.IssueDate.ToString("yyyy-MM-dd")}
                };
                await _databaseService.ExecuteNonQueryAsync(insertSql, p);
            }
            else
            {
                string updateSql = @"UPDATE passport SET series=@series, number=@number, issued_by=@issuedBy, division_code=@divisionCode, issue_date=@issueDate WHERE person_id=@pid";
                var p = new Dictionary<string, object>
                {
                    {"@series", passport.Series},
                    {"@number", passport.Number},
                    {"@issuedBy", passport.IssuedBy},
                    {"@divisionCode", passport.DivisionCode ?? (object)DBNull.Value},
                    {"@issueDate", passport.IssueDate.ToString("yyyy-MM-dd")},
                    {"@pid", passport.PersonId}
                };
                await _databaseService.ExecuteNonQueryAsync(updateSql, p);
            }
        }

        public async Task ReplaceAddressesAsync(int personId, IEnumerable<Address> addresses)
        {
            string deleteSql = "DELETE FROM address WHERE person_id=@pid";
            await _databaseService.ExecuteNonQueryAsync(deleteSql, new Dictionary<string, object> { { "@pid", personId } });

            if (addresses != null)
            {
                foreach (var address in addresses)
                {
                    string insertSql = @"INSERT INTO address (
                                        person_id, postal_code, country, region, district, city,
                                        locality, street, house_number, building, apartment)
                                    VALUES (
                                        @pid, @postalCode, @country, @region, @district, @city,
                                        @locality, @street, @houseNumber, @building, @apartment)";

                    var p = new Dictionary<string, object>
                    {
                        {"@pid", personId},
                        {"@postalCode", address.PostalCode ?? (object)DBNull.Value },
                        {"@country", address.Country },
                        {"@region", address.Region ?? (object)DBNull.Value },
                        {"@district", address.District ?? (object)DBNull.Value },
                        {"@city", address.City ?? (object)DBNull.Value },
                        {"@locality", address.Locality ?? (object)DBNull.Value },
                        {"@street", address.Street ?? (object)DBNull.Value },
                        {"@houseNumber", address.HouseNumber ?? (object)DBNull.Value },
                        {"@building", address.Building ?? (object)DBNull.Value },
                        {"@apartment", address.Apartment ?? (object)DBNull.Value }
                    };
                    await _databaseService.ExecuteNonQueryAsync(insertSql, p);
                }
            }
        }
    }
}