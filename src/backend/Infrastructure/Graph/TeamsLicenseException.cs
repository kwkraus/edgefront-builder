namespace EdgeFront.Builder.Infrastructure.Graph;

public class TeamsLicenseException : Exception
{
    public TeamsLicenseException() : base("Teams webinar license required") { }
    public TeamsLicenseException(string message) : base(message) { }
}
