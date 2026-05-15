using ASB.Entitlements.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace ASB.Entitlements.Infrastructure.Seeding;

public sealed class DemoDataSeeder : IDataSeeder
{
    private readonly INeo4jContext _context;
    private readonly ILogger<DemoDataSeeder> _logger;

    public DemoDataSeeder(
        INeo4jContext context,
        ILogger<DemoDataSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting demo data seeding...");

        var session = _context.GetSession();
        try
        {
            // Clear existing demo data
            await ClearDemoDataAsync(session);

            // Create Identities (BIAN: Party)
            await CreateIdentitiesAsync(session);

            // Create Roles (BIAN: Party Role)
            await CreateRolesAsync(session);

            // Create Permissions (BIAN: Authorization)
            await CreatePermissionsAsync(session);

            // Create Resources (BIAN: Product/Service)
            await CreateResourcesAsync(session);

            // Create Relationships
            await CreateRelationshipsAsync(session);

            _logger.LogInformation("Demo data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during demo data seeding");
            throw;
        }
        finally
        {
            await session.CloseAsync();
        }
    }

    private async Task ClearDemoDataAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Clearing existing demo data...");

        const string clearQuery = @"
            MATCH (n)
            WHERE n:Identity OR n:Role OR n:Permission OR n:Resource
            DETACH DELETE n";

        await session.RunAsync(clearQuery);
    }

    private async Task CreateIdentitiesAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Creating identities...");

        const string query = @"
            CREATE (alice:Identity {
                id: 'user1',
                name: 'Alice Johnson',
                type: 'Customer',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (bob:Identity {
                id: 'user2',
                name: 'Bob Smith',
                type: 'Customer',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (admin:Identity {
                id: 'admin1',
                name: 'Admin User',
                type: 'Employee',
                isActive: true,
                createdAt: datetime()
            })";

        await session.RunAsync(query);
    }

    private async Task CreateRolesAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Creating roles...");

        const string query = @"
            CREATE (customer:Role {
                id: 'role_customer',
                name: 'Customer',
                description: 'Standard customer role with read access',
                type: 'SystemDefined',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (admin:Role {
                id: 'role_admin',
                name: 'Admin',
                description: 'Administrator role with full access',
                type: 'SystemDefined',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (accountManager:Role {
                id: 'role_account_manager',
                name: 'AccountManager',
                description: 'Account manager role with edit access',
                type: 'SystemDefined',
                isActive: true,
                createdAt: datetime()
            })";

        await session.RunAsync(query);
    }

    private async Task CreatePermissionsAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Creating permissions...");

        const string query = @"
            CREATE (viewAccount:Permission {
                id: 'perm_view_account',
                name: 'ViewAccount',
                action: 'ViewAccount',
                description: 'Permission to view account details',
                scope: 'Read',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (editAccount:Permission {
                id: 'perm_edit_account',
                name: 'EditAccount',
                action: 'EditAccount',
                description: 'Permission to edit account details',
                scope: 'Write',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (deleteAccount:Permission {
                id: 'perm_delete_account',
                name: 'DeleteAccount',
                action: 'DeleteAccount',
                description: 'Permission to delete accounts',
                scope: 'Delete',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (viewTransaction:Permission {
                id: 'perm_view_transaction',
                name: 'ViewTransaction',
                action: 'ViewTransaction',
                description: 'Permission to view transactions',
                scope: 'Read',
                isActive: true,
                createdAt: datetime()
            })";

        await session.RunAsync(query);
    }

    private async Task CreateResourcesAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Creating resources...");

        const string query = @"
            CREATE (acc1:Resource {
                id: 'acc1',
                name: 'Savings Account 001',
                type: 'SavingsAccount',
                description: 'Primary savings account',
                classification: 'Internal',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (acc2:Resource {
                id: 'acc2',
                name: 'Checking Account 002',
                type: 'CheckingAccount',
                description: 'Primary checking account',
                classification: 'Internal',
                isActive: true,
                createdAt: datetime()
            })
            CREATE (acc3:Resource {
                id: 'acc3',
                name: 'Investment Account 003',
                type: 'InvestmentAccount',
                description: 'Investment portfolio account',
                classification: 'Confidential',
                isActive: true,
                createdAt: datetime()
            })";

        await session.RunAsync(query);
    }

    private async Task CreateRelationshipsAsync(Neo4j.Driver.IAsyncSession session)
    {
        _logger.LogInformation("Creating relationships...");

        const string query = @"
            // Alice (Customer) -> Customer Role -> ViewAccount Permission -> acc1
            MATCH (alice:Identity {id: 'user1'})
            MATCH (customerRole:Role {id: 'role_customer'})
            MATCH (viewPerm:Permission {id: 'perm_view_account'})
            MATCH (acc1:Resource {id: 'acc1'})
            MATCH (acc2:Resource {id: 'acc2'})
            CREATE (alice)-[:HAS_ROLE]->(customerRole)
            CREATE (customerRole)-[:GRANTS]->(viewPerm)
            CREATE (viewPerm)-[:ON]->(acc1)
            CREATE (viewPerm)-[:ON]->(acc2)

            // Bob (Customer) -> AccountManager Role -> Edit & View -> acc2
            WITH *
            MATCH (bob:Identity {id: 'user2'})
            MATCH (managerRole:Role {id: 'role_account_manager'})
            MATCH (editPerm:Permission {id: 'perm_edit_account'})
            MATCH (viewPerm:Permission {id: 'perm_view_account'})
            MATCH (acc2:Resource {id: 'acc2'})
            CREATE (bob)-[:HAS_ROLE]->(managerRole)
            CREATE (managerRole)-[:GRANTS]->(editPerm)
            CREATE (managerRole)-[:GRANTS]->(viewPerm)
            CREATE (editPerm)-[:ON]->(acc2)

            // Admin -> Admin Role -> All Permissions -> All Resources
            WITH *
            MATCH (admin:Identity {id: 'admin1'})
            MATCH (adminRole:Role {id: 'role_admin'})
            MATCH (viewPerm:Permission {id: 'perm_view_account'})
            MATCH (editPerm:Permission {id: 'perm_edit_account'})
            MATCH (deletePerm:Permission {id: 'perm_delete_account'})
            MATCH (acc1:Resource {id: 'acc1'})
            MATCH (acc2:Resource {id: 'acc2'})
            MATCH (acc3:Resource {id: 'acc3'})
            CREATE (admin)-[:HAS_ROLE]->(adminRole)
            CREATE (adminRole)-[:GRANTS]->(viewPerm)
            CREATE (adminRole)-[:GRANTS]->(editPerm)
            CREATE (adminRole)-[:GRANTS]->(deletePerm)
            CREATE (deletePerm)-[:ON]->(acc1)
            CREATE (deletePerm)-[:ON]->(acc2)
            CREATE (deletePerm)-[:ON]->(acc3)";

        await session.RunAsync(query);

        _logger.LogInformation("Demo data relationships created successfully");
    }
}
