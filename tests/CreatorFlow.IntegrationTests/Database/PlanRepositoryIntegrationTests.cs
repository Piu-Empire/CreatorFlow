using CreatorFlow.Data;
using CreatorFlow.Models;
using CreatorFlow.Repositories;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using NpgsqlTypes;

namespace CreatorFlow.IntegrationTests.Database;

[TestClass]
public sealed class PlanRepositoryIntegrationTests
{
    [TestMethod]
    public async Task OpenConnectionAsync_WithValidConfiguration_OpensConnection()
    {
        var connectionFactory = CreateConnectionFactory();

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync();

        Assert.AreEqual(
            System.Data.ConnectionState.Open,
            connection.State);
    }

    [TestMethod]
    public async Task AddAndGetByIdAsync_WithValidPlan_PersistsAndReadsPlan()
    {
        NpgsqlConnectionFactory connectionFactory = CreateConnectionFactory();
        var repository = new PlanRepository(connectionFactory);
        Plan? insertedPlan = null;

        var expectedPlan = new Plan
        {
            Code = $"TEST_{Guid.NewGuid():N}",
            Name = "Repository integration test",
            Description = "Temporary row created by SCRUM-16 integration verification",
            PriceMonthly = 12345.67m,
            MaxProjects = 3,
            MaxMembers = 7,
            AiRequestLimit = 11,
            IsActive = true
        };

        try
        {
            insertedPlan = await repository.AddAsync(expectedPlan);
            Plan? actualPlan = await repository.GetByIdAsync(insertedPlan.PlanId);

            Assert.IsNotNull(actualPlan);
            Assert.AreEqual(expectedPlan.Code, actualPlan.Code);
            Assert.AreEqual(expectedPlan.Name, actualPlan.Name);
            Assert.AreEqual(expectedPlan.Description, actualPlan.Description);
            Assert.AreEqual(expectedPlan.PriceMonthly, actualPlan.PriceMonthly);
            Assert.AreEqual(expectedPlan.MaxProjects, actualPlan.MaxProjects);
            Assert.AreEqual(expectedPlan.MaxMembers, actualPlan.MaxMembers);
            Assert.AreEqual(expectedPlan.AiRequestLimit, actualPlan.AiRequestLimit);
            Assert.IsTrue(actualPlan.IsActive);
        }
        finally
        {
            if (insertedPlan is not null)
            {
                await DeletePlanAsync(connectionFactory, insertedPlan.PlanId);
            }
        }
    }

    private static NpgsqlConnectionFactory CreateConnectionFactory()
    {
        string connectionString = DatabaseConfiguration.GetConnectionString();
        return new NpgsqlConnectionFactory(connectionString);
    }

    private static async Task DeletePlanAsync(
        IDbConnectionFactory connectionFactory,
        long planId)
    {
        const string sql = "DELETE FROM plans WHERE plan_id = @plan_id;";

        await using NpgsqlConnection connection =
            await connectionFactory.OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("plan_id", NpgsqlDbType.Bigint, planId);
        await command.ExecuteNonQueryAsync();
    }
}
