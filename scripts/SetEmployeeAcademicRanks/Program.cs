using Npgsql;

const short AssociateProfessor = 3;
const short Professor = 4;
const short EmployeeKind = 1;

string connectionString = args.FirstOrDefault()
    ?? Environment.GetEnvironmentVariable("DIPLOMA_REPAIR_PG")
    ?? throw new InvalidOperationException("Pass connection string as arg or set DIPLOMA_REPAIR_PG.");

await using NpgsqlConnection connection = new(connectionString);
await connection.OpenAsync();
await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

await using (NpgsqlCommand setAll = new(
    """
    UPDATE users
    SET "AcademicRank" = @rank
    WHERE "UserKind" = @employeeKind
    """,
    connection,
    transaction))
{
    setAll.Parameters.AddWithValue("rank", AssociateProfessor);
    setAll.Parameters.AddWithValue("employeeKind", EmployeeKind);
    int updated = await setAll.ExecuteNonQueryAsync();
    Console.WriteLine($"Set доцент for all employees: {updated}");
}

await using (NpgsqlCommand clearLazoriv = new(
    """
    UPDATE users
    SET "AcademicRank" = NULL
    WHERE "UserKind" = @employeeKind
      AND "FullName" ILIKE '%' || @pattern || '%'
    """,
    connection,
    transaction))
{
    clearLazoriv.Parameters.AddWithValue("employeeKind", EmployeeKind);
    clearLazoriv.Parameters.AddWithValue("pattern", "Лазорів");
    int updated = await clearLazoriv.ExecuteNonQueryAsync();
    Console.WriteLine($"Cleared rank for Лазорів: {updated}");
}

await using (NpgsqlCommand clearHarasymiv = new(
    """
    UPDATE users
    SET "AcademicRank" = NULL
    WHERE "UserKind" = @employeeKind
      AND "FullName" ILIKE '%' || @pattern || '%'
    """,
    connection,
    transaction))
{
    clearHarasymiv.Parameters.AddWithValue("employeeKind", EmployeeKind);
    clearHarasymiv.Parameters.AddWithValue("pattern", "Гарасимів");
    int updated = await clearHarasymiv.ExecuteNonQueryAsync();
    Console.WriteLine($"Cleared rank for Гарасимів: {updated}");
}

await using (NpgsqlCommand setMelnychuk = new(
    """
    UPDATE users
    SET "AcademicRank" = @rank
    WHERE "UserKind" = @employeeKind
      AND "FullName" ILIKE '%' || @pattern || '%'
    """,
    connection,
    transaction))
{
    setMelnychuk.Parameters.AddWithValue("rank", Professor);
    setMelnychuk.Parameters.AddWithValue("employeeKind", EmployeeKind);
    setMelnychuk.Parameters.AddWithValue("pattern", "Мельничук");
    int updated = await setMelnychuk.ExecuteNonQueryAsync();
    Console.WriteLine($"Set професор for Мельничук: {updated}");
}

await transaction.CommitAsync();

await using NpgsqlCommand list = new(
    """
    SELECT "FullName", "AcademicRank"
    FROM users
    WHERE "UserKind" = @employeeKind
    ORDER BY "FullName"
    """,
    connection);
list.Parameters.AddWithValue("employeeKind", EmployeeKind);

await using NpgsqlDataReader reader = await list.ExecuteReaderAsync();
while (await reader.ReadAsync())
{
    string name = reader.GetString(0);
    string rank = reader.IsDBNull(1) ? "—" : reader.GetInt16(1) switch
    {
        3 => "Доцент",
        4 => "Професор",
        _ => reader.GetInt16(1).ToString(),
    };
    Console.WriteLine($"{rank,-10} {name}");
}
