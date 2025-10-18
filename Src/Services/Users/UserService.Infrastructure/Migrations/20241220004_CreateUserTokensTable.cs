using FluentMigrator;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela user_tokens
/// </summary>
[Migration(20241220004)]
public class CreateUserTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("user_tokens")
            .WithColumn("token_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("token_type").AsString(50).NotNullable()
            .WithColumn("token_value").AsString(2048).NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable()
            .WithColumn("revoked_at").AsDateTime().Nullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Criar chave estrangeira
        Create.ForeignKey("FK_user_tokens_users")
            .FromTable("user_tokens").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Criar índices com filtros condicionais
        Create.Index("idx_user_tokens_user_id")
            .OnTable("user_tokens")
            .OnColumn("user_id");

        // Índice composto para tokens ativos por tipo e usuário
        Execute.Sql(@"CREATE INDEX idx_user_tokens_active_by_type_user ON user_tokens (user_id, token_type) WHERE revoked_at IS NULL AND deleted_at IS NULL;");

        // Índice para tokens válidos (não revogados e não expirados)
        Execute.Sql(@"CREATE INDEX idx_user_tokens_valid ON user_tokens (expires_at, token_type) WHERE revoked_at IS NULL AND deleted_at IS NULL;");

        // Constraint único para token_value considerando soft delete
        Execute.Sql(@"CREATE UNIQUE INDEX uq_user_tokens_token_value ON user_tokens (token_value) WHERE deleted_at IS NULL;");

        // Índice para limpeza de tokens expirados
        Execute.Sql(@"CREATE INDEX idx_user_tokens_expired_cleanup ON user_tokens (expires_at) WHERE deleted_at IS NULL;");

        Create.Index("IX_user_tokens_token_type")
            .OnTable("user_tokens")
            .OnColumn("token_type");
    }

    public override void Down()
    {
        Delete.Table("user_tokens");
    }
}