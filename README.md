# Book Store Application 🔖

## Overview ⭕

<p style="font-family: 'Courier New', Courier, monospace;">
This bookstore application is built using ASP.NET Core 6 and Entity Framework Core. It follows the Model-View-Controller (MVC) architecture to provide a structured and maintainable codebase. The application includes user authentication and authorization using JWT (JSON Web Tokens) and allows users to perform CRUD (Create, Read, Update, Delete) operations on books.
</p>

## Features ⭕

<p style="font-family: 'Courier New', Courier, monospace;">
<strong>User Registration and Login:</strong> Users can register and log in to the application. Passwords are securely hashed using BCrypt. JWT Authentication: Secure authentication using JSON Web Tokens (JWT).
</p>

<p style="font-family: 'Courier New', Courier, monospace;">
<strong>CRUD Operations for Books:</strong>
<ul>
    <li>Create: Add new books to the store.</li>
    <li>Read: View details of all books or a specific book.</li>
    <li>Update: Edit details of existing books.</li>
    <li>Delete: Remove books from the store.</li>
</ul>
</p>

<p style="font-family: 'Courier New', Courier, monospace;">
<strong>Entity Framework Core:</strong> Used for database operations and migrations.
</p>

## Technologies Used ⭕
<p style="font-family: 'Courier New', Courier, monospace;">
<ul>
    <li><strong>ASP.NET Core 6:</strong> For building the web application and API.</li>
    <li><strong>Entity Framework Core:</strong> For database access and ORM (Object-Relational Mapping).</li>
    <li><strong>SQL Server:</strong> As the database provider.</li>
    <li><strong>JWT (JSON Web Tokens):</strong> For secure authentication and authorization.</li>
    <li><strong>BCrypt:</strong> For password hashing.</li>
</ul>
</p>

## Setup Instructions ⭕

### Clone the Repository:
<p style="font-family: 'Courier New', Courier, monospace;">
<pre>
<code>
git clone https://github.com/your-username/book-store-app.git
cd book-store-app
</code>
</pre>
</p>

### Update Connection String:
<p style="font-family: 'Courier New', Courier, monospace;">
Open <code>appsettings.json</code> and update the DefaultConnection string with your SQL Server connection details.
</p>

### Run Migrations:
<p style="font-family: 'Courier New', Courier, monospace;">
<pre>
<code>
dotnet ef database update
</code>
</pre>
</p>

### Run the Application:
<p style="font-family: 'Courier New', Courier, monospace;">
<pre>
<code>
dotnet run
</code>
</pre>
</p>

### Test Endpoints:
<p style="font-family: 'Courier New', Courier, monospace;">
Use Postman or any other API testing tool to test the endpoints.
</p>

### API Documentation:
<p style="font-family: 'Courier New', Courier, monospace;">
<pre>
<span>Click <a href="https://docs.google.com/document/d/1COy6nJGWXbEoQVnid87DrPrlZGIxkg9mLjYzTP0-Or4/edit?usp=sharing">here</a> for documentation.</span>
</pre>
</p>
