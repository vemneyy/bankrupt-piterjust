-- ============================================
-- Database Initialization Script
-- Database: PostgreSQL
-- This script sets up the database schema and stored procedures
-- ============================================

-- Enable pgcrypto extension for password hashing
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Create database schema
CREATE SCHEMA IF NOT EXISTS bankrupt_app;

-- Set search path
SET search_path TO bankrupt_app, public;

-- Create employee table
CREATE TABLE IF NOT EXISTS employee (
    employee_id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    position VARCHAR(200) NOT NULL,
    login VARCHAR(100) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    created_date TIMESTAMP NOT NULL DEFAULT NOW(),
    is_active BOOLEAN NOT NULL DEFAULT true
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_employee_login ON employee(login) WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_employee_name ON employee(last_name, first_name) WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_employee_active ON employee(is_active);

-- Create or replace the stored procedure for adding employees
CREATE OR REPLACE FUNCTION add_employee(
    p_last_name VARCHAR(100),
    p_first_name VARCHAR(100),
    p_middle_name VARCHAR(100) DEFAULT NULL,
    p_position VARCHAR(200),
    p_login VARCHAR(100),
    p_password_hash TEXT
) 
RETURNS TABLE(
    employee_id INTEGER,
    success BOOLEAN,
    message TEXT
) 
LANGUAGE plpgsql
AS $$
BEGIN
    -- Check if login already exists
    IF EXISTS (SELECT 1 FROM employee WHERE login = p_login) THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Пользователь с таким логином уже существует'::TEXT as message;
        RETURN;
    END IF;
    
    -- Validate required fields
    IF p_last_name IS NULL OR trim(p_last_name) = '' THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Фамилия обязательна для заполнения'::TEXT as message;
        RETURN;
    END IF;
    
    IF p_first_name IS NULL OR trim(p_first_name) = '' THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Имя обязательно для заполнения'::TEXT as message;
        RETURN;
    END IF;
    
    IF p_position IS NULL OR trim(p_position) = '' THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Должность обязательна для заполнения'::TEXT as message;
        RETURN;
    END IF;
    
    IF p_login IS NULL OR trim(p_login) = '' THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Логин обязателен для заполнения'::TEXT as message;
        RETURN;
    END IF;
    
    IF p_password_hash IS NULL OR trim(p_password_hash) = '' THEN
        RETURN QUERY SELECT 
            NULL::INTEGER as employee_id, 
            FALSE as success, 
            'Хеш пароля обязателен для заполнения'::TEXT as message;
        RETURN;
    END IF;
    
    -- Insert the new employee
    BEGIN
        INSERT INTO employee (
            last_name, 
            first_name, 
            middle_name, 
            position, 
            login, 
            password_hash, 
            created_date, 
            is_active
        )
        VALUES (
            trim(p_last_name),
            trim(p_first_name),
            CASE WHEN p_middle_name IS NULL OR trim(p_middle_name) = '' THEN NULL ELSE trim(p_middle_name) END,
            trim(p_position),
            trim(p_login),
            p_password_hash,
            NOW(),
            true
        )
        RETURNING employee.employee_id INTO employee_id;
        
        RETURN QUERY SELECT 
            employee_id, 
            TRUE as success, 
            'Сотрудник успешно добавлен'::TEXT as message;
            
    EXCEPTION
        WHEN unique_violation THEN
            RETURN QUERY SELECT 
                NULL::INTEGER as employee_id, 
                FALSE as success, 
                'Пользователь с таким логином уже существует'::TEXT as message;
        WHEN OTHERS THEN
            RETURN QUERY SELECT 
                NULL::INTEGER as employee_id, 
                FALSE as success, 
                ('Ошибка при добавлении сотрудника: ' || SQLERRM)::TEXT as message;
    END;
END;
$$;

-- Create helper function to authenticate employee
CREATE OR REPLACE FUNCTION authenticate_employee(
    p_login VARCHAR(100),
    p_password_hash TEXT
)
RETURNS TABLE(
    employee_id INTEGER,
    last_name VARCHAR(100),
    first_name VARCHAR(100),
    middle_name VARCHAR(100),
    position VARCHAR(200),
    login VARCHAR(100),
    created_date TIMESTAMP,
    is_active BOOLEAN,
    authenticated BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY 
    SELECT 
        e.employee_id,
        e.last_name,
        e.first_name,
        e.middle_name,
        e.position,
        e.login,
        e.created_date,
        e.is_active,
        (e.password_hash = p_password_hash) as authenticated
    FROM employee e
    WHERE e.login = p_login AND e.is_active = true;
    
    -- If no rows returned, return a row with authenticated = false
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            NULL::INTEGER, 
            NULL::VARCHAR(100), 
            NULL::VARCHAR(100), 
            NULL::VARCHAR(100), 
            NULL::VARCHAR(200), 
            NULL::VARCHAR(100),
            NULL::TIMESTAMP, 
            NULL::BOOLEAN, 
            FALSE::BOOLEAN;
    END IF;
END;
$$;

-- Function to get employee by name and position (for GetOrCreateEmployeeAsync)
CREATE OR REPLACE FUNCTION find_employee_by_name_position(
    p_last_name VARCHAR(100),
    p_first_name VARCHAR(100),
    p_middle_name VARCHAR(100) DEFAULT NULL,
    p_position VARCHAR(200)
)
RETURNS TABLE(
    employee_id INTEGER,
    last_name VARCHAR(100),
    first_name VARCHAR(100),
    middle_name VARCHAR(100),
    position VARCHAR(200),
    login VARCHAR(100),
    created_date TIMESTAMP,
    is_active BOOLEAN
)
LANGUAGE plpgsql
AS $$
BEGIN
    IF p_middle_name IS NULL OR trim(p_middle_name) = '' THEN
        RETURN QUERY 
        SELECT 
            e.employee_id,
            e.last_name,
            e.first_name,
            e.middle_name,
            e.position,
            e.login,
            e.created_date,
            e.is_active
        FROM employee e
        WHERE e.last_name = p_last_name 
            AND e.first_name = p_first_name
            AND e.position = p_position
            AND e.middle_name IS NULL
            AND e.is_active = true;
    ELSE
        RETURN QUERY 
        SELECT 
            e.employee_id,
            e.last_name,
            e.first_name,
            e.middle_name,
            e.position,
            e.login,
            e.created_date,
            e.is_active
        FROM employee e
        WHERE e.last_name = p_last_name 
            AND e.first_name = p_first_name
            AND e.position = p_position
            AND (e.middle_name = p_middle_name OR e.middle_name IS NULL)
            AND e.is_active = true;
    END IF;
END;
$$;

-- Create default admin user (password: admin123)
-- Hash generated with PostgreSQL crypt function for password 'admin123'
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM employee WHERE login = 'admin') THEN
        INSERT INTO employee (last_name, first_name, middle_name, position, login, password_hash, created_date, is_active)
        VALUES (
            'Администратор',
            'Система',
            NULL,
            'Системный администратор',
            'admin',
            crypt('admin123', gen_salt('bf')),
            NOW(),
            true
        );
    END IF;
END $$;

-- Grant necessary permissions
GRANT USAGE ON SCHEMA bankrupt_app TO PUBLIC;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA bankrupt_app TO PUBLIC;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA bankrupt_app TO PUBLIC;
GRANT EXECUTE ON ALL FUNCTIONS IN SCHEMA bankrupt_app TO PUBLIC;