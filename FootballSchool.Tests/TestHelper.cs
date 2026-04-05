using FootballSchool.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace FootballSchool.Tests.Helpers
{
    public static class TestHelper
    {
        /// <summary>
        /// Создает уникальный InMemory контекст БД для каждого теста, 
        /// чтобы тесты были изолированы друг от друга.
        /// </summary>
        public static FootballSchoolContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<FootballSchoolContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                // Игнорируем ошибку отсутствия поддержки транзакций в InMemory БД
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            return new FootballSchoolContext(options);
        }
    }
}