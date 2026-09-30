using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.DTO.Requests;
using Domain.DTO.Result;
using Domain.Email;
using Domain.Identity;
using Domain.JWT;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Interface;

namespace Service.Implementation;

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IFacultyService _facultyService;
    private readonly IEmailSender _emailSender;
    private readonly FrontendOptions _frontendOptions;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<AppUser> userManager,
        IFacultyService facultyService,
        IEmailSender emailSender,
        IOptions<FrontendOptions> frontendOptions,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _facultyService = facultyService;
        _emailSender = emailSender;
        _frontendOptions = frontendOptions.Value;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<RegisterResult> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken ct = default)
    {
        var faculty = await _facultyService.GetByIdAsync(request.FacultyId);

        if (faculty == null)
            return Fail("Selected faculty does not exist.");

        var expectedDomain = $"@students.{faculty.EmailDomainSegment}";
        if (!request.Email.EndsWith(expectedDomain, StringComparison.OrdinalIgnoreCase))
            return Fail($"Email must end with {expectedDomain} for {faculty.Name}.");

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FacultyId = request.FacultyId,
            StudentIndexNumber = request.StudentIndexNumber
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return new RegisterResult { Errors = createResult.Errors.Select(e => e.Description).ToList() };

        var roleResult = await _userManager.AddToRoleAsync(user, "Student");
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return new RegisterResult { Errors = roleResult.Errors.Select(e => e.Description).ToList() };
        }
        
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = Uri.EscapeDataString(token);
        var confirmationLink = $"{_frontendOptions.BaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";

        await _emailSender.SendEmailAsync(
            user.Email!,
            "Confirm your email",
            $"<p>Welcome, {user.FirstName}!</p><p><a href=\"{confirmationLink}\">Click here to confirm your email</a></p>",
            ct);

        return new RegisterResult { IsSuccess = true };
    }

    public async Task<RegisterResult> RegisterAdminAsync(RegisterAdminRequest request, CancellationToken ct = default)
    {
        var user = new AppUser()
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            FacultyId = null,
            Faculty = null,
            StudentIndexNumber = null,
            EmailConfirmed = true
        };
        
        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return new RegisterResult { Errors = createResult.Errors.Select(e => e.Description).ToList() };

        return new RegisterResult { IsSuccess = true };
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string token, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }
    public async Task AddUserToAdminAsync(string email, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            throw new KeyNotFoundException("User not found.");
        
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
            throw new InvalidOperationException("User is already an admin.");
        
        var result = await _userManager.AddToRoleAsync(user, "Admin");
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return new LoginResult { Errors = new List<string> { "Invalid email or password." } };
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);

        if (!passwordValid)
        {
            return new LoginResult { Errors = new List<string> { "Invalid email or password." } };
        }

        if (!user.EmailConfirmed)
            return new LoginResult { Errors = new List<string> { "Please confirm your email before logging in." } };

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAt) = GenerateJwtToken(user, roles);

        return new LoginResult { IsSuccess = true, Token = token, ExpiresAt = expiresAt };
    }

    private static RegisterResult Fail(string message) =>
        new() { Errors = new List<string> { message } };

    private (string Token, DateTime ExpiresAt) GenerateJwtToken(AppUser user, IList<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new("firstName", user.FirstName),
            new("lastName", user.LastName)
        };
        
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);
        
        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}