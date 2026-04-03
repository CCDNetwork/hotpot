using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Ccd.Server.Deduplication;
using Ccd.Server.Helpers;

namespace Ccd.Server.Migrations
{
    public partial class EncryptBookingIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var connectionString = StaticConfiguration.DbConnectionString;
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();

            EncryptTable(connection, "booking");
            EncryptTable(connection, "booking_log");
        }

        private static void EncryptTable(NpgsqlConnection connection, string tableName)
        {
            using var readCmd = new NpgsqlCommand(
                $"SELECT id, household_id, spouse_id FROM {tableName} " +
                "WHERE (household_id IS NOT NULL AND length(household_id) = 9) " +
                "OR (spouse_id IS NOT NULL AND length(spouse_id) = 9)",
                connection
            );

            var updates = new List<Tuple<Guid, string, string>>();

            using (var reader = readCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = reader.GetGuid(0);
                    var householdId = reader.IsDBNull(1) ? null : reader.GetString(1);
                    var spouseId = reader.IsDBNull(2) ? null : reader.GetString(2);

                    var encryptedHouseholdId = householdId != null && householdId.Length == 9
                        ? IdEncryptor.Encrypt(householdId)
                        : householdId;
                    var encryptedSpouseId = spouseId != null && spouseId.Length == 9
                        ? IdEncryptor.Encrypt(spouseId)
                        : spouseId;

                    updates.Add(Tuple.Create(id, encryptedHouseholdId, encryptedSpouseId));
                }
            }

            foreach (var update in updates)
            {
                using var updateCmd = new NpgsqlCommand(
                    $"UPDATE {tableName} SET household_id = @householdId, spouse_id = @spouseId WHERE id = @id",
                    connection
                );
                updateCmd.Parameters.AddWithValue("id", update.Item1);
                updateCmd.Parameters.AddWithValue("householdId", (object)update.Item2 ?? DBNull.Value);
                updateCmd.Parameters.AddWithValue("spouseId", (object)update.Item3 ?? DBNull.Value);
                updateCmd.ExecuteNonQuery();
            }

            if (updates.Count > 0)
            {
                Console.WriteLine($"Encrypted {updates.Count} rows in {tableName}.");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Decryption would require the same ENCRYPTION_KEY to be available
        }
    }
}
