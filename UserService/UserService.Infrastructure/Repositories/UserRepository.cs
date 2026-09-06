using Microsoft.EntityFrameworkCore;
using UserService.Application.Repositories;
using UserService.Domain.Models;
using UserService.Infrastructure.DataAccess;

namespace UserService.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken)
    {
        return await _db.Users.FirstOrDefaultAsync(b => b.Login == login, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await _db.Users.AddAsync(user, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}