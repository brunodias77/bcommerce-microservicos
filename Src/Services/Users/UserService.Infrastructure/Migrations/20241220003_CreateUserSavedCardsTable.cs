using FluentMigrator;

namespace UserService.Infrastructure.Migrations;

/// <summary>
/// Migration para criar a tabela user_saved_cards
/// </summary>
[Migration(20241220003)]
public class CreateUserSavedCardsTable : Migration
{
    public override void Up()
    {
        Create.Table("user_saved_cards")
            .WithColumn("saved_card_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("user_id").AsGuid().NotNullable()
            .WithColumn("nickname").AsString(50).Nullable()
            .WithColumn("last_four_digits").AsString(4).NotNullable()
            .WithColumn("brand").AsString(20).NotNullable()
            .WithColumn("gateway_token").AsString(255).NotNullable()
            .WithColumn("expiry_date").AsDate().NotNullable()
            .WithColumn("is_default").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
            .WithColumn("updated_at").AsDateTime().Nullable()
            .WithColumn("deleted_at").AsDateTime().Nullable()
            .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

        // Criar chave estrangeira
        Create.ForeignKey("FK_user_saved_cards_users")
            .FromTable("user_saved_cards").ForeignColumn("user_id")
            .ToTable("users").PrimaryColumn("user_id")
            .OnDelete(System.Data.Rule.Cascade);

        // Criar índices com filtros condicionais
        Create.Index("idx_user_saved_cards_user_id")
            .OnTable("user_saved_cards")
            .OnColumn("user_id");

        // Índice único para garantir apenas um cartão padrão por usuário (considerando soft delete)
        Execute.Sql(@"CREATE UNIQUE INDEX uq_user_saved_cards_default_per_user ON user_saved_cards (user_id) WHERE is_default = TRUE AND deleted_at IS NULL;");

        // Constraint único para gateway_token considerando soft delete
        Execute.Sql(@"CREATE UNIQUE INDEX uq_user_saved_cards_gateway_token ON user_saved_cards (gateway_token) WHERE deleted_at IS NULL;");

        Create.Index("IX_user_saved_cards_brand")
            .OnTable("user_saved_cards")
            .OnColumn("brand");

        // Índice para cartões próximos ao vencimento (não expirados)
        Execute.Sql(@"CREATE INDEX idx_user_saved_cards_expiry_active ON user_saved_cards (expiry_date) WHERE deleted_at IS NULL;");
    }

    public override void Down()
    {
        Delete.Table("user_saved_cards");
    }
}