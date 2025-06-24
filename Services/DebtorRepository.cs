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
                var person = new Person
                {
                    PersonId = Convert.ToInt32(row["person_id"]),
                    LastName = row["last_name"].ToString(),
                    FirstName = row["first_name"].ToString(),
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null
                };

                var debtor = Debtor.FromPerson(person);
                
                // Set the region if available
                if (row["region"] != DBNull.Value)
                {
                    debtor.Region = row["region"].ToString();
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
        /// Insert a new person with their passport and address information
        /// </summary>
        public async Task<int> AddPersonWithDetailsAsync(
            Person person, 
            Passport passport, 
            IEnumerable<Address> addresses)
        {
            // Insert person first to get the person_id
            string insertPersonSql = @"
                INSERT INTO person (last_name, first_name, middle_name, phone, email)
                VALUES (@lastName, @firstName, @middleName, @phone, @email)
                RETURNING person_id";

            var personParams = new Dictionary<string, object>
            {
                { "@lastName", person.LastName },
                { "@firstName", person.FirstName },
                { "@middleName", person.MiddleName },
                { "@phone", person.Phone },
                { "@email", person.Email }
            };

            int personId = await _databaseService.ExecuteScalarAsync<int>(insertPersonSql, personParams);

            // Insert passport data
            if (passport != null)
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
                    { "@divisionCode", passport.DivisionCode },
                    { "@issueDate", passport.IssueDate }
                };

                await _databaseService.ExecuteNonQueryAsync(insertPassportSql, passportParams);
            }

            // Insert addresses
            if (addresses != null && addresses.Any())
            {
                foreach (var address in addresses)
                {
                    string insertAddressSql = @"
                        INSERT INTO address (person_id, address_type, address_text)
                        VALUES (@personId, @addressType, @addressText)";

                    var addressParams = new Dictionary<string, object>
                    {
                        { "@personId", personId },
                        { "@addressType", address.AddressType.ToString().ToLower() },
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
        public async Task<Person> GetPersonByIdAsync(int personId)
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
            return new Person
            {
                PersonId = Convert.ToInt32(row["person_id"]),
                LastName = row["last_name"].ToString(),
                FirstName = row["first_name"].ToString(),
                MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
            };
        }

        /// <summary>
        /// Get passport details by person ID
        /// </summary>
        public async Task<Passport> GetPassportByPersonIdAsync(int personId)
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
            return new Passport
            {
                PassportId = Convert.ToInt32(row["passport_id"]),
                PersonId = Convert.ToInt32(row["person_id"]),
                Series = row["series"].ToString(),
                Number = row["number"].ToString(),
                IssuedBy = row["issued_by"].ToString(),
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
                Enum.TryParse(row["address_type"].ToString(), true, out addressType);

                addresses.Add(new Address
                {
                    AddressId = Convert.ToInt32(row["address_id"]),
                    PersonId = Convert.ToInt32(row["person_id"]),
                    AddressType = addressType,
                    AddressText = row["address_text"].ToString()
                });
            }

            return addresses;
        }
    }
}