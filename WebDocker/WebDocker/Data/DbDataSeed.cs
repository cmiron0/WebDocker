using Microsoft.AspNetCore.Identity;

namespace WebDocker.Data
{
       
    public class DbDataSeed
    {

        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            // Obtener los servicios necesarios
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<DbDataSeed>>();

            string[] roleNames = { "Administrador", "Usuario" };

            foreach (var roleName in roleNames)
            {
                // Verificar si el rol existe
                var roleExist = await roleManager.RoleExistsAsync(roleName);

                if (!roleExist)
                {
                    // Si no existe, crearlo
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));

                    if (result.Succeeded)
                    {
                        logger.LogInformation($"Role '{roleName}' creado exitosamente.");
                    }
                    else
                    {
                        logger.LogError($"Error al crear el Role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
        //public static async Task SeedPermissionsAsync(IServiceProvider serviceProvider)
        //{
        //    var db = serviceProvider.GetRequiredService<AppDbContext>();
        //    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        //    var logger = serviceProvider.GetRequiredService<ILogger<IdentitySeed>>();

        //    var permissions = new (string Code, string Description)[]
        //    {
        //        ("servers.read",    "Ver servidores Docker"),
        //        ("servers.write",   "Crear, modificar y borrar servidores Docker"),
        //        ("containers.read", "Ver contenedores"),
        //        ("containers.write","Crear, iniciar, detener y eliminar contenedores"),
        //        ("images.read",     "Ver imágenes"),
        //        ("images.write",    "Descargar y eliminar imágenes"),
        //        ("volumes.read",    "Ver volúmenes"),
        //        ("volumes.write",   "Crear y eliminar volúmenes"),
        //        ("networks.read",   "Ver redes"),
        //        ("networks.write",  "Crear y eliminar redes"),
        //        ("users.admin",     "Administrar usuarios y roles del sistema"),
        //    };

        //    foreach (var (code, description) in permissions)
        //    {
        //        if (!await db.Permissions.AnyAsync(p => p.Code == code))
        //        {
        //            db.Permissions.Add(new Permission { Code = code, Description = description });
        //        }
        //    }
        //    await db.SaveChangesAsync();

        //    // Asignar permisos a roles
        //    var adminRole = await roleManager.FindByNameAsync("Administrador");
        //    var userRole = await roleManager.FindByNameAsync("Usuario");

        //    if (adminRole != null)
        //    {
        //        await AssignAllPermissions(db, adminRole.Id);
        //    }

        //    if (userRole != null)
        //    {
        //        var userPermissionCodes = new[]
        //        {
        //            "servers.read", "servers.write",
        //            "containers.read", "containers.write",
        //            "images.read",
        //            "volumes.read",
        //            "networks.read"
        //        };
        //        await AssignPermissions(db, userRole.Id, userPermissionCodes);
        //    }

        //    logger.LogInformation("Permisos sembrados y asignados a roles.");
        //}

        //private static async Task AssignAllPermissions(AppDbContext db, string roleId)
        //{
        //    var allPermissions = await db.Permissions.ToListAsync();
        //    foreach (var permission in allPermissions)
        //    {
        //        var exists = await db.RolePermissions
        //            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
        //        if (!exists)
        //        {
        //            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
        //        }
        //    }
        //    await db.SaveChangesAsync();
        //}

        //private static async Task AssignPermissions(AppDbContext db, string roleId, string[] permissionCodes)
        //{
        //    foreach (var code in permissionCodes)
        //    {
        //        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Code == code);
        //        if (permission == null) continue;

        //        var exists = await db.RolePermissions
        //            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
        //        if (!exists)
        //        {
        //            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
        //        }
        //    }
        //    await db.SaveChangesAsync();
        //}

        //public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        //{
        //    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        //    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        //    var logger = serviceProvider.GetRequiredService<ILogger<IdentitySeed>>();

        //    // 1. Usuario Administrador
        //    const string adminUserName = "Admin";
        //    const string adminEmail = "cmiron@outlook.es";
        //    const string adminPassword = "Admin";
        //    const string adminRole = "Administrador";

        //    await CreateUserWithRole(userManager, roleManager, logger, adminUserName, adminEmail, adminPassword, adminRole);


        //    // 2. Usuario de pruebas
        //    const string userUserName = "User";
        //    const string userEmail = "cmiron@outlook.es";
        //    const string userPassword = "User";
        //    const string userRole = "Usuario";

        //    await CreateUserWithRole(userManager, roleManager, logger, userUserName, userEmail, userPassword, userRole);
        //}

        //private static async Task CreateUserWithRole(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<IdentitySeed> logger,
        //    string userName, string email, string password, string roleName)
        //{
        //    // 1. Verificar si el usuario ya existe por UserName
        //    if (await userManager.FindByNameAsync(userName) == null)
        //    {
        //        var newUser = new AppUser
        //        {
        //            UserName = userName,
        //            Email = email,
        //            EmailConfirmed = true
        //        };

        //        // 2. Crear el usuario
        //        var result = await userManager.CreateAsync(newUser, password);

        //        if (result.Succeeded)
        //        {
        //            logger.LogInformation($"Usuario '{userName}' creado exitosamente.");

        //            // 3. Asignar el rol si el rol existe
        //            if (await roleManager.RoleExistsAsync(roleName))
        //            {
        //                await userManager.AddToRoleAsync(newUser, roleName);
        //                logger.LogInformation($"Rol '{roleName}' asignado a {userName}.");
        //            }
        //            else
        //            {
        //                logger.LogWarning($"Advertencia: El rol '{roleName}' no existe para el usuario {userName}.");
        //            }
        //        }
        //        else
        //        {
        //            logger.LogError($"Error al crear el Usuario '{userName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
        //        }
        //    }
        //    else
        //    {
        //        logger.LogInformation($"El usuario '{userName}' ya existe. Saltando la creación.");
        //    }
        //}

        //public static async Task SeedDockerServersAsync(IServiceProvider serviceProvider)
        //{
        //    var db = serviceProvider.GetRequiredService<AppDbContext>();
        //    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        //    var logger = serviceProvider.GetRequiredService<ILogger<IdentitySeed>>();

        //    // Solo sembrar si la tabla está vacía, para no pisar los datos del usuario.
        //    if (await db.DockerServers.AnyAsync())
        //    {
        //        return;
        //    }

        //    var admin = await userManager.FindByNameAsync("Admin");
        //    var user = await userManager.FindByNameAsync("User");

        //    if (admin == null || user == null)
        //    {
        //        logger.LogWarning("No se siembran servidores Docker: faltan los usuarios Admin/User.");
        //        return;
        //    }

        //    // Los certificados TLS NO van en el código: se leen de DataBase/seed-certs si existen.
        //    var certsFolder = Path.Combine("DataBase", "seed-certs");
        //    var caPem = ReadPemIfExists(Path.Combine(certsFolder, "ca.pem"));
        //    var certPem = ReadPemIfExists(Path.Combine(certsFolder, "cert.pem"));
        //    var keyPem = ReadPemIfExists(Path.Combine(certsFolder, "key.pem"));

        //    db.DockerServers.Add(new DockerServer
        //    {
        //        Name = "LenovoHypervUbuntu",
        //        Host = "172.28.148.198",
        //        Port = 2375,
        //        OperatingSystem = "Linux",
        //        UseTls = false,
        //        UserId = admin.Id
        //    });

        //    db.DockerServers.Add(new DockerServer
        //    {
        //        Name = "LenovoHypervUbuntu2",
        //        Host = "172.24.186.22",
        //        Port = 2375,
        //        OperatingSystem = "Linux",
        //        UseTls = false,
        //        UserId = admin.Id
        //    });

        //    db.DockerServers.Add(new DockerServer
        //    {
        //        Name = "OCI Oracle",
        //        Host = "51.170.39.109",
        //        Port = 2376,
        //        OperatingSystem = "Linux",
        //        UseTls = true,
        //        CaCertPem = caPem,
        //        ClientCertPem = certPem,
        //        ClientKeyPem = keyPem,
        //        UserId = user.Id
        //    });

        //    await db.SaveChangesAsync();
        //    logger.LogInformation("Servidores Docker de desarrollo sembrados.");
        //}

        //private static string? ReadPemIfExists(string path)
        //{
        //    return File.Exists(path) ? File.ReadAllText(path) : null;
        //}
    }
}

