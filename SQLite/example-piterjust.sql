-- Включаем поддержку внешних ключей для сессии.
PRAGMA foreign_keys = ON;

-- Таблица person остается практически без изменений, за исключением типов данных.
CREATE TABLE person (
	person_id INTEGER PRIMARY KEY,
	last_name TEXT NOT NULL,
	first_name TEXT NOT NULL,
	middle_name TEXT NULL,
	phone TEXT NULL,
	email TEXT NULL,
	is_male INTEGER NOT NULL -- 0 for false, 1 for true
);

-- Таблица address ссылается на person.
CREATE TABLE address (
	person_id INTEGER NOT NULL PRIMARY KEY,
	postal_code TEXT NULL,
	country TEXT DEFAULT 'Россия' NOT NULL,
	region TEXT NULL,
	district TEXT NULL,
	city TEXT NULL,
	locality TEXT NULL,
	street TEXT NULL,
	house_number TEXT NULL,
	building TEXT NULL,
	apartment TEXT NULL,
	FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
);

-- Таблица basis.
CREATE TABLE basis (
	basis_id INTEGER PRIMARY KEY,
	basis_type TEXT NOT NULL,
	document_number TEXT NOT NULL,
	document_date TEXT NOT NULL, -- Stored as 'YYYY-MM-DD'
	CONSTRAINT basis_basis_type_check CHECK (basis_type IN ('Доверенность', 'Приказ'))
);

-- Таблица main_category
CREATE TABLE main_category (
	main_category_id INTEGER PRIMARY KEY,
	"name" TEXT NOT NULL,
	CONSTRAINT main_category_name_key UNIQUE (name)
);

-- Таблица filter_category
CREATE TABLE filter_category (
	filter_category_id INTEGER PRIMARY KEY,
	main_category_id INTEGER NOT NULL,
	"name" TEXT NOT NULL,
	CONSTRAINT filter_category_main_name_key UNIQUE (main_category_id, name),
	CONSTRAINT filter_category_main_category_id_fkey FOREIGN KEY (main_category_id) REFERENCES main_category(main_category_id)
);

-- Таблица debtor
CREATE TABLE debtor (
	debtor_id INTEGER PRIMARY KEY,
	person_id INTEGER NOT NULL,
	filter_category_id INTEGER NOT NULL,
	created_date TEXT NOT NULL DEFAULT (CURRENT_DATE),
	CONSTRAINT debtor_person_id_fkey FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE,
	CONSTRAINT debtor_filter_category_id_fkey FOREIGN KEY (filter_category_id) REFERENCES filter_category(filter_category_id)
);

-- Таблица employee (убраны поля login и password_hash)
CREATE TABLE employee (
	employee_id INTEGER PRIMARY KEY,
	"position" TEXT NOT NULL,
	created_date TEXT NULL, -- Stored as 'YYYY-MM-DD'
	is_active INTEGER NOT NULL DEFAULT 1, -- 0 for false, 1 for true
	basis_id INTEGER NULL,
	person_id INTEGER NULL,
	CONSTRAINT employee_basis_fk FOREIGN KEY (basis_id) REFERENCES basis(basis_id) ON DELETE SET NULL,
	CONSTRAINT employee_person_fk FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
);

-- Таблица contract
CREATE TABLE contract (
	contract_id INTEGER PRIMARY KEY,
	contract_number TEXT NOT NULL,
	city TEXT NOT NULL,
	contract_date TEXT NOT NULL, -- Stored as 'YYYY-MM-DD'
	debtor_id INTEGER NOT NULL,
	employee_id INTEGER NOT NULL,
	total_cost NUMERIC NOT NULL,
	mandatory_expenses NUMERIC NOT NULL,
	manager_fee NUMERIC NOT NULL,
	other_expenses NUMERIC NOT NULL,
	services_amount NUMERIC NULL,
	CONSTRAINT contract_debtor_fk FOREIGN KEY (debtor_id) REFERENCES debtor(debtor_id) ON DELETE CASCADE,
	CONSTRAINT contract_employee_fk FOREIGN KEY (employee_id) REFERENCES employee(employee_id) ON DELETE CASCADE
);

-- Таблица contract_stage
CREATE TABLE contract_stage (
	contract_stage_id INTEGER PRIMARY KEY,
	contract_id INTEGER NOT NULL,
	stage INTEGER NOT NULL,
	amount NUMERIC NOT NULL,
	due_date TEXT NOT NULL, -- Stored as 'YYYY-MM-DD'
	is_active INTEGER DEFAULT 0, -- 0 for false, 1 for true
	CONSTRAINT contract_stage_contract_fk FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
);

-- Таблица passport
CREATE TABLE passport (
	person_id INTEGER NOT NULL PRIMARY KEY,
	series TEXT NOT NULL,
	"number" TEXT NOT NULL,
	issued_by TEXT NOT NULL,
	division_code TEXT NULL,
	issue_date TEXT NOT NULL, -- Stored as 'YYYY-MM-DD'
	CONSTRAINT passport_unique UNIQUE (number),
	CONSTRAINT passport_person_id_fkey FOREIGN KEY (person_id) REFERENCES person(person_id) ON DELETE CASCADE
);

-- Таблица payment_schedule
CREATE TABLE payment_schedule (
	schedule_id INTEGER PRIMARY KEY,
	contract_id INTEGER NOT NULL,
	stage INTEGER NOT NULL,
	description TEXT NOT NULL,
	amount NUMERIC NOT NULL,
	due_date TEXT NOT NULL, -- Stored as 'YYYY-MM-DD'
	is_paid INTEGER DEFAULT 0, -- 0 for false, 1 for true
	CONSTRAINT payment_schedule_amount_positive CHECK (amount > 0),
	CONSTRAINT payment_schedule_contract_stage_unique UNIQUE (contract_id, stage),
	CONSTRAINT payment_schedule_contract_id_fkey FOREIGN KEY (contract_id) REFERENCES contract(contract_id) ON DELETE CASCADE
);