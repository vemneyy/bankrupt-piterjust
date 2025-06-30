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
            _ = EnsureAllTablesExistAsync();
        }

        private async Task EnsureAllTablesExistAsync()
        {
            await EnsurePersonTableExistsAsync();
            await EnsureBasisTableExistsAsync();
            await EnsurePassportTableExistsAsync();
            await EnsureAddressTableExistsAsync();
            await EnsureEmployeeTableExistsAsync();
            await EnsureStatusTablesExistAsync();
            await EnsureContractTableExistsAsync();
            await EnsurePaymentScheduleTableExistsAsync();
        }

        private async Task EnsurePersonTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS person (
                    person_id SERIAL PRIMARY KEY,
                    last_name VARCHAR(100) NOT NULL,
                    first_name VARCHAR(100) NOT NULL,
                    middle_name VARCHAR(100),
                    is_male BOOLEAN DEFAULT true,
                    phone VARCHAR(20),
                    email VARCHAR(100)
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);

            string checkColumnsSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='person' AND column_name='is_male'
                    ) THEN
                        ALTER TABLE person ADD COLUMN is_male BOOLEAN DEFAULT true;
                    END IF;
                END $$;";

            await _databaseService.ExecuteNonQueryAsync(checkColumnsSql);
        }

        private async Task EnsureBasisTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS basis (
                    basis_id SERIAL PRIMARY KEY,
                    basis_type VARCHAR(50) NOT NULL,
                    document_number VARCHAR(50) NOT NULL,
                    document_date DATE NOT NULL,
                    CONSTRAINT basis_basis_type_check CHECK ((basis_type::text = ANY ((ARRAY['Доверенность', 'Приказ'])::text[])))
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);
        }

        private async Task EnsurePassportTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS passport (
                    passport_id SERIAL PRIMARY KEY,
                    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
                    series VARCHAR(10) NOT NULL,
                    number VARCHAR(20) NOT NULL,
                    issued_by TEXT NOT NULL,
                    division_code VARCHAR(20),
                    issue_date DATE NOT NULL
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);
        }

        private async Task EnsureAddressTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS address (
                    address_id SERIAL PRIMARY KEY,
                    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
                    postal_code VARCHAR(20),
                    country VARCHAR(100) NOT NULL DEFAULT 'Россия',
                    region VARCHAR(100),
                    district VARCHAR(100),
                    city VARCHAR(100),
                    locality VARCHAR(100),
                    street VARCHAR(100),
                    house_number VARCHAR(20),
                    building VARCHAR(20),
                    apartment VARCHAR(20)
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);
        }

        private async Task EnsureEmployeeTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS employee (
                    employee_id SERIAL PRIMARY KEY,
                    position VARCHAR(100) NOT NULL,
                    login VARCHAR(100) UNIQUE NOT NULL,
                    password_hash TEXT NOT NULL,
                    created_date DATE DEFAULT CURRENT_DATE,
                    is_active BOOLEAN DEFAULT true NOT NULL,
                    basis VARCHAR(255),
                    person_id INTEGER REFERENCES person(person_id) ON DELETE CASCADE ON UPDATE CASCADE
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);
        }

        private async Task EnsureStatusTablesExistAsync()
        {
            string createStatusSql = @"
                CREATE TABLE IF NOT EXISTS status (
                    status_id SERIAL PRIMARY KEY,
                    name VARCHAR(100) UNIQUE NOT NULL
                )";

            string createMainCategorySql = @"
                CREATE TABLE IF NOT EXISTS main_category (
                    main_category_id SERIAL PRIMARY KEY,
                    name VARCHAR(100) UNIQUE NOT NULL
                )";

            string createFilterCategorySql = @"
                CREATE TABLE IF NOT EXISTS filter_category (
                    filter_category_id SERIAL PRIMARY KEY,
                    main_category_id INTEGER NOT NULL REFERENCES main_category(main_category_id),
                    name VARCHAR(100) UNIQUE NOT NULL
                )";

            string createDebtorSql = @"
                CREATE TABLE IF NOT EXISTS debtor (
                    debtor_id SERIAL PRIMARY KEY,
                    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
                    status_id INTEGER NOT NULL REFERENCES status(status_id),
                    main_category_id INTEGER NOT NULL REFERENCES main_category(main_category_id),
                    filter_category_id INTEGER NOT NULL REFERENCES filter_category(filter_category_id),
                    created_date DATE NOT NULL DEFAULT CURRENT_DATE
                )";

            await _databaseService.ExecuteNonQueryAsync(createStatusSql);
            await _databaseService.ExecuteNonQueryAsync(createMainCategorySql);
            await _databaseService.ExecuteNonQueryAsync(createFilterCategorySql);
            await _databaseService.ExecuteNonQueryAsync(createDebtorSql);
        }

        private async Task EnsureContractTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS contract (
                    contract_id SERIAL PRIMARY KEY,
                    contract_number VARCHAR(50) NOT NULL,
                    city VARCHAR(100) NOT NULL,
                    contract_date DATE NOT NULL,
                    debtor_id INTEGER NOT NULL REFERENCES debtor(debtor_id) ON DELETE CASCADE ON UPDATE CASCADE,
                    employee_id INTEGER NOT NULL REFERENCES employee(employee_id) ON DELETE CASCADE ON UPDATE CASCADE,
                    total_cost NUMERIC(12, 2) NOT NULL,
                    mandatory_expenses NUMERIC(12, 2) NOT NULL,
                    manager_fee NUMERIC(12, 2) NOT NULL,
                    other_expenses NUMERIC(12, 2) NOT NULL,
                    first_stage_cost NUMERIC(12, 2),
                    second_stage_cost NUMERIC(12, 2),
                    third_stage_cost NUMERIC(12, 2)
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);

            string checkColumnsSql = @"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='contract' AND column_name='first_stage_cost') THEN
                        ALTER TABLE contract ADD COLUMN first_stage_cost NUMERIC(12,2);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='contract' AND column_name='second_stage_cost') THEN
                        ALTER TABLE contract ADD COLUMN second_stage_cost NUMERIC(12,2);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='contract' AND column_name='third_stage_cost') THEN
                        ALTER TABLE contract ADD COLUMN third_stage_cost NUMERIC(12,2);
                    END IF;
                END $$;";
            await _databaseService.ExecuteNonQueryAsync(checkColumnsSql);
        }

        private async Task EnsurePaymentScheduleTableExistsAsync()
        {
            string sql = @"
                CREATE TABLE IF NOT EXISTS payment_schedule (
                    schedule_id SERIAL PRIMARY KEY,
                    contract_id INTEGER NOT NULL REFERENCES contract(contract_id) ON DELETE CASCADE,
                    stage INTEGER NOT NULL,
                    description TEXT NOT NULL,
                    amount NUMERIC(12, 2) NOT NULL,
                    amount_words TEXT,
                    due_date DATE
                )";
            await _databaseService.ExecuteNonQueryAsync(sql);
        }

        #region Employee Methods
        public async Task<int> CreateEmployeeAsync(Employee employee)
        {
            string sql = @"
                INSERT INTO employee (position, login, password_hash, created_date, is_active, basis, person_id)
                VALUES (@position, @login, @passwordHash, @createdDate, @isActive, @basis, @personId)
                RETURNING employee_id";

            var parameters = new Dictionary<string, object>
            {
                { "@position", employee.Position },
                { "@login", employee.Login },
                { "@passwordHash", employee.PasswordHash },
                { "@createdDate", employee.CreatedDate ?? (object)DBNull.Value },
                { "@isActive", employee.IsActive },
                { "@basis", employee.Basis ?? (object)DBNull.Value },
                { "@personId", employee.PersonId ?? (object)DBNull.Value }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters);
            return result is Int64 value ? (int)value : Convert.ToInt32(result);
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
        {
            string sql = @"
                SELECT e.*, p.last_name, p.first_name, p.middle_name, p.phone, p.email
                FROM employee e
                LEFT JOIN person p ON e.person_id = p.person_id
                WHERE e.employee_id = @employeeId";

            var parameters = new Dictionary<string, object> { { "@employeeId", employeeId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0) return null;

            var row = dataTable.Rows[0];
            var employee = new Employee
            {
                EmployeeId = Convert.ToInt32(row["employee_id"]),
                Position = row["position"].ToString() ?? string.Empty,
                Login = row["login"].ToString() ?? string.Empty,
                PasswordHash = row["password_hash"].ToString() ?? string.Empty,
                CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                IsActive = Convert.ToBoolean(row["is_active"]),
                Basis = row["basis"] != DBNull.Value ? row["basis"].ToString() : null,
                PersonId = row["person_id"] != DBNull.Value ? Convert.ToInt32(row["person_id"]) : null
            };

            if (employee.PersonId.HasValue && row["last_name"] != DBNull.Value)
            {
                employee.Person = new Person
                {
                    PersonId = employee.PersonId.Value,
                    LastName = row["last_name"].ToString() ?? string.Empty,
                    FirstName = row["first_name"].ToString() ?? string.Empty,
                    MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                    Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                    Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
                };
            }

            return employee;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            string sql = @"
                SELECT e.*, p.last_name, p.first_name, p.middle_name, p.phone, p.email
                FROM employee e
                LEFT JOIN person p ON e.person_id = p.person_id
                ORDER BY e.position, p.last_name";

            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var employees = new List<Employee>();

            foreach (DataRow row in dataTable.Rows)
            {
                var employee = new Employee
                {
                    EmployeeId = Convert.ToInt32(row["employee_id"]),
                    Position = row["position"].ToString() ?? string.Empty,
                    Login = row["login"].ToString() ?? string.Empty,
                    PasswordHash = row["password_hash"].ToString() ?? string.Empty,
                    CreatedDate = row["created_date"] != DBNull.Value ? Convert.ToDateTime(row["created_date"]) : null,
                    IsActive = Convert.ToBoolean(row["is_active"]),
                    Basis = row["basis"] != DBNull.Value ? row["basis"].ToString() : null,
                    PersonId = row["person_id"] != DBNull.Value ? Convert.ToInt32(row["person_id"]) : null
                };

                if (employee.PersonId.HasValue && row["last_name"] != DBNull.Value)
                {
                    employee.Person = new Person
                    {
                        PersonId = employee.PersonId.Value,
                        LastName = row["last_name"].ToString() ?? string.Empty,
                        FirstName = row["first_name"].ToString() ?? string.Empty,
                        MiddleName = row["middle_name"] != DBNull.Value ? row["middle_name"].ToString() : null,
                        Phone = row["phone"] != DBNull.Value ? row["phone"].ToString() : null,
                        Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
                    };
                }

                employees.Add(employee);
            }

            return employees;
        }

        public async Task UpdateEmployeeAsync(Employee employee)
        {
            string sql = @"
                UPDATE employee SET
                    position = @position, login = @login, password_hash = @passwordHash,
                    is_active = @isActive, basis = @basis, person_id = @personId
                WHERE employee_id = @employeeId";

            var parameters = new Dictionary<string, object>
            {
                { "@position", employee.Position },
                { "@login", employee.Login },
                { "@passwordHash", employee.PasswordHash },
                { "@isActive", employee.IsActive },
                { "@basis", employee.Basis ?? (object)DBNull.Value },
                { "@personId", employee.PersonId ?? (object)DBNull.Value },
                { "@employeeId", employee.EmployeeId }
            };

            await _databaseService.ExecuteNonQueryAsync(sql, parameters);
        }

        public async Task<string?> GetEmployeeBasisStringAsync(int employeeId)
        {
            string sql = @"
                SELECT b.basis_type, b.document_number, b.document_date
                FROM employee e
                JOIN basis b ON e.basis_id = b.basis_id
                WHERE e.employee_id = @eid";

            var p = new Dictionary<string, object> { { "@eid", employeeId } };
            var table = await _databaseService.ExecuteReaderAsync(sql, p);
            if (table.Rows.Count == 0)
                return null;

            var row = table.Rows[0];
            string type = row["basis_type"].ToString() ?? string.Empty;
            string number = row["document_number"].ToString() ?? string.Empty;
            DateTime date = Convert.ToDateTime(row["document_date"]);
            return $"{type} № {number} от {date:dd.MM.yyyy}";
        }
        #endregion

        #region Contract Methods
        public async Task<int> CreateContractAsync(Contract contract)
        {
            string sql = @"
                INSERT INTO contract (contract_number, city, contract_date, debtor_id, employee_id,
                                    total_cost, mandatory_expenses,
                                    manager_fee, other_expenses, first_stage_cost, second_stage_cost, third_stage_cost)
                VALUES (@contractNumber, @city, @contractDate, @debtorId, @employeeId,
                        @totalCost, @mandatoryExpenses,
                        @managerFee, @otherExpenses, @stage1, @stage2, @stage3)
                RETURNING contract_id";

            var parameters = new Dictionary<string, object>
            {
                { "@contractNumber", contract.ContractNumber },
                { "@city", contract.City },
                { "@contractDate", contract.ContractDate },
                { "@debtorId", contract.DebtorId },
                { "@employeeId", contract.EmployeeId },
                { "@totalCost", contract.TotalCost },
                { "@mandatoryExpenses", contract.MandatoryExpenses },
                { "@managerFee", contract.ManagerFee },
                { "@otherExpenses", contract.OtherExpenses },
                { "@stage1", contract.Stage1Cost },
                { "@stage2", contract.Stage2Cost },
                { "@stage3", contract.Stage3Cost }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters);
            return result is Int64 value ? (int)value : Convert.ToInt32(result);
        }

        public async Task<Contract?> GetContractByIdAsync(int contractId)
        {
            string sql = @"
                SELECT c.*, 
                       e.position, e.login, 
                       ep.last_name as emp_last_name, ep.first_name as emp_first_name, ep.middle_name as emp_middle_name,
                       dp.last_name as debtor_last_name, dp.first_name as debtor_first_name, dp.middle_name as debtor_middle_name
                FROM contract c
                LEFT JOIN employee e ON c.employee_id = e.employee_id
                LEFT JOIN person ep ON e.person_id = ep.person_id
                LEFT JOIN debtor d ON c.debtor_id = d.debtor_id
                LEFT JOIN person dp ON d.person_id = dp.person_id
                WHERE c.contract_id = @contractId";

            var parameters = new Dictionary<string, object> { { "@contractId", contractId } };
            var dataTable = await _databaseService.ExecuteReaderAsync(sql, parameters);

            if (dataTable.Rows.Count == 0) return null;

            var row = dataTable.Rows[0];
            var contract = new Contract
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
                Stage1Cost = row["first_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["first_stage_cost"]) : 0m,
                Stage2Cost = row["second_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["second_stage_cost"]) : 0m,
                Stage3Cost = row["third_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["third_stage_cost"]) : 0m
            };

            if (row["position"] != DBNull.Value)
            {
                contract.Employee = new Employee
                {
                    EmployeeId = contract.EmployeeId,
                    Position = row["position"].ToString() ?? string.Empty,
                    Login = row["login"].ToString() ?? string.Empty,
                };

                if (row["emp_last_name"] != DBNull.Value)
                {
                    contract.Employee.Person = new Person
                    {
                        LastName = row["emp_last_name"].ToString() ?? string.Empty,
                        FirstName = row["emp_first_name"].ToString() ?? string.Empty,
                        MiddleName = row["emp_middle_name"] != DBNull.Value ? row["emp_middle_name"].ToString() : null
                    };
                }
            }

            if (row["debtor_last_name"] != DBNull.Value)
            {
                contract.Debtor = new Person
                {
                    LastName = row["debtor_last_name"].ToString() ?? string.Empty,
                    FirstName = row["debtor_first_name"].ToString() ?? string.Empty,
                    MiddleName = row["debtor_middle_name"] != DBNull.Value ? row["debtor_middle_name"].ToString() : null
                };
            }

            return contract;
        }

        public async Task<List<Contract>> GetAllContractsAsync()
        {
            string sql = @"
                SELECT c.*, 
                       e.position, e.login,
                       ep.last_name as emp_last_name, ep.first_name as emp_first_name, ep.middle_name as emp_middle_name,
                       dp.last_name as debtor_last_name, dp.first_name as debtor_first_name, dp.middle_name as debtor_middle_name
                FROM contract c
                LEFT JOIN employee e ON c.employee_id = e.employee_id
                LEFT JOIN person ep ON e.person_id = ep.person_id
                LEFT JOIN debtor d ON c.debtor_id = d.debtor_id
                LEFT JOIN person dp ON d.person_id = dp.person_id
                ORDER BY c.contract_date DESC";

            var dataTable = await _databaseService.ExecuteReaderAsync(sql);
            var contracts = new List<Contract>();

            foreach (DataRow row in dataTable.Rows)
            {
                var contract = new Contract
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
                    Stage1Cost = row["first_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["first_stage_cost"]) : 0m,
                    Stage2Cost = row["second_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["second_stage_cost"]) : 0m,
                    Stage3Cost = row["third_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["third_stage_cost"]) : 0m
                };

                if (row["position"] != DBNull.Value)
                {
                    contract.Employee = new Employee
                    {
                        EmployeeId = contract.EmployeeId,
                        Position = row["position"].ToString() ?? string.Empty,
                        Login = row["login"].ToString() ?? string.Empty,
                    };

                    if (row["emp_last_name"] != DBNull.Value)
                    {
                        contract.Employee.Person = new Person
                        {
                            LastName = row["emp_last_name"].ToString() ?? string.Empty,
                            FirstName = row["emp_first_name"].ToString() ?? string.Empty,
                            MiddleName = row["emp_middle_name"] != DBNull.Value ? row["emp_middle_name"].ToString() : null
                        };
                    }
                }

                if (row["debtor_last_name"] != DBNull.Value)
                {
                    contract.Debtor = new Person
                    {
                        LastName = row["debtor_last_name"].ToString() ?? string.Empty,
                        FirstName = row["debtor_first_name"].ToString() ?? string.Empty,
                        MiddleName = row["debtor_middle_name"] != DBNull.Value ? row["debtor_middle_name"].ToString() : null
                    };
                }

                contracts.Add(contract);
            }

            return contracts;
        }

        public async Task UpdateContractAsync(Contract contract)
        {
            string sql = @"
                UPDATE contract SET 
                    contract_number = @contractNumber, city = @city, contract_date = @contractDate,
                    debtor_id = @debtorId, employee_id = @employeeId, total_cost = @totalCost,
                    mandatory_expenses = @mandatoryExpenses, manager_fee = @managerFee,
                    other_expenses = @otherExpenses,
                    first_stage_cost = @stage1,
                    second_stage_cost = @stage2,
                    third_stage_cost = @stage3
                WHERE contract_id = @contractId";

            var parameters = new Dictionary<string, object>
            {
                { "@contractNumber", contract.ContractNumber },
                { "@city", contract.City },
                { "@contractDate", contract.ContractDate },
                { "@debtorId", contract.DebtorId },
                { "@employeeId", contract.EmployeeId },
                { "@totalCost", contract.TotalCost },
                { "@mandatoryExpenses", contract.MandatoryExpenses },
                { "@managerFee", contract.ManagerFee },
                { "@otherExpenses", contract.OtherExpenses },
                { "@stage1", contract.Stage1Cost },
                { "@stage2", contract.Stage2Cost },
                { "@stage3", contract.Stage3Cost },
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
        #endregion

        #region Payment Schedule Methods
        public async Task<int> CreatePaymentScheduleAsync(PaymentSchedule schedule)
        {
            string sql = @"
                INSERT INTO payment_schedule (contract_id, stage, description, amount, amount_words, due_date)
                VALUES (@contractId, @stage, @description, @amount, @amountWords, @dueDate)
                RETURNING schedule_id";

            var parameters = new Dictionary<string, object>
            {
                { "@contractId", schedule.ContractId },
                { "@stage", schedule.Stage },
                { "@description", schedule.Description },
                { "@amount", schedule.Amount },
                { "@amountWords", schedule.AmountWords ?? (object)DBNull.Value },
                { "@dueDate", schedule.DueDate ?? (object)DBNull.Value }
            };

            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters);
            return result is Int64 value ? (int)value : Convert.ToInt32(result);
        }

        public async Task<List<PaymentSchedule>> GetPaymentScheduleByContractIdAsync(int contractId)
        {
            string sql = @"
                SELECT ps.*, c.contract_number
                FROM payment_schedule ps
                LEFT JOIN contract c ON ps.contract_id = c.contract_id
                WHERE ps.contract_id = @contractId
                ORDER BY ps.stage";

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
                    AmountWords = row["amount_words"] != DBNull.Value ? row["amount_words"].ToString() : null,
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
            string sql = @"
                UPDATE payment_schedule SET 
                    stage = @stage, description = @description, amount = @amount,
                    amount_words = @amountWords, due_date = @dueDate
                WHERE schedule_id = @scheduleId";

            var parameters = new Dictionary<string, object>
            {
                { "@stage", schedule.Stage },
                { "@description", schedule.Description },
                { "@amount", schedule.Amount },
                { "@amountWords", schedule.AmountWords ?? (object)DBNull.Value },
                { "@dueDate", schedule.DueDate ?? (object)DBNull.Value },
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

        public async Task<Contract?> GetLatestContractByDebtorIdAsync(int debtorId)
        {
            string sql = @"
                SELECT * FROM contract
                WHERE debtor_id = @debtorId
                ORDER BY contract_date DESC
                LIMIT 1";

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
                Stage1Cost = row["first_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["first_stage_cost"]) : 0m,
                Stage2Cost = row["second_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["second_stage_cost"]) : 0m,
                Stage3Cost = row["third_stage_cost"] != DBNull.Value ? Convert.ToDecimal(row["third_stage_cost"]) : 0m
            };
        }
        #endregion

        #region Helper Methods
        public async Task<int> GetDebtorIdByPersonIdAsync(int personId)
        {
            string sql = "SELECT debtor_id FROM debtor WHERE person_id = @personId";
            var parameters = new Dictionary<string, object> { { "@personId", personId } };
            var result = await _databaseService.ExecuteScalarAsync<object>(sql, parameters) ?? throw new Exception($"Должник с person_id {personId} не найден в базе данных.");
            return result is Int64 value ? (int)value : Convert.ToInt32(result);
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
                Email = row["email"] != DBNull.Value ? row["email"].ToString() : null
            };
        }
        #endregion
    }
}