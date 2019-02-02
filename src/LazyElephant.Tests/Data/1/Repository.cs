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
                var command = new NpgsqlCommand(@"SELECT id,
                name,
                description,
                created,
                user_id
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
                var command = new NpgsqlCommand(@"SELECT id,
                name,
                description,
                created,
                user_id
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
                task.Id = Guid.NewGuid();

                var command = new NpgsqlCommand(@"INSERT INTO public.task(id, name, description, user_id)
                VALUES(@id, @name, @description, @userId)
                RETURNING id, name, description, created, user_id;", connection);

                command.Parameters.AddWithValue("id", task.Id);
                command.Parameters.AddWithValue("name", task.Name);
                command.Parameters.AddWithValue("description", task.Description);
                command.Parameters.AddWithValue("userId", task.UserId);

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
                var command = new NpgsqlCommand(@"UPDATE public.task
                SET name = @name,
                description = @description,
                created = @created,
                user_id = @userId
                WHERE id = @id
                RETURNING id, name, description, created, user_id;", connection);

                command.Parameters.AddWithValue("id", task.Id);
                command.Parameters.AddWithValue("name", task.Name);
                command.Parameters.AddWithValue("description", task.Description);
                command.Parameters.AddWithValue("created", task.Description);
                command.Parameters.AddWithValue("userId", task.UserId);

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
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? default(string) : reader.GetString(2),
                Created = reader.GetDateTime(3),
                UserId = reader.GetGuid(4)
            };
        }
    }
}