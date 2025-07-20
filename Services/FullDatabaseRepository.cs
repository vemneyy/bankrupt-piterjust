using bankrupt_piterjust.Models;
using System.Data;

namespace bankrupt_piterjust.Services
{
    public class FullDatabaseRepository
    {
        private readonly DatabaseService _databaseService;

        public FullDatabaseRepository()
        {
            _databaseService = new DatabaseService();
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            var sql = "SELECT e.employee_id, e.position, e.created_date, e.is_active, e.basis_id, e.person_id, " +
                     "p.last_name, p.first_name, p.middle_name, p.phone, p.email, p.is_male, " +
                     "b.basis_type, b.document_number, b.document_date " +
                     "FROM employee e " +
                     "INNER JOIN person p ON e.person_id = p.person_id " +
                     "LEFT JOIN basis b ON e.basis_id = b.basis_id " +
                     "ORDER BY e.position, p.last_name";

            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var employees = new List<Employee>();

            foreach (DataRow row in dataTable.Rows)
            {
                var employee = new Employee
                {
                    EmployeeId = Convert.ToInt32(row["employee_id"]),
                    Position = row["position"].ToString() ?? string.Empty,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = row["is_active"] != DBNull.Value ? Convert.ToBoolean(row["is_active"]) : true,
                    BasisId = row["basis_id"] != DBNull.Value ? Convert.ToInt32(row["basis_id"]) : null,
                    PersonId = row["person_id"] != DBNull.Value ? Convert.ToInt32(row["person_id"]) : null
                };

                if (employee.PersonId.HasValue)
                {
                    employee.Person = new Person
                    {
                        PersonId = employee.PersonId.Value,
                        LastName = row["last_name"].ToString() ?? string.Empty,
                        FirstName = row["first_name"].ToString() ?? string.Empty,
                        MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                        Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                        Email = row["email"] != DBNull.Value ? row["email"].ToString() : null,
                        IsMale = row["is_male"] != DBNull.Value ? Convert.ToBoolean(row["is_male"]) : true
                    };
                }

                if (employee.BasisId.HasValue && row["basis_type"] != DBNull.Value)
                {
                    employee.BasisInfo = new Basis
                    {
                        BasisId = employee.BasisId.Value,
                        BasisType = row["basis_type"].ToString() ?? string.Empty,
                        DocumentNumber = row["document_number"].ToString() ?? string.Empty,
                        DocumentDate = Convert.ToDateTime(row["document_date"])
                    };

                    employee.Basis = $"{employee.BasisInfo.BasisType} № {employee.BasisInfo.DocumentNumber} от {employee.BasisInfo.DocumentDate:dd.MM.yyyy}";
                }

                employees.Add(employee);
            }

            return employees;
        }

        public async Task<string?> GetEmployeeBasisStringAsync(int employeeId)
        {
            var sql = "SELECT b.basis_type, b.document_number, b.document_date " +
                     "FROM employee e " +
                     "LEFT JOIN basis b ON e.basis_id = b.basis_id " +
                     "WHERE e.employee_id = @eid";

            var p = new Dictionary<string, object> { { "@eid", employeeId } };
            var table = await _databaseService.ExecuteReaderAsync(sql, p);
            if (table.Rows.Count == 0 || table.Rows[0]["basis_type"] == DBNull.Value)
                return null;

            var row = table.Rows[0];
            string type = row["basis_type"].ToString() ?? string.Empty;
            string number = row["document_number"].ToString() ?? string.Empty;
            DateTime date = Convert.ToDateTime(row["document_date"]);
            return $"{type} № {number} от {date:dd.MM.yyyy}";
        }

        public async Task<List<Contract>> GetAllContractsAsync()
        {
            string sql = "SELECT * FROM contract ORDER BY contract_date DESC";
            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var contracts = new List<Contract>();

            foreach (DataRow row in dataTable.Rows)
            {
                contracts.Add(new Contract
                {
                    ContractId = Convert.ToInt32(row["contract_id"]),
                    ContractNumber = row["contract_number"].ToString() ?? string.Empty,
                    City = row["city"].ToString() ?? string.Empty,
                    ContractDate = Convert.ToDateTime(row["contract_date"]),
                    DebtorId = Convert.ToInt32(row["debtor_id"]),
                    EmployeeId = Convert.ToInt32(row["employee_id"]),
                    TotalCost = Convert.ToDecimal(row["total_cost"]),
                    MandatoryExpenses = Convert.ToDecimal(row["mandatory_expenses"]),
                    ManagerFee = Convert.ToDecimal(row["manager_fee"]),
                    OtherExpenses = Convert.ToDecimal(row["other_expenses"]),
                    ServicesAmount = row["services_amount"] != DBNull.Value ? Convert.ToDecimal(row["services_amount"]) : 0m
                });
            }

            return contracts;
        }

        public async Task<int> CreateContractAsync(Contract contract)
        {
            var sql = "INSERT INTO contract (contract_number, city, contract_date, debtor_id, employee_id, " +
                     "total_cost, mandatory_expenses, manager_fee, other_expenses, services_amount) " +
                     "VALUES (@contractNumber, @city, @contractDate, @debtorId, @employeeId, " +
                     "@totalCost, @mandatoryExpenses, @managerFee, @otherExpenses, @servicesAmount); " +
                     "SELECT last_insert_rowid();";

            var parameters = new Dictionary<string, object>
            {
                { "@contractNumber", contract.ContractNumber },
                { "@city", contract.City },
                { "@contractDate", contract.ContractDate.ToString("yyyy-MM-dd") },
                { "@debtorId", contract.DebtorId },
                { "@employeeId", contract.EmployeeId },
                { "@totalCost", contract.TotalCost },
                { "@mandatoryExpenses", contract.MandatoryExpenses },
                { "@managerFee", contract.ManagerFee },
                { "@otherExpenses", contract.OtherExpenses },
                { "@servicesAmount", contract.ServicesAmount }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters);
            return Convert.ToInt32(result);
        }

        public async Task UpdateContractAsync(Contract contract)
        {
            var sql = "UPDATE contract SET " +
                     "contract_number = @contractNumber, city = @city, contract_date = @contractDate, " +
                     "debtor_id = @debtorId, employee_id = @employeeId, total_cost = @totalCost, " +
                     "mandatory_expenses = @mandatoryExpenses, manager_fee = @managerFee, " +
                     "other_expenses = @otherExpenses, services_amount = @servicesAmount " +
                     "WHERE contract_id = @contractId";

            var parameters = new Dictionary<string, object>
            {
                { "@contractNumber", contract.ContractNumber },
                { "@city", contract.City },
                { "@contractDate", contract.ContractDate.ToString("yyyy-MM-dd") },
                { "@debtorId", contract.DebtorId },
                { "@employeeId", contract.EmployeeId },
                { "@totalCost", contract.TotalCost },
                { "@mandatoryExpenses", contract.MandatoryExpenses },
                { "@managerFee", contract.ManagerFee },
                { "@otherExpenses", contract.OtherExpenses },
                { "@servicesAmount", contract.ServicesAmount },
                { "@contractId", contract.ContractId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task DeleteContractAsync(int contractId)
        {
            string sql = "DELETE FROM contract WHERE contract_id = @contractId";
            var parameters = new Dictionary<string, object> { { "@contractId", contractId } };
            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<Contract?> GetLatestContractByDebtorIdAsync(int debtorId)
        {
            var sql = "SELECT * FROM contract WHERE debtor_id = @debtorId ORDER BY contract_date DESC LIMIT 1";

            var parameters = new Dictionary<string, object> { { "@debtorId", debtorId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            if (dataTable.Rows.Count == 0) return null;

            var row = dataTable.Rows[0];
            return new Contract
            {
                ContractId = Convert.ToInt32(row["contract_id"]),
                ContractNumber = row["contract_number"].ToString() ?? string.Empty,
                City = row["city"].ToString() ?? string.Empty,
                ContractDate = Convert.ToDateTime(row["contract_date"]),
                DebtorId = Convert.ToInt32(row["debtor_id"]),
                EmployeeId = Convert.ToInt32(row["employee_id"]),
                TotalCost = Convert.ToDecimal(row["total_cost"]),
                MandatoryExpenses = Convert.ToDecimal(row["mandatory_expenses"]),
                ManagerFee = Convert.ToDecimal(row["manager_fee"]),
                OtherExpenses = Convert.ToDecimal(row["other_expenses"]),
                ServicesAmount = row["services_amount"] != DBNull.Value ? Convert.ToDecimal(row["services_amount"]) : 0m
            };
        }

        public async Task<int> CreateContractStageAsync(ContractStage stage)
        {
            var sql = "INSERT INTO contract_stage (contract_id, stage, amount, due_date) " +
                     "VALUES (@contractId, @stage, @amount, @dueDate); " +
                     "SELECT last_insert_rowid();";

            var p = new Dictionary<string, object>
            {
                {"@contractId", stage.ContractId},
                {"@stage", stage.Stage},
                {"@amount", stage.Amount},
                {"@dueDate", stage.DueDate.ToString("yyyy-MM-dd") }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, p);
            return Convert.ToInt32(result);
        }

        public async Task<List<ContractStage>> GetContractStagesByContractIdAsync(int contractId)
        {
            var sql = "SELECT * FROM contract_stage WHERE contract_id = @cid ORDER BY stage";

            var table = await _databaseService.ExecuteReaderAsync(sql, new Dictionary<string, object> { { "@cid", contractId } });
            var list = new List<ContractStage>();
            foreach (DataRow row in table.Rows)
            {
                list.Add(new ContractStage
                {
                    ContractStageId = Convert.ToInt32(row["contract_stage_id"]),
                    ContractId = Convert.ToInt32(row["contract_id"]),
                    Stage = Convert.ToInt32(row["stage"]),
                    Amount = Convert.ToDecimal(row["amount"]),
                    DueDate = Convert.ToDateTime(row["due_date"])
                });
            }
            return list;
        }

        public async Task DeleteContractStagesByContractIdAsync(int contractId)
        {
            string sql = "DELETE FROM contract_stage WHERE contract_id = @cid";
            var p = new Dictionary<string, object> { { "@cid", contractId } };
            await _databaseService.ExecuteNonQueryAsync(sql, p);
        }

        public async Task<int> CreatePaymentScheduleAsync(PaymentSchedule schedule)
        {
            var sql = "INSERT INTO payment_schedule (contract_id, stage, description, amount, due_date) " +
                     "VALUES (@contractId, @stage, @description, @amount, @dueDate); " +
                     "SELECT last_insert_rowid();";

            var parameters = new Dictionary<string, object>
            {
                { "@contractId", schedule.ContractId },
                { "@stage", schedule.Stage },
                { "@description", schedule.Description },
                { "@amount", schedule.Amount },
                { "@dueDate", schedule.DueDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters);
            return Convert.ToInt32(result);
        }

        public async Task<List<PaymentSchedule>> GetPaymentScheduleByContractIdAsync(int contractId)
        {
            var sql = "SELECT ps.*, c.contract_number FROM payment_schedule ps " +
                     "LEFT JOIN contract c ON ps.contract_id = c.contract_id " +
                     "WHERE ps.contract_id = @contractId ORDER BY ps.stage";

            var parameters = new Dictionary<string, object> { { "@contractId", contractId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);
            var schedules = new List<PaymentSchedule>();

            foreach (DataRow row in dataTable.Rows)
            {
                var schedule = new PaymentSchedule
                {
                    ScheduleId = Convert.ToInt32(row["schedule_id"]),
                    ContractId = Convert.ToInt32(row["contract_id"]),
                    Stage = Convert.ToInt32(row["stage"]),
                    Description = row["description"].ToString() ?? string.Empty,
                    Amount = Convert.ToDecimal(row["amount"]),
                    DueDate = row["due_date"] != DBNull.Value ? Convert.ToDateTime(row["due_date"]) : null
                };

                if (row["contract_number"] != DBNull.Value)
                {
                    schedule.Contract = new Contract
                    {
                        ContractId = schedule.ContractId,
                        ContractNumber = row["contract_number"].ToString() ?? string.Empty
                    };
                }

                schedules.Add(schedule);
            }

            return schedules;
        }

        public async Task UpdatePaymentScheduleAsync(PaymentSchedule schedule)
        {
            var sql = "UPDATE payment_schedule SET " +
                     "stage = @stage, description = @description, amount = @amount, due_date = @dueDate " +
                     "WHERE schedule_id = @scheduleId";

            var parameters = new Dictionary<string, object>
            {
                { "@stage", schedule.Stage },
                { "@description", schedule.Description },
                { "@amount", schedule.Amount },
                { "@dueDate", schedule.DueDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value },
                { "@scheduleId", schedule.ScheduleId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task DeletePaymentScheduleAsync(int scheduleId)
        {
            string sql = "DELETE FROM payment_schedule WHERE schedule_id = @scheduleId";
            var parameters = new Dictionary<string, object> { { "@scheduleId", scheduleId } };
            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task DeletePaymentScheduleByContractIdAsync(int contractId)
        {
            string sql = "DELETE FROM payment_schedule WHERE contract_id = @contractId";
            var parameters = new Dictionary<string, object> { { "@contractId", contractId } };
            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<int> GetDebtorIdByPersonIdAsync(int personId)
        {
            string sql = "SELECT debtor_id FROM debtor WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters) ?? throw new Exception($"Должник с person_id {personId} не найден в базе данных.");
            return Convert.ToInt32(result);
        }

        public async Task<Person?> GetPersonByIdAsync(int personId)
        {
            string sql = "SELECT * FROM person WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0) return null;

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
    }
}