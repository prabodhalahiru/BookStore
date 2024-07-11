namespace BookStoreMainSup.Resources
{
    public static class ErrorMessages
    {
        public const string RequiredFields = "All fields are required.";
        public const string InvalidEmailFormat = "Invalid email format.";
        public const string InvalidUsernameFormat = "Username must be between 3 and 20 characters and contain only letters and numbers.";
        public const string InvalidPasswordFormat = "Password must be at least 5 characters and contain at least one uppercase, lowercase letter, one digit, and one special character.";
        public const string EmailExists = "Email already exists. Existing User? Try signing in.";
        public const string UsernameExists = "Username already exists. Existing User? Try signing in.";
        public const string EmailAndUsernameExists = "Email and Username both exist. Try signing in.";
        public const string InvalidCredentials = "Invalid credentials.";
        public const string UserNotLoggedIn = "User not logged in.";
    }
}
