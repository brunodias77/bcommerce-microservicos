using FluentMigrator;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela revoked_jwt_tokens
/// </summary>
[Migration(20241220006)]
public class CreateRevokedJwtTokensTable : Migration
{
    public override void Up()
    {
        Create.Table("revoked_jwt_tokens")
            .WithColumn("jti").AsString(255).PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("expires_at").AsDateTime().NotNullable();

        // Criar chave estrangeira
        Create.ForeignKey("FK_revoked_jwt_tokens_users")
            .FromTable("revoked_jwt_tokens").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Criar índices
        Create.Index("IX_revoked_jwt_tokens_user_id")
            .OnTable("revoked_jwt_tokens")
            .OnColumn("user_id");

        Create.Index("IX_revoked_jwt_tokens_expires_at")
            .OnTable("revoked_jwt_tokens")
            .OnColumn("expires_at");

        // Índice composto para consultas de validação de token
        Create.Index("IX_revoked_jwt_tokens_jti_expires_at")
            .OnTable("revoked_jwt_tokens")
            .OnColumn("jti");
    }

    public override void Down()
    {
        Delete.Table("revoked_jwt_tokens");
    }
}