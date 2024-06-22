Book Store Application 🔖

Overview ⭕

This bookstore application is built using ASP.NET Core 6 and Entity Framework Core. It follows the Model-View-Controller (MVC) architecture to provide a structured and maintainable codebase. The application includes user authentication and authorization using JWT (JSON Web Tokens) and allows users to perform CRUD (Create, Read, Update, Delete) operations on books.

Features ⭕

* User Registration and Login: Users can register and log in to the application. Passwords are securely hashed using BCrypt.
JWT Authentication: Secure authentication using JSON Web Tokens (JWT).

* CRUD Operations for Books:
  
   Create: Add new books to the store.
  
  Read: View details of all books or a specific book.
  
  Update: Edit details of existing books.
  
  Delete: Remove books from the store.
  
* Entity Framework Core: Used for database operations and migrations.
  
Technologies Used ⭕

ASP.NET Core 6: For building the web application and API.

Entity Framework Core: For database access and ORM (Object-Relational Mapping).

SQL Server: As the database provider.

JWT (JSON Web Tokens): For secure authentication and authorization.

BCrypt: For password hashing.

Setup Instructions ⭕

Clone the Repository:

bash

Copy code

git clone https://github.com/your-username/book-store-app.git

cd book-store-app

Update Connection String:


Open appsettings.json and update the DefaultConnection string with your SQL Server connection details.
Run Migrations:

bash

Copy code

dotnet ef migrations add InitialCreate

dotnet ef database update

Run the Application:


bash

Copy code

dotnet run

Test Endpoints:


Use Postman or any other API testing tool to test the endpoints.

Endpoints  ⭕

User Authentication:


POST /api/Auth/register: Register a new user.

POST /api/Auth/login: Log in and get a JWT token.

Books Management:

GET /api/Books: Get all books.

GET /api/Books/{id}: Get a specific book by ID.

POST /api/Books: Add a new book (requires authentication).

PUT /api/Books/{id}: Update an existing book (requires authentication).

DELETE /api/Books/{id}: Delete a book (requires authentication).
