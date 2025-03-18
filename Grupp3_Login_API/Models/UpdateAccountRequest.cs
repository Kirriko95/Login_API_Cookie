public class UpdateAccountRequest
{
    public string NewUserName { get; set; }  // Nytt användarnamn (frivilligt)
    public string CurrentPassword { get; set; }  // Nuvarande lösenord (KRÄVS om lösenordet ska ändras)
    public string NewPassword { get; set; }  // Nytt lösenord (frivilligt)
}
