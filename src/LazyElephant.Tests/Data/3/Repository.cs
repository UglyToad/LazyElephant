using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using PostPig.DataAccess.Florp;

namespace PostPig.DataAccess.Repositories
{
    public class TaskRepository
    {
        private readonly string connectionString;

        public TaskRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<PostPig.DataAccess.Florp.Task>> GetAll()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"SELECT id
                FROM public.task;", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    return Enumerate(reader);
                }
            }
        }

        public async Task<PostPig.DataAccess.Florp.Task> Get(Guid id)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"SELECT id
                FROM public.task
                WHERE id = @id;", connection);

                command.Parameters.AddWithValue("id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        return GetCurrent(reader);
                    }
                }

                return null;
            }
        }

        public async Task<bool> Delete(Guid id)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"DELETE FROM public.task WHERE id = @id;", connection);

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }

        public async Task<PostPig.DataAccess.Florp.Task> Create(PostPig.DataAccess.Florp.Task task)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"INSERT INTO public.task(id)
                VALUES(DEFAULT)
                RETURNING id;", connection);
                
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        return GetCurrent(reader);
                    }
                }

                return null;
            }
        }

        public async Task<PostPig.DataAccess.Florp.Task> Update(PostPig.DataAccess.Florp.Task task)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                return task;
            }
        }

        private static IEnumerable<PostPig.DataAccess.Florp.Task> Enumerate(DbDataReader reader)
        {
            while (reader.Read())
            {
                yield return GetCurrent(reader);
            }
        }

        private static PostPig.DataAccess.Florp.Task GetCurrent(DbDataReader reader)
        {
            return new PostPig.DataAccess.Florp.Task
            {
                Id = reader.GetGuid(0)
            };
        }
    }
}