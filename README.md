# Lazy Elephant #

Lazy Elephant defines a simple template syntax used by the application to generate a full data access layer for PostgreSQL using PostgreSQL conventions for tables and C# conventions for classes.

For each Elephant template we generate:

+ A create table statement in PostgresSQL
+ A C# POCO representing the data class
+ A C# repository containing async methods to create, retrieve, update and delete the data.

Unlike migrations in Entity Framework, Lazy Elephant gives you full control over the resulting code and SQL, it is simply designed to save you time defining the classes needed for data access in C# and the SQL for PostgreSQL.

## Elephant Templates ##

The format of the template is close to C# style syntax with terse additions to define PostgreSQL attributes for the columns.

Apart from the column name field the entire template is case insensitive.

An empty table and class is defined as:

    Name {}

This will generate the following C# class:

    public class Name {}

We can then add columns/properties in the form:

    columnName data_type [attributes, ...],

For example:

    sales.customer { id Guid pk ag,
      name string uq,
      createdDate DateTime df [now],
      age int null,
    }

There are quite a few new concepts here, the resulting C# class would be:

    public class Customer
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedDate { get; set; }

        public int? Age { get; set; }
    }

The corresponding SQL script would be:

    CREATE TABLE sales.customer (
        id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
        name TEXT NOT NULL UNIQUE,
        created_date TIMESTAMP NOT NULL DEFAULT NOW(),
        age INT4
    );

### Table Schema ###

By default tables are created in the PostgreSQL schema `public` however the schema can be specified in the form `schema.table` in the Elephant template. In the example above the schema would be `sales` and the table name would be `customer`.

### Data Types ###

The following C# data types are currently supported:

+ Guid
+ string
+ DateTime
+ bool
+ short
+ int
+ long
+ float
+ double
+ decimal
+ TimeSpan
+ byte[]

The data type is case insensitive.

### Primary Key ###

A column is indicated to be a primary key using the `pk` token. Only `string`, `guid` and `int` columns may be primary keys at the moment.

### Auto-Generate ###

A primary key column of the type `int` or `guid` may be set to use autogenerated values using the `ag` token. This will use the Postgres `SERIAL` type for `int` and `DEFAULT uuid_generate_v4()` for `guid`.

### Default ###

Columns may use default values for insertion by specifying a value in brackets following the `df` token. The valid values for the default for a `datetime` column are `[now]` and `[utcnow]`.

### Nullability ###

Good schema design generally avoids nullable columns, to make the templates shorter the column is assumed to be not null unless the `null` token is specified. Primary keys are always non-null in Postgres. For value types in C# the presence of the `null` token causes the type to be a nullable type `Nullable<T>`/`T?`.

### Unique ###

A `UNIQUE` constraint may be added to the column by specifying the `uq` token. Primary keys are unique by default.

## Repository ##

Rather than using any magic for its data access layer LazyElephant generates simple SQL statements in valid C#.

For the earlier template:

    sales.customer { id Guid pk ag,
      name string uq,
      createdDate DateTime df [now],
      age int null,
    }

The repository generated contains the following methods:

    public async Task<Customer> Create(Customer customer);
