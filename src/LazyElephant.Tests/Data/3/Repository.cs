using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Npgsql;
using PostPig.DataAccess.Florp;

namespace PostPig.DataAccess.Repositories
{
    public class CustomerRepository
    {
        private readonly string connectionString;

        public CustomerRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<IEnumerable<Customer>> GetAll()
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"SELECT id,
                name,
                created_date,
                age
                FROM sales.customer;", connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    return Enumerate(reader);
                }
            }
        }

        public async Task<Customer> Get(Guid id)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"SELECT id,
                name,
                created_date,
                age
                FROM sales.customer
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
                var command = new NpgsqlCommand(@"DELETE FROM sales.customer WHERE id = @id;", connection);

                command.Parameters.AddWithValue("id", id);

                var result = await command.ExecuteNonQueryAsync();

                return result > 0;
            }
        }

        public async Task<Customer> Create(Customer customer)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"INSERT INTO sales.customer(name, created_date, age)
                VALUES (@name, @createdDate, @age)
                RETURNING id, name, created_date, age;", connection);

                command.Parameters.AddWithValue("name", customer.Name);
                command.Parameters.AddWithValue("createdDate", customer.CreatedDate);
                command.Parameters.AddWithValue("age", customer.Age);
                
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

        public async Task<Customer> Update(Customer customer)
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                var command = new NpgsqlCommand(@"UPDATE sales.customer
                SET name = @name,
                created_date = @createdDate,
                age = @age
                WHERE id = @id
                RETURNING id, name, created_date, age;", connection);

                command.Parameters.AddWithValue("id", customer.Id);
                command.Parameters.AddWithValue("name", customer.Name);
                command.Parameters.AddWithValue("createdDate", customer.CreatedDate);
                command.Parameters.AddWithValue("age", customer.Age);

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

        private static IEnumerable<Customer> Enumerate(DbDataReader reader)
        {
            while (reader.Read())
            {
                yield return GetCurrent(reader);
            }
        }

        private static Customer GetCurrent(DbDataReader reader)
        {
            return new Customer
            {
                Id = reader.GetGuid(0),
                Name = reader.GetString(1),
                CreatedDate = reader.GetDateTime(2),
                Age = reader.IsDBNull(3) ? default(int?) : reader.GetInt32(3)
            };
        }
    }
}