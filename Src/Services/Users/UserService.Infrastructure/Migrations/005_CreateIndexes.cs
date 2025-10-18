using FluentMigrator;

namespace UserService.Infrastructure.Migrations
{
    [Migration(20241201005)]
    public class CreateIndexes : Migration
    {
        public override void Up()
        {
            // Índices para user_addresses
            Create.Index("idx_user_addresses_user_id")
                .OnTable("user_addresses")
                .OnColumn("user_id");

            Execute.Sql("CREATE UNIQUE INDEX uq_user_addresses_default_per_user_type ON user_addresses (user_id, type) WHERE is_default = TRUE AND deleted_at IS NULL;");

            // Índices para user_saved_cards
            Create.Index("idx_user_saved_cards_user_id")
                .OnTable("user_saved_cards")
                .OnColumn("user_id");

            Execute.Sql("CREATE UNIQUE INDEX uq_user_saved_cards_default_per_user ON user_saved_cards (user_id) WHERE is_default = TRUE AND deleted_at IS NULL;");

            // Índices para user_tokens
            Create.Index("idx_user_tokens_user_id")
                .OnTable("user_tokens")
                .OnColumn("user_id");

            Create.Index("idx_user_tokens_type")
                .OnTable("user_tokens")
                .OnColumn("token_type");

            Create.Index("idx_user_tokens_expires_at")
                .OnTable("user_tokens")
                .OnColumn("expires_at");

            // Índices para user_consents
            Create.Index("idx_user_consents_user_id")
                .OnTable("user_consents")
                .OnColumn("user_id");

            // Índices para revoked_jwt_tokens
            Create.Index("idx_revoked_jwt_tokens_expires_at")
                .OnTable("revoked_jwt_tokens")
                .OnColumn("expires_at");
        }

        public override void Down()
        {
            // Remover índices (ordem inversa)
            Delete.Index("idx_revoked_jwt_tokens_expires_at").OnTable("revoked_jwt_tokens");
            Delete.Index("idx_user_consents_user_id").OnTable("user_consents");
            Delete.Index("idx_user_tokens_expires_at").OnTable("user_tokens");
            Delete.Index("idx_user_tokens_type").OnTable("user_tokens");
            Delete.Index("idx_user_tokens_user_id").OnTable("user_tokens");
            
            Execute.Sql("DROP INDEX IF EXISTS uq_user_saved_cards_default_per_user;");
            Delete.Index("idx_user_saved_cards_user_id").OnTable("user_saved_cards");
            
            Execute.Sql("DROP INDEX IF EXISTS uq_user_addresses_default_per_user_type;");
            Delete.Index("idx_user_addresses_user_id").OnTable("user_addresses");
        }
    }
}