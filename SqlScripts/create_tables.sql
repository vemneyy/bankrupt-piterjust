-- SQL script to create tables for the PiterJust bankruptcy application
-- Designed for PostgreSQL

CREATE TABLE IF NOT EXISTS person (
    person_id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    phone VARCHAR(20),
    email VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS passport (
    passport_id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
    series VARCHAR(10) NOT NULL,
    number VARCHAR(20) NOT NULL,
    issued_by TEXT NOT NULL,
    division_code VARCHAR(20),
    issue_date DATE NOT NULL
);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'address_type_enum') THEN
        CREATE TYPE address_type_enum AS ENUM ('registration','residence','mailing');
    END IF;
END$$;

CREATE TABLE IF NOT EXISTS address (
    address_id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
    address_type address_type_enum NOT NULL,
    address_text TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS status (
    status_id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL
);

CREATE TABLE IF NOT EXISTS main_category (
    main_category_id SERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL
);

CREATE TABLE IF NOT EXISTS filter_category (
    filter_category_id SERIAL PRIMARY KEY,
    main_category_id INTEGER NOT NULL REFERENCES main_category(main_category_id),
    name VARCHAR(100) UNIQUE NOT NULL
);

CREATE TABLE IF NOT EXISTS debtor (
    debtor_id SERIAL PRIMARY KEY,
    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
    status_id INTEGER NOT NULL REFERENCES status(status_id),
    main_category_id INTEGER NOT NULL REFERENCES main_category(main_category_id),
    filter_category_id INTEGER NOT NULL REFERENCES filter_category(filter_category_id),
    created_date DATE NOT NULL DEFAULT CURRENT_DATE
);

CREATE TABLE IF NOT EXISTS company (
    company_id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    inn VARCHAR(12) NOT NULL,
    kpp VARCHAR(20),
    ogrn VARCHAR(20),
    okpo VARCHAR(20),
    address TEXT,
    phone VARCHAR(20),
    email VARCHAR(100)
);

CREATE TABLE IF NOT EXISTS company_representative (
    representative_id SERIAL PRIMARY KEY,
    company_id INTEGER NOT NULL REFERENCES company(company_id) ON DELETE CASCADE,
    person_id INTEGER NOT NULL REFERENCES person(person_id) ON DELETE CASCADE,
    basis TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS contract (
    contract_id SERIAL PRIMARY KEY,
    contract_number VARCHAR(50) NOT NULL,
    city VARCHAR(100) NOT NULL,
    contract_date DATE NOT NULL,
    customer_id INTEGER NOT NULL REFERENCES person(person_id),
    executor_company_id INTEGER NOT NULL REFERENCES company(company_id),
    representative_id INTEGER NOT NULL REFERENCES company_representative(representative_id),
    total_cost NUMERIC(12,2) NOT NULL,
    total_cost_words TEXT,
    mandatory_expenses NUMERIC(12,2) NOT NULL,
    mandatory_expenses_words TEXT,
    manager_fee NUMERIC(12,2) NOT NULL,
    other_expenses NUMERIC(12,2) NOT NULL
);

CREATE TABLE IF NOT EXISTS payment_schedule (
    schedule_id SERIAL PRIMARY KEY,
    contract_id INTEGER NOT NULL REFERENCES contract(contract_id) ON DELETE CASCADE,
    stage INTEGER NOT NULL,
    description TEXT NOT NULL,
    amount NUMERIC(12,2) NOT NULL,
    amount_words TEXT,
    due_date DATE
);

CREATE TABLE IF NOT EXISTS employee (
    employee_id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    position VARCHAR(100) NOT NULL,
    login VARCHAR(100) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    created_date DATE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE
);
