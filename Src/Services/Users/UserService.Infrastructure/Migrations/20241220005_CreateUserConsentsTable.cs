using FluentMigrator;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela user_consents
/// </summary>
[Migration(20241220005)]
public class CreateUserConsentsTable : Migration
{
    public override void Up()
    {
        Create.Table("user_consents")
            .WithColumn("consent_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("type").AsString(50).NotNullable()
            .WithColumn("terms_version").AsString(30).Nullable()
            .WithColumn("is_granted").AsBoolean().NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Criar chave estrangeira
        Create.ForeignKey("FK_user_consents_users")
            .FromTable("user_consents").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Criar índices com filtros condicionais
        Create.Index("idx_user_consents_user_id")
            .OnTable("user_consents")
            .OnColumn("user_id");

        // Constraint único composto considerando soft delete
        Execute.Sql(@"CREATE UNIQUE INDEX uq_user_consents_user_type ON user_consents (user_id, type) WHERE deleted_at IS NULL;");

        // Índice para consentimentos ativos por tipo
        Execute.Sql(@"CREATE INDEX idx_user_consents_active_by_type ON user_consents (type, is_granted) WHERE deleted_at IS NULL;");

        // Índice para consentimentos concedidos
        Execute.Sql(@"CREATE INDEX idx_user_consents_granted ON user_consents (is_granted, type) WHERE is_granted = TRUE AND deleted_at IS NULL;");

        Create.Index("IX_user_consents_terms_version")
            .OnTable("user_consents")
            .OnColumn("terms_version");
    }

    public override void Down()
    {
        Delete.Table("user_consents");
    }
}