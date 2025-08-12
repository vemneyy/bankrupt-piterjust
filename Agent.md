# Agent: Bankrupt Piterjust

## Overview
Bankrupt Piterjust is a desktop application built with .NET 9 WPF for managing clients in a bankruptcy process. It authenticates employees, stores debtor information in a local SQLite database, and generates legal service contracts from Word templates.

## Capabilities
- Authenticate employees and maintain a session for the active user.
- Store and query debtor, passport, address, contract, stage, and payment schedule data.
- Create Word contracts by replacing tags in template documents and inserting payment schedules.
- Display a graphical interface using WPF, including converters, commands, and views.
- Format numbers and amounts in words for Russian-language documents.

## Tools
- **Microsoft.Data.Sqlite** for database connectivity and queries.
- **Dapper** for lightweight object mapping against SQLite tables.
- **BCrypt.Net-Next** for password hashing and verification.
- **DocumentFormat.OpenXml** for editing and generating `.docx` files.

## Usage Patterns
1. Install the .NET 9 SDK on a Windows machine.
2. Build the solution with `dotnet build bankrupt-piterjust.sln`.
3. Run the application with `dotnet run` to open the WPF interface.
4. Log in with valid employee credentials; a successful login opens the main window.
5. Use the interface to manage debtor records and generate contracts saved under `Documents/ПитерЮст/Созданные договора` in the user's profile.

## Metadata
- **Primary language:** C#
- **Framework:** .NET 9 (WPF)
- **Entry point:** `App` class in `App.xaml.cs`
- **Database:** SQLite file initialized at runtime
- **Maintainer:** Сапрыкин Семён Максимович
- **Repository:** github.com/vemneyy/bankrupt-piterjust
