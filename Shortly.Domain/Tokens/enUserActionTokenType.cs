namespace Shortly.Domain.Tokens;

public enum enUserActionTokenType : byte
{
    EmailVerification = 1,
    PasswordReset = 2,
    EmailChange = 3
}