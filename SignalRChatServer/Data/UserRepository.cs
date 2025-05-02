using Microsoft.EntityFrameworkCore;
using SignalRChatServer.Data;
using SignalRChatServer.Models;
using SignalRChatServer.Services;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(
        AppDbContext context,
        IAuthService authService,
        ILogger<UserRepository> logger)
    {
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    public async Task<UserModel> GetByIdAsync(string id)
    {
        try
        {
            return await _context.Users.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID: {Id}", id);
            throw;
        }
    }

    public async Task<UserModel> GetByUsernameAsync(string username)
    {
        try
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username: {Username}", username);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string username)
    {
        try
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user exists: {Username}", username);
            throw;
        }
    }

    public async Task AddAsync(UserModel user)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding user: {Username}", user.Username);
                throw;
            }
        });
    }

    public async Task UpdateAsync(UserModel user)
    {
        var executionStrategy = _context.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Users.Update(user);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating user: {Id}", user.Id);
                throw;
            }
        });
    }

    public async Task<UserModel> AuthenticateAsync(string username, string password)
    {
        try
        {
            var user = await GetByUsernameAsync(username);
            if (user == null) return null;

            if (!_authService.VerifyPassword(password, user.PasswordHash, user.Salt))
            {
                _logger.LogWarning("Invalid password for user: {Username}", username);
                return null;
            }

            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user: {Username}", username);
            throw;
        }
    }
}