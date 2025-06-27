CREATE TABLE public.address (
	address_id serial4 NOT NULL,
	person_id int4 NOT NULL,
	address_type public."address_type_enum" NOT NULL,
	address_text text NOT NULL,
	CONSTRAINT address_pkey PRIMARY KEY (address_id),
	CONSTRAINT address_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);

CREATE TABLE public.contract (
	contract_id serial4 NOT NULL,
	contract_number varchar(50) NOT NULL,
	city varchar(100) NOT NULL,
	contract_date date NOT NULL,
	debtor_id int4 NOT NULL,
	employee_id int4 NOT NULL,
	total_cost numeric(12, 2) NOT NULL,
	total_cost_words text NULL,
	mandatory_expenses numeric(12, 2) NOT NULL,
	mandatory_expenses_words text NULL,
	manager_fee numeric(12, 2) NOT NULL,
	other_expenses numeric(12, 2) NOT NULL,
	CONSTRAINT contract_pkey PRIMARY KEY (contract_id),
	CONSTRAINT contract_debtor_fk FOREIGN KEY (debtor_id) REFERENCES public.debtor(debtor_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT contract_employee_fk FOREIGN KEY (employee_id) REFERENCES public.employee(employee_id) ON DELETE CASCADE ON UPDATE CASCADE,
	CONSTRAINT contract_executor_company_id_fkey FOREIGN KEY (employee_id) REFERENCES public.company(company_id)
);

CREATE TABLE public.debtor (
	debtor_id serial4 NOT NULL,
	person_id int4 NOT NULL,
	status_id int4 NOT NULL,
	main_category_id int4 NOT NULL,
	filter_category_id int4 NOT NULL,
	created_date date DEFAULT CURRENT_DATE NOT NULL,
	CONSTRAINT debtor_pkey PRIMARY KEY (debtor_id),
	CONSTRAINT debtor_filter_category_id_fkey FOREIGN KEY (filter_category_id) REFERENCES public.filter_category(filter_category_id),
	CONSTRAINT debtor_main_category_id_fkey FOREIGN KEY (main_category_id) REFERENCES public.main_category(main_category_id),
	CONSTRAINT debtor_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE,
	CONSTRAINT debtor_status_id_fkey FOREIGN KEY (status_id) REFERENCES public.status(status_id)
);

CREATE TABLE public.employee (
	employee_id serial4 NOT NULL,
	"position" varchar(100) NOT NULL,
	login varchar(100) NOT NULL,
	password_hash text NOT NULL,
	created_date date NULL,
	is_active bool DEFAULT true NOT NULL,
	basis varchar NULL,
	person_id int4 NULL,
	CONSTRAINT employee_login_key UNIQUE (login),
	CONSTRAINT employee_pkey PRIMARY KEY (employee_id),
	CONSTRAINT employee_person_fk FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE ON UPDATE CASCADE
);

CREATE TABLE public.filter_category (
	filter_category_id serial4 NOT NULL,
	main_category_id int4 NOT NULL,
	"name" varchar(100) NOT NULL,
	CONSTRAINT filter_category_name_key UNIQUE (name),
	CONSTRAINT filter_category_pkey PRIMARY KEY (filter_category_id),
	CONSTRAINT filter_category_main_category_id_fkey FOREIGN KEY (main_category_id) REFERENCES public.main_category(main_category_id)
);

CREATE TABLE public.main_category (
	main_category_id serial4 NOT NULL,
	"name" varchar(100) NOT NULL,
	CONSTRAINT main_category_name_key UNIQUE (name),
	CONSTRAINT main_category_pkey PRIMARY KEY (main_category_id)
);

CREATE TABLE public.passport (
	passport_id serial4 NOT NULL,
	person_id int4 NOT NULL,
	series varchar(10) NOT NULL,
	"number" varchar(20) NOT NULL,
	issued_by text NOT NULL,
	division_code varchar(20) NULL,
	issue_date date NOT NULL,
	CONSTRAINT passport_pkey PRIMARY KEY (passport_id),
	CONSTRAINT passport_person_id_fkey FOREIGN KEY (person_id) REFERENCES public.person(person_id) ON DELETE CASCADE
);

CREATE TABLE public.payment_schedule (
	schedule_id serial4 NOT NULL,
	contract_id int4 NOT NULL,
	stage int4 NOT NULL,
	description text NOT NULL,
	amount numeric(12, 2) NOT NULL,
	amount_words text NULL,
	due_date date NULL,
	CONSTRAINT payment_schedule_pkey PRIMARY KEY (schedule_id),
	CONSTRAINT payment_schedule_contract_id_fkey FOREIGN KEY (contract_id) REFERENCES public.contract(contract_id) ON DELETE CASCADE
);

CREATE TABLE public.person (
	person_id serial4 NOT NULL,
	last_name varchar(100) NOT NULL,
	first_name varchar(100) NOT NULL,
	middle_name varchar(100) NULL,
	phone varchar(20) NULL,
	email varchar(100) NULL,
	CONSTRAINT person_pkey PRIMARY KEY (person_id)
);

CREATE TABLE public.status (
	status_id serial4 NOT NULL,
	"name" varchar(100) NOT NULL,
	CONSTRAINT status_name_key UNIQUE (name),
	CONSTRAINT status_pkey PRIMARY KEY (status_id)
);