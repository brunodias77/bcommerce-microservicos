using FluentMigrator;
using FluentMigrator.Postgres;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela users
/// </summary>
[Migration(20241220001)]
public class CreateUsersTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("user_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("keycloak_id").AsString(255).Nullable()
            .WithColumn("first_name").AsString(100).NotNullable()
            .WithColumn("last_name").AsString(155).NotNullable()
            .WithColumn("email").AsString(255).NotNullable()
            .WithColumn("email_verified_at").AsDateTime().Nullable()
            .WithColumn("password_hash").AsString(255).Nullable()
            .WithColumn("phone").AsString(20).Nullable()
            .WithColumn("cpf").AsString(11).Nullable()
            .WithColumn("date_of_birth").AsDate().Nullable()
            .WithColumn("status").AsString(20).NotNullable().WithDefaultValue("Active")
            .WithColumn("role").AsString(20).NotNullable().WithDefaultValue("Customer")
            .WithColumn("newsletter_opt_in").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("failed_login_attempts").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("account_locked_until").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Criar índices únicos com filtros condicionais usando SQL direto
        // Índice único para email apenas para registros ativos (não deletados)
        Execute.Sql(@"CREATE UNIQUE INDEX idx_users_active_email ON users (email) WHERE deleted_at IS NULL;");

        // Índice único para keycloak_id apenas para registros não nulos e não deletados
        Execute.Sql(@"CREATE UNIQUE INDEX IX_users_keycloak_id ON users (keycloak_id) WHERE keycloak_id IS NOT NULL AND deleted_at IS NULL;");

        // Índice único para CPF apenas para registros não nulos e não deletados
        Execute.Sql(@"CREATE UNIQUE INDEX IX_users_cpf ON users (cpf) WHERE cpf IS NOT NULL AND deleted_at IS NULL;");

        // Criar índices de performance com filtros condicionais
        // Índice para status apenas para registros ativos
        Execute.Sql(@"CREATE INDEX idx_users_status ON users (status) WHERE deleted_at IS NULL;");

        // Índice para role (sem filtro pois é sempre necessário)
        Create.Index("idx_users_role")
            .OnTable("users")
            .OnColumn("role");

        // Índices adicionais para otimização de consultas
        Create.Index("IX_users_deleted_at")
            .OnTable("users")
            .OnColumn("deleted_at");

        Create.Index("IX_users_email_verified_at")
            .OnTable("users")
            .OnColumn("email_verified_at");
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}