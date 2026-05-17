using ASB.Entitlements.Domain.Entities;
using ASB.Entitlements.Domain.Repositories;
using ASB.Entitlements.Domain.Services;
using ASB.Entitlements.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ASB.Entitlements.Infrastructure.Seeding;

public sealed class DemoDataSeeder : IDataSeeder
{
    private readonly INeo4jContext _context;
    private readonly IIdentityRepository _identityRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IEntitlementDomainService _domainService;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        INeo4jContext context,
        IIdentityRepository identityRepository,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IResourceRepository resourceRepository,
        IEntitlementDomainService domainService,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _identityRepository = identityRepository ?? throw new ArgumentNullException(nameof(identityRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _domainService = domainService ?? throw new ArgumentNullException(nameof(domainService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting demo data seeding using domain entities...");

        try
        {
            // Clear existing demo data
            await ClearAllDataAsync();

            // Create domain entities using proper repositories
            await CreateIdentitiesAsync(cancellationToken);
            await CreateRolesAsync(cancellationToken);
            await CreatePermissionsAsync(cancellationToken);
            await CreateResourcesAsync(cancellationToken);

            // Create relationships using domain service
            await CreateRelationshipsAsync(cancellationToken);

            _logger.LogInformation("Demo data seeding completed successfully using domain entities");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo data seeding");
            throw;
        }
    }

    private async Task ClearAllDataAsync()
    {
        _logger.LogInformation("Clearing existing demo data...");

        const string clearQuery = @"
            MATCH (n)
            WHERE n:Identity OR n:Role OR n:Permission OR n:Resource
            DETACH DELETE n";

        var session = _context.GetSession();
        try
        {
            await session.RunAsync(clearQuery);
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private async Task CreateIdentitiesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating identities using domain entities...");

        // Create domain entities - business logic is in the entity constructors
        var alice = new Identity("user1", "Alice Johnson", IdentityType.Customer);
        var bob = new Identity("user2", "Bob Smith", IdentityType.Customer);
        var admin = new Identity("admin1", "Admin User", IdentityType.Employee);

        // Persist using repository
        await _identityRepository.CreateAsync(alice, cancellationToken);
        await _identityRepository.CreateAsync(bob, cancellationToken);
        await _identityRepository.CreateAsync(admin, cancellationToken);

        _logger.LogInformation("Identities created: Alice, Bob, Admin");
    }

    private async Task CreateRolesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating roles using domain entities...");

        var customerRole = new Role(
            "role_customer",
            "Customer",
            "Standard customer role with read access",
            RoleType.SystemDefined);

        var adminRole = new Role(
            "role_admin",
            "Admin",
            "Administrator role with full access",
            RoleType.SystemDefined);

        var managerRole = new Role(
            "role_account_manager",
            "AccountManager",
            "Account manager role with edit access",
            RoleType.SystemDefined);

        await _roleRepository.CreateAsync(customerRole, cancellationToken);
        await _roleRepository.CreateAsync(adminRole, cancellationToken);
        await _roleRepository.CreateAsync(managerRole, cancellationToken);

        _logger.LogInformation("Roles created: Customer, Admin, AccountManager");
    }

    private async Task CreatePermissionsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating permissions using domain entities...");

        var viewAccount = new Permission(
            "perm_view_account",
            "ViewAccount",
            "ViewAccount",
            "Permission to view account details",
            PermissionScope.Read);

        var editAccount = new Permission(
            "perm_edit_account",
            "EditAccount",
            "EditAccount",
            "Permission to edit account details",
            PermissionScope.Write);

        var deleteAccount = new Permission(
            "perm_delete_account",
            "DeleteAccount",
            "DeleteAccount",
            "Permission to delete accounts",
            PermissionScope.Delete);

        var viewTransaction = new Permission(
            "perm_view_transaction",
            "ViewTransaction",
            "ViewTransaction",
            "Permission to view transactions",
            PermissionScope.Read);

        await _permissionRepository.CreateAsync(viewAccount, cancellationToken);
        await _permissionRepository.CreateAsync(editAccount, cancellationToken);
        await _permissionRepository.CreateAsync(deleteAccount, cancellationToken);
        await _permissionRepository.CreateAsync(viewTransaction, cancellationToken);

        _logger.LogInformation("Permissions created: View, Edit, Delete, ViewTransaction");
    }

    private async Task CreateResourcesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating resources using domain entities...");

        var savingsAccount = new Resource(
            "acc1",
            "Savings Account 001",
            "SavingsAccount",
            "Primary savings account",
            ResourceClassification.Internal);

        var checkingAccount = new Resource(
            "acc2",
            "Checking Account 002",
            "CheckingAccount",
            "Primary checking account",
            ResourceClassification.Internal);

        var investmentAccount = new Resource(
            "acc3",
            "Investment Account 003",
            "InvestmentAccount",
            "Investment portfolio account",
            ResourceClassification.Confidential);

        await _resourceRepository.CreateAsync(savingsAccount, cancellationToken);
        await _resourceRepository.CreateAsync(checkingAccount, cancellationToken);
        await _resourceRepository.CreateAsync(investmentAccount, cancellationToken);

        _logger.LogInformation("Resources created: Savings, Checking, Investment accounts");
    }

    private async Task CreateRelationshipsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating relationships using domain service...");

        // Alice (Customer) -> Customer Role -> ViewAccount Permission -> acc1, acc2
        await _domainService.AssignRoleToIdentityAsync("user1", "role_customer", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_customer", "perm_view_account", "acc1", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_customer", "perm_view_account", "acc2", cancellationToken);

        // Bob (AccountManager) -> AccountManager Role -> Edit & View -> acc2
        await _domainService.AssignRoleToIdentityAsync("user2", "role_account_manager", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_account_manager", "perm_edit_account", "acc2", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_account_manager", "perm_view_account", "acc2", cancellationToken);

        // Admin -> Admin Role -> All Permissions -> All Resources
        await _domainService.AssignRoleToIdentityAsync("admin1", "role_admin", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_view_account", "acc1", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_view_account", "acc2", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_view_account", "acc3", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_edit_account", "acc1", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_edit_account", "acc2", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_edit_account", "acc3", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_delete_account", "acc1", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_delete_account", "acc2", cancellationToken);
        await _domainService.GrantPermissionToRoleAsync("role_admin", "perm_delete_account", "acc3", cancellationToken);

        _logger.LogInformation("Relationships created successfully using domain service");
    }
}
