using FluentMigrator;

namespace UserService.Infrastructure.Migrations
{
    [Migration(20241201004)]
    public class CreateOtherTables : Migration
    {
        public override void Up()
        {
            // Tabela user_addresses
            Create.Table("user_addresses")
                .WithColumn("address_id").AsGuid().PrimaryKey()
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("type").AsCustom("address_type_enum").NotNullable()
                .WithColumn("postal_code").AsFixedLengthString(8).NotNullable()
                .WithColumn("street").AsString(150).NotNullable()
                .WithColumn("street_number").AsString(20).NotNullable()
                .WithColumn("complement").AsString(100).Nullable()
                .WithColumn("neighborhood").AsString(100).NotNullable()
                .WithColumn("city").AsString(100).NotNullable()
                .WithColumn("state_code").AsFixedLengthString(2).NotNullable()
                .WithColumn("country_code").AsFixedLengthString(2).NotNullable().WithDefaultValue("BR")
                .WithColumn("is_default").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
                .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

            // Adicionar valor padrão para address_id usando SQL
            Execute.Sql("ALTER TABLE user_addresses ALTER COLUMN address_id SET DEFAULT gen_random_uuid();");

            Create.ForeignKey("fk_user_addresses_user_id")
                .FromTable("user_addresses").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            // Tabela user_saved_cards
            Create.Table("user_saved_cards")
                .WithColumn("saved_card_id").AsGuid().PrimaryKey()
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("nickname").AsString(50).Nullable()
                .WithColumn("last_four_digits").AsFixedLengthString(4).NotNullable()
                .WithColumn("brand").AsCustom("card_brand_enum").NotNullable()
                .WithColumn("gateway_token").AsString(255).NotNullable().Unique()
                .WithColumn("expiry_date").AsDate().NotNullable()
                .WithColumn("is_default").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
                .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

            // Adicionar valor padrão para saved_card_id usando SQL
            Execute.Sql("ALTER TABLE user_saved_cards ALTER COLUMN saved_card_id SET DEFAULT gen_random_uuid();");

            Create.ForeignKey("fk_user_saved_cards_user_id")
                .FromTable("user_saved_cards").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            // Tabela user_tokens
            Create.Table("user_tokens")
                .WithColumn("token_id").AsGuid().PrimaryKey()
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("token_type").AsCustom("user_token_type_enum").NotNullable()
                .WithColumn("token_value").AsString(2048).NotNullable().Unique()
                .WithColumn("expires_at").AsDateTimeOffset().NotNullable()
                .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
                .WithColumn("revoked_at").AsDateTimeOffset().Nullable()
                .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

            // Adicionar valor padrão para token_id usando SQL
            Execute.Sql("ALTER TABLE user_tokens ALTER COLUMN token_id SET DEFAULT gen_random_uuid();");

            Create.ForeignKey("fk_user_tokens_user_id")
                .FromTable("user_tokens").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            // Tabela user_consents
            Create.Table("user_consents")
                .WithColumn("consent_id").AsGuid().PrimaryKey()
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("type").AsCustom("consent_type_enum").NotNullable()
                .WithColumn("terms_version").AsString(30).Nullable()
                .WithColumn("is_granted").AsBoolean().NotNullable()
                .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentDateTime)
                .WithColumn("deleted_at").AsDateTimeOffset().Nullable()
                .WithColumn("version").AsInt32().NotNullable().WithDefaultValue(1);

            // Adicionar valor padrão para consent_id usando SQL
            Execute.Sql("ALTER TABLE user_consents ALTER COLUMN consent_id SET DEFAULT gen_random_uuid();");

            Create.ForeignKey("fk_user_consents_user_id")
                .FromTable("user_consents").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            Create.UniqueConstraint("uq_user_consent_type")
                .OnTable("user_consents")
                .Columns("user_id", "type");

            // Tabela revoked_jwt_tokens
            Create.Table("revoked_jwt_tokens")
                .WithColumn("jti").AsGuid().PrimaryKey()
                .WithColumn("user_id").AsGuid().NotNullable()
                .WithColumn("expires_at").AsDateTimeOffset().NotNullable();

            Create.ForeignKey("fk_revoked_jwt_tokens_user_id")
                .FromTable("revoked_jwt_tokens").ForeignColumn("user_id")
                .ToTable("users").PrimaryColumn("user_id")
                .OnDelete(System.Data.Rule.Cascade);

            // Criar triggers para updated_at
            Execute.Sql("CREATE TRIGGER set_timestamp_user_addresses BEFORE UPDATE ON user_addresses FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();");
            Execute.Sql("CREATE TRIGGER set_timestamp_user_saved_cards BEFORE UPDATE ON user_saved_cards FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();");
            Execute.Sql("CREATE TRIGGER set_timestamp_user_consents BEFORE UPDATE ON user_consents FOR EACH ROW EXECUTE FUNCTION trigger_set_timestamp();");
        }

        public override void Down()
        {
            // Remover triggers
            Execute.Sql("DROP TRIGGER IF EXISTS set_timestamp_user_consents ON user_consents;");
            Execute.Sql("DROP TRIGGER IF EXISTS set_timestamp_user_saved_cards ON user_saved_cards;");
            Execute.Sql("DROP TRIGGER IF EXISTS set_timestamp_user_addresses ON user_addresses;");

            // Remover tabelas (ordem inversa das dependências)
            Delete.Table("revoked_jwt_tokens");
            Delete.Table("user_consents");
            Delete.Table("user_tokens");
            Delete.Table("user_saved_cards");
            Delete.Table("user_addresses");
        }
    }
}