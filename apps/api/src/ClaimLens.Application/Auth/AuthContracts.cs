namespace ClaimLens.Application.Auth;

public sealed record RegisterRequest(string Email, string Password, string FirstName, string LastName);

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role);

public sealed record AuthResponse(string AccessToken, string RefreshToken, UserResponse User);
