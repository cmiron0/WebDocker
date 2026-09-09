using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace WebDocker.Data
{
       
    public class AppDbDataSeed
    {

        // Seed Roles
        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            // Obtener los servicios necesarios
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<AppDbDataSeed>>();

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
                        logger.LogInformation($"DataSeed: Role '{roleName}' creado exitosamente.");
                    }
                    else
                    {
                        logger.LogError($"DataSeed: Error al crear el Role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        // Seed USers
        public static async Task SeedUsers(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<AppDbDataSeed>>();

            // Administrador (Admin)
            const string adminUserName = "Admin";
            const string adminEmail = "cmiron@outlook.es";
            const string adminPassword = "Admin";
            const string adminRole = "Administrador";

            await CreateUserWithRole(userManager, roleManager, logger, adminUserName, adminEmail, adminPassword, adminRole);


            // Usuario de pruebas (User)
            const string userUserName = "User";
            const string userEmail = "cmiron@outlook.es";
            const string userPassword = "User";
            const string userRole = "Usuario";

            await CreateUserWithRole(userManager, roleManager, logger, userUserName, userEmail, userPassword, userRole);
        }
        private static async Task CreateUserWithRole(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<AppDbDataSeed> logger,
            string userName, string email, string password, string roleName)
        {
            // Verificar si el usuario ya existe
            if (await userManager.FindByNameAsync(userName) == null)
            {
                var newUser = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    EmailConfirmed = true
                };

                // Crear el usuario
                var result = await userManager.CreateAsync(newUser, password);

                if (result.Succeeded)
                {
                    logger.LogInformation($"DataSeed: Usuario '{userName}' creado con exito.");

                    // Asignar roles
                    if (await roleManager.RoleExistsAsync(roleName))
                    {
                        await userManager.AddToRoleAsync(newUser, roleName);
                        logger.LogInformation($"DataSeed: Rol '{roleName}' asignado a {userName}.");
                    }
                    else
                    {
                        logger.LogWarning($"DataSeed: (Advertencia) El rol '{roleName}' no existe para el usuario {userName}.");
                    }
                }
                else
                {
                    logger.LogError($"DataSeed: Error al crear el Usuario '{userName}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                logger.LogInformation($"DataSeed: El usuario '{userName}' ya existe.");
            }
        }

        // Seed Permissions
        public static async Task SeedPermissions(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<AppDbContext>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<AppDbDataSeed>>();


            // Alta de permisos
            var permissions = new (string Code, string Description)[]
            {
                ("servers.read",    "Ver servidores Docker"),
                ("servers.write",   "Crear, modificar y borrar servidores Docker"),
                ("containers.read", "Ver contenedores"),
                ("containers.write","Crear, iniciar, detener y eliminar contenedores"),
                ("images.read",     "Ver imágenes"),
                ("images.write",    "Descargar y eliminar imágenes"),
                ("volumes.read",    "Ver volúmenes"),
                ("volumes.write",   "Crear y eliminar volúmenes"),
                ("networks.read",   "Ver redes"),
                ("networks.write",  "Crear y eliminar redes"),
                ("users.admin",     "Administrar usuarios y roles del sistema"),
            };

            foreach (var (code, description) in permissions)
            {
                if (!await db.Permissions.AnyAsync(p => p.Code == code))
                {
                    db.Permissions.Add(new Permission { Code = code, Description = description });
                }
            }
            await db.SaveChangesAsync();


            // Asignar permisos a rol Administrador
            var adminRole = await roleManager.FindByNameAsync("Administrador");
            if (adminRole != null)
            {
                await AssignAllPermissions(db, adminRole.Id);
            }

            // Asignar permisos a rol Usuario
            var userRole = await roleManager.FindByNameAsync("Usuario");
            if (userRole != null)
            {
                var userPermission = new[]
                {
                    "servers.read", 
                    "servers.write",
                    "containers.read",
                    "containers.write",
                    "images.read",
                    "volumes.read",
                    "networks.read"
                };
                await AssignPermissions(db, userRole.Id, userPermission);
            }

            logger.LogInformation("DataSeed: Permisos sembrados y asignados a roles.");
        }

        // Asignar Todos los permisos a un rol
        private static async Task AssignAllPermissions(AppDbContext db, string roleId)
        {
            var allPermissions = await db.Permissions.ToListAsync();
            foreach (var permission in allPermissions)
            {
                var exists = await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
                if (!exists)
                {
                    db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
                }
            }
            await db.SaveChangesAsync();
        }

        // Asignar permisos a un rol
        private static async Task AssignPermissions(AppDbContext db, string roleId, string[] permissions)
        {
            foreach (var code in permissions)
            {
                var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Code == code);
                if (permission != null)
                {
                    var exists = await db.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id);
                    if (!exists)
                    {
                        db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = permission.Id });
                    }
                }
            }
            await db.SaveChangesAsync();
        }


        // Seed Servidores Docker
        public static async Task SeedDockerServers(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<AppDbDataSeed>>();

            // Solo sembrar si la tabla está vacía, para no pisar los datos.
            if (await db.DockerServers.AnyAsync())
            {
                return;
            }

            var admin = await userManager.FindByNameAsync("Admin");
            var user = await userManager.FindByNameAsync("User");

            if (admin == null || user == null)
            {
                logger.LogWarning("DataSeed: No se siembran servidores Docker, faltan los usuarios Admin/User.");
                return;
            }

            // Los certificados TLS se leen de DataBase/seed-certs si existen.
            var certsFolder = Path.Combine("DataBase", "seed-certs");
            var caPem = ReadPemIfExists(Path.Combine(certsFolder, "ca.pem"));
            var certPem = ReadPemIfExists(Path.Combine(certsFolder, "cert.pem"));
            var keyPem = ReadPemIfExists(Path.Combine(certsFolder, "key.pem"));


            // Asignación de servidores
            db.DockerServers.Add(new DockerServer
            {
                Name = "LenovoHypervUbuntu",
                Host = "172.28.148.198",
                Port = 2375,
                OperatingSystem = "Linux",
                UseTls = false,
                UserId = admin.Id
            });

            db.DockerServers.Add(new DockerServer
            {
                Name = "LenovoHypervUbuntu2",
                Host = "172.24.186.22",
                Port = 2375,
                OperatingSystem = "Linux",
                UseTls = false,
                UserId = admin.Id
            });

            db.DockerServers.Add(new DockerServer
            {
                Name = "OCI Oracle",
                Host = "51.170.39.109",
                Port = 2376,
                OperatingSystem = "Linux",
                UseTls = true,
                CaCertPem = caPem,
                ClientCertPem = certPem,
                ClientKeyPem = keyPem,
                UserId = admin.Id
            });

            db.DockerServers.Add(new DockerServer
            {
                Name = "OCI Oracle",
                Host = "51.170.39.109",
                Port = 2376,
                OperatingSystem = "Linux",
                UseTls = true,
                CaCertPem = caPem,
                ClientCertPem = certPem,
                ClientKeyPem = keyPem,
                UserId = user.Id
            });

            await db.SaveChangesAsync();
            logger.LogInformation("DataSeed: Servidores Docker de desarrollo sembrados.");
        }

        private static string? ReadPemIfExists(string path)
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
    }
}

