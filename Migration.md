# WebDocker DataBase Migrations

1.- add-migration {nombre} -Context {contexto}
2.- remove-migration
3.- update-database {nombre} -Context {contexto}
4.- drop-database -Context {contexto}

Vamos a explicar los comandos:

1.- add-migration: Con este comando, generaremos la migración que lanzaremos a la base de datos. Este comando tiene 2 parámetros:
    {nombre}: Con este parámetro indicaremos el nombre que queremos indicarle a la migración, ademas, es obligatorio.
    -Context {contexto}: En caso de tener más de un contexto de datos en nuestro programa, indicaremos cual de los contextos es mediante este parámetro. Si solo tenemos un contexto de datos, no es obligatorio usarlo.

2.- remove-migration: Con este comando, eliminaremos la ultima migración que hemos generado. Se puede utilizar varias veces consecutivamente para ir eliminando migraciones desde la última a la primera.

3.- update-database: Con este comando, enviaremos a la base de datos los cambios de la migración, haciéndola efectiva:
    {nombre}: Con este parámetro indicaremos el nombre de la migración que queremos aplicar.
    -Context {contexto}: En caso de tener más de un contexto de datos en nuestro programa, indicaremos cual de los contextos es mediante este parámetro. Si solo tenemos un contexto de datos, no es obligatorio usarlo.

4.- drop-database: Con este comando, eliminaremos la base de datos. Este comando tiene 1 parámetro:
    -Context {contexto}: En caso de tener más de un contexto de datos en nuestro programa, indicaremos cual de los contextos es mediante este parámetro. Si solo tenemos un contexto de datos, no es obligatorio usarlo.

Una vez que conocemos los comandos, vamos a ponernos en faena… Para empezar, utilizamos el comando:
    add-migration init -Context PostDbContext

---------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------

# Package Manager Console

---------------------------------------------------------------------------------------------------
# Inicializar migración
-> Borrar carpeta Migrations y DB
Add-Migration 00000000000000_CreateInitialSchema -Context AppDbContext -OutputDir Data/Migrations
-> Renombrar los fichero y las clases


---------------------------------------------------------------------------------------------------
# Generar migración y actualiza Base de Datos
Add-Migration 00000000000000_CreateInitialSchema -Context AppDbContext -OutputDir Data/Migrations
Update-Database 00000000000000_CreateInitialSchema

---------------------------------------------------------------------------------------------------
# Deshacer ultima migración
Update-Database <previous-migration-name>
Remove-Migration

---------------------------------------------------------------------------------------------------
# Eliminar Base de Datos
Drop-Database -Context AppDbContext
 
